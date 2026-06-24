# Gateway Operational Baseline (Weekly)

Weekly operational baseline metrics for payment gateways to enable proactive problem detection.

## Purpose

Define expected operational ranges for each payment gateway to establish:
- Normal behavior patterns
- Alert thresholds for anomaly detection
- Performance regression detection
- Capacity planning insights

## Gateways

### EfiBank (Pix)

#### Volume Metrics
- **Daily transactions**: Expected 50-200 transactions/day
- **Peak hours**: 19:00-22:00 UTC-3 (60% of daily volume)
- **Weekend pattern**: 30-40% of weekday volume
- **Baseline trend**: Growing 5-10% week-over-week

#### Latency Metrics
- **Webhook delivery**: < 5s p95, < 10s p99
- **QR code generation**: < 2s p95
- **Status check API**: < 1s p95
- **Reconciliation sweep**: < 30s for 50 confirmations

#### Error Rate Metrics
- **Webhook failures**: < 1% of total webhooks
- **QR generation failures**: < 0.5%
- **API timeout rate**: < 0.1%
- **Stale pending rate**: < 5% of pending confirmations after 10min

#### Success Rate Metrics
- **Payment confirmation rate**: > 95% within 30min
- **Reconciliation match rate**: > 90% of pending confirmations
- **Auto-sweep success rate**: > 80% of stale pendings

### AbacatePay

#### Volume Metrics
- **Daily transactions**: Expected 10-50 transactions/day
- **Peak hours**: 12:00-14:00 UTC-3 (50% of daily volume)
- **Weekend pattern**: 20-30% of weekday volume
- **Baseline trend**: Stable ±5% week-over-week

#### Latency Metrics
- **Webhook delivery**: < 10s p95, < 20s p99
- **Payment link generation**: < 3s p95
- **Status check API**: < 2s p95
- **Reconciliation sweep**: < 45s for 50 confirmations

#### Error Rate Metrics
- **Webhook failures**: < 2% of total webhooks
- **Link generation failures**: < 1%
- **API timeout rate**: < 0.5%
- **Stale pending rate**: < 10% of pending confirmations after 15min

#### Success Rate Metrics
- **Payment confirmation rate**: > 90% within 45min
- **Reconciliation match rate**: > 85% of pending confirmations
- **Auto-sweep success rate**: > 70% of stale pendings

### Appmax

#### Volume Metrics
- **Daily transactions**: Expected 5-20 transactions/day
- **Peak hours**: 10:00-12:00 UTC-3 (40% of daily volume)
- **Weekend pattern**: 10-20% of weekday volume
- **Baseline trend**: Declining -5% week-over-week (consider deprecation)

#### Latency Metrics
- **Webhook delivery**: < 15s p95, < 30s p99
- **Payment link generation**: < 5s p95
- **Status check API**: < 3s p95
- **Reconciliation sweep**: < 60s for 50 confirmations

#### Error Rate Metrics
- **Webhook failures**: < 3% of total webhooks
- **Link generation failures**: < 2%
- **API timeout rate**: < 1%
- **Stale pending rate**: < 15% of pending confirmations after 20min

#### Success Rate Metrics
- **Payment confirmation rate**: > 85% within 60min
- **Reconciliation match rate**: > 80% of pending confirmations
- **Auto-sweep success rate**: > 60% of stale pendings

### BtcPay

#### Volume Metrics
- **Daily transactions**: Expected 1-5 transactions/day
- **Peak hours**: No clear pattern (sporadic)
- **Weekend pattern**: Similar to weekday
- **Baseline trend**: Stable ±2% week-over-week

#### Latency Metrics
- **Webhook delivery**: < 30s p95, < 60s p99
- **Invoice generation**: < 10s p95
- **Status check API**: < 5s p95
- **Reconciliation sweep**: < 90s for 50 confirmations

#### Error Rate Metrics
- **Webhook failures**: < 5% of total webhooks
- **Invoice generation failures**: < 3%
- **API timeout rate**: < 2%
- **Stale pending rate**: < 20% of pending confirmations after 30min

#### Success Rate Metrics
- **Payment confirmation rate**: > 80% within 120min
- **Reconciliation match rate**: > 75% of pending confirmations
- **Auto-sweep success rate**: > 50% of stale pendings

## Alert Thresholds

### P1 Alerts (Immediate Action Required)

