# Auditoria de Estrutura CSS — Ciclo 13

**Data:** 2026-07-06
**Motivo:** Após resolver o bug de background das telas de eventos, foi solicitada revisão completa da estrutura CSS.

---

## 1. Inventário

### CSS Global (`wwwroot/css/`)
| Arquivo | Linhas | Tamanho |
|---|---|---|
| `site.css` | 5.996 | 182 KB |
| `events.css` | 3.593 | 96.8 KB |
| `marketplace.css` | 452 | 12.6 KB |
| `identity.css` | 254 | 6.8 KB |

### Scoped CSS (`.razor.css`)
- **58 arquivos** no total
- **26 arquivos vazios** (criados no Ciclo 12 para eliminar warnings)

### Ordem de carregamento (`_Host.cshtml`)
1. `site.css` → 2. `marketplace.css` → 3. `identity.css` → 4. `events.css` → 5. `Confirmai.styles.css` (scoped bundle)

**Problema:** O bundle scoped carrega por último e sempre vince sobre globais com mesma especificidade.

---

## 2. Más Práticas — site.css (182 KB)

### P0 — Regras duplicadas do mesmo seletor (CRÍTICO)

Causador do bug de background. O mesmo seletor é definido múltiplas vezes:

| Seletor | Definições de `background` | Linhas |
|---|---|---|
| `body` | 3 | 488, 3010, 5167 |
| `body .oldsite-page` | 3 | 3332, 5177, 5672 |
| `.main-content` | 3 (2 global + 1 scoped) | 847, 3053, MainLayout.razor.css:183 |

**Impacto:** A última regra vince por ordem de origem. Mudar uma não adianta — foi necessário mudar **8 regras em 3 arquivos**.

**Recomendação:** Consolidar cada seletor em uma única definição.

### P1 — Scoped CSS sobrescrevendo globais (ALTO)

`MainLayout.razor.css` redefine seletores globais:

| Seletor scoped | Linha scoped | Linha global |
|---|---|---|
| `.main-content` | 180 | 844, 3046 |
| `.main-content::before` | 197 | 3058 |
| `.main-content.main-content-route-padded` | 201 | 854, 3070 |
| `.sidebar` | 51 | 509+ |

**Recomendação:** Mover regras estruturais para `site.css`. Scoped deve ter apenas estilos isolados.

### P2 — 26 arquivos `.razor.css` vazios (MÉDIO)

Dashboard, Error, Users, AdminGateways, AdminUserEdit, AdminPaymentsAdvancedToolsModal, AdminPaymentsFilters, EscalacaoConfirmed, EscalacaoVoting, ProfileLoadingStates, DateTimeSelector, DetailAdminPanel, DetailChipsRow, DetailEventHeader, DetailQuorumBar, EscalacaoControls, EscalacaoDraftTeamBuilder, EscalacaoShare, FutsalGoalkeeperGroup, FutsalMatchIdentity, FutsalOutfieldGroup, PriceInput, VenueSelector, PaymentCheckoutPanel, PaymentProductSummary, VenueEdit.

**Recomendação:** Remover ou adicionar comentário `/* intentionally empty */`.

### P2 — `!important` (11 ocorrências)

| Linha | Uso | Justificável? |
|---|---|---|
| 882 | `display: none` | Sim |
| 1254-1256 | `prefers-reduced-motion` | Sim |
| 6462 | `z-index: 9999` | Verificar |
| 6547-6553 | autofill do browser | Sim |

### P3 — Cores hardcoded fora de `:root` (2 ocorrências)

- Linha 5936: `#e0f7f4` (`.history-btn`)
- Linha 5949: `#ecfdf5` (`.success-btn`)

### P3 — `body .oldsite-main-content` com 70 ocorrências

Prefixo legado para aumentar especificidade sobre regras do projeto original (Tibia fansite).

---

## 3. events.css — Estado saudável

- 0 `!important` ✅
- 0 cores hardcoded ✅
- Usa consistentemente `var()` ✅

No Ciclo 13, movemos `.sports-page`, `.sports-shell`, `.listing-page`, `.listing-block` dos scoped CSS para cá (global) devido a issues de Blazor CSS isolation.

