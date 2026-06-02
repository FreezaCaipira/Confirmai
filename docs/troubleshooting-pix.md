# Pix Webhook Troubleshooting Runbook

**Última atualização:** June 2, 2026  
**Versão:** 1.0  
**Escopo:** EfiBank Pix mTLS + BTCPay Pix integrations

---

## 🚨 Sintomas Críticos

### 1. Payment Não É Marcado Como Paid Após Confirmação Pix

**Sintoma:** Usuário enviou Pix, mas `EventConfirmation.HasPaid` continua `false`

**Root Causes:**
- ❌ Webhook não foi recebido (firewall/TLS)
- ❌ Idempotência falhou (webhook duplicado rejeitado)
- ❌ Assinatura mTLS inválida

**Diagnóstico:**

```powershell
# 1. Verificar logs de webhook
SELECT * FROM AdminLogs 
WHERE Activity LIKE '%webhook%' 
  AND CreatedAt > NOW() - INTERVAL '1 hour'
ORDER BY CreatedAt DESC;

# 2. Verificar payment record status
SELECT Id, IsPaid, CreatedAt, PaidAt 
FROM Payments 
WHERE CreatedAt > NOW() - INTERVAL '1 hour'
ORDER BY CreatedAt DESC;

# 3. Verificar se há logs de erro
SELECT * FROM Logs 
WHERE Level = 'Error' 
  AND CreatedAt > NOW() - INTERVAL '1 hour'
ORDER BY CreatedAt DESC;
```

**Ações Corretivas:**

1. **Se webhook recente não consta em logs:**
   - Verificar firewall: `netstat -an | grep 443`
   - Testar TLS: `curl -v --cert client.crt --key client.key https://webhook-endpoint`
   - Verificar certificate renewal (mTLS expira em 1 ano)

2. **Se payment status é "Pending" há >24h:**
   - Manual mark paid: `AdminMarkPaid()` em Pages/Groups/Detail.razor
   - Registrar em audit log para análise posterior

3. **Se webhook duplicado foi rejeitado:**
   - Verificar `Payments.IdempotencyKey`
   - Retry com novo ID se necessário

---

### 2. Webhook Recebido Mas Não Atualiza EventConfirmation

**Sintoma:** `AdminLogs` mostra webhook recebido, mas `EventConfirmation.HasPaid` = false

**Root Cause:**
- EventConfirmation.Id mismatch (webhook referencia ID errado)
- `EventConfirmationPaymentStatusService` erro em lógica de status

**Diagnóstico:**

```csharp
// No EventNotificationSchedulerService.cs
var confs = db.EventConfirmations
  .Where(c => c.Event.StartsAt.Date == DateTime.UtcNow.Date 
           && c.RachaScheduleId.HasValue)
  .ToList();

// Verificar se HasPaid foi atualizado
foreach (var c in confs)
{
  Console.WriteLine($"ConfId: {c.Id}, UserId: {c.UserId}, HasPaid: {c.HasPaid}");
}
```

**Ações Corretivas:**

1. Verificar `EventConfirmationPaymentStatusService.ProcessPaymentConfirmation()`:
   ```csharp
   var confirmation = await db.EventConfirmations.FindAsync(confirmationId);
   if (confirmation == null)
       throw new InvalidOperationException($"Confirmation {confirmationId} not found");
   ```

2. Forçar status sync via admin dashboard:
   - Abrir modal de pagamentos em Pages/Groups/Detail.razor
   - Clicar "Confirmar Pagamento" manualmente

---

### Múltiplos Webhooks Simultâneos Causam Race Condition

**Sintoma:** Mesmo pagamento aparece 2-3x em histórico de pagamentos

**Root Cause:**
- Falta de índice de idempotência em `Payments`
- Múltiplas confirmações processadas em paralelo

**Diagnóstico:**

```sql
SELECT 
  id, 
  COUNT(*) as occurrences
FROM Payments
GROUP BY id
HAVING COUNT(*) > 1;
```

**Ações Corretivas:**

1. **Adicionar índice de idempotência:**
   ```sql
   CREATE UNIQUE INDEX idx_payments_idempotencykey 
     ON Payments(IdempotencyKey) 
     WHERE IdempotencyKey IS NOT NULL;
   ```

2. **Implementar lock pessimista em webhook handler:**
   ```csharp
   using (var transaction = await db.Database.BeginTransactionAsync())
   {
       var lockedRecord = await db.Payments
           .FromSql($"SELECT * FROM Payments WHERE Id = {paymentId} FOR UPDATE")
           .FirstOrDefaultAsync();
       
       lockedRecord.IsPaid = true;
       await db.SaveChangesAsync();
       await transaction.CommitAsync();
   }
   ```

---

## 🔐 Problemas de TLS/mTLS

### Certificate Expired

**Sintoma:** `WebException: The remote certificate is invalid according to the validation procedure`

**Root Cause:**
- EfiBank mTLS certificate expirou (renovação anual)

**Diagnóstico:**

