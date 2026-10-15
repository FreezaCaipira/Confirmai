# Métricas de produto (C42-B)

Hipóteses de produto registradas para o go-live e onde cada uma é medida.
Os dados vêm da tabela `Logs` (auditoria persistida) — o painel
`/admin/metrics` **não depende de OTLP** estar configurado.

## Hipóteses

| # | Hipótese | Evento(s) / fonte | Janela |
|---|----------|-------------------|--------|
| H1 | >= 90% dos logins via Google | `user.login.success` (`metadata.method`: `google`/`password`) | 7d / 30d |
| H2 | >= 60% das aberturas de convite viram membro | `group.invite.opened` -> `group.member.joined` | 7d / 30d |
| H3 | >= 70% das partidas com cobrança 100% pagas | `Events` (Price > 0, já iniciadas) sem confirmação não-paga | 30d |
| H4 | >= 80% das páginas vistas em mobile | `page.view` (`metadata.device`: `mobile`/`desktop`) | 7d / 30d |
| H5 | Mediana criar -> lotar < 24h | **não instrumentado** — exige evento de "lotou" | — |
| H6 | p95 do render de página < 1,5s | histograma OTel `confirmai_page_render_ms` (tag `route`) | — |

H5 ficou de fora de propósito: medir "tempo até lotar" exige derivar o
instante em que a última vaga foi preenchida — possível, mas caro por
consulta e melhor servido por um evento dedicado num ciclo futuro.

## Eventos novos

| EventType | Emissor | EntityId | MetadataJson |
|-----------|---------|----------|--------------|
| `user.login.success` | `Login` / `ExternalLogin` | UserId | `{ method: "password"\|"google" }` |
| `user.registered` | `Register` / `ExternalLogin` | UserId | `{ method: "password"\|"google" }` |
| `group.invite.opened` | `Join.razor` (`/convite/{code}`) | GroupId quando válido | `{ valid, loggedIn }` — **sem o código** |
| `group.member.joined` | `GroupDetailService` | GroupId | `{ via: "invite"\|"approval" }` |
| `page.view` | `PageViewTracker` (MainLayout) | template de rota | `{ route, device, renderMs }` |

## Regras de privacidade do `page.view`

- `Message` e `EntityId` carregam o **template de rota** produzido por
  `PageRoutes.Normalize` — segmentos numéricos viram `{id}`, códigos viram
  `{code}`, emails viram `{email}`, GUIDs viram `{guid}`.
- Nunca persistimos URL crua, query string, IP (além do enriquecimento
  padrão do `LogService`, que o `LogRetentionService` anonimiza em 30 dias)
  nem o código do convite.
- Debounce: o mesmo template dentro do mesmo circuito grava uma vez.
- Amostragem: `Metrics:PageViewSampleRate` (0.0–1.0, default `1.0`).
  Só afeta a linha persistida; o histograma OTel registra sempre.

## Auditoria (furos fechados no C42-B)

| Furo (passo 3c do plano) | Evento | Emissor |
|---|---|---|
| Confirmar presença | `event.confirmation.added` / `event.waitlist.joined` | `EventDetailService.ConfirmPresenceAsync`, `PokerDetailService.ConfirmAsync` |
| Poker criar/editar/mesa/recarimbo | `event.created`, `event.updated`, `event.confirmation.table.changed`, `event.confirmation.restamped` | `PokerCreateService`, `Pages/Poker/Edit`, `PokerDetailService.ChangeTableAsync` |
| Promoção da espera / vagas | `event.waitlist.promoted`, `event.capacity.changed` | `EventDetailService.PromoteFromWaitlistAsync`, edits + `AdminRemoveGoalkeeperSlotAsync` |
| Placar e voto de MVP | `event.score.registered`, `event.vote.cast` | `EscalacaoService.SaveScoreAsync` / `CastVoteAsync` |
| Política de segurança | `admin.security_policy.changed` (antes/depois no metadata) | `AdminSecurityPolicyService` |
| Trilha de auditoria vista | `admin.audit.viewed` | `AdminAuditTimeline` |
| Edição do grupo embutida no edit de partida | `group.updated` | `Pages/Futsal/Edit`, `Pages/Poker/Edit` |

Toda auditoria nova é gravada **depois** do `SaveChangesAsync` de negócio —
o `AuditAsync` escreve em `DbContext` próprio e só registra fato confirmado.

## Retenção

- `page.view` e demais logs operacionais: **90 dias** (regra já existente do
  `LogRetentionService`, agora com teste dedicado).
- IPs: anonimizados após 30 dias.
- Eventos financeiros (`payment.*`, `order.*`) e de auditoria: **nunca**
  expiram.

## Banco

Migration `Ciclo42B_AppLogEventTimestampIndex`: índice composto
`(EventType, Timestamp)` — todas as consultas do painel filtram por esses
dois campos.
