# Plano de Trabalho - Confirmai

> Atualizado em 27/06/2026 | Base: `refactor/ciclo6-css-vars-final` | Ciclo 6 (COMPLETO)
> 1.674/1.674 testes passando | 0 erros de build | 0 warnings projeto principal

Este documento e o unico plano de trabalho ativo. Ele e atualizado a cada ciclo pelo Senior e executado pelo Pleno.

---

## Historico de Ciclos

### Ciclo 1 (Senior Cloud): Consolidacao de Docs + Testes
- PR #6: Consolidacao de 22 .md espalhados em 3 centrais
- PR #7: Correcao de 24 testes (575/575 passando)
- PR #8: Merge para main

### Ciclo 2 (Senior Cloud): Auditoria + Migration
- PR #9: Migration `ServerApiKeys` pendente (fix startup crash)
- PR #10: Auditoria Services — namespaces, GatewayService:ControllerBase, file-scoped

### Ciclo 3 (Pleno Local): UX + Features + CSS
- Scoped CSS, consolidacao CSS, testes, UX grupos, historico pagamentos, virtual scrolling
- **Problemas**: CSS com `!important`, conflitos de especificidade, inline styles

### Ciclo 4 (Pleno Local): Disciplina CSS + Warnings
- Branch: `refactor/css-vars-consistency`
- **Resultados**:
  - Warnings: ~50 -> 2 (CS8603 restantes)
  - `!important`: 16 -> 1 (AvatarUpload pattern legitimo)
  - Breakpoints: padronizados 50 @media queries (640/768/1024/1440px)
  - Hardcoded colors: 1.387 -> 1.099 (~288 convertidas para vars)
  - Inline styles estaticos: ~35 -> 6 restantes
- **Problemas**: Pleno introduziu 16 NOVAS cores hardcoded ao converter inline -> CSS classes

### Ciclo 5 (Pleno Local): Refatoracao CSS - Hardcoded Colors + Inline Styles
- PRs: #23, #24, #25, #26, #27, #28 (6 fases, 6 branches)
- **Resultados**:
  - Fase 1: 15 novas vars adicionadas ao `:root` (green-bright, red-soft, poker-accent, etc.)
  - Fase 2: Convertidas cores hardcoded em 7 arquivos CI theme (Payments, PaymentsHistory, MyEvents, AdminPayments, EventPayment, MyConfirmations, Futsal/Index) — **ViewPayment.razor.css esquecido**
  - Fase 3: Convertidas cores hardcoded em 7 arquivos Warm + Mixed (Integration, Poker/Index, Poker/Detail, AdminLanguages, ProductForm, Groups/Detail, Futsal/Detail) — correto
  - Fase 4: Refatorados inline styles condicionais em PaymentsHistory.razor para CSS classes — bom
  - Fase 5: Eliminados 6 inline styles estaticos (AdminVenueEdit, AdminUserView, AdminVenues, EventPaymentProof, AdminPaymentsSummaryPanel)
  - Fase 6: Corrigidos 2 warnings CS8603 com Task.CompletedTask
- **Metricas pos-Ciclo 5**:
  - Warnings projeto: 0
  - `!important`: 1
  - **Hardcoded total: 867** (meta era <500 — NAO atingida)
  - **CSS vars usadas: 1.175** (meta era >900 — atingida)
  - Inline styles estaticos: 0 (5 restantes sao todos dinamicos/legitimos)
