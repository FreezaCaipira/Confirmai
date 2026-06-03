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

### Monitoramento e Alertas Operacionais

#### Background Services (`IHostedService`)

Dois serviços de monitoramento contínuo registrados em `Program.cs` (linhas 133-134):

1. **`PendingWebhooksAlertService`** (`Services/PendingWebhooksAlertService.cs`)
   - **Propósito**: Detectar pagamentos Pix pendentes há mais de 24 horas (stale webhooks)
   - **Frequência**: Executa a cada **1 hora** após inicialização
   - **Lógica**: Consulta `EventConfirmations` onde `HasPaid=false`, `PaymentStatus=Pending` e `ConfirmedAt` > 24h atrás
   - **Alerta**: Log `WARNING` se encontrar **>5 confirmações pendentes**
   - **Saída**: Estruturada em `ILogger<PendingWebhooksAlertService>` (Serilog)
   - **Uso**: `dotnet run` ativa automaticamente; não há UI de configuração necessária

2. **`CertificateHealthCheckService`** (`Services/CertificateHealthCheckService.cs`)
   - **Propósito**: Monitorar expiração do certificado mTLS EfiBank
   - **Frequência**: Verifica a cada **12 horas** após inicialização
   - **Lógica**: Lê X509Certificate2 do caminho em `IConfiguration["EfiBank:ClientCertificatePath"]`
   - **Alertas progressivos**:
     - 🔴 `EXPIRADO` — ≤0 dias (crítico, immediate action)
     - 🔴 `CRÍTICO` — ≤7 dias (critical, renew ASAP)
     - 🟠 `URGENTE` — 7-14 dias (urgent, plan renewal)
     - 🟡 `AVISO` — 14-30 dias (warning, schedule soon)
   - **Saída**: Logs estruturados com data exata de expiração
   - **Uso**: Não requer configuração; função automaticamente em produção

#### Monitorando em Produção

**Buscar alertas de webhooks pendentes**:
```bash
# Linux/Docker
grep "ALERTA.*Pix pendentes\|PendingWebhooksAlertService" /var/log/confirmai/confirmai.log | tail -20

# Windows (PowerShell)
Get-Content -Path "C:\logs\confirmai.log" -Tail 100 | Select-String "ALERTA.*Pix"
```

**Buscar alertas de certificado**:
```bash
# Qualquer alerta de certificado (EXPIRADO, CRÍTICO, URGENTE, AVISO)
grep "EXPIRADO\|CRÍTICO\|URGENTE\|AVISO" /var/log/confirmai/confirmai.log

# Apenas logs do serviço
grep "CertificateHealthCheckService" /var/log/confirmai/confirmai.log
```

#### Integração com Stack de Monitoramento

**DataDog / New Relic / Splunk**:

Alertas são estruturados com Serilog e incluem:
- **ServiceName**: `PendingWebhooksAlertService` | `CertificateHealthCheckService`
- **Level**: `Information` (check), `Warning` (threshold exceeded), `Error` (exception)
- **Timestamp**: ISO 8601
- **Structured fields**: `PendingCount`, `ExpiryDate`, `DaysRemaining`, etc.

Exemplo de parser para DataDog:
```yaml
# datadog-agent.yaml
logs:
  - service: confirmai-api
    source: dotnet
    tags:
      - env:prod
      - app:payments
    query: "source:confirmai service:(PendingWebhooksAlertService OR CertificateHealthCheckService)"
```

#### Troubleshooting

| Problema | Diagnóstico | Solução |
|----------|-------------|---------|
| Serviço não inicia | Log `StartAsync` ausente | Verificar `Program.cs` registrou `AddHostedService<>` |
| Webhook alert nunca acionado | Consultar `SELECT * FROM "EventConfirmations" WHERE "HasPaid"=false` | Dados de teste? Verificar filtro de 24h |
| Certificado alert não aparece | Verificar caminho em `appsettings.json` `EfiBank:ClientCertificatePath` | Path errado? Arquivo inacessível? |
| Log não aparece em produção | Verificar nível de log em `Serilog:MinimumLevel` | Elevar para `Information` se estiver em `Error` |

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

- Base estável para futebol + poker, com foco operacional em eventos e confirmações.
- Build limpo e suíte principal passando (421/421 testes).
- Fluxo de futsal completo para operação diária: vagas por posição, fila, cancelamento, status de pagamento e escalação.
- Fluxo de pagamentos por evento com multi-gateway e reconciliação operacional já entregue (webhook + worker + fallback manual).
- Operação/admin de reconciliação com painel de saúde, histórico de varreduras, tendência 24h e limiares configuráveis via dashboard admin.
- Fluxo de grupos privados evoluido: solicitação de entrada iniciada pelo usuário nas páginas de evento e triagem priorizada em `/grupos` para admins.
- Hardening de release entregue com smoke pós-deploy, checklist de rollback, runbook operacional de incidentes e templates de monitoramento.
- Infra consolidada: Identity, CSP nonce por request, headers de segurança, audit/log estruturado, CI.- [x] Alertas operacionais de webhooks e certificados (IHostedService) — veja seção "Monitoramento e Alertas Operacionais".
---

## Roadmap Confirmai (Atualizado)

