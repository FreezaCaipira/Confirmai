# Guia de Contribuição — Confirmai

## Estrutura do Projeto

```
Confirmai/
├── Areas/Identity/          # Razor Pages do ASP.NET Identity (login, registro, senha)
├── Configuration/           # ApiKeyAuth, BtcPayOptions, EfiBankOptions, EmailOptions, etc.
├── Data/                    # AppDbContext, AppDbContextFactory
├── Enums/                   # PaymentStatus, EventConfirmationPaymentStatus, GroupMemberRole, etc.
├── Hubs/                    # PaymentHub (SignalR — [Authorize])
├── Migrations/              # EF Core migrations históricas
├── Models/                  # Entidades do domínio
├── Pages/                   # Blazor Server pages organizadas por feature
│   ├── Admin/               # Dashboard + componentes admin (AdminPayments, AdminLogs, etc.)
│   ├── Futsal/              # Partidas + componentes (Detail, Escalação, Schedule, etc.)
│   ├── Groups/              # Grupos + componentes (Detail, Ranking, Config, etc.)
│   ├── Payment/             # Pagamentos + componentes (EventPayment, Checkout, etc.)
│   ├── Poker/               # Torneios de poker
│   ├── VenueManager/        # Gestão de quadras
│   └── Components/          # Componentes de página compartilhados (Profile, Mailbox, etc.)
├── Shared/Components/       # Componentes globais reutilizáveis (Layout, Toast, Breadcrumb, etc.)
├── Services/                # Lógica de negócio organizada por domínio
│   ├── Admin/               # Logs, filtros, auditoria, segurança, delinquência
│   ├── Core/                # LogService, UiTextService, AuditEvents, Auth, certificados
│   ├── Crypto/              # Cotações BTC/USD/BRL
│   ├── EventPayments/       # Gateways de pagamento por evento (AbacatePay, Appmax, EfiBank)
│   ├── Events/              # Métricas, colisão de horários, notificações, scheduler
│   ├── Factories/           # BitcoinPaymentFactory, EventPaymentGatewayFactory
│   ├── Interfaces/          # IBitcoinPaymentService, IEventPaymentGateway
│   ├── Payment/             # Gateways, webhooks, reconciliação, alertas
│   ├── User/                # Preferências, claims, perfil
│   └── Utility/             # Email, PII, produtos, testnet
├── wwwroot/                 # Estáticos (CSS, JS, imagens, uploads)
├── Confirmai.Tests/         # xUnit + Moq (641 testes)
└── e2e/                     # Playwright E2E (TypeScript, 20 specs)
```

---

## Convenções

### Nomenclatura

| Artefato | Padrão | Exemplo |
|----------|--------|---------|
| Páginas | PascalCase | `AdminPayments.razor` |
| Componentes | PascalCase, subpasta `Components/` | `AdminPaymentsTable.razor` |
| Serviços | `{Domain}Service` | `PaymentConfirmationService` |
| Interfaces | `I{ServiceName}` | `IBitcoinPaymentService` |
| Models | PascalCase | `PaymentRecord`, `ApplicationUser` |
| Enums | PascalCase | `PaymentStatus`, `EventConfirmationPaymentStatus` |
| CSS classes | kebab-case + BEM | `.entity-shell`, `.entity-shell__card` |
| CSS vars | prefixo semântico | `--bg-deep`, `--parchment`, `--accent-gold` |

---

## Services

### DI e Lifetimes

```csharp
// Singleton — estado global compartilhado entre circuitos
builder.Services.AddSingleton<BitcoinQuoteService>();
builder.Services.AddSingleton<PaymentEventBus>();

// Scoped — um por circuito Blazor (padrão para a maioria)
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<PaymentConfirmationService>();

// HostedService — background workers
builder.Services.AddHostedService<PendingWebhooksAlertService>();
```

**Regra**: novos serviços são `Scoped` por padrão. Só usar `Singleton` se o serviço precisa compartilhar estado entre circuitos (cotações, event bus). Nunca injetar `Scoped` em `Singleton`.

