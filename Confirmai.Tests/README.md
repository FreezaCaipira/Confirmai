# Confirmai.Tests — Mapeamento de Cobertura

Este diretório concentra testes unitários e de integração da aplicação.

## Status atual

- Ultima validacao local: 2026-06-14
- Resultado: 551 passando, 24 falhando, 575 total
- Comando: `dotnet test Confirmai.Tests/Confirmai.Tests.csproj -v minimal`

## Mapa por dominio

### Admin e seguranca

- Convencoes/autorizacao: [AdminAuthorizationConventionsTests.cs](AdminAuthorizationConventionsTests.cs)
- Filtros e estado (logs, users, payments):
  - [AdminLogsFilterStateServiceTests.cs](AdminLogsFilterStateServiceTests.cs)
  - [AdminLogsFilterInferenceTests.cs](AdminLogsFilterInferenceTests.cs)
  - [AdminLogsFilterStateMergerTests.cs](AdminLogsFilterStateMergerTests.cs)
  - [AdminLogsFilterStateRulesTests.cs](AdminLogsFilterStateRulesTests.cs)
  - [AdminUsersFilterStateServiceTests.cs](AdminUsersFilterStateServiceTests.cs)
  - [AdminPaymentsFilterStateServiceTests.cs](AdminPaymentsFilterStateServiceTests.cs)
- Query/export/log sorting:
  - [AdminLogsQueryServiceIntegrationTests.cs](AdminLogsQueryServiceIntegrationTests.cs)
  - [AdminLogsQueryStringIntegrationTests.cs](AdminLogsQueryStringIntegrationTests.cs)
  - [AdminLogsQueryOverridesParserTests.cs](AdminLogsQueryOverridesParserTests.cs)
  - [AdminLogsExportServiceTests.cs](AdminLogsExportServiceTests.cs)
  - [AdminLogSortingIntegrationTests.cs](AdminLogSortingIntegrationTests.cs)
  - [AdminLogFilteringIntegrationTests.cs](AdminLogFilteringIntegrationTests.cs)
  - [AdminLogsDeepLinkBuilderTests.cs](AdminLogsDeepLinkBuilderTests.cs)
- Politica de seguranca e configuracoes admin:
  - [AdminSecurityPolicyServiceTests.cs](AdminSecurityPolicyServiceTests.cs)
  - [AdminSettingsServiceTests.cs](AdminSettingsServiceTests.cs)
- Admin payments:
  - [AdminPaymentsDeepLinkIntegrationTests.cs](AdminPaymentsDeepLinkIntegrationTests.cs)
  - [AdminPaymentsModalTests.cs](AdminPaymentsModalTests.cs)
- Admin confirmacao e auditoria:
  - [AdminConfirmationServiceTests.cs](AdminConfirmationServiceTests.cs)
  - [AuditLoggingHooksTests.cs](AuditLoggingHooksTests.cs)

### Auth e Identity

- Integracao de autenticacao e lockout:
  - [AuthenticationIntegrationTests.cs](AuthenticationIntegrationTests.cs)
  - [FullFlowAuthenticationIdentityScenariosIntegrationTests.cs](FullFlowAuthenticationIdentityScenariosIntegrationTests.cs)
- Localizacao/idioma no fluxo de login:
  - [FullFlowIdentityLocalizationIntegrationTests.cs](FullFlowIdentityLocalizationIntegrationTests.cs)
- Features recentes e fluxos completos:
  - [FullFlowRecentFeaturesIntegrationTests.cs](FullFlowRecentFeaturesIntegrationTests.cs)
  - [RecentFeaturesIntegrationTests.cs](RecentFeaturesIntegrationTests.cs)
- Smoke de PageModels Identity:
  - [IdentityPageModelsIntegrationTests.cs](IdentityPageModelsIntegrationTests.cs)
- Email sender/fallback:
  - [IdentityEmailSenderTests.cs](IdentityEmailSenderTests.cs)
- Auth navigation:
  - [AuthNavigationHelperTests.cs](AuthNavigationHelperTests.cs)
- Security headers:
  - [SecurityHeadersIntegrationTests.cs](SecurityHeadersIntegrationTests.cs)

### Pagamentos e webhooks

- Confirmacao e status:
  - [PaymentConfirmationServiceTests.cs](PaymentConfirmationServiceTests.cs)
  - [EventConfirmationPaymentStatusServiceTests.cs](EventConfirmationPaymentStatusServiceTests.cs)
  - [EventConfirmationPaymentWebhookFlowIntegrationTests.cs](EventConfirmationPaymentWebhookFlowIntegrationTests.cs)
- BtcPay:
  - [BtcPayServerPaymentServiceTests.cs](BtcPayServerPaymentServiceTests.cs)
  - [BtcPayWebhookServiceTests.cs](BtcPayWebhookServiceTests.cs)
- EfiBank:
  - [EfiBankPixServiceTests.cs](EfiBankPixServiceTests.cs)
  - [EfiBankWebhookServiceTests.cs](EfiBankWebhookServiceTests.cs)
- AbacatePay:
  - [AbacatePayWebhookServiceTests.cs](AbacatePayWebhookServiceTests.cs)
- Gateway:
  - [GatewayServiceTests.cs](GatewayServiceTests.cs)
