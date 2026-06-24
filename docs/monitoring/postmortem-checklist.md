# Post-Incident Review Checklist

Structured checklist for conducting post-incident reviews (post-mortems) to ensure root cause analysis and continuous improvement.

## Purpose

- Understand what happened
- Identify root causes
- Prevent recurrence
- Improve system resilience
- Share learnings across the team

## When to Conduct

- **P1 incidents**: Within 24-48 hours after resolution
- **P2 incidents**: Within 3-5 business days after resolution
- **P3 incidents**: Optional, during monthly review if significant impact

## Participants

- **Required**: Incident commander, on-call engineer, service owner
- **Optional**: Product owner, security team, customer support
- **Facilitator**: Neutral party (not directly involved in incident)

## Pre-Meeting Preparation

### 1. Gather Timeline
- [ ] Collect alert timestamps
- [ ] Gather log entries with timestamps
- [ ] Compile metric graphs (Prometheus/Grafana)
- [ ] Document user-reported issues
- [ ] Note any manual interventions

### 2. Prepare Data
- [ ] Export relevant logs (AdminLogs, application logs)
- [ ] Capture error messages and stack traces
- [ ] Document configuration changes (deployments, settings)
- [ ] List affected users/transactions
- [ ] Note any recent code changes

### 3. Schedule Meeting
- [ ] Invite required participants
- [ ] Send timeline and data 24h in advance
- [ ] Allocate 60-90 minutes for review
- [ ] Assign note-taker

## Meeting Agenda

### 1. Incident Overview (5 min)
- [ ] What was the incident?
- [ ] When did it occur?
- [ ] What was the impact?
- [ ] Who was affected?

### 2. Timeline Review (15-20 min)
- [ ] Walk through the timeline chronologically
- [ ] Identify key decision points
- [ ] Note any delays in detection/response
- [ ] Highlight manual interventions

### 3. Root Cause Analysis (20-30 min)
Use the **5 Whys** technique:

- [ ] Why did the incident occur?
- [ ] Why did that cause happen?
- [ ] Why was that condition present?
- [ ] Why was that not detected earlier?
- [ ] Why was that mitigation not in place?

### 4. Response Effectiveness (10 min)
- [ ] Was the incident detected promptly?
- [ ] Was the severity correctly assessed?
- [ ] Was the response timely?
- [ ] Was communication effective?
- [ ] Were the right people involved?

### 5. Action Items (10-15 min)
- [ ] Identify immediate fixes
- [ ] Identify process improvements
- [ ] Identify monitoring/alerting improvements
- [ ] Assign owners and deadlines
- [ ] Prioritize by impact/effort

## Root Cause Categories

### Code Issues
- [ ] Bug in application logic
- [ ] Race condition
- [ ] Memory leak
- [ ] Performance regression
- [ ] Edge case not handled

### Configuration Issues
- [ ] Incorrect configuration value
- [ ] Missing configuration
- [ ] Configuration drift between environments
- [ ] Secret/key rotation failure

### Infrastructure Issues
- [ ] Resource exhaustion (CPU, memory, disk)
- [ ] Network connectivity
- [ ] Database connection pool exhaustion
- [ ] Third-party service outage
- [ ] CDN/proxy issues

### Process Issues
- [ ] Insufficient monitoring/alerting
- [ ] Runbook not followed
- [ ] Missing documentation
- [ ] Communication breakdown
- [ ] Change management failure

### Human Error
- [ ] Manual mistake
- [ ] Fatigue/stress
- [ ] Lack of training
- [ ] Process unclear

## Action Item Types

### Immediate (Fix within 24-48h)
- [ ] Hotfix deployment
- [ ] Configuration change
- [ ] Alert threshold adjustment
- [ ] Temporary workaround

### Short-term (Fix within 1-2 weeks)
- [ ] Code refactoring
- [ ] Monitoring enhancement
- [ ] Documentation update
- [ ] Runbook improvement

### Long-term (Fix within 1-3 months)
- [ ] Architecture change
- [ ] Process redesign
- [ ] Tooling improvement
- [ ] Training program

