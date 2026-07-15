## Objetivo
Implementar sistema de pagamento com taxas fixas e repasse automático via Pix para organizadores.

## Resumo das alterações
- **32 arquivos alterados**: +5491 linhas, -10 linhas
- **10 commits** organizados por fases de implementação

## Principais funcionalidades

### 1. Taxas fixas de serviço
- R$ 0,50 (app) + R$ 0,25 (gateway)
- Configuração em `FeeOptions` com suporte a valores fixos
- UI mostra breakdown do valor total

### 2. Sistema de repasse automático (PayoutService)
- `PayoutService` processa repasses após pagamento confirmado
- `EfiBankPixPayoutService` envia Pix via API EfiBank
- `WebhookPaymentMarker` integrado com PayoutService
- Status de payout rastreado em `PaymentRecord`

### 3. Conta de repasse para grupos
- `GroupPayoutAccount` com CPF e chave Pix
- Editor de conta de repasse na UI
- Guarda-corpos: bloqueia pagamento sem chave Pix configurada

### 4. Relatório de receita para admin
- `AdminRevenueReportService` gera relatório
- Página `AdminRevenue.razor` com visualização

### 5. Configuração de produção
- Endpoint webhook ajustado para `/api/webhooks/efibank/pix`
- Taxas ativadas em produção
- `appsettings.Production.json` configurado

## Migrations
- `AddGroupPayoutAccount` - Tabela de contas de repasse
- `AddPayoutFieldsToPaymentRecord` - Campos de rastreamento de payout

## Testes
- `FeeCalculatorTests` - Testes de cálculo de taxas
- `PayoutServiceTests` - Testes do serviço de repasse

## Configuração necessária em produção
- Variáveis de ambiente no EasyPanel configuradas
- Certificado de produção EfiBank carregado
- Webhook URL: `https://confirmai.m2gpju.easypanel.host/api/webhooks/efibank/pix`
