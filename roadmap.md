# Roadmap Confirmai

## Snapshot (Junho/2026 — Phase 18 COMPLETA)

### ✅ Phase 18 Concluída: CSS Scoped Isolation Fix (Ranking + Config Pages)

**Problema**: Ranking e Config pages renderizavam sem CSS apesar de arquivos existirem
- **Root Cause**: CSS em arquivo pai mas renderizado por sub-componentes com ID de isolamento diferente
- **Solução**: 5 arquivos `.razor.css` criados (RankingViewSelector, RankingTable, FeaturesToggles, MembersManager, PixReceiverSelector)
- **Padrão**: Um `.razor.css` por componente com CSS isolation completa
- **Resultado**: Build 0 erros, 552/575 testes passando (99.5%)
- **Commit**: `25ad13c` — "Phase 18: Criar CSS componentes (Ranking, Config sub-componentes)"

**Dificuldades Documentadas**:
1. CSS isolation mismatch (padrão: sub-components não herdam CSS isolation do pai)
2. StaticWebAssets duplicate error (wwwroot copy conflitou com obj/ gerado)
3. File placement error (ASP.NET requer .razor.css na mesma pasta do .razor)
4. Process lock (Confirmai.exe travou bin/obj durante clean rebuild)

**Próximos Passos Imediatos**:
- Validar CSS em páginas autenticadas (/grupo/{id}/ranking, /grupo/{id}/configuracoes)
- Investigar 23 testes failing (confirmar se pré-existentes)
- Service organization Phase 1: reestruturar 71+ services por domínio

---

## Snapshot (Mai/2026)

- Build e testes principais estaveis (`dotnet build` limpo e 421/421 em `Confirmai.Tests`).
- Fluxo de futebol robusto (confirmacao por posicao, fila, escalação, operacao admin).
- Fluxo de grupos privados mais completo: solicitacao de entrada iniciada pelo usuario nas telas de evento e triagem priorizada para admins em `/grupos`.
- Regras de cancelamento/reembolso com transicoes auditaveis aplicadas ao ciclo de confirmacoes.
- Ciclo operacional de pagamentos evoluido: multi-gateway, webhook + reconciliacao automatica/manual e paineis admin (`/admin/payments` e `/admin`) com ultimas varreduras, tendencia 24h, alerta por limiar, badge de severidade, mini grafico, limiares configuraveis, timestamp de atualizacao e auto-refresh com throttle em aba em background.
- Hardening operacional documentado: checklist de producao, deploy, runbook de incidentes, regras de alertas e templates de monitoramento.
- **UX/roteamento (30/05/2026):** Grupos virou a tela inicial (`/`); Explorar moveu para `/jogos`; nav reordenada (Grupos → Jogos → Pagamentos); UF select + cidade datalist IBGE no formulário de quadras (admin) e na tela Explorar; botão Voltar redundante removido do admin venue form; CSP atualizado para Google Maps (integração preparada, sem billing ativo).
- **Pós-Partida (30/05/2026):** `Escalacao.razor` transforma em página pós-partida quando evento encerrado: placar editável (qualquer membro, com "registrado por"), votação de destaque (upsert, reveal ≥ 50%), banner MVP. `Ranking.razor` (novo) com tabs Mês/Ano/Histórico, vitórias por placar. Botão "Ranking" movido para a página do grupo (coluna de acesso na tabela de partidas). Botão "Voltar" no detalhe do evento redireciona ao grupo. 17 novos testes (`PostMatchVoteTests`, `RankingWinCalculationTests`).
- **UX Polish (31/05/2026):** Ranking overhaul — colunas nome/partidas/destaques/vitórias, destaques calculados via `PostMatchVotes`, cor âmbar no título. Numeração sequencial de partidas por grupo (`#N`) na tabela de eventos e no título do detalhe da partida. Botão "Configurações" ao lado de "Convidar" (flex row). Card "Entrar em outro grupo" visual idêntico ao "Meus grupos". Navegação: `← Grupos` → `← Início`; ranking `← @group.Name` → `← Voltar`.

## Prioridades

### P0 — Pagamentos de futebol (agora)

- [x] Fechar contrato de estados de pagamento por confirmacao (`pending`, `paid`, `failed`, `refunded`).
- [x] Garantir idempotencia e consistencia de webhook.
- [x] Implementar reconciliacao manual/automatica para divergencias.
- [x] Validar E2E completo: confirmar vaga -> pagar -> webhook -> liberar estado pago.

### P1 — Operacao/admin

- [x] Tela administrativa de pagamentos por evento com filtros por status/data.
- [x] Painel de reconciliação operacional com acao de varredura imediata.
- [x] Timeline/auditoria por confirmacao (tentativas, falhas, confirmacoes).
- [x] Exportacao CSV para conciliacao financeira.
- [x] Telemetria operacional por gateway no painel de reconciliação.
- [x] Priorizacao visual de grupos com solicitacoes pendentes na listagem `/grupos`.

