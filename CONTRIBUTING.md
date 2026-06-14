# Guia de Contribuição — Confirmai

## Estrutura do Projeto

```
Confirmai/
├── Areas/Identity/          # Razor Pages do ASP.NET Identity (login, registro, senha)
├── Configuration/           # ApiKeyAuth, BtcPayOptions, EmailOptions, SecurityPolicyDefaults
├── Data/                    # AppDbContext, AppDbContextFactory
├── Enums/                   # PaymentStatus, ServerMemberRole, etc.
├── Hubs/                    # PaymentHub (SignalR — [Authorize])
├── Migrations/              # EF Core migrations históricas
├── Models/                  # Entidades do domínio
├── Pages/                   # Blazor Server pages organizadas por feature
│   ├── Admin/               # Dashboard + componentes admin (AdminPayments, AdminLogs, etc.)
│   ├── Futsal/              # Partidas + componentes (Detail, Escalacao, Schedule, etc.)
│   ├── Groups/              # Grupos + componentes (Detail, Ranking, Config, etc.)
│   ├── Payment/             # Pagamentos + componentes (EventPayment, Checkout, etc.)
│   ├── Poker/               # Torneios de poker
│   ├── VenueManager/        # Gestão de quadras
│   └── Components/          # Componentes de página compartilhados (Profile, Mailbox, etc.)
├── Shared/Components/       # Componentes globais reutilizáveis (Layout, Toast, Breadcrumb, etc.)
├── Services/                # Lógica de negócio organizada por domínio
│   ├── Admin/               # Logs, filtros, auditoria, segurança, delinquência
│   ├── Core/                # LogService, UiTextService, Auth, certificados, inicialização
│   ├── Crypto/              # Cotações BTC/USD/BRL
│   ├── EventPayments/       # Gateways de pagamento por evento (AbacatePay, Appmax, EfiBank)
│   ├── Events/              # Métricas, colisão de horários, notificações, scheduler
│   ├── Factories/           # BitcoinPaymentFactory, EventPaymentGatewayFactory
│   ├── Interfaces/          # IBitcoinPaymentService, IEventPaymentGateway
│   ├── Payment/             # Gateways, webhooks, reconciliação, alertas
│   ├── User/                # Preferências, claims, perfil
│   └── Utility/             # Email, PII, produtos, testnet
├── wwwroot/                 # Estáticos (CSS, JS, imagens, uploads)
├── Confirmai.Tests/         # xUnit + Moq
└── e2e/                     # Playwright (TypeScript)
```

---

## Convenções

### Nomenclatura

| Artefato | Padrão | Exemplo |
|----------|--------|---------|
| Páginas | PascalCase | `AdminPayments.razor` |
| Componentes | PascalCase | `MainLayout.razor` |
| Serviços | `{Domain}Service` | `PaymentConfirmationService` |
| Interfaces | `I{ServiceName}` | `IBitcoinPaymentService` |
| Models | PascalCase | `PaymentRecord`, `ApplicationUser` |
| Enums | PascalCase | `Sport`, `PaymentStatus` |
| CSS classes | kebab-case + BEM | `.entity-shell`, `.entity-shell__card` |

### Services

- Construtores recebem dependências opcionais (`LogService? log = null`) para facilitar testes sem mocks
- Audit via `LogService.AuditAsync(eventType, entityType, entityId, message, actorUserId, source)`
- Constantes de audit em `AuditEvents` e `AuditEntities` — **nunca renomear** (são chaves de analytics)
- Novos serviços devem ir na subpasta de domínio correspondente em `Services/`

### Pages (Blazor Server)

- Cada página/componente tem seu `.razor.css` isolado — sem `!important`, sem inline styles estáticos
- Inline styles **só** para valores dinâmicos em runtime (ex.: `style="@BuildAccentStyle(color)"`)
- Injeção via `@inject`; acesso ao usuário via `AuthProvider` + `UserManager.GetUserId(user)`
- Páginas com timers, polling ou `Task.Delay` devem implementar `IAsyncDisposable` com `CancellationTokenSource`

### CSS

- **`site.css`**: design tokens (`--ci-*`), layout, shells, tabelas, sistema de botões
- **`events.css`**: estilos compartilhados Futsal + Poker
- **`identity.css`**: páginas Identity
- **`marketplace.css`**: vitrine de servidores
- **`.razor.css` por componente**: escopo isolado via Blazor CSS isolation
- Botões: `.btn` + `.btn--primary/--success/--danger/--neutral/--info/--gold`
- Design tokens: ~45 CSS custom properties em `:root` (usar `--ci-*` para código novo)