### Padrões de Serviço

- **Construtor com LogService opcional**: facilita testes sem mocks
  ```csharp
  public AdminSettingsService(IDbContextFactory<AppDbContext> dbFactory, LogService? log = null)
  ```
- **Audit**: `LogService.AuditAsync(eventType, entityType, entityId, message, actorUserId, source)`
- **Constantes de audit**: `AuditEvents.*` e `AuditEntities.*` (72 event types) — **nunca renomear** (são chaves de analytics; só adicionar novos valores)
- **Novos serviços**: criar na subpasta de domínio correspondente em `Services/`

### Factory Pattern

- `EventPaymentGatewayFactory` — resolve `IEventPaymentGateway` por nome do gateway
- `BitcoinPaymentFactory` — resolve `IBitcoinPaymentService` por tipo (BTCPay, AbacatePay, Testnet)

---

## Pages (Blazor Server)

### DbContext — sempre via Factory

Blazor Server mantém circuitos longos. **Nunca injetar `AppDbContext` diretamente.** Usar `IDbContextFactory`:

```razor
@inject IDbContextFactory<AppDbContext> DbFactory

@code {
    private async Task LoadData()
    {
        using var db = await DbFactory.CreateDbContextAsync();
        // usar db aqui — descartado ao final do bloco
    }
}
```

### Autorização

```razor
@attribute [Authorize(Roles = "admin")]     // páginas admin
@attribute [AllowAnonymous]                 // páginas públicas
```

Acesso ao usuário: `@inject AuthenticationStateProvider AuthStateProvider` → `AuthStateProvider.GetAuthenticationStateAsync()`.

### PageTitle

Toda página deve ter:
```razor
<PageTitle>Nome da Página · Confirmai</PageTitle>
```

### i18n — UiTextService

```razor
@inject UiTextService T

<h1>@T["AdminPayments.Title"]</h1>
```

Strings em `Services/Core/UiText/` (6 arquivos de domínio), organizadas por locale (`pt-BR`, `en-US`, `es-ES` completo - 691 chaves traduzidas).

### IAsyncDisposable

Páginas com timers, polling, `Task.Delay` ou `CancellationTokenSource` devem implementar:

```razor
@implements IAsyncDisposable

@code {
    private CancellationTokenSource? _cts = new();

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

Atualmente implementado em: AdminPayments, AdminVenueEdit, Escalacao, Groups/Detail, EventPayment, Payment, Poker/Index, VenueManager/VenueEdit.

### Decomposição de Componentes

Páginas grandes extraem sub-componentes em `Components/`:

```
Pages/Admin/
├── AdminPayments.razor              # página principal (1161 linhas)
└── Components/
    ├── AdminPaymentsTable.razor      # tabela de pagamentos
    ├── AdminPaymentsFilters.razor    # barra de filtros
    ├── AdminPaymentsSummaryPanel.razor # painel de resumo
    └── AdminPaymentsAdvancedToolsModal.razor
```

Sub-componentes recebem dados via `[Parameter]` e comunicam eventos via `EventCallback`.

---

## CSS

### Arquivos globais

| Arquivo | Escopo |
|---------|--------|
| `site.css` | Design tokens, layout shell, tabelas, botões, entity shell |
| `events.css` | Estilos compartilhados Futsal + Poker |
| `identity.css` | Páginas Identity (login, registro) |
| `marketplace.css` | Vitrine de servidores |

### CSS Isolation (scoped)

- Cada componente/página tem seu `.razor.css` isolado (56 arquivos)
- **Proibido**: `!important`, inline styles estáticos
- **Permitido**: inline styles para valores dinâmicos em runtime (`style="@BuildAccentStyle(color)"`)

### Design Tokens

68 CSS custom properties em `:root` de `site.css`. Principais famílias:

```css
/* Backgrounds (tema escuro/warm) */
--bg-deepest, --bg-deep, --bg-dark, --bg-dark-mid