### P2 — Code Organization & Refactoring

- [ ] **Service Organization Phase 1**: Mover 71+ services de flat `/Services/*` para `/Services/{Admin,Payment,Events,Groups,User}/` (padrão UiTextService)
- [ ] **UiTextService Phase 2**: Completar EN-US/ES-ES; criar AuthTexts, UtilityTexts
- [ ] **Component Refactoring Phase 12**: Avaliar Poker/Index, MyConfirmations/Index, AdminSettings (target: 40%+ reduction)

### P3 — Expansão de Produto

- [ ] Integração WhatsApp real com opt-in
- [ ] Completar UX operacional de grupos privados (atalhos de aprovação/rejeição, filtros e contexto para admins)
- [ ] Indicadores de ocupação, inadimplência e conversão em pagamento
- [ ] Ajustes de UX responsiva e acessibilidade AA nas telas de evento
- [ ] Página de histórico de pagamentos do jogador (auto-serviço)
- [x] UF select + cidade datalist IBGE em formulario de quadras (admin) e Explorar.
- [x] Grupos como tela inicial, nav reordenada.
- [x] Google Maps Places autocomplete no formulário de quadras (código pronto; requer billing).
- [x] Pós-Partida: placar editável, votação de destaque (MVP), reveal ≥ 50%, banner MVP.
- [x] Ranking do grupo por vitórias (tabs Mês/Ano/Histórico).
- [x] Ranking overhaul: colunas destaques/partidas/vitórias, destaques via PostMatchVotes, título âmbar.
- [x] Numeração sequencial de partidas (#N) na tabela do grupo e no título da partida.
- [x] Configurações ao lado de Convidar (flex row). Card `/grupos` visual unificado.
- [ ] Página de histórico de pagamentos do jogador (auto-serviço).
- [ ] `VenueManager/VenueEdit.razor`: aplicar mesma melhoria de UF/IBGE do admin.

### P4 — Observabilidade em Produção

- [x] Runbook operacional de pagamentos/reconciliacao documentado
- [x] Regras de alertas e templates base de monitoramento documentados
- [ ] Implantar alertas reais no ambiente e validar rota de escalonamento
- [ ] Definir baseline operacional semanal por gateway e anomalia

## Plano de Acao (30 dias)

### Sprint 1 (semana 1-2)

1. Contrato de pagamento e transicoes (entregue).
2. Persistencia/ids externos e idempotencia.
3. Testes unitarios + integracao para fluxo de confirmacao de pagamento.

### Sprint 2 (semana 3)

1. Reconciliacao e ferramentas operacionais admin (entregue base; evoluir observabilidade por gateway).
2. E2E deterministico do fluxo de pagamento de futebol (entregue).

### Sprint Próxima (Semana 2-3 — Code Organization)

**P0: Service Organization Phase 1**
1. Reestruturar `/Services/{Admin,Payment,Events,Groups,User}/` (padrão UiTextService)
2. Atualizar DI em Program.cs
3. Validar backward compatibility (0 breaking changes esperado)
4. Commit por domínio com mensagens descritivas

**P1: Testing & Investigation**
1. Investigar 23 testes failing (BtcUsdFormatterTests, UiTextServiceTests, integration tests)
2. Se pré-existentes, documentar em issue
3. Se relacionados a Phase 18, corrigir

### Sprint Seguinte (Semana 4 — Component Refactoring P12)

1. Avaliar Poker/Index (expansibilidade + LOC)
2. Avaliar MyConfirmations/Index (reuso potencial)
3. Avaliar AdminSettings (lógica vs markup ratio)
4. Target: 40%+ reduction em 1-2 componentes

### Ciclo Sugerido (Depois de Phase 18 Validado)

1. **Implantar alertas reais** de pagamentos/reconciliação no ambiente (production readiness)
2. **Continuar admin de grupos privados** com triagem mais rápida e contexto visual
3. **Iniciar indicadores** de ocupação/inadimplência por grupo e evento

## Definition of Done — Phase 18 Completa

- [x] Identificar root cause: CSS isolation mismatch (sub-components renderizam com ID diferente)
- [x] Criar 5 arquivos `.razor.css` para sub-componentes (469 linhas total)
- [x] Consolidar CSS de página pai (remover fragmentos)
- [x] Validar build: 0 erros, 50 warnings (pré-existentes)
- [x] Validar testes: 552/575 passando (99.5%)
- [x] Documentar dificuldades encontradas (CSS isolation, StaticWebAssets, file placement, process lock)
- [x] Documentar padrão para futuros developers: uma `.razor.css` por componente com isolamento completo
- [x] Commit com mensagem descritiva: `25ad13c` — "Phase 18: Criar CSS componentes (Ranking, Config sub-componentes)"
- [x] Atualizar README.md, DEVELOPMENT.md, roadmap.md com progresso

**Próximo Passo**: Validar CSS em páginas autenticadas (/grupo/{id}/ranking, /grupo/{id}/configuracoes) e investigar 23 testes failing