- **Any gateway**: Error rate > 5% for 5min
- **Any gateway**: Webhook failure rate > 10% for 10min
- **EfiBank**: Stale pending rate > 20% for 15min
- **AbacatePay**: Stale pending rate > 25% for 20min
- **Any gateway**: API timeout rate > 2% for 10min

### P2 Alerts (Investigate Within 1 Hour)

- **Any gateway**: Latency p95 > 2x baseline for 15min
- **Any gateway**: Success rate < 80% for 30min
- **EfiBank**: Daily volume < 50% of baseline
- **AbacatePay**: Daily volume < 40% of baseline
- **Any gateway**: Reconciliation match rate < 70% for 1h

### P3 Alerts (Review Within 24 Hours)

- **Any gateway**: Gradual volume trend > 20% week-over-week
- **Any gateway**: Latency p99 > 3x baseline for 1h
- **Appmax**: Daily volume < 20% of baseline (consider deprecation)
- **Any gateway**: Weekend pattern deviation > 30%

## Weekly Review Process

### Monday Morning (09:00 UTC-3)

1. **Volume Review**
   - Compare daily volumes vs baseline for each gateway
   - Identify trends > 10% deviation
   - Note seasonal patterns (holidays, events)

2. **Latency Review**
   - Check p95/p99 latency metrics
   - Identify regressions > 20% from baseline
   - Correlate with system load/maintenance windows

3. **Error Rate Review**
   - Review error rates by gateway
   - Analyze error patterns (time of day, transaction type)
   - Track error rate trends week-over-week

4. **Success Rate Review**
   - Confirm payment confirmation rates
   - Check reconciliation match rates
   - Validate auto-sweep effectiveness

5. **Alert Review**
   - Review all P1/P2 alerts from previous week
   - Document root causes and resolutions
   - Update alert thresholds if needed

### Reporting Template

```markdown
## Gateway Baseline Report - Week of [DATE]

### EfiBank
- Volume: [actual] vs baseline [expected] ([deviation]%)
- Latency p95: [actual]s vs baseline [expected]s ([deviation]%)
- Error rate: [actual]% vs baseline <1% ([status])
- Success rate: [actual]% vs baseline >95% ([status])
- Alerts: [count] P1, [count] P2

### AbacatePay
- Volume: [actual] vs baseline [expected] ([deviation]%)
- Latency p95: [actual]s vs baseline [expected]s ([deviation]%)
- Error rate: [actual]% vs baseline <2% ([status])
- Success rate: [actual]% vs baseline >90% ([status])
- Alerts: [count] P1, [count] P2

### Appmax
- Volume: [actual] vs baseline [expected] ([deviation]%)
- Latency p95: [actual]s vs baseline [expected]s ([deviation]%)
- Error rate: [actual]% vs baseline <3% ([status])
- Success rate: [actual]% vs baseline >85% ([status])
- Alerts: [count] P1, [count] P2

### BtcPay
- Volume: [actual] vs baseline [expected] ([deviation]%)
- Latency p95: [actual]s vs baseline [expected]s ([deviation]%)
- Error rate: [actual]% vs baseline <5% ([status])
- Success rate: [actual]% vs baseline >80% ([status])
- Alerts: [count] P1, [count] P2

### Issues & Actions
- [Issue 1]: [Action taken]
- [Issue 2]: [Action taken]

### Recommendations
- [Recommendation 1]
- [Recommendation 2]
```

## Data Sources

### Prometheus Metrics
- `confirmai_payment_gateway_transactions_total{gateway="..."}`
- `confirmai_payment_gateway_latency_seconds{gateway="..."}`
- `confirmai_payment_gateway_errors_total{gateway="..."}`
- `confirmai_payment_gateway_success_rate{gateway="..."}`

### Admin Dashboard
- AdminPayments summary panel (real-time counts)
- Gateway telemetry panel (per-gateway breakdown)
- Reconciliation sweep history

### Logs
- Payment webhook delivery logs
- API call logs with latency
- Reconciliation sweep logs
- Error logs with gateway context

## Threshold Adjustment Process

1. **Monitor**: Collect 4 weeks of baseline data
2. **Analyze**: Identify normal patterns and outliers
3. **Adjust**: Update thresholds based on observed data
4. **Validate**: Monitor for 2 weeks with new thresholds
5. **Document**: Record rationale for threshold changes

## Related Docs

- `../monitoring/README.md` - Monitoring setup and integration
- `../observability-payments-runbook.md` - Incident response procedures
- `../payments-alert-rules.md` - Alert rule definitions
- `./prometheus-payments-alerts.example.yml` - Prometheus alert rules