/* Superfícies parchment */
--parchment-dark, --parchment, --parchment-mid, --parchment-light

/* Acentos */
--accent-gold, --accent-copper, --accent-amber

/* Texto */
--text-primary, --text-muted, --text-on-dark
```

Para código novo, usar as variáveis existentes em vez de cores hardcoded.

### Entity Shell System

Layout padrão para páginas de dados. Usado em todas as páginas admin, grupos, integração:

```html
<div class="entity-shell admin-page">
    <section class="entity-shell-grid">
        <article class="entity-shell-card entity-shell-main">
            <!-- conteúdo principal -->
        </article>
        <aside class="entity-shell-card entity-shell-side">
            <!-- painel lateral -->
        </aside>
    </section>
</div>
```

Variantes de tema: `parchment-a` (6 páginas), `parchment-b` (3 páginas).

### Botões

`.btn` + modificador: `.btn--primary`, `.btn--success`, `.btn--danger`, `.btn--neutral`, `.btn--info`, `.btn--gold`, `.btn--icon`, `.btn--sm`, `.btn--variant`.

---

## Testes

### Unitários (`Confirmai.Tests/`)

```bash
dotnet test Confirmai.Tests/Confirmai.Tests.csproj
```

- **xUnit + Moq**, in-memory EF Core via `TestDataFactory.CreateDbContext()`
- Padrão de audit test: DB real + `LogService` real → chama serviço → `Assert` em `db.Logs`
- `SignalRTestFactory.CreateHubContext()` para testes que precisam de `IHubContext<PaymentHub>`
- `IntegrationTestWebAppFactory` para testes HTTP (`WebApplicationFactory`)
- Feature flags: `factory.EnsureLuaDeliveryEnabledAsync()` antes de testes que dependem de `LuaDeliveryEnabled`

### E2E (`e2e/`)

```bash
cd e2e && npm install && npm run install:browsers && npm test
```

- Playwright (TypeScript), config aponta para `http://localhost:5000`
- Admin E2E requer `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD`; testes são pulados quando ausentes
- Seletor de checkbox: usar `role/id`, não `name` (conflito com hidden fallback do ASP.NET)

### CI

- `.github/workflows/ci.yml`: build + `dotnet test` com Coverlet (cobertura Cobertura XML → ReportGenerator → badge)

---

## Segurança

