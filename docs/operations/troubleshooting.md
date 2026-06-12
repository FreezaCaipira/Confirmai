# Troubleshooting Guide

## Common Issues and Solutions

### Database Connection Issues
**Error**: `password authentication failed for user`
**Solution**: 
1. Verify PostgreSQL user was created correctly
2. Confirm password in `appsettings.json` is correct
3. Ensure PostgreSQL service is running

**Error**: `database "Confirmai" does not exist`
**Solution**:
1. Run database creation commands:
   ```bash
   sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER youruser;"
   sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE Confirmai TO youruser;"
   ```
2. Verify database name in `appsettings.json`

### Payment Gateway Issues
**Webhook not processing**:
1. Check webhook URL configuration
2. Verify webhook secrets match
3. Review logs for webhook processing errors

**Payment not confirming**:
1. Check gateway API keys
2. Verify payment status in gateway dashboard
3. Review reconciliation service logs

### Authentication Problems
**Cannot login after password reset**:
1. Check email configuration
2. Verify email delivery in `wwwroot/uploads/dev-emails`
3. Confirm email confirmation requirements for environment

**Session timeout issues**:
1. Review cookie configuration in `Program.cs`
2. Check security stamp validation settings
3. Verify client-side session handling

### Performance Issues
**Slow page loads**:
1. Check for excessive `StateHasChanged()` calls
2. Review database query performance
3. Implement virtualization for large data sets

**Memory leaks**:
1. Ensure `IAsyncDisposable` is implemented in components
2. Check for unsubscribed event handlers
3. Review SignalR connection management

### CSS/Styling Issues
**Styles not applying**:
1. Verify CSS isolation files exist for components
2. Check for naming conflicts in CSS classes
3. Confirm Blazor CSS isolation is working properly

## Log Analysis
### Finding Payment Issues
```bash
# Search for payment-related logs
grep -i "payment" /var/log/confirmai/confirmai.log

# Find webhook processing errors
grep "webhook" /var/log/confirmai/confirmai.log | grep "error"
```

### Monitoring Service Health
```bash
# Check for certificate expiration warnings
grep "CertificateHealthCheckService" /var/log/confirmai/confirmai.log

# Find pending webhook alerts
grep "PendingWebhooksAlertService" /var/log/confirmai/confirmai.log
```

## Environment-Specific Issues

### Development Environment
**Emails not sending**:
1. Check if `Email:Enabled` is set to true
2. Verify SMTP configuration
3. Check fallback email files in `wwwroot/uploads/dev-emails`

### Production Environment
**Security warnings**:
1. Verify HTTPS configuration
2. Check CSP headers
3. Review security stamp validation settings

**Performance degradation**:
1. Monitor memory usage of circuits
2. Check for database connection pool exhaustion
3. Review background service execution times

## Diagnostic Tools
### Health Checks
1. `/health` endpoint for basic service status
2. Admin dashboard for system metrics
3. Log analysis for error patterns

### Performance Monitoring
1. Database query timing logs
2. Circuit retention metrics
3. Memory usage tracking

## Escalation Process
1. Check application logs
2. Review system metrics
3. Contact infrastructure team for server issues
4. Engage payment gateway support for payment issues