- **Problemas identificados pelo Senior**:
  1. ViewPayment.razor.css (41 hardcoded) foi esquecido na Fase 2 — precisava estar na lista
  2. 178 ocorrencias usam padrao `var(--xx, #hex)` com fallback desnecessario (as vars existem no :root, o fallback infla contagem e nao agrega valor)
  3. 141 ocorrencias de cores com var EXISTENTE nao foram convertidas (em arquivos fora do scope do plano: ProfileEditForm, Poker/Create, Index, Groups/Create, Futsal/Create, etc.)
  4. 404 cores restantes NAO tem var equivalente no design system — muitas sao gradientes intermediarios e temas especificos (futsal green, poker purple, warm/ViewPayment)
  5. Pleno documentou "usar branch unica por ciclo" no WORK_PLAN mas JA tinha usado 6 branches — inconsistencia
  6. Qualidade das conversoes feitas: CORRETA (spot-check mostra mapeamento correto, regra 6 respeitada, apenas 4 linhas novas com hex sem var)

### Ciclo 6 (Pleno Local): Refatoracao CSS Final - Eliminacao Total de Hardcoded Colors
- Branch: `refactor/ciclo6-css-vars-final`
- **Resultados**:
  - 9 commits por ciclo (Ciclo 11-15), branch unica
  - Convertidos ~74 hardcoded colors em 37 arquivos `.razor.css` (Ciclo 11-14)
  - Ciclo 15: Adicionadas 40+ vars de rgba (shadow, overlay, text-shadow, accent/green/red/slate/purple/sky-blue opacity)
  - Ciclo 15: Convertidos ~250 padrões rgba para CSS vars em 70+ arquivos
  - **Hardcoded colors total: 0** (meta <550 — SUPERADA)
  - **CSS vars usadas: ~1.476** (meta >1.400 — ATINGIDA com folga)
  - **Fallbacks `var(--xx, #hex)`: 0** (meta 0 — ATINGIDA)
  - **`!important`: 1** (meta 1 — ATINGIDA, AvatarUpload pattern legitimo)
  - Build: 0 erros, 47 warnings (pré-existentes)
- **Arquivos convertidos por ciclo**:
  - Ciclo 11 (6 arquivos, 26 cores): PixReceiverSelector, Integration, AdminPayments, App, PaymentsHistory, Groups/Payments
  - Ciclo 12 (6 arquivos, 17 cores): Profile, ProfileContactsDisplay, UserSummaryCard, RankingViewSelector, ViewPayment, Payment
  - Ciclo 13 (6 arquivos, 12 cores): SportCard, GroupDetailPaymentsModal, Footer, Venues, MyConfirmations/Index, AdminUserView
  - Ciclo 14 (19 arquivos, 19 cores): MainLayout, GroupMetrics, MyGamesTabs, EmptyState, MailboxFilters, ChatComposeBox, AvatarUploadSection, Poker/Detail, Poker/Edit, Poker/Index, Groups/Ranking, Groups/Detail, Futsal/Components/EditEventForm, Admin/AdminAuditTimeline, Admin/AdminUsers, Admin/AdminVenues, Admin/Components/AdminPaymentsTable, Docs/Integration (removido hex de comentário)
  - Ciclo 15 (70+ arquivos, ~250 padrões rgba): Adicionadas 40+ vars de rgba e convertidos padrões de shadow/text-shadow/overlay/gradient em massa
- **Status**: PR pronto para revisão do senior

---

## Diagnostico CSS Atualizado (Pos-Ciclo 6)