## Post-Mortem Template

```markdown
# Post-Incident Review: [Incident Title]

## Incident Summary
- **Date**: [YYYY-MM-DD]
- **Duration**: [X hours/minutes]
- **Severity**: [P1/P2/P3]
- **Impact**: [Description of user/business impact]
- **Affected Systems**: [List of systems/services]

## Timeline
| Time (UTC-3) | Event | Source |
|--------------|-------|--------|
| HH:MM | [Event description] | [Alert/log/human] |
| HH:MM | [Event description] | [Alert/log/human] |

## Root Cause Analysis

### Primary Root Cause
[Description of the primary root cause]

### Contributing Factors
- [Factor 1]
- [Factor 2]
- [Factor 3]

### 5 Whys Analysis
1. Why did [incident] occur?
   - [Answer]
2. Why did [cause] happen?
   - [Answer]
3. Why was [condition] present?
   - [Answer]
4. Why was it not detected earlier?
   - [Answer]
5. Why was mitigation not in place?
   - [Answer]

## Response Effectiveness

### What Went Well
- [Positive aspect 1]
- [Positive aspect 2]

### What Could Be Improved
- [Improvement area 1]
- [Improvement area 2]

### Detection & Response Time
- **Time to detect**: [X minutes]
- **Time to acknowledge**: [X minutes]
- **Time to resolve**: [X minutes]
- **Target times**: [detect/acknowledge/resolve]

## Action Items

| Priority | Action | Owner | Due Date | Status |
|----------|--------|-------|----------|--------|
| P1 | [Action description] | [Name] | [YYYY-MM-DD] | [Open/In Progress/Done] |
| P2 | [Action description] | [Name] | [YYYY-MM-DD] | [Open/In Progress/Done] |
| P3 | [Action description] | [Name] | [YYYY-MM-DD] | [Open/In Progress/Done] |

## Follow-Up

### Review Date
[Date for follow-up review]

### Success Criteria
- [Criteria 1]
- [Criteria 2]

### Lessons Learned
- [Lesson 1]
- [Lesson 2]

## Attachments
- [Link to logs]
- [Link to metrics]
- [Link to relevant tickets]
```

## Blameless Culture Guidelines

### Do
- Focus on systems and processes
- Acknowledge human factors as system design issues
- Share learnings openly
- Encourage honest participation
- Thank participants for their work

### Don't
- Assign blame to individuals
- Use language like "human error" without context
- Punish for mistakes
- Hide incidents or learnings
- Rush to conclusions without data

## Follow-Up Process

### 1 Week After Review
- [ ] Check status of P1 action items
- [ ] Verify immediate fixes are working
- [ ] Confirm monitoring/alerting updates deployed

### 1 Month After Review
- [ ] Check status of P2 action items
- [ ] Verify short-term fixes are effective
- [ ] Review if similar incidents occurred

### 3 Months After Review
- [ ] Check status of P3 action items
- [ ] Verify long-term improvements are in place
- [ ] Conduct retrospective on effectiveness

## Incident Categories for Tracking

### Payment Processing
- Webhook failures
- Reconciliation errors
- Gateway outages
- Status mismatches

### Infrastructure
- Database issues
- API failures
- Resource exhaustion
- Network problems

### Application
- Code bugs
- Performance issues
- Configuration errors
- Deployment failures

### Security
- Unauthorized access
- Data exposure
- Authentication failures
- Rate limiting issues

## Metrics to Track

### Incident Metrics
- Number of incidents per month
- Mean time to detect (MTTD)
- Mean time to resolve (MTTR)
- Incident severity distribution

### Action Item Metrics
- Number of action items per incident
- Action item completion rate
- Time to complete action items
- Recurrence rate for similar incidents

## Related Docs

- `../observability-payments-runbook.md` - Incident response procedures
- `../monitoring/gateway-baseline.md` - Operational baselines
- `../monitoring/README.md` - Monitoring setup
- `./prometheus-payments-alerts.example.yml` - Alert rules
