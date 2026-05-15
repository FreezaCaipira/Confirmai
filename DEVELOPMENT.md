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

- Fork do OtServ Market adaptado para Confirmai
- Infraestrutura intacta: Identity, CSP, SignalR, EF Core, Serilog, OpenTelemetry, CI
- CSS restructurado: site.css (global), componentes isolados via .razor.css
- Cookie consent funcional (JS renomeado para privacy-prefs.js — ad blockers bloqueavam cookie-consent.js)

---

## Roadmap Confirmai

### Fase 1 — Modelo de dados base (atual)

**Objetivo**: substituir entidades do fork pelas entidades do domínio real.

Mapeamento fork → Confirmai:

| Fork | Ação | Confirmai |
|---|---|---|
| `TibiaServer` | Adaptar | `Group` (casa de poker / pelada de fut) |
| `ServerMember` | Adaptar | `GroupMember` |
| `ServerMemberRole` | Renomear valores | `GroupMemberRole` (Admin / Member) |
| `Product` | Substituir | `Event` (torneio ou partida) |
| `Order` | Substituir | `EventConfirmation` |
| `PaymentStatus` | Manter | Reaproveitado quando pagamento entrar |
| `PaymentOrder` | Arquivar | Substituído no futuro por Pix |
| `BtcPay` | Remover | Fora do escopo atual |

Novas entidades sem equivalente no fork:
- `Sport` — Futsal | Poker | (futuramente Xadrez, etc.)
- `WaitingList` — fila de espera quando Event está lotado

Modelo `Event` tabela única com campos nullable por esporte:
```
Event
  GroupId, SportId, StartsAt, Location
  MaxPlayers, MaxGoalkeepers?          ← futsal
  BuyIn?, Rebuy?, Addon?, BonusInfo?   ← poker
```

- [ ] Criar enum/tabela `Sport`
- [ ] Criar `Group` a partir de `TibiaServer`
- [ ] Criar `GroupMember` / `GroupMemberRole`
- [ ] Criar `Event` (tabela única com campos nullable)
- [ ] Criar `EventConfirmation` + índice único (UserId, EventId)
- [ ] Criar `WaitingList`
- [ ] Migration e seed de dados de teste

---

### Fase 2 — UI tela inicial e listagem

**Objetivo**: tela de esportes e listagem de eventos funcionais.

- [ ] Tela `/` — lista de esportes disponíveis (cards: Futsal, Poker)
- [ ] Tela `/[sport]` — lista de grupos por esporte, filtro por cidade (default: Pouso Alegre MG ou última cidade do usuário)
- [ ] Tela `/[sport]/[groupId]` — lista de eventos do grupo
- [ ] Tela `/[sport]/[groupId]/[eventId]` — detalhe do evento com lista de confirmados
- [ ] Botão "Confirmar presença" — redireciona para login se não autenticado
- [ ] Detecção de conflito de horário ao confirmar

---

### Fase 3 — Admin de grupo

**Objetivo**: admin do grupo cria e gerencia eventos.

- [ ] Painel do admin de grupo
- [ ] CRUD de eventos (com campos específicos por esporte)
- [ ] Gerenciar lista de confirmados (remover, promover da fila)
- [ ] Definir vagas por posição (goleiro/linha no futsal)

---

### Fase 4 — Admin do site

**Objetivo**: admin global gerencia grupos e esportes.

- [ ] Aprovar/rejeitar novos grupos
- [ ] Gerenciar esportes disponíveis
- [ ] Dashboard de uso (grupos ativos, confirmações por período)

---

### Fase 5 — Pagamento (standby)

**Objetivo**: monetização via taxa por confirmação.

- [ ] Integração Pix (R$ 1,00 por confirmação ou similar)
- [ ] Reaproveitamento de `PaymentStatus` e infraestrutura SignalR existente
- [ ] Relatório financeiro para admin do grupo

---

### Fase 6 — Notificações WhatsApp (planejado)

**Objetivo**: notificar participantes via WhatsApp além do mailbox interno e email, para avisos de cancelamento e mudança de data/hora.

#### Pré-requisitos de modelo

1. Adicionar campo `PhoneNumber` (formato E.164, ex: `+5535999990000`) em `ApplicationUser`
2. Adicionar `WhatsAppOptIn bool` (consentimento explícito) em `ApplicationUser`
3. Exibir opção de opt-in nas configurações de perfil do usuário

#### Pré-requisitos de infraestrutura

Criar abstração em `Services/`:

```csharp
// Services/IWhatsAppSender.cs
public interface IWhatsAppSender
{
    Task SendTextAsync(string toE164, string text);
}
```

Implementar com um dos providers abaixo (ordem de preferência):

| Provider | Tipo | Observação |
|---|---|---|
| **Evolution API** | Self-hosted (Docker) | Gratuito; usa sessão QR Code via WhatsApp Web; mais simples para MVP |
| **Z-API** | SaaS BR | Plano gratuito com limite; boa SDK REST |
| **Twilio WhatsApp** | SaaS global | Requer aprovação de template no Meta; mais robusto para produção |

Registrar em `Program.cs`:

```csharp
// exemplo com Evolution API
builder.Services.AddHttpClient<IWhatsAppSender, EvolutionWhatsAppSender>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WhatsApp:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("apikey", builder.Configuration["WhatsApp:ApiKey"]);
});
```

#### Ponto de integração

O envio já está estruturado em `Services/EventNotificationService.cs` no método privado `SendToParticipantsAsync`.
Há um bloco `// TODO (WhatsApp)` comentado com o código exato a descomentar após a implementação do provider.

#### Configuração (`appsettings.json`)

```json
"WhatsApp": {
  "Provider": "EvolutionApi",  // ou "ZApi" | "Twilio"
  "BaseUrl": "",               // ex: http://localhost:8080
  "ApiKey": "",
  "Instance": ""               // Evolution API: nome da instância conectada
}
```
- Inline styles estáticos eliminados de todas as páginas
- Sem `@layer` em `site.css` (era bloco unclosed — removido)
