# Guia de Desenvolvimento e Fork

> Para configuração do ambiente, deploy e checklist de produção, veja o `README.md` e `docs/production-checklist.md`.

---

## Estrutura do Projeto

```
Confirmai/
├── Areas/Identity/      # Razor Pages do ASP.NET Identity (login, registro, senha)
├── Configuration/       # ApiKeyAuth, BtcPayOptions, EmailOptions, SecurityPolicyDefaults
├── Data/                # AppDbContext, AppDbContextFactory
├── Endpoints/           # ServerIntegrationEndpoints (API REST para servidores OT)
├── Enums/               # PaymentStatus, ServerMemberRole, ServerRegistrationRequestStatus
├── Hubs/                # PaymentHub (SignalR — [Authorize], notificações de pagamento)
├── Migrations/          # EF Core migrations históricas
├── Models/              # Entidades do domínio (OrderModel, Product, PaymentRecord, ...)
├── Pages/               # Blazor Server pages (Admin/, Order/, Payment/, Server/, ...)
├── Services/            # Toda a lógica de negócio
├── Shared/              # Componentes compartilhados e helpers (MainLayout, AuthHelpers, ...)
├── wwwroot/             # Arquivos estáticos (CSS, JS, imagens, downloads Lua)
├── e2e/                 # Testes Playwright (TypeScript)
└── Confirmai.Tests/  # Testes xUnit (.NET)
```

---

## Camadas e Convenções

### Services

Toda lógica de negócio vive em `Services/`. Padrão:

- Construtores recebem dependências opcionais (`LogService? log = null`) para facilitar testes unitários sem mocks
- Audit via `LogService.AuditAsync(eventType, entityType, entityId, message, actorUserId, source)` — constantes estáveis em `AuditEvents` e `AuditEntities`
- Nunca renomear constantes de `AuditEvents.*` — são chaves de analytics

### Pages (Blazor Server)

- Cada página tem seu `.razor.css` isolado — sem `!important`, sem inline styles estáticos
- Inline styles só são aceitáveis para valores **dinâmicos em runtime** (ex.: `style="@BuildItemAccentStyle(color)"`)
- Injeção de dependências via `@inject`; acesso ao usuário via `AuthProvider` + `UserManager.GetUserId(user)`

### CSS

- **`wwwroot/css/site.css`**: layout, componentes globais, tema Tibia. Sem `@layer` (removido — era unclosed)
- **`wwwroot/css/marketplace.css`**: vitrine de servidores e cards
- **`wwwroot/css/identity.css`**: páginas Identity
- **`.razor.css` por componente**: escopo isolado via Blazor CSS isolation
- Sistema de botões: `.btn` + `.btn--primary/--success/--danger/--neutral/--info/--gold`. Aliases legados (`.save-btn`, `.cancel-btn`, etc.) mantidos como sinônimos — novo código usa `.btn .btn--variant`
- Design tokens: ~45 CSS custom properties em `:root` em `site.css` (cores, gradientes, bordas)

### Testes

#### Unitários (`Confirmai.Tests/`)

- **xUnit + Moq**, in-memory EF Core via `TestDataFactory.CreateDbContext()`
- Padrão de audit test: cria DB real + `LogService` real → chama serviço → `Assert` em `db.Logs`
- `SignalRTestFactory.CreateHubContext()` para testes que precisam de `IHubContext<PaymentHub>`
- `IntegrationTestWebAppFactory` para testes de integração HTTP (`WebApplicationFactory`)
- Seed de feature flags: `factory.EnsureLuaDeliveryEnabledAsync()` antes de testes que dependem de `LuaDeliveryEnabled`

#### E2E (`e2e/`)

- Playwright (TypeScript), `playwright.config.ts` aponta para `http://localhost:5000`
- Executar: `node .\node_modules\@playwright\test\cli.js test` (não `npx` — bloqueia no Windows com política de execução)
- Admin E2E precisa de `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` no ambiente; testes são pulados quando ausentes
- Seletor de checkbox: usar `role/id`, não `name` (conflito com hidden fallback do ASP.NET)

#### CI

- `.github/workflows/ci.yml`: build + `dotnet test` com Coverlet (cobertura Cobertura XML → ReportGenerator → badge)

### Segurança

- **CSP**: nonce por request (`HttpContext.Items["csp-nonce"]`), sem `'unsafe-inline'` em `script-src`
- **API Key**: `ApiKeyAuth` valida header `X-Api-Key` com HMAC; chaves armazenadas com hash em `ServerApiKeys`
- **Secrets**: nunca em `appsettings.json`; usar User Secrets (dev) ou variáveis de ambiente (prod)
- **OWASP**: parametrização via EF Core, `[ValidateAntiForgeryToken]` em forms, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`
- **Dependências externas** (Google Fonts, Font Awesome): carregadas com `media="print"` + promoção via JS pós-load para não bloquear render e respeitar a CSP com nonce

---

## Fazer um Fork

1. **Banco**: renomear `AppDbContext`, ajustar migrations se mudar modelos
2. **Tema**: tokens em `:root` de `site.css`, trocar logo em `wwwroot/images/`
3. **Integração OT**: `Endpoints/ServerIntegrationEndpoints.cs` + script Lua em `wwwroot/downloads/` — adaptar para outros jogos se necessário
4. **Gateway de pagamento**: implementar `IBitcoinPaymentService` e registrar em `BitcoinPaymentFactory`
5. **i18n**: strings em `Services/UiTextService.cs` — adicionar novos idiomas seguindo o padrão `Dictionary<string, string>` por locale
6. **Auditoria**: sempre usar `AuditEvents.*` para novos eventos; nunca reutilizar constantes existentes

---

## Estado Atual (Mai/2026)

- 601 testes unitários/integração passando, 0 falhas
- Coverlet configurado, CI rodando em GitHub Actions
- Todos os audit hooks ativos: Identity, Products, Servers, ServerMembers, ApiKeys, ItemOffers, Payments, Orders (criados/liberados/entregues/disputados), AdminSettings
- Inline styles estáticos eliminados de todas as páginas
- Sem `@layer` em `site.css` (era bloco unclosed — removido)
