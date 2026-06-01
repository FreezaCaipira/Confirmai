# Roadmap Confirmai

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

### P2 — Expansao de produto

- [ ] Integracao WhatsApp real com opt-in.
- [ ] Completar UX operacional de grupos privados (atalhos de aprovacao/rejeicao, filtros e contexto para admins).
- [ ] Indicadores de ocupacao, inadimplencia e conversao em pagamento.
- [ ] Ajustes de UX responsiva e acessibilidade AA nas telas de evento.
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

### P3 — Observabilidade em producao

- [x] Runbook operacional de pagamentos/reconciliacao documentado.
- [x] Regras de alertas e templates base de monitoramento documentados.
- [ ] Implantar alertas reais no ambiente e validar rota de escalonamento.
- [ ] Definir baseline operacional semanal por gateway e anomalia.

## Plano de Acao (30 dias)

### Sprint 1 (semana 1-2)

1. Contrato de pagamento e transicoes (entregue).
2. Persistencia/ids externos e idempotencia.
3. Testes unitarios + integracao para fluxo de confirmacao de pagamento.

### Sprint 2 (semana 3)

1. Reconciliacao e ferramentas operacionais admin (entregue base; evoluir observabilidade por gateway).
2. E2E deterministico do fluxo de pagamento de futebol (entregue).

### Sprint 3 (semana 4)

1. Validacao de producao (webhook, segredos, observabilidade). (entregue base)
2. Smoke test e rollback documentado. (entregue)
3. Preparacao para release da frente de pagamentos. (entregue base)

### Proximo ciclo sugerido

1. Implantar alertas de pagamentos/reconciliacao com thresholds do runbook e validar notificacao real.
2. Elevar a UX admin de grupos privados: separar backlog pendente, atalhos de triagem e estados vazios mais explicitos.
3. Iniciar indicadores de ocupacao/inadimplencia para grupos e eventos.

## Definition of Done — Sprint 1 (Pagamentos)

- [x] Webhook com idempotencia e consistencia no core de pagamentos.
- [x] Reconciliacao automatica + manual usando servico compartilhado.
- [x] Painel `/admin/payments` com KPI operacional, tendencia, severidade e historico.
- [x] Auto-refresh com throttle em aba em background.
- [x] Observabilidade de staleness com evento `payment.reconciliation.panel.stale`.
- [x] Navegacao operacional: deep-link para `/admin/logs` com filtros e periodo padrao.
- [x] `/admin/logs` respeitando querystring no carregamento inicial (SSR).
- [x] Cobertura de testes para parser/merge/deep-link/querystring e integracao HTTP de filtro efetivo.
- [x] Documentacao de progresso sincronizada (`README.md` e `docs/payments-sprint1-kickoff.md`).
- [x] Rodar suite completa de testes antes do fechamento formal da sprint.
- [x] Validar fluxo E2E completo de pagamento (confirmar vaga -> pagar -> webhook -> estado pago).