### Frentes concluídas

- [x] Modelo de domínio (Group, GroupMember, Event, EventConfirmation, WaitingList)
- [x] Fluxos principais de futsal e poker (criação/edição, confirmação, fila, cancelamento)
- [x] Escalação de futsal (`/futsal/{id}/escalacao`) com randomização, confirmação e reset
- [x] Hardening de segurança base (CSP, headers, políticas de autenticação)
- [x] Auditoria estruturada e observabilidade operacional

### Frentes em andamento

- [x] Pagamentos do fluxo de futebol em modo produção (contrato final de estados + E2E ponta a ponta)
- [x] Operação/admin de pagamentos (timeline por confirmação, exportação e métricas por gateway)
- [x] CI/CD de release com smoke test e rollback explícito
- [x] UX inicial de grupos privados para solicitação de entrada e priorização de pendências admin
- [x] Alertas operacionais automatizados a partir do runbook (webhook warning, staleness e anomalia de pendências)

### Backlog planejado

- [ ] **Service Organization**: Reestruturar `/Services/*` (71+ serviços) para domínios (`/Services/Admin/`, `/Services/Payments/`, `/Services/Events/`, etc.) — padrão UiTextService
- [ ] **UiTextService Phase 2**: Completar EN-US/ES-ES para AdminTexts, ServerTexts, PaymentTexts; criar AuthTexts, UtilityTexts
- [ ] **Component Refactoring Phase 12**: Avaliar Poker/Index, MyConfirmations/Index, AdminSettings para decomposição (target: 40%+ reduction)
- [ ] **WhatsApp provider real** (`IWhatsAppSender`) com opt-in e envio transacional
- [ ] **Fluxo admin de grupos privados** com mais contexto de decisão e atalhos diretos de aprovação/rejeição
- [ ] **Dashboard consolidado por grupo** (ocupação, no-show, receita, inadimplência)
- [ ] **Refinos de UX mobile e acessibilidade AA** nas telas de evento
- [ ] **Página de histórico de pagamentos do jogador** (auto-serviço)

---

## TODO Priorizado (Próximas Implementações)

### P0 — Pagamentos (crítico)

- [x] Fechar contrato de pagamento para eventos de futebol:
  - Status oficiais (`pending`, `paid`, `failed`, `refunded`)
  - Regra de cancelamento por status
  - Regra de idempotência de webhook
- [x] Garantir vínculo forte entre `EventConfirmation` e cobrança (charge/tx id)
- [x] Cobrir reconciliação:
  - webhook confirma pagamento
  - polling/manual check corrige divergências
  - worker automático mantém paridade sem clique manual
- [x] E2E de pagamento do futebol (`confirmar` → `pagar` → `webhook` → `hasPaid=true`)

### P1 — Operação/Admin

- [x] Tela/admin de pagamentos por evento (filtro por status e período)
- [x] Painel operacional de reconciliação em `/admin/payments` (pendências + varredura imediata)
- [x] Timeline/audit por confirmação (tentativas, confirmações, falhas)
- [x] Exportação CSV de pagamentos e conciliação
- [x] Telemetria operacional por gateway (pendências, share, severidade)

### P2 — Produto

- [ ] WhatsApp transacional (cancelamento/alteração) com consentimento explícito
- [ ] Melhorar operação de grupos privados para admins (triagem rápida, contexto e ações diretas)
- [ ] Melhorias analíticas (KPI de ocupação e conversão em pagamento)
- [ ] Ajustes de UX responsiva e acessibilidade AA nas telas de evento

### P3 — Operação contínua

- [x] Converter runbook em alertas acionáveis no stack de monitoramento (PendingWebhooksAlertService, CertificateHealthCheckService)
- [ ] Definir baseline por gateway (pending/stale/failed/refunded) com revisão semanal
- [ ] Formalizar ritual pós-incidente com checklist de causa raiz e follow-up

---

## Plano de Ação (30 dias)

### Sprint 1 — Fechamento do núcleo de pagamento

1. Definir contrato final de estados e transições.
2. Ajustar persistência e idempotência.
3. Entregar testes unitários e integração do núcleo.

### Sprint 2 — Fluxo ponta a ponta e operação

1. Completar webhook + reconciliação automática/manual.
2. Publicar tela admin operacional de pagamentos (entregue).
3. Entregar cenário E2E de futebol com pagamento confirmado.

### Sprint 3 — Release readiness

1. Checklist de produção (variáveis, webhook URL, rotação de segredos). (entregue)
2. Smoke test de deploy com rollback documentado. (entregue)
3. Monitoramento de eventos de pagamento e alertas básicos. (entregue base)
4. Próximo passo: automatizar alertas do runbook em produção.

### Próximo ciclo sugerido

1. Implantar e validar alertas reais de pagamentos/reconciliação no ambiente.
2. Continuar a superfície admin de grupos privados com triagem mais rápida.
3. Iniciar métricas de ocupação/inadimplência por grupo e evento.

## Referências operacionais (produção)

- `docs/production-checklist.md`
- `docs/deploy.md`
- `docs/observability-payments-runbook.md`
- `docs/payments-alert-rules.md`
- `docs/monitoring/README.md`
