# Roadmap Confirmai

## Estado Atual (Julho 2026) — pos-Ciclo 14

### Números do projeto

| Métrica | Valor |
|---------|-------|
| Páginas Blazor | 139 |
| Componentes compartilhados | 37 |
| Sub-componentes (Pages) | 38 (Admin 4, Futsal 19, Groups 3, Payment 7, Other 5) |
| Serviços | 87 (organizados em 12 domínios) |
| Testes (xUnit) | 1.674 passando / 0 falhando / 192 arquivos |
| Cobertura | 9.9% (11.733/118.427 linhas) |
| CSS scoped | 118 arquivos `.razor.css` |
| CSS globais | 4 arquivos (site.css, events.css, identity.css, marketplace.css) |
| CSS vars no `:root` | 315 (0 indefinidas, 0 mortas) |
| CSS vars total usos | 4.244 |
| Hardcoded hex scoped | 0 (desde Ciclo 6) |
| AppDbContext direto | 0 (100% IDbContextFactory desde Ciclo 12) |
| StateHasChanged() | 15 chamadas |
| IAsyncDisposable | 7 páginas implementam |
| Warnings build | 0 (47 preexistentes em dependencies) |
| Idiomas | PT-BR, EN-US, ES-ES (833 chaves) |
| Sessão | Keep-alive JS 15min + timeout 720min dev / 480min prod |

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
- [x] UiTextService com i18n (PT-BR, EN-US, ES-ES completo — 833 chaves traduzidas)
- [x] StateHasChanged() reduzido de 101 → 15 chamadas
- [x] IAsyncDisposable implementado em 7 páginas críticas
- [x] CSS Scoped Isolation corrigido: sub-componentes com `.razor.css` próprio
- [x] Inline styles estáticos: ~35 → 0 (Ciclo 5)
- [x] CSS scoped: 118 arquivos `.razor.css`
- [x] Warnings: CS1998, CS0649, CS86xx → todos zerados
- [x] CI com build + test + coverage (Coverlet → badge)
- [x] Documentação consolidada: 22 .md → 3 centrais (README, ROADMAP, CONTRIBUTING) + docs operacionais
- [x] Testes: 575 → 1.674 (24 corrigidos + novos adicionados)
- [x] AdminPayments sub-componentes com _Imports.razor e parâmetros tipados
- [x] IDbContextFactory: 100% das páginas migradas (0 `@inject AppDbContext` direto desde Ciclo 12)
- [x] Migrar 6 services de AppDbContext → IDbContextFactory
- [x] xUnit2013 warnings: 49 → 0 (Ciclo 12)

### Refatoração CSS (Ciclos 4—11)
- [x] Hardcoded hex em scoped CSS: 1.387 → 0
- [x] rgba() hardcoded scoped: 645 → 185 (restantes são padrões únicos)
- [x] CSS vars no `:root`: 68 → 315
- [x] CSS vars usos totais: 673 → 4.244
- [x] Fallbacks `var(--xx, #hex)`: 178 → 0
- [x] `!important` scoped: 16 → 1 (AvatarUpload pattern legítimo)
- [x] Páginas >400L sem code-behind: 12 → 0 (todas decompostas)
- [x] Vars indefinidas: 0 (desde Ciclo 13)
- [x] Vars mortas: 0 (desde Ciclo 13)

### Infraestrutura de Sessão (Ciclo 14)
- [x] Refresh token via keep-alive JS (`/api/ping` a cada 15min)
- [x] Session timeout: 720min dev, 480min prod
- [x] `AddControllers()` + `MapControllers()` registrados

### UX
- [x] Eventos como tela inicial (`/` = `/eventos` desde Ciclo 13)
- [x] Navegação: Eventos → Grupos → Pagamentos
- [x] Caixa de mensagens (`/mailbox`) com anexos e arquivamento
- [x] Tema escuro, layout responsivo
- [x] Cookie consent com personalização
- [x] Histórico de pagamentos com exportação CSV/HTML
- [x] Acessibilidade WCAG AA: skip link, foco aprimorado, high contrast, reduced motion
- [x] Dashboard de métricas por grupo (ocupação, presença, pagamentos)
- [x] Integração WhatsApp com opt-in

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
- [ ] Deploy em produção e testes do usuário
- [ ] Testar fluxo Pix completo em produção (EfiBank)
- [ ] Ativar webhook AbacatePay em produção
- [ ] Implantar alertas reais e validar escalonamento fim a fim
- [x] ~~Investigar 24 testes falhando~~ → Corrigido (PR #7)

### Concluído recentemente (Ciclos 8—14)
- [x] Decomposição de TODAS as 12 páginas >400L com code-behind
- [x] 36 melhorias de UX/UI (escalação, pagamentos, grupos, admin, navegação)
- [x] Eliminação completa de Virtualize → paginação manual
- [x] Causa raiz card grupo: 17 vars auto-referenciais corrigidas
- [x] Background `.sports-shell`/`.listing-block` movido para global (events.css)
- [x] Tela inicial reestruturada: `/` = `/eventos`
- [x] Refresh token implementado (keep-alive JS + session timeout ampliado)
- [x] IDbContextFactory ZERADO (0 `@inject AppDbContext` direto)
- [x] xUnit2013 warnings ZERADO

---

## Backlog

### P1 — Testes (trabalho para o Pleno)
- [ ] Testes do `PingController` (novo no Ciclo 14 — 0% cobertura)
- [ ] Testes dos 5 services sem cobertura: `CityService`, `GroupMetricsService`, `LocationService`, `WhatsAppNotificationService`, `IBitcoinPaymentService`
- [ ] Aumentar cobertura de testes: 9.9% → meta 15%+

### P2 — UX/Produto
- [ ] Testar responsividade mobile (375px/414px)
- [ ] Atalhos de aprovação/rejeição direta para admins (grupos privados)
- [ ] Filtros e contexto visual para decisões de triagem
- [ ] `VenueManager/VenueEdit.razor`: aplicar melhoria UF/IBGE do admin
- [ ] Integração WhatsApp real com opt-in
- [ ] Indicadores de ocupação, inadimplência e conversão por grupo
- [ ] Ajustes de UX responsiva e acessibilidade AA

### P3 — Qualidade de Código
- [x] ~~Expandir cobertura scoped CSS~~ → 118 arquivos `.razor.css` (concluído)
- [x] ~~Consolidar CSS duplicado~~ → 315 vars no `:root`, refatoração concluída
- [ ] Reduzir `!important` em site.css (11 ocorrências, maioria legítima)

### P4 — Operação Contínua
- [ ] Definir baseline operacional semanal por gateway
- [ ] Formalizar ritual pós-incidente com checklist de causa raiz
- [x] ~~Virtual scrolling para listas grandes~~ → Substituído por paginação manual (20 itens/página) em todas as tabelas admin

---

## Referências Operacionais

- [docs/deploy.md](docs/deploy.md) — Deploy com Docker
- [docs/production-checklist.md](docs/production-checklist.md) — Checklist de produção
- [docs/observability-payments-runbook.md](docs/observability-payments-runbook.md) — Runbook de incidentes
- [docs/troubleshooting-pix.md](docs/troubleshooting-pix.md) — Troubleshooting Pix
- [docs/monitoring/](docs/monitoring/) — Templates de monitoramento
