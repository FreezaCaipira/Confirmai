# Regras de Alerta — Pagamentos e Reconciliação

Este documento converte o runbook em regras objetivas para monitoramento e on-call.

---

## 1. Objetivo

Definir alertas mínimos para detectar cedo degradação de pagamento, webhook e reconciliação.

Escopo atual:

1. Eventos auditados em `/admin/logs`
2. Indicadores do painel `/admin/payments`
3. Consultas SQL em `Logs` e `EventConfirmations`

---

## 2. Baseline inicial (ajustar após 2 semanas)

Use este baseline como ponto de partida:

1. `payment.reconciliation.panel.stale`: esperado 0/h
2. `payment.failed`: até 5 em 15 min
3. `payment.refunded`: até 3 em 30 min
4. `Webhook Warning`: até 10 em 15 min
5. `stillPending` em sweep: tendência estável/decrescente

---

## 3. Matriz de alertas

## Alerta A1 — Staleness do painel de reconciliação

- Sinal: evento `payment.reconciliation.panel.stale`
- Condição: `count >= 1` em 10 min
- Severidade: P2
- Ação imediata:
  1. Abrir `/admin/payments` e executar varredura manual
  2. Abrir `/admin/logs?eventType=payment.reconciliation.panel.stale`
  3. Validar warnings de webhook no mesmo período

## Alerta A2 — Pendências crescendo em reconciliação

- Sinal: `payment.reconciliation.sweep` com `stillPending`
- Condição: crescimento em 3 varreduras consecutivas
- Severidade: P2 (P1 se também houver falha de webhook)
- Ação imediata:
  1. Revisar telemetria por gateway em `/admin/payments`
  2. Rodar reconciliação manual por casos críticos (`chargeId/txId`)

## Alerta A3 — Falhas de pagamento acima do limite

- Sinal: evento `payment.failed`
- Condição: `count > 5` em 15 min
- Severidade: P1
- Ação imediata:
  1. Identificar gateway dominante
  2. Correlacionar com `Webhook Warning`
  3. Avaliar desabilitar gateway degradado em `/admin/gateways`

## Alerta A4 — Reembolsos em pico

- Sinal: evento `payment.refunded`
- Condição: `count > 3` em 30 min
- Severidade: P1
- Ação imediata:
  1. Revisar timelines `/admin/audit/Payment/{confirmationId}`
  2. Validar origem (`admin.payment.status.transition` vs webhook/reconciliação)

## Alerta A5 — Warnings de webhook

- Sinal: logs com `Source=Webhook` e `Level=Warning`
- Condição: `count > 10` em 15 min
- Severidade: P1
- Ação imediata:
  1. Verificar segredo/assinatura/configuração do endpoint
  2. Correlacionar com `payment.failed` no mesmo período

---

## 4. SQL de referência para automação

## 4.1 A1/A5 — Contagem por janela (15 min)

```sql
SELECT
    date_trunc('minute', "Timestamp") -
    ((EXTRACT(minute FROM "Timestamp")::int % 15) * INTERVAL '1 minute') AS bucket_15m,
    SUM(CASE WHEN "EventType" = 'payment.reconciliation.panel.stale' THEN 1 ELSE 0 END) AS stale_count,
    SUM(CASE WHEN "Source" = 'Webhook' AND "Level" = 'Warning' THEN 1 ELSE 0 END) AS webhook_warning_count
FROM "Logs"
WHERE "Timestamp" >= NOW() - INTERVAL '6 hours'
GROUP BY bucket_15m
ORDER BY bucket_15m DESC;
```

## 4.2 A3/A4 — Falha e reembolso por janela

```sql
SELECT
    date_trunc('minute', "Timestamp") -
    ((EXTRACT(minute FROM "Timestamp")::int % 15) * INTERVAL '1 minute') AS bucket_15m,
    SUM(CASE WHEN "EventType" = 'payment.failed' THEN 1 ELSE 0 END) AS failed_count,
    SUM(CASE WHEN "EventType" = 'payment.refunded' THEN 1 ELSE 0 END) AS refunded_count
FROM "Logs"
WHERE "Timestamp" >= NOW() - INTERVAL '24 hours'
GROUP BY bucket_15m
ORDER BY bucket_15m DESC;
```

---

## 4.3 Mapeamento evento -> métrica (Prometheus)

As regras em `docs/monitoring/prometheus-payments-alerts.example.yml` usam os seguintes contadores emitidos pelo app:

1. `payment.reconciliation.panel.stale` -> `confirmai_payment_reconciliation_panel_stale_total`
2. `payment.failed` -> `confirmai_payment_failed_total`
3. `payment.refunded` -> `confirmai_payment_refunded_total`
4. `Source=Webhook` + `Level=Warning` -> `confirmai_webhook_warnings_total`
5. `payment.reconciliation.sweep` com `stillPending` maior que a varredura anterior -> `confirmai_payment_reconciliation_pending_growth_events_total`

Observação: o contador de crescimento de pendências é process-local (worker/app instance). Em ambiente com múltiplas réplicas, agregue por instância no backend de métricas.

---

## 5. Política de escalonamento

1. P2 por 30 min sem melhora: escalar para P1
2. P1 por 15 min: envolver responsável de gateway e liderança técnica
3. Incidente encerrado somente com critérios do runbook atendidos

---

## 6. Cadência operacional

1. Revisão semanal do baseline por gateway
2. Revisão quinzenal de thresholds e falso-positivo
3. Revisão pós-incidente obrigatória com ações preventivas

---

## 7. Referências

1. `docs/observability-payments-runbook.md`
2. `docs/production-checklist.md`
3. `docs/deploy.md`
4. `docs/monitoring/prometheus-payments-alerts.example.yml`
5. `docs/monitoring/alertmanager-payments-routing.example.yml`
6. `docs/monitoring/README.md`
7. `docs/monitoring/logql-payments-alerts.example.md`
8. `docs/monitoring/grafana-payments-dashboard.example.json`
