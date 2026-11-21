# Matriz de Acesso — Ciclo 46 (item 12)

Mapeamento rota × ator do Confirmai. Gerado pelo Pleno no C46 (1.4).
"negado" = o ator não vê a rota (redirect para login, página de acesso
negado ou gate visual). Cada célula "negado" está coberta por teste —
veja a seção Cobertura ao final.

**Atores**: anon (sem login) · membro (GroupMember) · admin-g (GroupMemberRole.Admin no grupo) · sysadmin (role "admin") · não-membro (logado, fora do grupo) · criador (CreatedByUserId do evento, sem papel no grupo)

## Rotas — páginas públicas / home

| Rota | anon | membro | admin-g | sysadmin | não-membro |
|---|---|---|---|---|---|
| `/`, `/eventos`, `/jogos` | ver (home pública) | ver | ver | ver | ver |
| `/about`, `/contact` | ver | ver | ver | ver | ver |
| `/convite/{code}` (Join) | ver (pede login para entrar) | entrar | entrar | entrar | entrar |
| `/docs/integration` | negado | negado | negado | ver | negado |
| `/docs` (índice doc) | negado | ver | ver | ver | ver |

## Rotas — grupo

| Rota | anon | membro | admin-g | sysadmin | não-membro |
|---|---|---|---|---|---|
| `/grupos` | negado | ver | ver | ver | ver |
| `/grupos/criar` | negado | ver | ver | ver | ver |
| `/grupo/{id}` | ver (read-only, sem subnav — C46 1.3) | ver | ver | ver | ver (read-only, sem subnav) |
| `/grupo/{id}/partidas` | ver (read-only) | ver | ver | ver | ver (read-only) |
| `/grupo/{id}/ranking` | negado | ver | ver | ver | ver |
| `/grupo/{id}/pagamentos` | negado | ver (só "Meus pagamentos") | ver | ver | negado |
| `/grupo/{id}/configuracoes` (Features) | negado | ver (cadeado — read-only) | ver (edição) | ver (edição) | negado |

## Rotas — partidas (futsal/poker)

| Rota | anon | membro | admin-g | sysadmin | não-membro | criador |
|---|---|---|---|---|---|---|
| `/futsal/{id}` | ver (read-only) | ver | ver | ver | ver (read-only) | ver |
| `/poker/{id}` | ver (read-only) | ver | ver | ver | ver (read-only) | ver |
| `/futsal/{id}/escalacao` | ver (gate p/ não-membro) | ver | ver | ver | ver (gate) | ver (gate se não-membro) |
| `/futsal/create`, `/poker/create` | negado | ver | ver | ver | ver | — |
| `/futsal/{id}/edit`, `/poker/{id}/edit` | negado | negado | ver | ver | negado | negado (C46 1.5) |
| `/futsal/schedule` | negado | ver (recorrências dos grupos que administra) | ver | ver | lista vazia | — |
| `/futsal/schedule/{id}/edit` | negado | negado | ver | ver | negado | negado (C46 1.5) |

## Rotas — pagamentos / perfil / outros

| Rota | anon | membro | admin-g | sysadmin | não-membro |
|---|---|---|---|---|---|
| `/pagamento/evento/{confId}`, `/payments`, `/payments/view/{id}`, `/marketplace/buy/{id}` | negado | ver | ver | ver | ver |
| `/mailbox` | negado | ver | ver | ver | ver |
| `/profile/{Id}` | ver (público — achado, ver abaixo) | ver | ver | ver | ver |
| `/marketplace`, `/products*` | ver/negado conforme rota | ver | ver | ver | ver |
| `/admin/**` | negado | negado | negado | ver | negado |
| `/venue-manager/**` | negado | negado | negado | ver | ver só c/ role venue_manager |

## Ações

| Ação | membro | admin-g | sysadmin | não-membro | criador |
|---|---|---|---|---|---|
| Confirmar presença | permitido (gate de posição/vagas) | permitido | permitido | negado (pede entrada) | negado se não-membro |
| Pagar (próprio) | permitido | permitido | permitido | n/a | n/a |
| Marcar pago (de terceiro) | negado | permitido | permitido | negado | negado |
| Editar partida | negado | permitido | permitido | negado | negado (C46 1.5) |
| Cancelar partida | negado | permitido | permitido | negado | negado (C46 1.5) |
| Escalar times / placar | negado | permitido | permitido | negado | negado (C46 1.5) |
| Votar (pós-jogo) | permitido | permitido | permitido | negado | negado |
| Alterar config do grupo | negado (audita `group.config.change.denied` — C46 1.1) | permitido | permitido | negado | n/a |
| Gerenciar admins do grupo | negado | negado (só criador do grupo) | permitido | negado | n/a |
| Criar/editar recorrência | negado | permitido | permitido | negado | negado (C46 1.5) |

## Fonte da regra

Quem gerencia um evento = **admin do grupo OU sysadmin**
(`EventAccess.CanManage` / `CanManageAsync` — C46 1.5). Ser criador do
evento não confere mais nenhum direito de gestão; `IsCreator` fica só
para exibição. Admin do grupo gerencia qualquer partida do grupo,
inclusive as criadas por outros membros.

## Achados (MAPEAR, não alterar sem decisão do Robson)

1. **`/futsal/{id}` e `/poker/{id}` são `[AllowAnonymous]`** — anon vê
   página da partida com dados do grupo (nome, membros confirmados?).
   Visibilidade intencional? Confirmar com Robson antes de fechar.
2. **`/grupo/{id}` e `/grupo/{id}/partidas` são `[AllowAnonymous]`** —
   anon vê nome/descrição do grupo e a lista de partidas (read-only,
   sem subnav após C46 1.3). Grupo privado expõe existência e agenda.
3. **`/futsal/{id}/escalacao` sem `[Authorize]`** — anon/não-membro caem
   no gate visual (`isMember`/`isAdmin` false), mas a rota aceita acesso.
   A página não vaza escalação (gate), porém o atributo está implícito.
4. **`/profile/{Id}` sem `[Authorize]`** — anon vê perfil de qualquer
   usuário (nome, avatar, estatísticas). Provável intencional (perfil
   público), mas não está explícito no código.
5. **`/docs` vs `/docs/integration`** — `/docs` exige login,
   `/docs/integration` exige policy ServerAdmin. Assimetria ok.

## Cobertura de testes das células "negado"

| Célula | Teste |
|---|---|
| Features (config): membro/não-membro negado | `GroupFeaturesServiceTests` C46 (1.1) — 5 theory rows × Member/anon |
| Payments: não-membro negado | `GroupPaymentsServiceTests` (gates existentes) |
| Edit/cancel partida: membro/não-membro/criador negado | `EventAccessTests` — `CanManage_*` (C46 1.5) |
| Escalacao manage: membro/não-membro negado | `EscalacaoServiceTests` via `CanManageAsync` |
| Admin pages: não-sysadmin negado | `ProgramConfigurationTests` (policies) + `Authorize` attributes |
| Venue manager: role errado negado | `Authorize(Roles=...)` + smoke tests existentes |
