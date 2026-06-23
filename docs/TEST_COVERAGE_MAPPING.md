# Test Coverage Mapping - Confirmai Project

## Overview
This document tracks test coverage progress for the Confirmai Blazor project.

**Current Coverage:** 9.9% (11,733/118,427 lines)
**Target:** Increase coverage progressively for system longevity and bug prevention

## Coverage Progress

### Checkpoint 1 - 2026-06-23
- **Coverage:** 9.9% (11,733/118,427 lines)
- **Total Tests:** 1,905
- **Test Status:** All passing
- **Lines Added:** 37
- **Test Files Added:** 2 (BtcPayWebhookServiceStaticTests.cs, AdminLogsQueryOverridesParserStaticTests.cs)

### Recent Test Additions

#### Static Method Tests (via Reflection)
| Service | Method | Lines Added | Status |
|---------|--------|-------------|--------|
| EfiBankWebhookService | TryGetCertFromHeader | +7 | ✅ Completed |
| DebounceDispatcher | TryCancelAndDispose | +3 | ✅ Completed |
| ProductService | SaveImageAsync | +26 | ✅ Completed |
| BtcPayWebhookService | IsValidWebhookSecret, GetSingleWebhookSecretHeader, TryParseWebhookPayload | +0 (already covered) | ✅ Completed |
| AdminLogsQueryOverridesParser | ReadString, ReadDate | +1 | ✅ Completed |

**Total Lines Added:** 37

## Test Coverage by Directory

### Services
- **Payment Services:** Comprehensive coverage for webhook handlers (AbacatePay, EfiBank, BtcPay, Appmax)
- **Admin Services:** Extensive tests for logs, filtering, security policies
- **Event Services:** Tests for notifications, confirmations, scheduling
- **User Services:** Tests for preferences, claims factory
- **Utility Services:** Tests for product service, email sender, PII sanitizer

### Components (Blazor)
- **Coverage Status:** Most components have 0% coverage
- **Reason:** Integration tests for Blazor components yield minimal coverage gains
- **Strategy:** Focus on service layer testing for better ROI

### Models
- **Coverage Status:** Most DTOs and models have partial or full coverage
- **Strategy:** Test critical models with validation logic

## Coverage Gaps

### High Priority (Services with 0% or Low Coverage)
- EventNotificationSchedulerService/ExecuteAsync (0.08% - state machine)
- Most Blazor components (0% - PaginationControls, BtcQuoteCard, EntityProfileShell, Toast, etc.)

### Medium Priority (Services with Partial Coverage)
- Program.cs (72% - integration tests exist but can be expanded)
- IdentityEmailSender (100% line-rate, 60% branch-rate)

### Low Priority (Already Well Covered)
- Payment webhook services
- Admin services
- Event services
- User services

## Testing Strategy

### 1. Static Method Testing
- Use reflection to test private static methods
- Focus on methods with business logic
- Avoid testing trivial helpers

### 2. Service Layer Testing
- Use in-memory EF Core databases
- Mock dependencies with Moq
- Test public methods comprehensively

### 3. Integration Testing
- Use WebApplicationFactory for Blazor pages
- Test critical user flows
- Limit scope due to low coverage ROI

### 4. Avoid
- Testing trivial models without logic
- Testing static helpers with minimal logic
- Direct AppDbContext injection in Blazor pages

## Next Steps

### Short Term
1. Continue adding tests for private static methods in services
2. Expand existing test suites for edge cases
3. Add tests for services with 0% coverage

### Medium Term
1. Create integration tests for critical user flows
2. Add tests for Program.cs uncovered branches
3. Improve branch coverage for services with high line-rate but low branch-rate

### Long Term
1. Achieve 15-20% coverage target
2. Establish CI/CD coverage gates
3. Create coverage reports per module

## Test Files

### Static Method Test Files
- `EfiBankWebhookServiceTests.cs` - Private method tests
- `DebounceDispatcherTests.cs` - Private static method tests
- `ProductServiceStaticTests.cs` - Private static method tests
- `ProductServiceTests.cs` - Public method tests
- `BtcPayWebhookServiceStaticTests.cs` - Private static method tests (NEW)
- `BtcPayWebhookServiceTests.cs` - Public method tests
- `AppmaxPixServiceStaticTests.cs` - Private static method tests
- `AppmaxPixServiceTests.cs` - Public method tests
- `AdminLogsQueryOverridesParserStaticTests.cs` - Private static method tests (NEW)
- `AdminLogsQueryOverridesParserTests.cs` - Public method tests
- `EventConfirmationPaymentStatusServiceStaticTests.cs` - Private static method tests
- `AdminLogsExportServiceStaticTests.cs` - Private static method tests

### Integration Test Files
- `ProgramConfigurationTests.cs` - Program.cs configuration
- Various `*IntegrationTests.cs` files for Blazor pages

## Running Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage report is generated in:
`Confirmai.Tests/TestResults/{guid}/coverage.cobertura.xml`

## Notes

- Focus on system longevity and bug prevention
- Not aiming for 100% coverage
- Prioritize critical business logic
- Balance test effort with coverage ROI