- Event bus e hub:
  - [PaymentEventBusTests.cs](PaymentEventBusTests.cs)
  - [PaymentHubTests.cs](PaymentHubTests.cs)
- Webhook endpoints:
  - [WebhookEndpointIntegrationTests.cs](WebhookEndpointIntegrationTests.cs)
- Reconciliacao:
  - [EventPaymentReconciliationServiceTests.cs](EventPaymentReconciliationServiceTests.cs)
- Pix manual/proof:
  - [PixManualPaymentFlowTests.cs](PixManualPaymentFlowTests.cs)
  - [PixProofUploadTests.cs](PixProofUploadTests.cs)
  - [PixProofAdminVisibilityTests.cs](PixProofAdminVisibilityTests.cs)
- Bitcoin testnet:
  - [TestnetBitcoinPaymentServiceTests.cs](TestnetBitcoinPaymentServiceTests.cs)
- Protecao de acesso:
  - [ProtectedPagesIntegrationTests.cs](ProtectedPagesIntegrationTests.cs)
  - [PurchaseFlowIntegrationTests.cs](PurchaseFlowIntegrationTests.cs)

### Eventos e Futsal

- Colisao de horarios:
  - [EventCollisionServiceTests.cs](EventCollisionServiceTests.cs)
- Notificacoes:
  - [EventNotificationServiceTests.cs](EventNotificationServiceTests.cs)
- Futsal (integracao e escalacao):
  - [FutsalIntegrationTests.cs](FutsalIntegrationTests.cs)
  - [FutsalEscalacaoIntegrationTests.cs](FutsalEscalacaoIntegrationTests.cs)
  - [FutsalRefactoringE2ETests.cs](FutsalRefactoringE2ETests.cs)
- Agendamento:
  - [RachaSchedulerServiceTests.cs](RachaSchedulerServiceTests.cs)
  - [MatchScheduleTests.cs](MatchScheduleTests.cs)
- Pos-partida:
  - [PostMatchVoteTests.cs](PostMatchVoteTests.cs)
  - [RankingWinCalculationTests.cs](RankingWinCalculationTests.cs)

### Grupos

- Integracao e convites:
  - [GroupsIntegrationTests.cs](GroupsIntegrationTests.cs)
  - [GroupInviteCodeGenerationTests.cs](GroupInviteCodeGenerationTests.cs)
- Delinquencia:
  - [DelinquencyServiceTests.cs](DelinquencyServiceTests.cs)
  - [DelinquencyNotificationTests.cs](DelinquencyNotificationTests.cs)

### Produto, cotacao e dashboard

- Produto e marketplace:
  - [ProductServiceTests.cs](ProductServiceTests.cs)
- Cotacoes e formatacao:
  - [BitcoinQuoteServiceTests.cs](BitcoinQuoteServiceTests.cs)
  - [CryptoQuoteServiceTests.cs](CryptoQuoteServiceTests.cs)
  - [BtcUsdFormatterTests.cs](BtcUsdFormatterTests.cs)
- Dashboard:
  - [DashboardMetricsServiceTests.cs](DashboardMetricsServiceTests.cs)
- Fee:
  - [OperationFeeCalculatorServiceTests.cs](OperationFeeCalculatorServiceTests.cs)
  - [OperationFeeFlowIntegrationTests.cs](OperationFeeFlowIntegrationTests.cs)

### Preferencias e UX

- Idioma/moeda/ui text:
  - [LanguagePreferenceServiceTests.cs](LanguagePreferenceServiceTests.cs)
  - [CurrencyPreferenceServiceTests.cs](CurrencyPreferenceServiceTests.cs)
  - [UiTextServiceTests.cs](UiTextServiceTests.cs)
  - [UiTextServiceRefactoringTests.cs](UiTextServiceRefactoringTests.cs)
- Local storage/debounce/inicializacao:
  - [LocalStorageStateHelpersTests.cs](LocalStorageStateHelpersTests.cs)
  - [DebounceDispatcherTests.cs](DebounceDispatcherTests.cs)
  - [AppInitializationServiceTests.cs](AppInitializationServiceTests.cs)
- User:
  - [UserServiceTests.cs](UserServiceTests.cs)
- Mailbox:
  - [MailboxConversationArchiveServiceTests.cs](MailboxConversationArchiveServiceTests.cs)

### Infra e configuracao

- Defaults de configuracao:
  - [ConfigurationDefaultsTests.cs](ConfigurationDefaultsTests.cs)
- Logging e retencao:
  - [LogServiceTests.cs](LogServiceTests.cs)
  - [LogRetentionServiceTests.cs](LogRetentionServiceTests.cs)
- Validacao de modelos:
  - [ModelValidationTests.cs](ModelValidationTests.cs)
- PII:
  - [PiiSanitizerTests.cs](PiiSanitizerTests.cs)
- DbContext design-time guard:
  - [AppDbContextFactoryTests.cs](AppDbContextFactoryTests.cs)

## Observacoes de cobertura

- 83 arquivos de teste cobrindo 575 cenarios.
- CI gera cobertura percentual via Coverlet (Cobertura XML → ReportGenerator → badge).
- Para cobertura local: `dotnet test --collect:"XPlat Code Coverage"`.
