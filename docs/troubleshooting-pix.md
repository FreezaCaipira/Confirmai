# Pix Webhook Troubleshooting Runbook

**Última atualização:** Junho 2026  
**Versão:** 1.1  
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

```sql
-- 1. Verificar logs de webhook
SELECT * FROM "Logs"
WHERE "EventType" LIKE '%webhook%'
  AND "Timestamp" > NOW() - INTERVAL '1 hour'
ORDER BY "Timestamp" DESC;

-- 2. Verificar payment record status
SELECT "Id", "PaymentStatus", "CreatedAt", "PaidAt"
FROM "Payments"
WHERE "CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY "CreatedAt" DESC;

-- 3. Verificar se há logs de erro
SELECT * FROM "Logs"
WHERE "Level" = 'Error'
  AND "Timestamp" > NOW() - INTERVAL '1 hour'
ORDER BY "Timestamp" DESC;
```

**Ações Corretivas:**

1. **Se webhook recente não consta em logs:**
   - Verificar firewall: `netstat -an | grep 443`
   - Testar TLS: `curl -v --cert client.crt --key client.key https://webhook-endpoint`
   - Verificar certificate renewal (mTLS expira em 1 ano)

2. **Se payment status é "Pending" há >24h:**
   - Reconciliação manual via `/admin/payments` (botão de reconciliação por `chargeId/txId`)
   - Registrar em audit log via `LogService.AuditAsync()` para análise posterior

3. **Se webhook duplicado foi rejeitado:**
   - Verificar `Payments.IdempotencyKey`
   - Retry com novo ID se necessário

---

### 2. Webhook Recebido Mas Não Atualiza EventConfirmation

**Sintoma:** Logs mostram webhook recebido, mas `EventConfirmation.PaymentStatus` continua `Pending`

**Root Cause:**
- EventConfirmation.Id mismatch (webhook referencia ID errado)
- `EventConfirmationPaymentStatusService` erro em lógica de transição de status

**Diagnóstico:**

```sql
-- Verificar confirmações do dia com status de pagamento
SELECT "Id", "UserId", "PaymentStatus", "PaymentGatewayName"
FROM "EventConfirmations"
WHERE "ConfirmedAt"::date = CURRENT_DATE
ORDER BY "ConfirmedAt" DESC;
```

**Ações Corretivas:**

1. Verificar `Services/Events/EventConfirmationPaymentStatusService.cs` para lógica de transição de status

2. Forçar reconciliação via admin dashboard:
   - Abrir `/admin/payments`
   - Usar reconciliação manual por `chargeId` ou `txId`

---

### Múltiplos Webhooks Simultâneos Causam Race Condition

**Sintoma:** Mesmo pagamento aparece 2-3x em histórico de pagamentos

**Root Cause:**
- Falta de índice de idempotência em `Payments`
- Múltiplas confirmações processadas em paralelo

**Diagnóstico:**

```sql
SELECT
  "Id",
  COUNT(*) as occurrences
FROM "Payments"
GROUP BY "Id"
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
// Serviços de webhook por gateway:
// Services/Payment/EfiBankWebhookService.cs
// Services/Payment/BtcPayWebhookService.cs
// Services/Payment/AbacatePayWebhookService.cs
//
// Cada um implementa idempotência internamente:
// - Verifica se o webhook já foi processado antes de atualizar status
// - Registra transições via LogService.AuditAsync()
// - Usa PaymentEventBus para notificação em tempo real via SignalR
```

---

## 📊 Monitoramento Proativo

### Daily Health Check

Execute todo dia às 9 AM UTC:

```sql
-- Pagamentos pendentes por >24h
SELECT COUNT(*) as pending_24h
FROM "EventConfirmations"
WHERE "PaymentStatus" = 0
  AND "ConfirmedAt" < NOW() - INTERVAL '24 hours';

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

1. Verificar status do pagamento:
   ```sql
   SELECT * FROM "Payments" WHERE "Id" = {paymentId};
   ```

2. Se `PaymentStatus = 0` (Pending) há >24h, usar reconciliação manual via `/admin/payments`

3. Verificar `EventConfirmation` associada:
   ```sql
   SELECT "Id", "PaymentStatus", "PaymentGatewayName"
   FROM "EventConfirmations"
   WHERE "Id" = {confirmationId};
   ```

4. Registrar via auditoria:
   ```csharp
   await logService.AuditAsync(
       eventType: "admin.payment.status.transition",
       entityType: "Payment",
       entityId: confirmationId.ToString(),
       message: "Reconciliação manual Pix",
       actorUserId: adminId
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
- `Services/Payment/EfiBankPixService.cs` — Lógica de integração Pix
- `Services/Payment/EfiBankWebhookService.cs` — Webhook handler EfiBank
- `Services/Admin/AdminConfirmationService.cs` — Confirmação manual de pagamento
- `Services/Payment/EventPaymentReconciliationService.cs` — Reconciliação automática/manual
