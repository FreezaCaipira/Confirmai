# Monitoring Templates (Payments)

Templates to operationalize payment/reconciliation alerts in production.

## Files

1. `prometheus-payments-alerts.example.yml`
   - Prometheus alert rules with severities (`p1`, `p2`) and runbook annotations.
2. `alertmanager-payments-routing.example.yml`
   - Alertmanager routing for escalation by severity and team.
3. `logql-payments-alerts.example.md`
   - Fallback alert queries using logs (Loki/LogQL) while domain counters are unavailable.
4. `grafana-payments-dashboard.example.json`
   - Grafana dashboard template for the 5 payment/reconciliation alert signals.

## Current status

1. OTLP pipeline for traces/metrics/logs is available in the app configuration.
2. Domain counters used by payment/reconciliation alerts are instrumented in the app via audit/log pipeline.
3. A compose-based monitoring stack is now versioned in `ops/monitoring/`:
   - `otel-collector.yml` receives OTLP from the app and exposes Prometheus metrics.
   - `prometheus.yml` loads the payment alert rules and forwards alerts to Alertmanager.
   - `alertmanager.yml` routes `p1`/`p2` payment alerts to a validation webhook inside the environment.
   - Grafana is provisioned with a Prometheus datasource (`uid=prometheus`) and the payment dashboard preloaded.
4. Keep LogQL fallback alerts active during rollout/validation windows.

## Compose rollout

1. Start the base environment normally with `docker compose up -d`.
2. Start the alerting stack with `docker compose --profile monitoring up -d prometheus alertmanager grafana alert-webhook`.
3. Confirm the app is exporting OTLP to `http://otel-collector:4318` (default in `docker-compose.yml`).
4. Open Prometheus at `http://localhost:9090`, Alertmanager at `http://localhost:9093`, and Grafana at `http://localhost:3000`.
5. Use the webhook sink at `http://localhost:18080` to validate that alerts are being delivered end to end.
6. Run `scripts/smoke-monitoring-alerts.ps1` to validate the app, collector, Prometheus, Alertmanager, Grafana, and validation webhook in one pass.

### Smoke command

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-monitoring-alerts.ps1 -RequirePaymentRules -RequireCollectorMetrics
```

## Integration Checklist

1. Review `ops/monitoring/prometheus-payments-alerts.yml` against your production thresholds.
2. Replace validation webhook receivers in `ops/monitoring/alertmanager.yml` with your Slack/PagerDuty/Teams endpoints.
3. Start `docker compose --profile monitoring up -d` and confirm the targets are healthy in Prometheus.
4. Validate alerts in staging with synthetic events.
5. If counters are missing in your backend, apply `logql-payments-alerts.example.md` in Grafana alerts.
6. Confirm the provisioned Grafana datasource/dashboard are healthy; import `grafana-payments-dashboard.example.json` manually only if you need a customized copy.
7. Confirm incident routing and resolution notifications.

## Grafana quick import

1. Open Grafana -> Dashboards -> New -> Import.
2. Upload `grafana-payments-dashboard.example.json`.
3. Select your Prometheus datasource for `DS_PROMETHEUS`.
4. Save dashboard and set refresh to 30s.

## Related docs

1. `../payments-alert-rules.md`
2. `../observability-payments-runbook.md`
3. `../production-checklist.md`
4. `../deploy.md`
5. `./logql-payments-alerts.example.md`
6. `./grafana-payments-dashboard.example.json`
