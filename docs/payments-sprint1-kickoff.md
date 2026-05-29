# Sprint 1 - Kickoff de Pagamentos (Mai/2026)

## Objetivo da sprint

Fechar o ciclo de pagamento do fluxo de futebol em producao com confiabilidade:

1. Criacao de cobranca Pix por confirmacao.
2. Confirmacao via webhook com idempotencia.
3. Reconciliacao para divergencias (webhook x polling/manual).
4. Validacao ponta a ponta com testes automatizados.

## Recomendacao inicial de gateway

Trilha principal: Efí Bank (ja integrado no codigo).

Motivos:

1. Integracao tecnica pronta no projeto (criacao de cobranca + consulta + webhook).
2. Vinculo direto com confirmacao de evento via PixTxId em EventConfirmation.
3. Menor risco para entrega da sprint (aproveita infraestrutura existente).

## Diretriz de arquitetura (multi-gateway)

Regra oficial para os proximos forks e para o core:

1. Aumentar numero de gateways e desejavel quando houver ganho de taxa/conversao/operacao.
2. Todo gateway novo deve nascer desabilitavel em `/admin/gateways`.
3. Integracao de gateway deve ser plugavel, sem acoplamento com pagina especifica.
4. Falha/indisponibilidade de um gateway nao pode indisponibilizar os demais.

## Appmax como opcao

Conclusao inicial: opcao comercial interessante, mas ainda sem evidencias tecnicas suficientes para substituir a trilha principal nesta sprint.

Sinais encontrados no site:

1. Forte proposta comercial (checkout, retentativa, antifraude, split, cross-border).
2. Mencao a API/PaaS em nivel institucional.
3. Nao foi identificada documentacao publica objetiva de fluxo server-to-server para Pix por item de dominio (charge API + webhook payload + assinatura + sandbox), pelo menos nas paginas institucionais.

## Criterios Go/No-Go para Appmax

Appmax so entra como gateway principal nesta sprint se atender TODOS:

1. API de cobranca Pix server-to-server com identificador externo por confirmacao.
2. Webhook de pagamento com assinatura verificavel e idempotencia clara.
3. Sandbox funcional com credenciais para homologacao.
4. Documentacao tecnica completa (endpoints, erros, limites, retries, timeout).
5. SLA e suporte tecnico para incidentes de conciliacao.

## Plano de execucao Sprint 1

### Bloco A - Entrega principal (Efí)

1. Revisar contrato de estados de pagamento por confirmacao.
2. Garantir idempotencia e trilha de auditoria no webhook.
3. Implementar reconciliacao operacional (consulta por txid e correcao de status).
4. Entregar cobertura de testes de integracao e E2E.

### Bloco B - Descoberta paralela (Appmax)

1. Solicitar documentacao tecnica oficial de API/webhook.
2. Validar disponibilidade de sandbox e credenciais de teste.
3. Montar prova de conceito minima (create charge + webhook receive).
4. Decidir Go/No-Go sem bloquear entrega do Bloco A.

## Decisao pratica para hoje

1. Sprint 1 continua com Efí como caminho de entrega.
2. Appmax entra como trilha de avaliacao paralela.
3. Janela para decisao final Appmax: ate o fim do Bloco B.

## Status de execucao (update)

1. Bloco A concluido no core: webhook idempotente + reconciliacao automatica/manual compartilhada.
2. Painel operacional em `/admin/payments` entregue com KPI, tendencia 24h, historico de varreduras e severidade.
3. Auto-refresh do painel com throttle em aba em background e indicador visual de pausa + timestamp da ultima pausa.
4. Indicador em tempo real de defasagem ("sem atualizar ha") no card operacional, com alerta visual para staleness.
5. Dica contextual no proprio indicador de defasagem explicando o limiar do alerta (2 ciclos / 60s).
6. Resumo de saude em `/admin` com timestamp da ultima atualizacao.
7. Auditoria automatica de staleness no painel (`Warning`) quando ficar sem atualizacao efetiva por mais de 5 minutos continuos.
8. Filtro rapido no `/admin/logs` para localizar incidentes `payment.reconciliation.panel.stale` sem busca manual.
9. Link contextual no painel operacional para abrir `/admin/logs` com filtro por `payment.reconciliation.panel.stale` e periodo padrao de 7 dias via querystring.
10. Parsing de overrides de querystring em `/admin/logs` extraido para helper testavel, cobrindo datas e filtros de auditoria.
11. Regra de precedencia entre filtros salvos e querystring formalizada e testada (querystring vence no primeiro carregamento).
12. Montagem do deep-link de incidentes de staleness extraida para helper testado, garantindo consistencia do periodo padrao (7 dias).
13. Cobertura ampliada para bordas de precedencia parcial e persistencia do quick-filter `PaymentPanelStale` no estado de filtros.
14. Teste de integracao HTTP validando renderizacao do deep-link de staleness no `/admin/payments` com periodo padrao.
15. Ajuste de ciclo de vida no `/admin/logs` para aplicar querystring no primeiro carregamento (SSR), com teste de integracao do filtro efetivo por periodo/evento.
16. Suite completa de testes validada com sucesso (412/412) apos alinhamento do baseline de gateways default no seed.
17. Teste de integracao E2E deterministico cobrindo confirmacao de vaga + webhook EfiBank + transicao para `HasPaid=true` (incluindo replay idempotente).
18. Suite completa revalidada apos o E2E final: 414/414 testes passando.
19. Contrato formal de estados de pagamento por confirmacao implementado no core (`Pending`, `Paid`, `Failed`, `Refunded`) com compatibilidade legada via `HasPaid`.
20. Painel `/admin/payments` ampliado com acao direta de timeline por confirmacao (`/admin/audit/Payment/{confirmationId}`) e exportacao CSV operacional de reconciliacao.
21. Suite completa revalidada apos timeline/export: 416/416 testes passando.
22. Telemetria operacional por gateway adicionada no painel `/admin/payments` (pendentes, pendentes antigos, pagas totais, share pendente e severidade por gateway).
23. Suite completa revalidada apos telemetria por gateway: 417/417 testes passando.
24. Regras finais de transicao de status aplicadas no core (`Pending->Paid/Failed`, `Failed->Pending/Paid`, `Paid->Refunded`) com auditoria por evento (`payment.failed`, `payment.refunded`, `payment.status.changed`).
25. Suite completa revalidada apos regras de cancelamento/reembolso auditaveis: 421/421 testes passando.
26. Runbook operacional de pagamentos/reconciliacao documentado em `docs/observability-payments-runbook.md`.
27. Regras de alerta e templates de monitoramento adicionados em `docs/payments-alert-rules.md` e `docs/monitoring/`.
28. `/admin/payments` consolidado como superficie principal de triagem operacional, com modal de ferramentas avancadas e sobreposicao responsiva corrigida.
29. Pivot de produto apos o sprint de pagamentos: grupos privados passaram a aceitar solicitacao de entrada direta nas telas de evento.
30. `/grupos` passou a priorizar pendencias administrativas com badge, chip de urgencia, filtro rapido e secao destacada de acao necessaria.

## Proximo passo apos o sprint

1. Tirar os alertas do papel: implantar thresholds/regras no stack de monitoramento do ambiente.
2. Fechar o fluxo admin de grupos privados com mais contexto e atalhos de aprovacao.
3. Iniciar indicadores operacionais de ocupacao/inadimplencia para orientar produto e cobranca.
