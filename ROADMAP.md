# Roadmap Confirmai

## Estado Atual (Junho 2026)

### Números do projeto

| Métrica | Valor |
|---------|-------|
| Páginas Blazor | ~97 |
| Componentes compartilhados | 28 |
| Serviços | 78 (organizados em 10 domínios) |
| Testes (xUnit) | 641 passando / 0 falhando / 641 total |
| CSS scoped | 110+ arquivos (todos os .razor têm .razor.css) |
| CSS globais | 4 arquivos |
| StateHasChanged() | 13 chamadas em 7 arquivos |
| IAsyncDisposable | 8 páginas implementam |
| Warnings build | 0 CS1998, 0 CS0649, 0 CS86xx |

---

## Concluído

### Fluxo de Futsal (core)
- [x] Criação/edição de partidas com vagas por posição (linha/goleiro)
- [x] Confirmação, fila de espera e promoção automática por cancelamento
- [x] Conflitos de horário por jogador (`EventCollisionService`)
- [x] Escalação com randomização, confirmação e reset
- [x] Pós-partida: placar editável, votação de destaque (MVP ≥ 50%), banner MVP
- [x] Ranking do grupo (tabs Mês/Ano/Histórico, vitórias por placar)
- [x] Numeração sequencial de partidas (#N)

### Pagamentos
- [x] Multi-gateway: BTCPayServer, AbacatePay, EfiBank, Appmax
- [x] Contrato de estados por confirmação (`Pending`, `Paid`, `Failed`, `Refunded`)
- [x] Webhooks com idempotência e transições auditáveis
- [x] Reconciliação automática (worker) + manual por `chargeId/txId`
- [x] Painel `/admin/payments`: contadores, tendência 24h, telemetria por gateway, severidade configurável
- [x] Staleness: alerta visual, deep-link para logs filtrados, auto-refresh com throttle em background

### Grupos
- [x] Grupos públicos e privados com roles (admin/membro)
- [x] Convite por código alfanumérico
- [x] Solicitação de entrada em páginas de evento
- [x] Priorização visual de grupos com pendências em `/grupos`
- [x] Configurações por grupo (features toggles, membros, receptor Pix)

### Operação e Admin
- [x] Auditoria estruturada (`EventType`/`EntityType`/`EntityId`/`MetadataJson`)
- [x] Logs com filtros avançados, ordenação, exportação CSV
- [x] Gestão de quadras com UF/cidade IBGE e Google Maps (preparado)
- [x] Gestão de idiomas no painel admin
- [x] Alertas operacionais: `PendingWebhooksAlertService` (1h), `CertificateHealthCheckService` (12h)
- [x] Filtros e estado persistentes em todas as telas administrativas

### Segurança
- [x] CSP com nonce por request, sem `unsafe-inline` em `script-src`
- [x] API Key com HMAC (`ApiKeyAuth`)
- [x] Headers: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`
- [x] Políticas de senha diferenciadas por ambiente (dev vs prod)
- [x] Lockout após 5 tentativas, cookie com sliding expiration

### Qualidade de Código
- [x] Services reorganizados por domínio (`Admin/`, `Core/`, `Payment/`, `Events/`, etc.)
- [x] UiTextService com i18n (PT-BR, EN-US, ES-ES completo - 691 chaves traduzidas)
- [x] StateHasChanged() reduzido de 101 → 13 chamadas
- [x] IAsyncDisposable implementado em 8 páginas críticas
- [x] CSS Scoped Isolation corrigido (Phase 18): sub-componentes com `.razor.css` próprio
- [x] L1: Eliminar ~20 inline styles estáticos (Groups/Join, Groups/Index, Groups/Detail, Index, Poker/Edit)
- [x] L2: Criar `.razor.css` para todas as páginas (110+ arquivos, incluindo 1 priority >400L)
- [x] L3: Corrigir 40 warnings CS1998 (async sem await) → remover async, retornar Task.CompletedTask
- [x] L4: Corrigir 18 warnings CS0649 (campos nunca atribuídos) → = null / = default
- [x] L5: Corrigir warnings CS8618/CS8602/CS8604/CS8601 (nullability) → default!, null!, null-coalescing
- [x] CI com build + test + coverage (Coverlet → badge)
- [x] Documentação consolidada: 22 .md → 3 centrais (README, ROADMAP, CONTRIBUTING) + docs operacionais
- [x] 24 testes corrigidos (encoding UTF-8, chaves UiText, wiring de componentes, assertions)
- [x] AdminPayments sub-componentes com _Imports.razor e parâmetros tipados
- [x] IDbContextFactory registrado no test factory para cobertura completa
- [x] Migrar 6 services de AppDbContext → IDbContextFactory (LogService, AdminSettingsService, ProductService, PaymentConfirmationService, AdminSecurityPolicyService, DashboardMetricsService)

### UX
- [x] Grupos como tela inicial (`/`), Explorar em `/jogos`
- [x] Navegação: Grupos → Jogos → Pagamentos
- [x] Caixa de mensagens (`/mailbox`) com anexos e arquivamento
- [x] Tema escuro, layout responsivo
- [x] Cookie consent com personalização

### Infraestrutura
- [x] Deploy guide com Docker + nginx + TLS
- [x] Checklist de produção documentado
- [x] Runbook de observabilidade e regras de alerta
- [x] Templates Prometheus/Alertmanager/Grafana versionados
- [x] Smoke test pós-deploy (`scripts/smoke-postdeploy.ps1`)
- [x] Smoke de monitoring stack (`scripts/smoke-monitoring-alerts.ps1`)

---

## Em Andamento

### P0 — Validação em Produção
- [ ] Testar fluxo Pix completo em produção (EfiBank)
- [ ] Ativar webhook AbacatePay em produção
- [ ] Implantar alertas reais e validar escalonamento fim a fim
- [x] ~~Investigar 24 testes falhando~~ → Corrigido (PR #7)

### P1 — Decomposição de Componentes Grandes
- [x] `Groups/Detail.razor` (947 linhas) — extrair sub-componentes
- [x] `Futsal/Detail.razor` (933 linhas) — extrair sub-componentes
- [x] `AdminLogs.razor` (695 linhas) — decompor
- [x] `Mailbox.razor` (674 linhas) — já tem Components/, mas página raiz grande
- [x] `Poker/Detail.razor` (628 linhas) — decompor

### P2 — UX de Grupos Privados
- [ ] Atalhos de aprovação/rejeição direta para admins
- [ ] Filtros e contexto visual para decisões de triagem
- [ ] `VenueManager/VenueEdit.razor`: aplicar melhoria UF/IBGE do admin

---

## Backlog

### P2 — Produto
- [ ] Integração WhatsApp real com opt-in
- [ ] Indicadores de ocupação, inadimplência e conversão por grupo
- [ ] Página de histórico de pagamentos do jogador (auto-serviço)
- [ ] Ajustes de UX responsiva e acessibilidade AA

### P3 — Qualidade de Código
- [ ] Expandir cobertura scoped CSS (42 de 97 páginas têm `.razor.css`, faltam 55)
- [ ] Consolidar CSS duplicado em utility classes / design tokens adicionais
- [ ] Aumentar cobertura de testes → meta 80%+

### P4 — Operação Contínua
- [ ] Definir baseline operacional semanal por gateway
- [ ] Formalizar ritual pós-incidente com checklist de causa raiz
- [ ] Virtual scrolling para listas grandes

---

## Referências Operacionais

- [docs/deploy.md](docs/deploy.md) — Deploy com Docker
- [docs/production-checklist.md](docs/production-checklist.md) — Checklist de produção
- [docs/observability-payments-runbook.md](docs/observability-payments-runbook.md) — Runbook de incidentes
- [docs/troubleshooting-pix.md](docs/troubleshooting-pix.md) — Troubleshooting Pix
- [docs/monitoring/](docs/monitoring/) — Templates de monitoramento
