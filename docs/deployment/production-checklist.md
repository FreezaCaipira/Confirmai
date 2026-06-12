# Production Deployment Checklist

## Pre-Deployment Requirements

### Environment Variables
- [ ] `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- [ ] `AdminSeed__Email` - Admin user email
- [ ] `AdminSeed__Password` - Admin user password
- [ ] `AdminSeed__FullName` - Admin user full name
- [ ] `Email__Username` - SMTP username
- [ ] `Email__Password` - SMTP password
- [ ] `BtcPay__WebhookSecret` - BTCPay webhook secret (if enabled)
- [ ] `BtcPay__ApiKey` - BTCPay API key (if enabled)
- [ ] `BtcPay__StoreId` - BTCPay store ID (if enabled)
- [ ] `CoinGecko__ApiKey` - CoinGecko API key (optional)
- [ ] `EfiBank__ClientCertificatePath` - Path to mTLS certificate

### Security Configuration
- [ ] SSL/TLS certificates installed and configured
- [ ] CSP headers properly configured with nonce support
- [ ] Security headers (X-Frame-Options, X-Content-Type-Options) enabled
- [ ] Cookie security settings verified (Secure, HttpOnly, SameSite)
- [ ] API keys rotated and secured

### Database Preparation
- [ ] Production database created and accessible
- [ ] Latest migrations applied
- [ ] Database backup performed
- [ ] Connection pooling configured

## Deployment Steps

### 1. Application Build
```bash
# Apply pending migrations
dotnet ef database update --project Confirmai.csproj

# Build for production
dotnet publish -c Release -o ./publish
```

### 2. Service Configuration
- [ ] Configure reverse proxy (nginx/Apache) with SSL termination
- [ ] Set up process manager (systemd/supervisor)
- [ ] Configure log rotation
- [ ] Set up monitoring and alerting

### 3. Application Startup
```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Start application
cd publish
dotnet Confirmai.dll
```

## Post-Deployment Verification

### Service Health
- [ ] Application starts without errors
- [ ] Database connectivity confirmed
- [ ] Authentication working
- [ ] Payment gateways responsive
- [ ] Email service functional

### Functional Testing
- [ ] User registration and login
- [ ] Event creation and management
- [ ] Payment processing flow
- [ ] Admin dashboard access
- [ ] API endpoints responsive

### Monitoring Setup
- [ ] Log aggregation configured
- [ ] Health check endpoints responding
- [ ] Performance metrics collection
- [ ] Alerting rules activated

## Rollback Plan

### If Critical Issues Occur
1. Stop current application instance
2. Restore database from pre-deployment backup
3. Deploy previous application version
4. Verify service functionality
5. Document incident and root cause

### Database Rollback
```bash
# Restore from backup
psql -U <user> Confirmai < backup_file.sql
```

## Ongoing Maintenance

### Regular Tasks
- [ ] Monitor certificate expiration
- [ ] Review and rotate API keys
- [ ] Check disk space and log rotation
- [ ] Update dependencies and apply security patches
- [ ] Review and test backup procedures

### Monitoring Checks
- [ ] Daily: Check application logs for errors
- [ ] Weekly: Review performance metrics
- [ ] Monthly: Verify backup integrity
- [ ] Quarterly: Security configuration review