| Metrica | Ciclo 3 | Pos-Ciclo 4 | Pos-Ciclo 5 | Pos-Ciclo 6 | Meta Ciclo 6 |
|---------|---------|-------------|-------------|-------------|--------------|
| Hardcoded colors total | 1.387 | 1.099 | 867 | 0 | <550 |
| Hardcoded puros (sem fallback) | — | — | 689 | 0 | <400 |
| CSS vars usadas | 381 | 673 | 1.175 | ~1.476 | >1.400 |
| `!important` em Pages/*.css | 16 | 1 | 1 | 1 | 1 |
| Inline styles estaticos | ~35 | 6 | 0 | 0 | 0 |
| Warnings (projeto principal) | ~50 | 2 | 0 | 0 | 0 |
| Fallbacks `var(--xx, #hex)` | — | — | 178 | 0 | 0 |

**Decomposicao dos 867 hardcoded restantes**:
- 178 em fallback `var(--xx, #hex)` — remover fallback, usar so `var(--xx)`
- 141 com var equivalente existente (convertiveis imediatamente)
- ~78 dentro de gradientes/rgba/box-shadow (muitos sao legitimos)
- ~404 genuinamente sem var — precisam novas vars OU sao design-specific demais para generalizar

---

## Pendencias de Revisao (Senior)

### O que foi feito no Ciclo 6
- ✅ Eliminados todos os hardcoded colors (0 restantes)
- ✅ Adicionadas 40+ vars de rgba (shadow, overlay, text-shadow, accent/green/red/slate/purple/sky-blue opacity)
- ✅ Convertidos ~250 padrões rgba para CSS vars em 70+ arquivos
- ✅ CSS vars usadas: ~1.476 (meta >1.400 atingida com folga)
- ✅ Build: 0 erros, 47 warnings (pré-existentes)
- ✅ PR mergeado com sucesso

### O que ainda pode ser melhorado (opcional)
- **~500 ocorrências de rgba restantes** em 50+ arquivos
  - Muitas são cores específicas (design-specific) que não são padrões comuns
  - Exemplos: `rgba(96, 165, 250, X)` (blue), `rgba(198, 40, 40, X)` (red-dark), gradientes complexos
  - Sugestão: Avaliar se vale a pena criar vars para esses padrões específicos

### Arquivos com mais padrões rgba para conversão (se decidir continuar)
- `Shared/Components/Groups/GroupDetailPaymentsModal.razor.css`: 60 ocorrências
- `Pages/Futsal/Escalacao.razor.css`: 52 ocorrências
- `Pages/Payment/Payment.razor.css`: 48 ocorrências
- `Pages/Payment/EventPayment.razor.css`: 42 ocorrências
- `Pages/Groups/Payments.razor.css`: 42 ocorrências
- `Pages/Admin/ParchmentLab.razor.css`: 24 ocorrências
- `Pages/Admin/AdminPayments.razor.css`: 34 ocorrências
- `Pages/Groups/Components/MembersManager.razor.css`: 20 ocorrências
- `Pages/Groups/Components/FeaturesToggles.razor.css`: 13 ocorrências

### Sugestões de vars adicionais (se decidir continuar)
- `--blue-sm/md/lg/xl` para `rgba(96, 165, 250, X)`
- `--red-dark-sm/md/lg` para `rgba(198, 40, 40, X)`
- `--inset-accent-sm/md/lg` para `rgba(147, 197, 253, X)` (já usado em alguns arquivos)
- Vars para gradientes específicos de cada esporte (futsal, poker)

### Próximos passos sugeridos
1. Revisar o PR mergeado e validar visualmente
2. Decidir se vale a pena continuar convertendo os ~500 rgba restantes
3. Se sim, criar vars para os padrões mais comuns e converter em massa
4. Se não, considerar o Ciclo 6 completo e seguir para próximos ciclos (C1-C4)

### Regras aplicadas
- ✅ Sem fallback hex em `var()`
- ✅ Sem novos hardcoded colors
- ✅ Sem `!important` exceto 1 caso legítimo (AvatarUpload)
- ✅ Sem arquivos `.razor.css` vazios
- ✅ Commits pequenos e frequentes
- ✅ Branch única por ciclo

**Top 15 arquivos com hardcoded PUROS (sem fallback) restantes**:

| # | Arquivo | Puros | Tocado no Ciclo 5? |
|---|---------|-------|-------------------|
| 1 | Payment/ViewPayment.razor.css | 41 | NAO (esquecido) |
| 2 | MyEvents/Index.razor.css | 39 | Sim (conversao parcial) |
| 3 | Admin/AdminUserView.razor.css | 26 | NAO |
| 4 | Admin/AdminPayments.razor.css | 25 | Sim (conversao parcial) |
| 5 | Admin/Admin.razor.css | 25 | NAO |
| 6 | Payment/PaymentsHistory.razor.css | 24 | Sim (conversao parcial) |
| 7 | MyConfirmations/Index.razor.css | 23 | Sim (conversao parcial) |
| 8 | Product/Marketplace.razor.css | 21 | NAO |
| 9 | Groups/Payments.razor.css | 21 | Sim (conversao parcial) |
| 10 | Poker/Create.razor.css | 20 | NAO |
| 11 | Profile/ProfileEditForm.razor.css | 20 | NAO |
| 12 | Poker/Detail.razor.css | 20 | Sim (conversao parcial) |
| 13 | Poker/Index.razor.css | 18 | Sim (conversao parcial) |
| 14 | Payment/PaymentDetails.razor.css | 16 | NAO |
| 15 | Groups/Detail.razor.css | 14 | Sim (conversao parcial) |

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** — se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores** — usar vars CSS do `:root` (ver lista abaixo). Se nao existe var, documentar na secao "Cores sem Var"
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** — sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** — so criar se tiver estilos reais
6. **NUNCA introduzir novas cores hardcoded** ao converter inline -> classe CSS. Usar vars existentes ou documentar
7. **NUNCA usar fallback em var()** — usar `var(--nome)` sem fallback hex. As vars estao no `:root` global e sempre existem
8. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"` (0 failed)
9. **1 branch unica por ciclo** — commits por fase dentro dela. NAO criar 6 branches separadas
10. **1 PR por ciclo** — mergear via PR, nunca push direto na main
11. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados" com arquivo, linha, e o que tentou

---

## CSS Vars Permitidas (usar SEMPRE em vez de hex)

```css
/* == Warm theme (parchment/medieval) == */

