# LogQL Alerts (Fallback) — Payments/Reconciliation

Use this fallback when domain counters are not exposed as Prometheus metrics yet.

Assumptions:

1. Application logs are exported to Loki via OTLP/collector.
2. Structured fields are mapped into labels or searchable JSON keys.
3. At minimum, you can filter by `service_name`, `EventType`, `Source`, `Level`.

---

## Suggested stream selector

Adjust labels to your pipeline:

```logql
{service_name="Confirmai", deployment_environment="production"}
```

---

## A1 — Panel staleness (P2)

Condition: at least 1 `payment.reconciliation.panel.stale` event in 10m.

```logql
sum(
  count_over_time(
    {service_name="Confirmai", deployment_environment="production"}
    |= "payment.reconciliation.panel.stale" [10m]
  )
) >= 1
```

---

## A3 — Payment failed spike (P1)

Condition: more than 5 `payment.failed` events in 15m.

```logql
sum(
  count_over_time(
    {service_name="Confirmai", deployment_environment="production"}
    |= "payment.failed" [15m]
  )
) > 5
```

---

## A4 — Refunded spike (P1)

Condition: more than 3 `payment.refunded` events in 30m.

```logql
sum(
  count_over_time(
    {service_name="Confirmai", deployment_environment="production"}
    |= "payment.refunded" [30m]
  )
) > 3
```

---

## A5 — Webhook warning spike (P1)

Condition: more than 10 warnings from webhook in 15m.

```logql
sum(
  count_over_time(
    {service_name="Confirmai", deployment_environment="production"}
    |= "Webhook"
    |= "Warning" [15m]
  )
) > 10
```

---

## Notes

1. Prefer exact JSON field filters in your Loki parser stage when available.
2. Keep this fallback active until domain counters are instrumented and validated.
3. Runbook for response: `docs/observability-payments-runbook.md`.
