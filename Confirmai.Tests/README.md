# Confirmai.Tests - Mapeamento de Cobertura

Este diretório concentra testes unitários e de integração da aplicação.

## Status atual

- Ultima validacao local: 2026-03-22
- Resultado: 287 testes passando, 0 falhas
- Comando: dotnet test .\Confirmai.Tests\Confirmai.Tests.csproj -v minimal

## Mapa por dominio

### Admin e seguranca

- Convencoes/autorizacao: [AdminAuthorizationConventionsTests.cs](AdminAuthorizationConventionsTests.cs)
- Filtros e estado (logs, users, products, payments, orders):
  - [AdminLogsFilterStateServiceTests.cs](AdminLogsFilterStateServiceTests.cs)
  - [AdminUsersFilterStateServiceTests.cs](AdminUsersFilterStateServiceTests.cs)
  - [AdminProductsFilterStateServiceTests.cs](AdminProductsFilterStateServiceTests.cs)
  - [AdminPaymentsFilterStateServiceTests.cs](AdminPaymentsFilterStateServiceTests.cs)
  - [AdminOrdersFilterStateServiceTests.cs](AdminOrdersFilterStateServiceTests.cs)
- Query/export/log sorting:
  - [AdminLogsQueryServiceIntegrationTests.cs](AdminLogsQueryServiceIntegrationTests.cs)
  - [AdminLogsExportServiceTests.cs](AdminLogsExportServiceTests.cs)
  - [AdminLogSortingIntegrationTests.cs](AdminLogSortingIntegrationTests.cs)
  - [AdminLogFilteringIntegrationTests.cs](AdminLogFilteringIntegrationTests.cs)
- Politica de seguranca e configuracoes admin:
  - [AdminSecurityPolicyServiceTests.cs](AdminSecurityPolicyServiceTests.cs)
  - [AdminSettingsServiceTests.cs](AdminSettingsServiceTests.cs)
- Fluxo de revisao/liberacao:
  - [AdminOrdersReviewIntegrationTests.cs](AdminOrdersReviewIntegrationTests.cs)
  - [AdminOrderReleaseServiceIntegrationTests.cs](AdminOrderReleaseServiceIntegrationTests.cs)

### Auth e Identity

- Integracao de autenticacao e lockout:
  - [AuthenticationIntegrationTests.cs](AuthenticationIntegrationTests.cs)
  - [FullFlowAuthenticationIdentityScenariosIntegrationTests.cs](FullFlowAuthenticationIdentityScenariosIntegrationTests.cs)
- Localizacao/idioma no fluxo de login:
  - [FullFlowIdentityLocalizationIntegrationTests.cs](FullFlowIdentityLocalizationIntegrationTests.cs)
- Smoke de PageModels Identity:
  - [IdentityPageModelsIntegrationTests.cs](IdentityPageModelsIntegrationTests.cs)
- Email sender/fallback:
  - [IdentityEmailSenderTests.cs](IdentityEmailSenderTests.cs)

### Compras, pedidos e pagamentos

- Fluxo de compra e protecao de acesso:
  - [PurchaseFlowIntegrationTests.cs](PurchaseFlowIntegrationTests.cs)
  - [ProtectedPagesIntegrationTests.cs](ProtectedPagesIntegrationTests.cs)
  - [OrderAccessServiceTests.cs](OrderAccessServiceTests.cs)
- Pagamentos e confirmacao:
  - [PaymentConfirmationServiceTests.cs](PaymentConfirmationServiceTests.cs)
  - [BtcPayServerPaymentServiceTests.cs](BtcPayServerPaymentServiceTests.cs)
  - [BtcPayWebhookServiceTests.cs](BtcPayWebhookServiceTests.cs)
  - [WebhookEndpointIntegrationTests.cs](WebhookEndpointIntegrationTests.cs)
- Regras de ordenacao/status:
  - [OrderReviewSortingTests.cs](OrderReviewSortingTests.cs)
  - [OrderStatusHelperTests.cs](OrderStatusHelperTests.cs)

### Produto, cotacao e dashboard

- Produto e marketplace:
  - [ProductServiceTests.cs](ProductServiceTests.cs)
- Cotacoes e formatacao:
  - [BitcoinQuoteServiceTests.cs](BitcoinQuoteServiceTests.cs)
  - [CryptoQuoteServiceTests.cs](CryptoQuoteServiceTests.cs)
  - [BtcUsdFormatterTests.cs](BtcUsdFormatterTests.cs)
- Dashboard:
  - [DashboardMetricsServiceTests.cs](DashboardMetricsServiceTests.cs)

### Preferencias e UX

- Idioma/moeda/ui text:
  - [LanguagePreferenceServiceTests.cs](LanguagePreferenceServiceTests.cs)
  - [CurrencyPreferenceServiceTests.cs](CurrencyPreferenceServiceTests.cs)
  - [UiTextServiceTests.cs](UiTextServiceTests.cs)
- Local storage/debounce/inicializacao:
  - [LocalStorageStateHelpersTests.cs](LocalStorageStateHelpersTests.cs)
  - [DebounceDispatcherTests.cs](DebounceDispatcherTests.cs)
  - [AppInitializationServiceTests.cs](AppInitializationServiceTests.cs)

### Infra e configuracao

- Defaults de configuracao:
  - [ConfigurationDefaultsTests.cs](ConfigurationDefaultsTests.cs)
- Logging e fee calculator:
  - [LogServiceTests.cs](LogServiceTests.cs)
  - [OperationFeeCalculatorServiceTests.cs](OperationFeeCalculatorServiceTests.cs)
- DbContext design-time guard:
  - [AppDbContextFactoryTests.cs](AppDbContextFactoryTests.cs)

## Observacoes de cobertura

- O mapeamento acima cobre os cenarios levantados no levantamento desta sprint.
- Nao representa cobertura percentual por linha (coverage tool), e sim cobertura funcional por comportamento/cenario.
- Para cobertura percentual por linha, adicionar coleta com coverlet collector em pipeline.