---

## 4. Hierarquia Visual do Background (pós-fix)

```
body                     → var(--ci-bg-card) #111927 (cinza — NIVEL 0)
  └─ .oldsite-page       → transparent
      └─ .oldsite-frame  → (sem bg)
          └─ .main-content → transparent
              └─ .sports-page / .groups-page → (sem bg, apenas layout)
                  └─ .sports-shell / .groups-block → gradient (NIVEL 1)
```

---

## 5. Recomendações Resumidas

| Prioridade | Ação |
|---|---|
| P0 | Consolidar regras duplicadas — cada seletor com uma única definição |
| P1 | Mover `.main-content` e `.sidebar` do scoped para global |
| P1 | Documentar padrão de mover CSS crítico para `events.css` |
| P2 | Limpar 26 `.razor.css` vazios |
| P2 | Auditar 11 `!important` |
| P3 | Mover 2 cores hardcoded para variáveis |
| P3 | Avaliar remoção do prefixo `body .oldsite-main-content` (70x) |
| P3 | Considerar split do `site.css` (182 KB) em arquivos menores |

---

## 7. Ajustes Adicionais — /eventos

### 7.1 Centralizar select de cidade + botão à direita

**Arquivo:** `Pages/Index.razor.css`

| Seletor | Antes | Depois |
|---|---|---|
| `.sports-tabs` | `justify-content: space-between` | `justify-content: center; position: relative` |
| `.city-selector` | `margin-left: auto` | `margin: 0 auto` |
| `.my-events-link-btn` | inline, sem posicionamento | `position: absolute; right: 0` |

A barra `.sports-tabs` centraliza o city selector e posiciona o botão "Meus Eventos" absolutamente à direita. No mobile (`max-width: 768px`), ambos voltam a `position: static` e `flex-direction: column`.

### 7.2 Suavizar coloração do botão Meus Eventos

**Arquivo:** `Pages/Index.razor.css`

O botão antes usava gradient azul brilhante (`var(--ci-accent)` → `var(--ci-accent-mid)`) com texto claro, destoando do resto da tela. Agora usa o mesmo padrão dos outros botões da página:

| Propriedade | Antes | Depois |
|---|---|---|
| `background` | `linear-gradient(accent → accent-mid)` | `var(--ci-bg-card-deep)` |
| `color` | `var(--ci-surface-light)` (claro) | `var(--ci-accent)` (azul texto) |
| `border` | `1px solid var(--ci-accent-light)` | `1px solid var(--ci-border)` |
| `box-shadow` | `accent-opacity-md` | `shadow-md` |
| `text-shadow` | sim | removido |
| hover `background` | gradient mais brilhante | `var(--ci-bg-card)` |
| hover `border` | `var(--link-info)` | `var(--ci-accent)` |

### 7.3 Gradient nos subcards de esporte

**Arquivo:** `Shared/Components/SportCard.razor.css`

O `.sport-card-body` antes tinha `background` sólido. Agora cada esporte tem seu próprio gradient com 3 paradas para melhor transição:

| Esporte | Gradient |
|---|---|
| Base (genérico) | `linear-gradient(180deg, var(--ci-bg-card) 0%, var(--ci-bg) 100%)` |
| Futsal | `linear-gradient(180deg, var(--futsal-green-dark) #14532d 0%, var(--futsal-bg-dark) #0a2018 50%, var(--ci-bg) 100%)` |
| Poker | `linear-gradient(180deg, var(--poker-bg-dark) #4c1d95 0%, var(--poker-bg-deepest) #3b0764 50%, var(--ci-bg) 100%)` |