### Testes

#### Unitários (`Confirmai.Tests/`)

```bash
dotnet test Confirmai.Tests/Confirmai.Tests.csproj
```

- **xUnit + Moq**, in-memory EF Core via `TestDataFactory.CreateDbContext()`
- Padrão de audit test: DB real + `LogService` real → chama serviço → `Assert` em `db.Logs`
- `SignalRTestFactory.CreateHubContext()` para testes que precisam de `IHubContext<PaymentHub>`
- `IntegrationTestWebAppFactory` para testes HTTP (`WebApplicationFactory`)
- Feature flags: `factory.EnsureLuaDeliveryEnabledAsync()` antes de testes que dependem de `LuaDeliveryEnabled`

#### E2E (`e2e/`)

```bash
cd e2e && npm install && npm run install:browsers && npm test
```

- Playwright (TypeScript), config aponta para `http://localhost:5000`
- Admin E2E requer `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD`; testes são pulados quando ausentes
- Seletor de checkbox: usar `role/id`, não `name` (conflito com hidden fallback do ASP.NET)

#### CI

- `.github/workflows/ci.yml`: build + `dotnet test` com Coverlet (cobertura Cobertura XML → ReportGenerator → badge)

---

## Segurança

- **CSP**: nonce por request (`HttpContext.Items["csp-nonce"]`), sem `'unsafe-inline'` em `script-src`
- **API Key**: `ApiKeyAuth` valida header `X-Api-Key` com HMAC; chaves com hash em `ServerApiKeys`
- **Secrets**: nunca em `appsettings.json`; usar User Secrets (dev) ou variáveis de ambiente (prod)
- **OWASP**: parametrização via EF Core, `[ValidateAntiForgeryToken]` em forms, headers de segurança
- **Fontes externas** (Google Fonts, Font Awesome): `media="print"` + promoção via JS pós-load (respeita CSP)

---

## Monitoramento

### Background Services (`IHostedService`)

1. **`PendingWebhooksAlertService`** — Detecta pagamentos Pix pendentes > 24h. Executa a cada 1h. Log `WARNING` se > 5 pendências.
2. **`CertificateHealthCheckService`** — Monitora expiração do certificado mTLS EfiBank. Executa a cada 12h. Alertas progressivos: Expirado (≤0d), Crítico (≤7d), Urgente (7-14d), Aviso (14-30d).

### Stack de Monitoramento

Templates versionados em `ops/monitoring/`:
- `otel-collector.yml` — OTLP → Prometheus
- `prometheus.yml` — Regras de alerta de pagamentos
- `alertmanager.yml` — Roteamento `p1`/`p2`
- Grafana com datasource e dashboard pré-provisionados

```bash
docker compose --profile monitoring up -d prometheus alertmanager grafana
```

---

## Fazer um Fork

1. **Banco**: renomear `AppDbContext`, ajustar migrations
2. **Tema**: tokens em `:root` de `site.css`, logo em `wwwroot/images/`
3. **Gateway**: implementar `IBitcoinPaymentService` e registrar em `BitcoinPaymentFactory`
4. **i18n**: strings em `Services/Core/UiTextService.cs` — `Dictionary<string, string>` por locale
5. **Auditoria**: usar `AuditEvents.*` para novos eventos; nunca reutilizar constantes existentes

---

## Workflow de Contribuição

1. Criar branch a partir de `main`
2. Implementar com escopo pequeno e verificável
3. Rodar testes: `dotnet test`
4. Verificar build: `dotnet build`
5. Commit com mensagem descritiva: `feat:`, `fix:`, `refactor:`, `docs:`
6. Abrir PR — CI roda automaticamente

---

## Referências Operacionais

- [docs/deploy.md](docs/deploy.md) — Deploy com Docker + nginx + TLS
- [docs/production-checklist.md](docs/production-checklist.md) — Checklist de produção
- [docs/observability-payments-runbook.md](docs/observability-payments-runbook.md) — Runbook de incidentes
- [docs/troubleshooting-pix.md](docs/troubleshooting-pix.md) — Troubleshooting Pix/webhooks
- [docs/monitoring/](docs/monitoring/) — Templates Prometheus, Alertmanager, Grafana