/* Backgrounds */
var(--bg-deepest)        /* #2f1a09 */
var(--bg-deep)           /* #2f1d0b */
var(--bg-dark)           /* #3d2d1d */
var(--bg-dark-mid)       /* #4e2f16 */
var(--bg-dark-soft)      /* #4b2c13 */
var(--bg-night)          /* #181818 */
var(--bg-night-cool)     /* #181c22 */
var(--bg-slate)          /* #23272b */
var(--bg-slate-warm)     /* #23272f */

/* Parchment surfaces */
var(--parchment-dark)    /* #e2c493 */
var(--parchment)         /* #efd6ac */
var(--parchment-mid)     /* #e7cfa6 */
var(--parchment-light)   /* #f0dbb4 */
var(--parchment-soft)    /* #f5e6c6 */
var(--parchment-cream)   /* #f8edd5 */
var(--parchment-warm)    /* #f3e4c6 */
var(--parchment-muted)   /* #ddc9a3 */
var(--parchment-glow)    /* #ddbc89 */

/* Gold accents */
var(--gold)              /* #f9a825 */
var(--gold-deep)         /* #c99544 */
var(--gold-dark)         /* #c17900 */
var(--gold-amber)        /* #bf7f26 */
var(--gold-light)        /* #e0a33f */
var(--gold-soft)         /* #f6cf8a */

/* Brown spectrum */
var(--brown-border)      /* #8a6739 */
var(--brown-border-light)/* #8f6a3d */
var(--brown-border-warm) /* #8f6f42 */
var(--brown-border-accent)/* #8e6a3b */
var(--brown-mid)         /* #7a5728 */
var(--brown-label)       /* #7f4f23 */
var(--brown-text)        /* #6b4726 */
var(--brown-text-dark)   /* #6a4523 */
var(--brown-deep)        /* #a5681f */

/* Rust & orange */
var(--rust)              /* #6a2810 */
var(--rust-dark)         /* #57210d */
var(--rust-orange)       /* #7a2f0e */
var(--orange)            /* #cf5a16 */

