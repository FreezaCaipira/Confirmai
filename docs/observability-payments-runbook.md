# Runbook de Observabilidade — Pagamentos e Reconciliação

Este runbook padroniza triagem e resposta a incidentes de pagamento em produção.

---

## 1. Objetivo e escopo

Cobrir incidentes de:

1. Falhas de webhook (autenticação, payload, indisponibilidade)
2. Pendências em reconciliação acima do esperado
3. Crescimento de staleness do painel operacional
4. Aumento de falhas/reembolsos em confirmações de evento

Referências operacionais no produto:

1. Painel: `/admin/payments`
2. Logs: `/admin/logs`
3. Timeline por confirmação: `/admin/audit/Payment/{confirmationId}`

---

## 2. Sinais de alerta (SLO operacional)

Acionar investigação quando qualquer condição ocorrer por 10+ minutos:

1. `payment.reconciliation.panel.stale` > 0 no período recente
2. `payment.reconciliation.sweep` com `stillPending` crescente por 3 varreduras consecutivas
3. `payment.failed` acima de 5 eventos em 15 minutos
4. `payment.refunded` acima de 3 eventos em 30 minutos
5. `Webhook` com nível `Warning` acima de 10 eventos em 15 minutos

---

## 3. Triage rápida (5 minutos)

### 3.1 Painel admin

1. Abrir `/admin/payments`
2. Verificar:
   - `Pendentes com chargeId`
   - `Pendentes antigos (+30 min)`
   - `Telemetria por gateway` (share e severidade)
   - Última varredura automática e tendência 24h

### 3.2 Logs com filtros prontos

1. Incidentes de staleness (últimos 7 dias):
   - `/admin/logs?eventType=payment.reconciliation.panel.stale&startDate={YYYY-MM-DD}&endDate={YYYY-MM-DD}`
2. Falhas de pagamento:
   - `/admin/logs?eventType=payment.failed`
3. Reembolsos:
   - `/admin/logs?eventType=payment.refunded`
4. Reconciliacao automática/manual:
   - `/admin/logs?eventType=payment.reconciliation.sweep`
5. Problemas de webhook:
   - `/admin/logs?source=Webhook&level=Warning`

### 3.3 Timeline da confirmação impactada

1. A partir do `confirmationId`, abrir:
   - `/admin/audit/Payment/{confirmationId}`
2. Confirmar sequência esperada:
   - `payment.confirmed` ou `payment.failed` ou `payment.refunded`
   - metadata com `origin`, `gateway`, `pixTxId/chargeId`

---

## 4. Consultas SQL de apoio (PostgreSQL)

Use quando o painel estiver indisponível ou para análise histórica.

### 4.1 Taxa por tipo de evento (últimas 24h)

```sql
SELECT
    "EventType",
    COUNT(*) AS total
FROM "Logs"
WHERE "Timestamp" >= NOW() - INTERVAL '24 hours'
  AND "EventType" IN (
      'payment.confirmed',
      'payment.failed',
      'payment.refunded',
      'payment.reconciliation.sweep',
      'payment.reconciliation.panel.stale'
  )
GROUP BY "EventType"
ORDER BY total DESC;
```

### 4.2 Warnings de webhook por janela de 15 min

```sql
SELECT
    date_trunc('minute', "Timestamp") -
    ((EXTRACT(minute FROM "Timestamp")::int % 15) * INTERVAL '1 minute') AS bucket_15m,
    COUNT(*) AS warnings
FROM "Logs"
WHERE "Timestamp" >= NOW() - INTERVAL '6 hours'
  AND "Source" = 'Webhook'
  AND "Level" = 'Warning'
GROUP BY bucket_15m
ORDER BY bucket_15m DESC;
```

### 4.3 Pendências por gateway (snapshot)

```sql
SELECT
    COALESCE("PaymentGatewayName", 'unknown') AS gateway,
    SUM(CASE WHEN "PaymentStatus" = 0 THEN 1 ELSE 0 END) AS pending,
    SUM(CASE WHEN "PaymentStatus" = 0 AND "ConfirmedAt" <= NOW() - INTERVAL '30 minutes' THEN 1 ELSE 0 END) AS stale_pending,
    SUM(CASE WHEN "PaymentStatus" = 1 THEN 1 ELSE 0 END) AS paid_total,
    SUM(CASE WHEN "PaymentStatus" = 2 THEN 1 ELSE 0 END) AS failed_total,
    SUM(CASE WHEN "PaymentStatus" = 3 THEN 1 ELSE 0 END) AS refunded_total
FROM "EventConfirmations"
GROUP BY COALESCE("PaymentGatewayName", 'unknown')
ORDER BY pending DESC, gateway;
```

### 4.4 Top confirmações com mais mudanças de status (7 dias)

```sql
SELECT
    "EntityId" AS confirmation_id,
    COUNT(*) AS transitions
FROM "Logs"
WHERE "Timestamp" >= NOW() - INTERVAL '7 days'
  AND "EntityType" = 'Payment'
  AND "EventType" IN ('payment.status.changed', 'payment.failed', 'payment.refunded', 'payment.confirmed')
GROUP BY "EntityId"
ORDER BY transitions DESC
LIMIT 20;
```

---

## 5. Playbook de resposta

### Cenário A — Staleness recorrente

1. Executar varredura manual em `/admin/payments`
2. Verificar warnings de webhook no mesmo intervalo
3. Se persistir por >30 min:
   - abrir incidente P2
   - reduzir taxa de entrada (se possível)
   - acompanhar `stillPending` em janelas de 15 min

### Cenário B — Falhas concentradas em gateway específico

1. Confirmar telemetria por gateway no painel
2. Checar warnings `Webhook` + eventos `payment.failed`
3. Executar reconciliação manual por `chargeId/txId` nos casos críticos
4. Se houver impacto de conversão:
   - abrir incidente P1
   - considerar desabilitar gateway degradado em `/admin/gateways`

### Cenário C — Reembolso em alta

1. Listar confirmações afetadas no período
2. Revisar timelines `/admin/audit/Payment/{confirmationId}`
3. Validar origem em metadata (`admin.payment.status.transition` vs webhook/reconciliação)
4. Se origem administrativa anômala: auditar conta e sessão do operador

---

## 6. Critérios de saída do incidente

Encerrar somente quando:

1. Smoke pós-deploy passa
2. `stillPending` estabiliza/caí por pelo menos 3 ciclos
3. `Webhook Warning` retorna para baseline
4. Não há novos `payment.reconciliation.panel.stale` por 30 min
5. Ação corretiva e causa raiz registradas

---

## 7. Pós-incidente

Registrar no relatório:

1. Janela do incidente (início/fim)
2. Gateway(s) afetados
3. Métricas antes/depois
4. Confirmações impactadas
5. Mitigação aplicada
6. Follow-ups técnicos (código/processo/alerta)