```powershell
# Verificar data de expiração do certificado
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new("client.crt")
Write-Host "Expires: $($cert.NotAfter)"

# Se expirado:
if ([DateTime]::Now -gt $cert.NotAfter) {
    Write-Host "❌ CERTIFICADO EXPIRADO"
}
```

**Ações Corretivas:**

1. Contatar EfiBank para renovação
2. Atualizar certificado em `Configuration/EfiBankOptions.cs`:
   ```csharp
   public string ClientCertificatePath { get; set; } = "/path/to/new/client.crt";
   ```
3. Redeploy aplicação

---

## 🔄 Idempotência e Retry Logic

### Webhook Recebido Múltiplas Vezes (Normal)

**Por que acontece:** Webhooks podem ser reenviados (garantia de entrega)

**Implementação Segura:**

```csharp
// Services/PaymentWebhookService.cs
public async Task ProcessPixConfirmationAsync(string idempotencyKey, PaymentData data)
{
    // Verificar se já foi processado
    var existing = await db.Payments
        .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
    
    if (existing != null)
    {
        _logger.LogInformation($"Webhook duplicate ignored: {idempotencyKey}");
        return; // Idempotente - não processa novamente
    }
    
    // Processa novo pagamento
    var record = new PaymentRecord { IdempotencyKey = idempotencyKey, ... };
    db.Payments.Add(record);
    await db.SaveChangesAsync();
}
```

---

## 📊 Monitoramento Proativo

### Daily Health Check

Execute todo dia às 9 AM UTC:

```sql
-- Pagamentos pendentes por >24h
SELECT COUNT(*) as pending_24h
FROM Payments
WHERE IsPaid = false
  AND CreatedAt < NOW() - INTERVAL '24 hours';

-- Alertar se > 5 pagamentos pendentes
-- (indica problema sistemático)
```

### Dashboard Metrics

- `eventconfirmations_unpaid_count`: Confirmações sem pagamento
- `payments_failed_count`: Pagamentos com falha
- `webhook_latency_ms`: Tempo do webhook até mark-paid

---

## 🛠️ Recovery Procedures

### Cenário A: Pagamento Pix Confirmado Mas Não Refletiu

**Passo a passo:**

1. Verificar `Payments.IsPaid`:
   ```sql
   SELECT * FROM Payments WHERE Id = {paymentId};
   ```

2. Se payment status é "IsPaid = false" há >24h, atualizar manualmente:
   ```sql
   UPDATE Payments 
   SET IsPaid = true, PaidAt = NOW()
   WHERE Id = {paymentId};
   ```

3. Reprocessar `EventConfirmation`:
   ```csharp
   var confirmation = await db.EventConfirmations.FindAsync(confirmationId);
   confirmation.HasPaid = true;
   confirmation.PaymentStatus = EventConfirmationPaymentStatus.Paid;
   await db.SaveChangesAsync();
   ```

4. Registrar em `AdminLogs`:
   ```csharp
   await logService.LogAdminActionAsync(
       userId: adminId,
       action: "ManualPaymentMarkPaid",
       details: $"PaymentId: {paymentId}, ConfirmationId: {confirmationId}",
       ipAddress: httpContext.Connection.RemoteIpAddress.ToString()
   );
   ```

---

### Cenário B: Webhook Bloqueado Por Firewall

**Sintoma:** Zero webhooks recebidos para Pix por >1h

**Verificação:**

```bash
# No servidor
sudo firewall-cmd --list-all

# Verificar se porta 443 está aberta
sudo netstat -an | grep 443

# Teste de conectividade
curl -v https://efibank-webhook-endpoint/confirm
```

**Ações:**

```bash
# Abrir porta 443 (já deveria estar)
sudo firewall-cmd --add-port=443/tcp --permanent

# Testar mTLS
curl --cert /path/to/client.crt \
     --key /path/to/client.key \
     https://efibank-webhook-endpoint/test
```

---

## 📋 Checklist de Deploy (Pré-Produção)

- [ ] Certificate mTLS válido por >30 dias
- [ ] Webhook endpoint respondendo 200 OK
- [ ] Idempotência implementada em todos os gateways
- [ ] Índices PostgreSQL criados (migration #AddPerformanceIndices)
- [ ] Logs estruturados ativados para webhook handling
- [ ] Alertas configurados para >5 pagamentos pendentes/24h

---

## 📞 Escalonamento

**Se nenhuma ação corrigir após 1h:**

1. Notificar EfiBank suporte: `suporte@efibank.com.br`
2. Verificar status de webhook em dashboard EfiBank
3. Reenviar webhook manualmente se disponível
4. Fazer mark-paid manual e registrar ticket

---

## Referências

- [EfiBank Pix Documentation](https://docs.efibank.com.br)
- [BTCPay Webhook Docs](https://docs.btcpayserver.org/API/Greenfield/Green%20field%20-%20Webhooks)
- `Services/EfiBankPixService.cs` — Lógica de integração Pix
- `Services/AdminConfirmationService.cs` — Mark-paid logic