**Ajuste de intensidade:** O futsal antes usava apenas `--futsal-bg-dark` (#0a2018, quase preto) como cor inicial, por isso o gradient era quase invisível. Agora começa com `--futsal-green-dark` (#14532d, verde visível) e faz transição por `--futsal-bg-dark` no meio. O poker ganhou uma parada intermediária com `--poker-bg-deepest` (#3b0764) para suavizar a transição.

---

## 8. Padronização de Background N2 e Legibilidade — Ciclo 13 (cont.)

**Data:** 2026-07-07
**Motivo:** Aplicar o padrão de background N2 (gradient `--ci-bg-alt` → `--ci-bg`) em todas as telas de detalhe/pagamento e corrigir legibilidade de textos contra fundos escuros.

### 8.1 Padrão N2 de Background

O padrão adotado para o segundo nível de background (N2) em todas as telas é:

```css
background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
border: 1px solid var(--ci-border);
border-radius: 14px;
box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
```

**Telas aplicadas:**

| Tela | Seletor | Arquivo | Antes |
|---|---|---|---|
| `/eventos` | `.sports-shell` | `events.css` | Já estava aplicado |
| `/futsal`, `/poker` | `.listing-block` | `events.css` | Já estava aplicado |
| `/futsal/{id}` | `.detail-card` | `events.css:155` | `background: var(--ci-bg-card)` (sólido, igual ao N1) |
| `/pagamento/evento/{id}` | `.evpay-main` | `EventPayment.razor.css:9` | Sem background (herdava `var(--ci-bg-card)` do global `body .entity-shell-card`) |

### 8.2 Legibilidade — Tela de Detalhe `/futsal/{id}`

**Arquivo:** `wwwroot/css/events.css`

| Seletor | Antes | Depois | Motivo |
|---|---|---|---|
| `.detail-group-ref` | `--ci-text-subtle` (#6082a0) | `--ci-text-muted` (#8aacc8) | Baixo contraste |
| `.detail-localname` | `--ci-text-muted` (#8aacc8) | `--ci-text-blue` (#deeeff) | Contraste marginal |
| `.detail-location` | `--ci-text-muted` (#8aacc8) | `--ci-text-blue` (#deeeff) | Contraste marginal |
| `.detail-organizer` | `--ci-text-subtle` (#6082a0) | `--ci-text-muted` (#8aacc8) | Baixo contraste |
| `.detail-chip` | `--ci-text-muted` (#8aacc8) | `--ci-text-blue` (#deeeff) | Contraste marginal |
| `.detail-map-placeholder-name` | `--slate-light` (#e2e8f0) | `--ci-text` (#f1f5f9) | Padronização |
| `.detail-map-placeholder-addr` | `--ci-text-subtle` (#6082a0) | `--ci-text-muted` (#8aacc8) | Baixo contraste |

### 8.3 Legibilidade — Tela de Pagamento `/pagamento/evento/{id}`

Múltiplos componentes usavam `--slate-muted` (#94a3b8) e `--ci-text-subtle` (#6082a0) com contraste insuficiente.

**`EventPayment.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-admin-proof-label` | `--slate-muted` | `--ci-text-muted` |
| `.evpay-admin-no-proof` | `--slate-muted` | `--ci-text-muted` |
| `.evpay-admin-review .detail-back-link` | `--slate-muted` | `--ci-text-muted` |
| `.evpay-status--paid p` | `--green-soft` | `--green-bright` |

**`EventPaymentHeader.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-meta` | `--ci-text-muted` | `--ci-text-blue` |

**`EventPaymentSummary.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-summary-label` | `--ci-text-muted` | `--ci-text-blue` |

**`EventPaymentQr.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-brcode` | `--ci-surface-muted` (#cbd5e1) | `--ci-text-blue` (#deeeff) |

**`EventPaymentGateways.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-disclaimer` | `--ci-text-subtle` | `--ci-text-muted` |
| `.evpay-method-badge--soon` | `--slate-muted` | `--ci-text-muted` |

**`EventPaymentPixAdmin.razor.css`:**

| Seletor | Antes | Depois |
|---|---|---|
| `.evpay-admin-pix-hint` | `--slate-muted` | `--ci-text-muted` |
| `.evpay-admin-pix-no-key` | `--slate-muted` | `--ci-text-muted` |

### 8.4 Redução do QR Code (~30%)

**Arquivo:** `EventPaymentQr.razor.css` e `EventPaymentPixAdmin.razor.css`

| Seletor | Antes | Depois | Redução |
|---|---|---|---|
| `.evpay-qr-image` (container) | `width: min(170px, 90%)` | `width: 120px` | ~29% |
| `.evpay-admin-pix-qr` (container) | sem width fixo | `width: 80px; height: 80px` | ~27% |

**Abordagem:** O tamanho é controlado pelo container (`.evpay-qr-image` / `.evpay-admin-pix-qr`), e a imagem interna usa `width: 100%; height: 100%` para preencher o container. Isso evita `!important` e respeita a cascata natural.

**Nota técnica sobre CSS scoped Blazor:** O `QRCode.razor.css` tem `.qr-code-image { width: 100% }` (scoped, vira `.qr-code-image[b-abc]`, especificidade 0,2,0). O seletor descendente `.evpay-qr-image :global(img)` vira `.evpay-qr-image[b-xyz] img` (0,2,1) — maior especificidade, vence sem `!important`. Para o PixAdmin, `.evpay-admin-pix-qr :global(.qr-code-image)` vira `.evpay-admin-pix-qr[b-xyz] .qr-code-image` (0,3,0) — também vence.

### 8.5 Background N2 — Tela de Pagamento (abordagem global)

**Arquivo:** `wwwroot/css/events.css:168`

A abordagem inicial tentou usar scoped CSS (`.evpay-main` no `EventPayment.razor.css`), mas falhou porque o Blazor hot reload não recompila scoped CSS sem um build completo. A solução correta foi mover a regra para `events.css` (global), onde já vivem os outros padrões N2:

```css
body .entity-shell-card.evpay-main {
    background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
    border: 1px solid var(--ci-border);
    border-top: 1px solid var(--ci-border);
    border-radius: 14px;
    box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
}
```

**Especificidade:** `body .entity-shell-card.evpay-main` = (0,2,1), que vence `body .entity-shell-card` = (0,1,1) do `site.css:5454`. Como `events.css` carrega após `site.css`, a ordem de origem também favorece a regra.

### 8.6 Alinhamento de Conteúdo — `/futsal`

**Arquivo:** `EventListingShell.razor.css`

| Seletor | Antes | Depois | Motivo |
|---|---|---|---|
| `.event-listing` | `max-width: 760px` | `max-width: 100%` | Conteúdo (location, date-selector, cards) ocupava apenas 760px dentro do `.listing-block` (860px), ficando desalinhado com o botão "Criar Partida" que usa 860px |

### 8.7 Badge "Confirmado" — `/futsal`

**Arquivo:** `Futsal/Index.razor.css`

| Seletor | Antes | Depois | Motivo |
|---|---|---|---|
| `.meta-chip--confirmed` color | `--green-soft` (#86efac) | `--futsal-green-dark` (#14532d) | Verde claro sobre verde claro = ilegível |

### 8.8 Hover do Botão "Confirmar" — `/futsal`

**Arquivo:** `Futsal/Index.razor.css`

| Seletor | Antes | Depois | Motivo |
|---|---|---|---|
| `.event-action:hover` color | `--green-soft` (#86efac) | `--futsal-green-dark` (#14532d) | Verde claro sobre verde claro = ilegível |

---

## 9. Reorganização de Layout e Novas Funcionalidades — Ciclo 13 (cont.)

**Data:** 2026-07-07
**Motivo:** Reorganização da tela de grupo, extração de partidas para página própria, ajustes de QR code, comprovante, e criação de grupo.

### 9.1 QR Code — Tela de Pagamento (ajustes finais)

**Arquivos:** `EventPaymentQr.razor.css`, `EventPaymentPixAdmin.razor.css`

Após múltiplas iterações com o usuário, os tamanhos finais do QR code foram definidos:

| Seletor | Valor final | Observação |
|---|---|---|
| `.evpay-qr-image` (container gateway) | `width: 384px` | Centralizado com `margin: 0 auto` |
| `.evpay-admin-pix-qr` (container PixAdmin) | `width: 260px; height: 260px` | Centralizado com `margin: 0.75rem auto` |

**Abordagem técnica:** O tamanho é controlado pelo container. A imagem interna usa `width: 100%; height: 100%` para preencher o container. Sem `!important`. A cascata funciona naturalmente pois `.evpay-qr-image[b-xyz] img` (0,2,1) > `.qr-code-image[b-abc]` (0,2,0).

### 9.2 Comprovante Centralizado — Tela de Pagamento

**Arquivo:** `EventPayment.razor.css`

| Seletor | Mudança | Motivo |
|---|---|---|
| `.evpay-admin-proof` | Adicionado `text-align: center` | Centralizar comprovante na exibição para confirmação |
| `.evpay-admin-proof-img` | Adicionado `display: inline-block` | Permitir centralização via `text-align` do parent |

### 9.3 Fallback de Nome de Usuário — Tela de Pagamento

**Arquivo:** `EventPayment.razor`

4 ocorrências de `@conf.User?.FullName` substituídas por `@(conf.User?.FullName ?? conf.User?.UserName ?? "Jogador")`, seguindo o padrão já usado no resto do codebase (DelinquencyService, EventNotificationService, etc.).

**Localizações:**
- Linha 65: "Pagamento de {nome} foi confirmado"
- Linha 82: "Revisão de pagamento — {nome}"
- Linha 97: "Confirmar pagamento de {nome}?"
- Linha 128: "Marcar {nome} como pago?"

### 9.4 Reorganização da Tela de Grupo `/grupo/{id}`

**Arquivos:** `Detail.razor`, `Detail.razor.cs`

**Mudança 1 — Inversão de ordem:** Métricas agora aparecem **antes** de Membros (antes era o inverso).

**Mudança 2 — Extração de Partidas:** A seção de partidas (tabs Próximas/Realizadas, tabela de eventos, botão "Nova partida") foi removida do `Detail.razor` e movida para uma nova página dedicada `/grupo/{id}/partidas`.

**Mudança 3 — Botão "Partidas":** Adicionado botão na seção de actions de Membros, ao lado de Ranking e Pagamentos, com link para a nova página.

**Limpeza do code-behind (`Detail.razor.cs`):**
- Removidos: `recentEvents`, `upcomingEvents`, `pastEvents`, `eventNumbers`, `displayedEvents`, `eventsYear`, `showPastEvents`, `MembersPreviewLimit`
- Removida query de events no `LoadGroup()`
- Removidos do razor: `newEventHref`, `orderedMembers`, `hasHiddenMembers`, `visibleMembers`, `@inject UiTextService`

### 9.5 Nova Página: `/grupo/{id}/partidas`

**Arquivos criados:**
- `Pages/Groups/Partidas.razor` — Página Blazor com rota `/grupo/{Id:int}/partidas`
- `Pages/Groups/Partidas.razor.cs` — Code-behind com carga de events (upcoming/past), event numbers, tabs
- `Pages/Groups/Partidas.razor.css` — Scoped CSS (espaçamento do título)

**Estrutura da página:**
1. Header com sport badge, back link, título "Partidas — {nome do grupo}"
2. Título "Partidas {ano}" com espaçamento (`margin-bottom: 1rem`)
3. Aviso "Partidas geradas automaticamente (semanal)" (quando aplicável)
4. Tabs: Próximas / Realizadas + botão "Nova partida" (admin)
5. Tabela de eventos (`GroupDetailEvents` component)

**Componente `GroupDetailEvents` modificado:** Removido o aviso de partidas recorrentes do componente (já que só era usado nesta página) — agora o aviso é renderizado pelo parent, permitindo controle de ordem.

**Padrão seguido:** Mesma estrutura de Ranking e Payments (header com back link, `detail-card`, `detail-section`).

### 9.6 Botão "Partidas" — Estilo

**Arquivo:** `wwwroot/css/events.css`

Adicionado `.detail-admin-btn--partidas` com mesmas dimensões de Ranking/Pagamentos:

```css
.detail-admin-btn--partidas {
    border-color: var(--ci-accent-mid);
    color: var(--ci-text-link-bright);
    padding: 0.54rem 1.05rem;
    border-radius: 10px;
    font-size: 0.84rem;
    font-weight: 700;
}
```

Cor azul padrão do sistema (`--ci-accent-mid` / `--ci-text-link-bright`), diferenciando dos botões verde (Pagamentos) e âmbar (Ranking).

### 9.7 Botão "Criar Grupo" — Listagem Futsal

**Arquivo:** `Futsal/Index.razor`

| Antes | Depois | Motivo |
|---|---|---|
| `<a href="/futsal/create">Criar Partida</a>` | `<a href="/grupos/criar">Criar Grupo</a>` | Não se pode criar partida sem grupo |

### 9.8 Tela de Criação de Grupo — Info Box

**Arquivos:** `Groups/Create.razor`, `Groups/Create.razor.css`

Adicionada box informativa antes da seção "Identidade do Grupo":

```html
<div class="create-group-info-box">
    <i class="fas fa-info-circle"></i>
    <span>Crie um grupo e convide jogadores para participarem das partidas!</span>
</div>
```

**CSS:** Box com fundo azul translúcido (`rgba(79, 156, 248, 0.08)`), borda azul, texto centralizado, ícone `info-circle`.

### 9.9 Título de Criação Centralizado — Global

**Arquivo:** `wwwroot/css/events.css`

`.create-event-title` ganhou `justify-content: center` como default global (antes era apenas `align-items: center` sem justify). Aplica-se a todas as telas de criação (grupo, futsal, poker).

---

## 10. Resumo para PR

### Escopo
Melhorias de layout, legibilidade, e reorganização de telas de grupo e pagamento.

### Arquivos modificados

| Arquivo | Tipo | Mudança |
|---|---|---|
| `wwwroot/css/events.css` | Global CSS | N2 background payment, botão partidas, título centralizado |
| `wwwroot/css/site.css` | Global CSS | (sem mudanças neste ciclo) |
| `Pages/Payment/EventPayment.razor` | Razor | Fallback de nome, 4 ocorrências |
| `Pages/Payment/EventPayment.razor.css` | Scoped CSS | Comprovante centralizado, legibilidade |
| `Pages/Payment/Components/EventPaymentQr.razor.css` | Scoped CSS | QR code 384px, container-controlled |
| `Pages/Payment/Components/EventPaymentPixAdmin.razor.css` | Scoped CSS | QR code 260px, container-controlled |
| `Pages/Payment/Components/EventPaymentGateways.razor.css` | Scoped CSS | Legibilidade |
| `Pages/Payment/Components/EventPaymentHeader.razor.css` | Scoped CSS | Legibilidade |
| `Pages/Payment/Components/EventPaymentSummary.razor.css` | Scoped CSS | Legibilidade |
| `Pages/Groups/Detail.razor` | Razor | Inversão métricas/membros, remoção partidas, botão partidas |
| `Pages/Groups/Detail.razor.cs` | Code-behind | Limpeza de campos não usados |
| `Pages/Groups/Partidas.razor` | **Novo** | Página de partidas do grupo |
| `Pages/Groups/Partidas.razor.cs` | **Novo** | Code-behind da página de partidas |
| `Pages/Groups/Partidas.razor.css` | **Novo** | CSS scoped (espaçamento título) |
| `Pages/Groups/Create.razor` | Razor | Info box, texto |
| `Pages/Groups/Create.razor.css` | Scoped CSS | Info box styling |
| `Pages/Futsal/Index.razor` | Razor | Botão "Criar Grupo" |
| `Shared/Components/Groups/GroupDetailEvents.razor` | Component | Removido aviso recorrente (movido para parent) |
| `docs/ciclo13-auditoria-css.md` | Docs | Esta documentação |

### Arquivos não modificados
- Testes (regra: nunca modificar testes)
- `site.css` (mudanças de background foram para `events.css` por especificidade)

### Breaking changes
Nenhum. A rota `/futsal/create` ainda existe (acessível via nova partida na tela de partidas do grupo). A rota `/grupo/{id}/partidas` é nova.

### Verificação
- Build: 0 erros de compilação (CS)
- Hot reload: mudanças scoped CSS requerem `dotnet build` + Ctrl+Shift+R
- Mudanças globais (events.css): aplicam-se imediatamente após refresh