- **CSP**: nonce por request (`HttpContext.Items["csp-nonce"]`), sem `'unsafe-inline'` em `script-src`
- **API Key**: `ApiKeyAuth` valida header `X-Api-Key` com HMAC; chaves com hash em `ServerApiKeys`
- **Secrets**: nunca em `appsettings.json`; usar User Secrets (dev) ou variáveis de ambiente (prod)
- **OWASP**: parametrização via EF Core, `[ValidateAntiForgeryToken]` em forms, headers de segurança
- **Headers** (em `Program.cs`): `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- **Fontes externas** (Google Fonts, Font Awesome): `media="print"` + promoção via JS pós-load (respeita CSP)
- **Identity**: lockout após 5 tentativas, cookie com sliding expiration, políticas de senha diferenciadas por ambiente

---

## Monitoramento

### Background Services (`IHostedService`)

| Serviço | Função | Intervalo |
|---------|--------|-----------|
| `LogRetentionService` | Purga logs antigos (IPs anonimizados 30d, não-financeiros 90d) | Diário |
| `RachaSchedulerService` | Cria eventos recorrentes a partir de `MatchSchedule` | Periódico |
| `EventNotificationSchedulerService` | Envia notificações de partidas próximas | Periódico |
| `EventPaymentReconciliationWorker` | Varredura automática de pagamentos pendentes | Periódico |
| `PendingWebhooksAlertService` | Detecta pagamentos Pix pendentes > 24h | 1h |
| `CertificateHealthCheckService` | Monitora expiração do cert mTLS EfiBank | 12h |

### Stack de Monitoramento

Templates versionados em `ops/monitoring/`:
- `otel-collector.yml` — OTLP → Prometheus
- `prometheus.yml` + `prometheus-payments-alerts.yml` — Regras de alerta
- `alertmanager.yml` — Roteamento `p1`/`p2`
- Grafana com datasource e dashboard pré-provisionados

```bash
docker compose --profile monitoring up -d prometheus alertmanager grafana
```

---

## Fazer um Fork

1. **Banco**: renomear `AppDbContext`, ajustar migrations
2. **Tema**: tokens em `:root` de `site.css` (68 vars), logo em `wwwroot/images/`
3. **Gateway**: implementar `IEventPaymentGateway` e registrar em `EventPaymentGatewayFactory`; ou `IBitcoinPaymentService` e registrar em `BitcoinPaymentFactory`
4. **i18n**: strings em `Services/Core/UiTextService.cs` — `Dictionary<string, string>` por locale
5. **Auditoria**: usar `AuditEvents.*` para novos eventos (72 existentes); nunca reutilizar constantes

---

## Workflow de Contribuição

1. Criar branch a partir de `main`
2. Implementar com escopo pequeno e verificável
3. Rodar testes: `dotnet test`
4. Verificar build: `dotnet build`
5. Commit com mensagem descritiva: `feat:`, `fix:`, `refactor:`, `docs:`
6. Abrir PR — CI roda automaticamente

---

## Workflow Senior x Pleno

Modelo de trabalho hibrido para combinar capacidade arquitetural (Senior/cloud) com execucao massiva (Pleno/local).

### Ciclo de Trabalho

```
Senior monta WORK_PLAN.md → Pleno executa fases → Senior revisa → Senior atualiza WORK_PLAN.md → ...
```

### Responsabilidades

| Senior (Cloud) | Pleno (Local) |
|----------------|---------------|
| Arquitetura e decisoes de design | Implementacao mecanica |
| Revisao de codigo pos-execucao | Execucao das fases do WORK_PLAN |
| Montar/atualizar WORK_PLAN.md | Documentar problemas encontrados |
| Definir interfaces de componentes | Aplicar patterns definidos |
| Resolver conflitos de especificidade | Seguir regras a risca |
| Auditoria de qualidade | Validacao por fase (build + test) |

### Regras de Handoff

1. **WORK_PLAN.md e o unico documento de trabalho** — nao criar novos .md
2. Cada fase tem: branch, descricao, arquivos-alvo, comandos de validacao
3. Pleno documenta bloqueios na secao "Problemas Encontrados" do WORK_PLAN
4. 1 PR por fase, mergear antes de comecar a proxima
5. Pleno NAO toma decisoes arquiteturais — quando em duvida, documentar e pular

### Anti-Patterns (Pleno)

- Usar `!important` em vez de resolver especificidade
- Hardcodar cores em vez de usar CSS vars
- Criar migrations sem verificar se relacao ja existe
- Commitar debug code (`Console.Write`, `@debug`)
- Modificar estrutura de arquivos sem autorizacao do Senior

### Prompt Base para o Pleno

```
Voce e um dev pleno executando o WORK_PLAN.md deste repositorio.
Siga as fases na ordem. Para cada fase:
1. Crie a branch indicada
2. Implemente as tarefas listadas
3. Valide com os comandos indicados
4. Se encontrar bloqueio, documente na secao "Problemas Encontrados"
5. Abra PR quando a fase estiver completa

REGRAS: [copiar secao "Regras para o Pleno" do WORK_PLAN.md]
```

---

## Referências Operacionais

- [docs/deploy.md](docs/deploy.md) — Deploy com Docker + nginx + TLS
- [docs/production-checklist.md](docs/production-checklist.md) — Checklist de produção
- [docs/observability-payments-runbook.md](docs/observability-payments-runbook.md) — Runbook de incidentes
- [docs/troubleshooting-pix.md](docs/troubleshooting-pix.md) — Troubleshooting Pix/webhooks
- [docs/monitoring/](docs/monitoring/) — Templates Prometheus, Alertmanager, Grafana