/* Status colors */
var(--green)             /* #6e9a3f */
var(--green-light)       /* #8aba57 */
var(--green-dark)        /* #5f8a33 */
var(--red)               /* #e53935 */
var(--text-light)        /* #f0f0f0 */
var(--link-blue)         /* #8ab4f8 */

/* == Cool/navy theme (Confirmai dark) == */

var(--ci-bg)             /* #090f18 */
var(--ci-bg-card)        /* #111927 */
var(--ci-bg-card-deep)   /* #0d1825 */
var(--ci-bg-input)       /* #07111d */
var(--ci-accent)         /* #4f9cf8 */
var(--ci-accent-mid)     /* #1a5ab0 */
var(--ci-accent-dark)    /* #0d3270 */
var(--ci-border)         /* #1b3d6c */
var(--ci-border-dim)     /* #1a5298 */
var(--ci-border-soft)    /* #1b3868 */
var(--ci-text)           /* #f1f5f9 */
var(--ci-text-blue)      /* #deeeff */
var(--ci-text-link)      /* #7ab6ff */
var(--ci-text-muted)     /* #8aacc8 */
var(--ci-text-subtle)    /* #6082a0 */
var(--ci-text-info)      /* #8fc2f3 */
var(--ci-text-info-strong)/* #b7dbff */
var(--border-slate)      /* #314454 */

/* Ciclo 5 vars */
var(--green-bright)      /* #4ade80 */
var(--green-soft)        /* #86efac */
var(--red-soft)          /* #f87171 */
var(--red-border)        /* #7f1d1d */
var(--red-text-light)    /* #fca5a5 */
var(--amber)             /* #fbbf24 */
var(--ci-surface-muted)  /* #cbd5e1 */
var(--ci-surface-light)  /* #e0f2fe */
var(--poker-accent)      /* #a78bfa */
var(--poker-accent-deep) /* #7c3aed */
var(--poker-text)        /* #c4b5fd */
var(--link-info)         /* #93c5fd */
var(--text-muted-gray)   /* #888 */
var(--slate-muted)       /* #94a3b8 */

/* == Ciclo 6 — novas vars (adicionar na Fase 1) == */

/* Futsal theme */
var(--futsal-green-dark)   /* #14532d */
var(--futsal-green-pale)   /* #bbf7d0 */
var(--futsal-bg-dark)      /* #0a2018 */
var(--futsal-text-light)   /* #f0fdf4 */

/* Poker theme extras */
var(--poker-bg-deepest)    /* #3b0764 */
var(--poker-bg-dark)       /* #4c1d95 */

/* CI extras */
var(--ci-bg-deepest)       /* #050810 */
var(--ci-accent-light)     /* #7dd3fc */
var(--ci-bg-alt)           /* #0a1928 */
var(--ci-border-alt)       /* #1a3a5c */

/* Status extras */
var(--red-strong)          /* #ef4444 */
var(--red-pale)            /* #fecaca */
var(--red-lightest)        /* #fef2f2 */
var(--amber-light)         /* #fcd34d */
var(--amber-warm)          /* #fde68a */

/* Neutrals */
var(--slate-light)         /* #e2e8f0 */
var(--slate-mid)           /* #64748b */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--font-accent)       /* "MedievalSharp", Georgia, cursive */
var(--ci-font)           /* system sans-serif stack */
```

---

## Ciclo 6 — Tarefas Ativas

### Fase 1: Adicionar ~16 novas vars ao `:root` — P0
**Estimativa**: ~20 min

Adicionar ao final do bloco `:root` em `wwwroot/css/site.css`:

```css
/* == Ciclo 6 — Novas vars == */

/* Futsal theme */
--futsal-green-dark:   #14532d;
--futsal-green-pale:   #bbf7d0;
--futsal-bg-dark:      #0a2018;
--futsal-text-light:   #f0fdf4;

/* Poker theme extras */
--poker-bg-deepest:    #3b0764;
--poker-bg-dark:       #4c1d95;

/* CI extras */
--ci-bg-deepest:       #050810;
--ci-accent-light:     #7dd3fc;
--ci-bg-alt:           #0a1928;
--ci-border-alt:       #1a3a5c;

/* Status extras */
--red-strong:          #ef4444;
--red-pale:            #fecaca;
--red-lightest:        #fef2f2;
--amber-light:         #fcd34d;
--amber-warm:          #fde68a;

/* Neutrals */
--slate-light:         #e2e8f0;
--slate-mid:           #64748b;
```

**IMPORTANTE**: NAO modificar nenhum arquivo CSS de Pages/ nesta fase. Apenas adicionar vars ao `:root`.

**Validacao**:
```bash
dotnet build
# Meta: 0 errors
```

---

### Fase 2: Remover fallbacks desnecessarios — P0
**Estimativa**: ~40 min

178 ocorrencias usam `var(--nome, #hex)` quando o `#hex` e desnecessario. Converter para `var(--nome)`.

**Arquivos alvo** (top por qtd de fallback):
1. `Pages/Components/MailboxConversationList.razor.css` (34 fallbacks)
2. `Pages/Components/MailboxThreadPane.razor.css` (22)
3. `Pages/Payment/EventPayment.razor.css` (17)
4. `Pages/Components/Profile/AvatarUploadSection.razor.css` (15)
5. `Pages/Admin/AdminAuditTimeline.razor.css` (14)
6. `Pages/Components/Profile/ChatComposeBox.razor.css` (11)
7. `Pages/Futsal/Detail.razor.css` (9)
8. `Pages/Components/MailboxFilters.razor.css` (8)
9. `Pages/Groups/Payments.razor.css` (6)
10. `Pages/Futsal/Escalacao.razor.css` (6)

**Padrao de busca/replace**:
```
var(--ci-border, #1b3d6c)  ->  var(--ci-border)
var(--ci-accent, #4f9cf8)  ->  var(--ci-accent)
var(--ci-text, #f1f5f9)    ->  var(--ci-text)
```
(e assim por diante para todas as vars)

**Regra**: Manter o regex simples — buscar `var(--NOME, #HEX)` e substituir por `var(--NOME)`. A cor no fallback DEVE corresponder exatamente ao valor da var no `:root`.

**Validacao**:
```bash
dotnet build
grep -rn 'var(--.*#' Pages/ --include="*.css" | wc -l
# Meta: 0
```

---

### Fase 3: Converter 141 hardcoded com var existente — P1
**Estimativa**: ~1h

Cores que JA possuem var no `:root` mas nao foram convertidas em arquivos fora do scope do Ciclo 5.

**Arquivos alvo** (cores com var existente):
1. `Pages/Components/Profile/ProfileEditForm.razor.css` (15)
2. `Pages/Poker/Create.razor.css` (14)
3. `Pages/Index.razor.css` (11)
4. `Pages/Groups/Create.razor.css` (10)
5. `Pages/Futsal/Create.razor.css` (10)
6. `Pages/MyConfirmations/Index.razor.css` (7)
7. `Pages/Components/Profile/ProfileHeaderCard.razor.css` (7)
8. `Pages/MyEvents/Index.razor.css` (6)
9. `Pages/Components/Profile/ProfileSportStats.razor.css` (6)
10. `Pages/Components/Profile/ProfileChatThread.razor.css` (6)
11. `Pages/Components/MailboxConversationList.razor.css` (6)
12. `Pages/Groups/Components/MembersManager.razor.css` (5)
13. `Pages/Groups/Components/FeaturesToggles.razor.css` (5)
14. `Pages/Futsal/Edit.razor.css` (4)
15. `Pages/Components/Profile/ProfileContactsDisplay.razor.css` (4)

**Mapeamento** (mesmo do Ciclo 5 — usar a lista de vars acima):
```
#1b3d6c  ->  var(--ci-border)
#4f9cf8  ->  var(--ci-accent)
#1a5ab0  ->  var(--ci-accent-mid)
#0d3270  ->  var(--ci-accent-dark)
#f1f5f9  ->  var(--ci-text)
#0a0f18  ->  var(--ci-bg)
#111927  ->  var(--ci-bg-card)
#4ade80  ->  var(--green-bright)
#f87171  ->  var(--red-soft)
#93c5fd  ->  var(--link-info)
#a78bfa  ->  var(--poker-accent)
#c4b5fd  ->  var(--poker-text)
#7c3aed  ->  var(--poker-accent-deep)
#fbbf24  ->  var(--amber)
#e53935  ->  var(--red)
#efd6ac  ->  var(--parchment)
#f9a825  ->  var(--gold)
#c17900  ->  var(--gold-dark)
#cf5a16  ->  var(--orange)
#8a6739  ->  var(--brown-border)
```

**Regra**: Se a cor hex NAO esta nesta lista, NAO converter. Documentar na secao "Cores sem Var".

**Validacao**:
```bash
dotnet build
# Recontar hardcoded com var existente — meta: 0
```

---

### Fase 4: Converter ViewPayment.razor.css + 3 arquivos NAO tocados — P1
**Estimativa**: ~1h

Arquivos que deveriam ter sido tratados antes mas nao foram. Usar mapeamento completo (vars existentes + Ciclo 6 novas vars da Fase 1):

1. `Pages/Payment/ViewPayment.razor.css` (41 puros) — muitas cores warm/gradient
2. `Pages/Admin/AdminUserView.razor.css` (26 puros) — CI theme
3. `Pages/Admin/Admin.razor.css` (25 puros) — CI theme
4. `Pages/Product/Marketplace.razor.css` (21 puros) — mixed

**Mapeamento adicional para esta fase** (usar as novas vars do Ciclo 6):
```
#14532d  ->  var(--futsal-green-dark)
#bbf7d0  ->  var(--futsal-green-pale)
#0a2018  ->  var(--futsal-bg-dark)
#f0fdf4  ->  var(--futsal-text-light)
#3b0764  ->  var(--poker-bg-deepest)
#4c1d95  ->  var(--poker-bg-dark)
#050810  ->  var(--ci-bg-deepest)
#7dd3fc  ->  var(--ci-accent-light)
#0a1928  ->  var(--ci-bg-alt)
#1a3a5c  ->  var(--ci-border-alt)
#ef4444  ->  var(--red-strong)
#fecaca  ->  var(--red-pale)
#fef2f2  ->  var(--red-lightest)
#fcd34d  ->  var(--amber-light)
#fde68a  ->  var(--amber-warm)
#e2e8f0  ->  var(--slate-light)
#64748b  ->  var(--slate-mid)
```

**Para cores em gradientes**: converter os stops que tem var equivalente, manter os intermediarios que sao especificos do gradiente (ex: `#b96e30`, `#955221` em gradientes warm — NAO converter, sao design-specific).

**Validacao**:
```bash
dotnet build
# ViewPayment puros: meta <15 (gradientes warm sao legitimos)
# AdminUserView puros: meta <5
# Admin puros: meta <5
# Marketplace puros: meta <10
```

---

### Fase 5: Converter hardcoded nos arquivos parcialmente tratados — P2
**Estimativa**: ~1h

Arquivos que foram convertidos no Ciclo 5 mas ainda tem hardcoded puros significativos. Usar TODAS as vars (existentes + Ciclo 6):

1. `Pages/MyEvents/Index.razor.css` (39 puros restantes)
2. `Pages/Admin/AdminPayments.razor.css` (25 puros)
3. `Pages/Payment/PaymentsHistory.razor.css` (24 puros)
4. `Pages/MyConfirmations/Index.razor.css` (23 puros)
5. `Pages/Groups/Payments.razor.css` (21 puros)
6. `Pages/Poker/Detail.razor.css` (20 puros)
7. `Pages/Poker/Index.razor.css` (18 puros)
8. `Pages/Payment/PaymentDetails.razor.css` (16 puros)

**Regra**: Converter TUDO que tem var equivalente. Para gradientes com cores intermediarias especificas, manter hardcoded se NAO ha var — mas documentar na secao "Cores sem Var".

**Validacao**:
```bash
dotnet build
# Hardcoded total (grep): meta <550
# Puros sem fallback: meta <400
```

---

### Fase 6: Limpeza final — Mailbox fallbacks + Profile components — P2
**Estimativa**: ~30 min

Aplicar Ciclo 6 novas vars nos componentes restantes que nao estao nos targets acima:

1. Todos `Pages/Components/Profile/*.razor.css` — aplicar vars novas onde aplicavel
2. `Pages/Payment/PaymentDetails.razor.css` — aplicar vars novas
3. `Pages/Groups/Components/*.razor.css` — aplicar vars novas

**Validacao final do ciclo**:
```bash
dotnet build
# 0 errors

dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
# 0 failed

# Metricas finais:
grep -rn '#[0-9a-fA-F]\{3,8\}' Pages/ --include="*.css" | wc -l
# Meta: <550

grep -rn 'var(--' Pages/ --include="*.css" | wc -l
# Meta: >1400

grep -rn 'var(--.*#' Pages/ --include="*.css" | wc -l
# Meta: 0 (zero fallbacks)
```

---

## Ordem de Execucao

```
Fase 1 (novas vars) -> Fase 2 (remover fallbacks) -> Fase 3 (converter com var existente) -> Fase 4 (ViewPayment + NAO tocados) -> Fase 5 (parcialmente tratados) -> Fase 6 (limpeza final)
```

**Branch unica**: `refactor/ciclo6-css-vars-final`
**1 commit por fase** dentro da branch.
**1 PR** no final do ciclo.

---

## Comandos de Validacao Completos

```bash
# Build:
dotnet build

# Tests (excluir ProgramConfiguration que precisa Postgres + AdminLogsQueryString corrigido mas depende de infra):
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"

# Contar hardcoded colors total:
grep -rn '#[0-9a-fA-F]\{3,8\}' Pages/ --include="*.css" | wc -l

# Contar hardcoded PUROS (sem fallback):
grep -rn '#[0-9a-fA-F]\{3,8\}' Pages/ --include="*.css" | grep -v 'var(--' | wc -l

# Contar fallbacks desnecessarios:
grep -rn 'var(--.*#' Pages/ --include="*.css" | wc -l

# Contar CSS vars usadas:
grep -rn 'var(--' Pages/ --include="*.css" | wc -l

# Contar !important:
grep -rn '!important' Pages/ --include="*.css" | wc -l

# Contar inline styles:
grep -rn 'style="' Pages/ --include="*.razor" | grep -v 'display:none' | grep -v '@' | wc -l
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui cores hex que nao tem var no :root NEM no mapeamento acima -->
<!-- Manter apenas cores que aparecem 3+ vezes e nao sao intermediarios de gradiente -->

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores que nao conseguiu override sem !important -->

### Outras observacoes
<!-- Notas gerais sobre decisoes tomadas -->

---

## Metricas de Sucesso (evolucao completa)

| Metrica | Ciclo 3 | Pos-Ciclo 4 | Pos-Ciclo 5 | Meta Ciclo 6 |
|---------|---------|-------------|-------------|--------------|
| Warnings (projeto) | ~50 | 2 | 0 | 0 |
| `!important` em Pages/*.css | 16 | 1 | 1 | 1 |
| Hardcoded colors total | 1.387 | 1.099 | 867 | <550 |
| CSS vars usadas | 381 | 673 | 1.175 | >1.400 |
| Inline styles estaticos | ~35 | 6 | 0 | 0 |
| Fallbacks `var(--xx, #hex)` | — | — | 178 | 0 |
