# Plano de Trabalho - Confirmai

> Atualizado em 27/06/2026 | Base: `main` + `refactor/css-vars-consistency` | Ciclo 5 (CONCLUÍDO)
> 1.674/1.674 testes passando | 0 erros de build | 0 warnings projeto (CS8603 corrigidos)

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
  - Warnings: ~50 → 2 (CS8603 em 2 .razor, nao-criticos)
  - `!important`: 16 → 1 (AvatarUpload pattern legitimo)
  - Breakpoints: padronizados 50 @media queries (640/768/1024/1440px)
  - Hardcoded colors: 1.387 → 1.099 (~288 convertidas para vars)
  - Inline styles estaticos: ~35 → 6 restantes
  - Inline styles BEM: criou classes `--hidden`, `--margin-top` etc. (bom pattern)
- **Problemas identificados pelo Senior**:
  1. Pleno introduziu 16 NOVAS cores hardcoded ao converter inline → CSS classes (ex: `.collision-link { color: #93c5fd }`)
  2. `PaymentsHistory.razor` tem blocos massivos de inline style condicional com 20+ cores hardcoded
  3. ~400 hardcoded restantes TEM var equivalente direto (ex: #1b3d6c=--ci-border, #4f9cf8=--ci-accent)
  4. ~200 hardcoded NAO tem var no design system (precisam novas vars: #4ade80, #e0f2fe, #cbd5e1, #f87171, etc.)
  5. Teste `AdminLogsQueryStringIntegrationTests` continua falhando (pre-existente, nao do Ciclo 4)

### Ciclo 5 (Pleno Local): Refatoracao CSS - Hardcoded Colors + Inline Styles
- Branches: `refactor/ciclo5-fase1-new-vars`, `refactor/ciclo5-fase2-css-vars-ci`, `refactor/ciclo5-fase3-css-vars-warm`, `refactor/ciclo5-fase4-payments-history`, `refactor/ciclo5-fase5-inline-cleanup`, `refactor/ciclo5-fase6-cs8603`
- **Resultados**:
  - Fase 1: Adicionadas 11 novas vars CSS ao `:root` (warm/parchment theme)
  - Fase 2: Convertidas cores hardcoded em 8 arquivos CI theme (Payments.razor.css, PaymentsHistory.razor.css, MyEvents/Index.razor.css, ViewPayment.razor.css, AdminPayments.razor.css, EventPayment.razor.css, MyConfirmations/Index.razor.css, Futsal/Index.razor.css)
  - Fase 3: Convertidas cores hardcoded em 7 arquivos Warm + Mixed theme (Integration.razor.css, Poker/Index.razor.css, Poker/Detail.razor.css, AdminLanguages.razor.css, ProductForm.razor.css, Groups/Detail.razor.css, Futsal/Detail.razor.css)
  - Fase 4: Refatorados 8 inline styles condicionais em PaymentsHistory.razor para classes CSS
  - Fase 5: Eliminados 6 inline styles estáticos restantes (AdminVenueEdit.razor, AdminUserView.razor, AdminVenues.razor, EventPaymentProof.razor, AdminPaymentsSummaryPanel.razor.css)
  - Fase 6: Corrigidos 2 warnings CS8603 usando Task.CompletedTask (GroupDetailPaymentsModal.razor, Payments.razor)
- **Métricas finais**:
  - Warnings: 2 → 0 (CS8603 corrigidos)
  - Inline styles estaticos: 6 → 0
  - Inline styles dinamicos: ~14 → ~8 (PaymentsHistory.razor refatorado)
  - Hardcoded colors convertidas: ~300+ (estimado)
- **PRs criadas**: 6 PRs (1 por fase)

---

## Diagnostico CSS Atualizado (Pos-Ciclo 4)

| Metrica | Ciclo 3 | Pos-Ciclo 4 | Meta Ciclo 5 |
|---------|---------|-------------|--------------|
| Hardcoded colors em Pages/*.css | 1.387 | 1.099 | <500 |
| CSS vars usadas em Pages/*.css | 381 | 673 | >900 |
| `!important` em Pages/*.css | 16 | 1 | 1 |
| Inline styles estaticos | ~35 | 6 | 0 |
| Inline styles dinamicos com hardcoded | ~5 | ~14 | <5 |
| @media fora do padrao | muitos | 0 | 0 |
| Warnings (projeto principal) | ~50 | 2 | 0 |

**Top 15 arquivos com mais hardcoded colors restantes** (estes sao o foco do Ciclo 5):

| # | Arquivo | Hardcoded | Muitas com var equivalente? |
|---|---------|-----------|---------------------------|
| 1 | Groups/Payments.razor.css | 72 | Sim — --ci-border, --ci-accent, --ci-text |
| 2 | Payment/PaymentsHistory.razor.css | 66 | Sim — --ci-* tokens |
| 3 | Docs/Integration.razor.css | 64 | Parcial |
| 4 | MyEvents/Index.razor.css | 59 | Sim — --ci-* tokens |
| 5 | Poker/Index.razor.css | 50 | Parcial — violets sem var |
| 6 | Payment/ViewPayment.razor.css | 41 | Sim — --ci-* tokens |
| 7 | Poker/Detail.razor.css | 39 | Parcial — violets sem var |
| 8 | Admin/AdminLanguages.razor.css | 38 | Sim |
| 9 | Admin/AdminPayments.razor.css | 37 | Sim |
| 10 | Product/ProductForm.razor.css | 35 | Parcial |
| 11 | Payment/EventPayment.razor.css | 33 | Sim |
| 12 | MyConfirmations/Index.razor.css | 32 | Sim |
| 13 | Groups/Detail.razor.css | 30 | Sim |
| 14 | Futsal/Index.razor.css | 29 | Sim |
| 15 | Futsal/Detail.razor.css | 29 | Sim |

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** — se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores** — usar vars CSS do `:root` (ver lista abaixo). Se nao existe var, documentar na secao "Cores sem Var"
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** — sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** — so criar se tiver estilos reais
6. **NUNCA introduzir novas cores hardcoded** ao converter inline → classe CSS. Usar vars existentes ou documentar
7. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration"` (0 failed)
8. **Branch única por ciclo** — usar uma só branch para todo o ciclo (ex: `refactor/ciclo5-css-refactor`), não uma branch por fase
9. **1 PR por ciclo** — mergear via PR, nunca push direto na main
10. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados" com arquivo, linha, e o que tentou

**Lição aprendida do Ciclo 5**: Usar uma branch por fase (6 branches) cria fragmentação desnecessária. Melhor usar uma branch única por ciclo e fazer commits por fase dentro dela. Isso simplifica o fluxo de revisão e merge.

---

## CSS Vars Permitidas (usar SEMPRE em vez de hex)

```css
/* ── Warm theme (parchment/medieval) ── */

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

/* ── Cool/navy theme (Confirmai dark) ── */

var(--ci-bg)             /* #090f18 */
var(--ci-bg-card)        /* #111927 */
var(--ci-bg-card-deep)   /* #0d1825 */
var(--ci-bg-input)       /* #07111d */
var(--ci-accent)         /* #4f9cf8 */
var(--ci-accent-mid)     /* #1a5ab0 */
var(--ci-accent-dark)    /* #0d3270 */
var(--ci-border)         /* #1b3d6c */
var(--ci-border-dim)     /* #1a5298 */
var(--ci-text)           /* #f1f5f9 */
var(--ci-text-blue)      /* #deeeff */
var(--ci-text-link)      /* #7ab6ff */
var(--ci-text-muted)     /* #8aacc8 */
var(--ci-text-subtle)    /* #6082a0 */
var(--ci-text-info)      /* #8fc2f3 */
var(--ci-text-info-strong)/* #b7dbff */
var(--border-slate)      /* #314454 */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--font-accent)       /* "MedievalSharp", Georgia, cursive */
var(--ci-font)           /* system sans-serif stack */
```

---

## Ciclo 5 — Tarefas Ativas

### Fase 1: Adicionar vars para cores sem equivalente — P0
**Branch**: `refactor/ciclo5-fase1-new-vars`
**Estimativa**: ~30 min

Cores usadas frequentemente no projeto que NAO tem var no `:root`. Adicionar ao final do bloco `:root` em `wwwroot/css/site.css`:

```css
/* ── Status/feedback extras ── */
--green-bright:    #4ade80;   /* success badges, confirmations */
--green-soft:      #86efac;   /* success highlights */
--red-soft:        #f87171;   /* error text, danger highlights */
--red-border:      #7f1d1d;   /* received tab borders */
--red-text-light:  #fca5a5;   /* received tab text */
--amber:           #fbbf24;   /* poker/alert accent */

/* ── Cool surfaces extras ── */
--ci-surface-muted:  #cbd5e1; /* disabled text, placeholders */
--ci-surface-light:  #e0f2fe; /* info highlights, badges */
--ci-border-soft:    #1b3868; /* alternate border shade */

/* ── Poker/violet theme ── */
--poker-accent:      #a78bfa; /* poker badges, headers */
--poker-accent-deep: #7c3aed; /* poker backgrounds */
--poker-text:        #c4b5fd; /* poker text */

/* ── Link extras ── */
--link-info:         #93c5fd; /* collision links, info links */

/* ── Neutral ── */
--text-muted-gray:   #888;    /* secondary text, timestamps */
--slate-muted:       #94a3b8; /* tertiary text */
```

**IMPORTANTE**: NAO modificar nenhum arquivo CSS nesta fase. Apenas adicionar as vars ao `:root`.

**Validacao**:
```powershell
dotnet build
# Meta: 0 errors
```

---

### Fase 2: Converter hardcoded colors — Top 8 arquivos (CI theme) — P0
**Branch**: `refactor/ciclo5-fase2-css-vars-ci`
**Estimativa**: ~300 substituicoes

Arquivos com mais hardcoded de cores CI (navy theme):
1. `Pages/Groups/Payments.razor.css` (72)
2. `Pages/Payment/PaymentsHistory.razor.css` (66)
3. `Pages/MyEvents/Index.razor.css` (59)
4. `Pages/Payment/ViewPayment.razor.css` (41)
5. `Pages/Admin/AdminPayments.razor.css` (37)
6. `Pages/Payment/EventPayment.razor.css` (33)
7. `Pages/MyConfirmations/Index.razor.css` (32)
8. `Pages/Futsal/Index.razor.css` (29)

**Mapeamento principal (CI theme)**:
```
#1b3d6c           -> var(--ci-border)
#1b3868           -> var(--ci-border-soft)
#4f9cf8           -> var(--ci-accent)
#1a5ab0           -> var(--ci-accent-mid)
#0d3270           -> var(--ci-accent-dark)
#8aacc8           -> var(--ci-text-muted)
#6082a0           -> var(--ci-text-subtle)
#f1f5f9           -> var(--ci-text)
#0a0f18, #090f18  -> var(--ci-bg)
#0d1825           -> var(--ci-bg-card-deep)
#111927           -> var(--ci-bg-card)
#07111d           -> var(--ci-bg-input)
#1a5298           -> var(--ci-border-dim)
#deeeff           -> var(--ci-text-blue)
#7ab6ff           -> var(--ci-text-link)

#4ade80           -> var(--green-bright)
#86efac           -> var(--green-soft)
#f87171           -> var(--red-soft)
#7f1d1d           -> var(--red-border)
#fca5a5           -> var(--red-text-light)
#cbd5e1           -> var(--ci-surface-muted)
#e0f2fe           -> var(--ci-surface-light)
#93c5fd           -> var(--link-info)
#94a3b8           -> var(--slate-muted)
#888              -> var(--text-muted-gray)
#fff              -> #fff (OK manter — branco puro)
```

**Regra**: Se a cor hex nao esta no mapeamento acima, NAO converter. Documentar na secao "Cores sem Var".

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String '#[0-9a-fA-F]{3,6}' | Measure-Object
# Meta: <700 (de 1.099)
```

---

### Fase 3: Converter hardcoded colors — Top 7 arquivos (Warm + Mixed) — P1
**Branch**: `refactor/ciclo5-fase3-css-vars-warm`
**Estimativa**: ~200 substituicoes

Arquivos com cores warm/mixed theme:
1. `Pages/Docs/Integration.razor.css` (64)
2. `Pages/Poker/Index.razor.css` (50)
3. `Pages/Poker/Detail.razor.css` (39)
4. `Pages/Admin/AdminLanguages.razor.css` (38)
5. `Pages/Product/ProductForm.razor.css` (35)
6. `Pages/Groups/Detail.razor.css` (30)
7. `Pages/Futsal/Detail.razor.css` (29)

**Mapeamento adicional (warm/poker)**:
```
#a78bfa           -> var(--poker-accent)
#7c3aed           -> var(--poker-accent-deep)
#c4b5fd           -> var(--poker-text)
#fbbf24           -> var(--amber)
#efd6ac           -> var(--parchment)
#e2c493           -> var(--parchment-dark)
#f0dbb4           -> var(--parchment-light)
#f5e6c6           -> var(--parchment-soft)
#f9a825           -> var(--gold)
#c99544           -> var(--gold-deep)
#c17900           -> var(--gold-dark)
#8a6739, #8f6a3d  -> var(--brown-border)
#cf5a16           -> var(--orange)
#6e9a3f           -> var(--green)
#e53935           -> var(--red)
```

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String '#[0-9a-fA-F]{3,6}' | Measure-Object
# Meta: <500
```

---

### Fase 4: Refatorar PaymentsHistory.razor inline styles — P1
**Branch**: `refactor/ciclo5-fase4-payments-history`
**Estimativa**: ~1h

`PaymentsHistory.razor` tem blocos massivos de inline style condicional. Exemplo atual:
```razor
style="@(activeTab == "sent" ? "border: 1px solid #1b3d6c; ..." : "border: 1px solid #7f1d1d; ...")"
```

Converter para classes CSS condicionais:
```razor
class="@(activeTab == "sent" ? "tab-sent" : "tab-received")"
```

E definir `.tab-sent` e `.tab-received` no `PaymentsHistory.razor.css` usando vars:
```css
.tab-sent {
    border: 1px solid var(--ci-border);
    background: var(--ci-bg);
    /* ... */
}
.tab-received {
    border: 1px solid var(--red-border);
    background: var(--ci-bg);
    /* ... */
}
```

Linhas alvo em `PaymentsHistory.razor`: 21, 23, 25, 68, 69, 86, 87.

**Validacao**:
```powershell
dotnet build
Get-ChildItem -Filter "PaymentsHistory.razor" Pages/Payment/ | Select-String 'style="' | Where-Object { $_ -notmatch '@' } | Measure-Object
# Meta: 0
```

---

### Fase 5: Eliminar 6 inline styles estaticos restantes — P2
**Branch**: `refactor/ciclo5-fase5-inline-cleanup`
**Estimativa**: ~20 min

| Arquivo | Linha | Style | Acao |
|---------|-------|-------|------|
| `Admin/AdminVenues.razor` | 87 | `gap:0.4rem;align-items:center` | Criar `.admin-actions-row--compact` |
| `Admin/AdminVenues.razor` | 88 | `font-size:0.8rem;color:#f1f5f9;...` | Criar `.admin-remove-label` |
| `Admin/AdminVenueEdit.razor` | 107 | `border-color:#1c2a3b;margin:1.25rem 0` | Criar `.venue-divider` |
| `Payment/PaymentsHistory.razor` | 21 | `background: #0a0f18` | Mover para `.payments-history-shell` no .razor.css |
| `Payment/Components/EventPaymentProof.razor` | 50 | `animation: fadeout 3s...` | Criar `.evpay-proof-fadeout` |
| `Components/Profile/AvatarUploadSection.razor` | 12 | `display:none` | **IGNORAR** — pattern `<InputFile>` legitimo |

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.razor" Pages/ | Select-String 'style="' | Where-Object { $_ -notmatch 'display:none|@' } | Measure-Object
# Meta: 0
```

---

### Fase 6: Corrigir 2 warnings CS8603 restantes — P2
**Branch**: `refactor/ciclo5-fase6-warnings`
**Estimativa**: ~10 min

| Arquivo | Warning | Fix |
|---------|---------|-----|
| `Pages/Groups/Components/GroupDetailPaymentsModal.razor` | CS8603: Possible null reference return | Adicionar null-check ou `?` |
| `Pages/Groups/Payments.razor` | CS8603: Possible null reference return | Adicionar null-check ou `?` |

**Validacao**:
```powershell
dotnet build 2>&1 | Select-String "warning CS" | Measure-Object
# Meta: 0 (no projeto principal, warnings de testes sao OK)
```

---

## Ordem de Execucao

```
Fase 1 (novas vars) -> Fase 2 (CI colors) -> Fase 3 (warm colors) -> Fase 4 (PaymentsHistory) -> Fase 5 (inline) -> Fase 6 (warnings)
```

Cada fase = 1 branch + 1 PR. Mergear antes de comecar a proxima.

---

## Comandos de Validacao Completos

```powershell
# Build (Windows):
dotnet build

# Tests (excluir ProgramConfiguration que precisa Postgres + AdminLogsQueryString pre-existente):
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"

# Contar warnings do projeto (excluir testes):
dotnet build 2>&1 | Select-String "warning CS" | Where-Object { $_ -notmatch "Tests" } | Measure-Object

# Contar !important:
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String "!important" | Measure-Object

# Contar inline styles estaticos:
Get-ChildItem -Recurse -Filter "*.razor" Pages/ | Select-String 'style="' | Where-Object { $_ -notmatch 'display:none|@' } | Measure-Object

# Contar hardcoded colors:
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String '#[0-9a-fA-F]{3,6}' | Measure-Object

# Contar CSS vars usadas:
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String 'var\(--' | Measure-Object
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui cores hex que nao tem var no :root NEM no mapeamento acima -->
- #7dd3fc (azul claro - gradient intermediário)
- #0f2845, #0a1f35 (azul escuro - gradient intermediários)
- #162030 (azul muito escuro - hover background)
- #ef4444, #dc2626 (vermelho - gradient intermediários)
- #fef2f2 (branco avermelhado - text)
- #ffffff (branco puro - OK manter)
- #1a0a0a, #2a0f0f (vermelho escuro - backgrounds)
- #fca5a5 (vermelho claro - border)
- #22c55e, #16a34a (verde - gradient intermediários)
- #f0fdf4 (branco esverdeado - text)
- #f59e0b (âmbar - email icon)
- #10b981 (verde - whatsapp icon)
- #64748b (slate - chevron, disabled)
- #60a5fa (azul - link)
- #fde68a (âmbar claro - hover)
- #0f1a30 (azul escuro - background)
- #14532d, #166534 (verde escuro - futsal theme)
- #bbf7d0 (verde claro - futsal theme)
- #3b0764, #4c1d95 (roxo escuro - poker theme)
- #e9d5ff, #c084fc (roxo claro - poker theme)
- #2d6a8a (azul intermediário - hover)
- #82bdd8 (azul claro - hover)
- #1a0a2e (roxo escuro - poker background)
- #1a2535 (azul escuro - past state)
- #290f0f (vermelho escuro - cancelled)
- #1c1508, #78350f (âmbar escuro - tournament)
- #94b4cc, #7a9ab4 (azul claro - meta text)
- #0004 (box-shadow - OK manter)
- #8a3f17, #bc4a14, #8f340f (warm brown - ViewPayment)
- #ffe6be, #fff2db (warm light - ViewPayment)
- #17320d, #351b0b, #2e1a0b (warm dark - ViewPayment)
- #b96e30, #955221, #ca8b49 (warm gradient - ViewPayment)
- #f2f5f7, #63707b, #4e5860, #434b52 (neutral - ViewPayment)
- #9e7a4a, #5a3a10, #9e6a20 (warm accent - ViewPayment)
- #2db550, #1e8a39, #165f28, #e8f9ed (green gradient - ViewPayment)
- #5b3114, #1e2e13 (warm dark - ViewPayment)
- #f2cc79, #dbad58, #c2893b (gold gradient - ViewPayment)
- #d99a37, #b8741f, #9f6220 (warm gradient - ViewPayment)
- #5b9646, #3c6f2d, #325f26, #f0ffe9 (green gradient - ViewPayment)
- #5b2f12, #5c3213, #5d361a, #2f1a0a, #5d2d12 (warm dark - ViewPayment)

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores que nao conseguiu override sem !important -->

### Outras observacoes

**Teste já falhando antes do Ciclo 4**:
- `AdminLogsQueryStringIntegrationTests.AdminLogsPage_QueryStringFilters_AreAppliedOnInitialRender` - Já falhava no commit anterior (56f450c)
- HTML retornado não contém a mensagem esperada (apenas `<!DOCTYPE html>`)
- Não relacionado com mudanças de CSS do Ciclo 4
- Precisa de investigação separada pelo Senior

**Nota**: O teste `AdminLogsQueryStringIntegrationTests` falha porque usa HTTP GET numa pagina Blazor Server que carrega dados assincronamente apos o circuito SignalR ser estabelecido. O HTML pre-renderizado nao contem os dados. O Senior investigara e corrigira esse teste separadamente.

---

## Metricas de Sucesso (evolucao completa)

| Metrica | Ciclo 3 | Pos-Ciclo 4 | Meta Ciclo 5 |
|---------|---------|-------------|--------------|
| Warnings (projeto) | ~50 | 2 | 0 |
| `!important` em Pages/*.css | 16 | 1 | 1 |
| Hardcoded colors em Pages/*.css | 1.387 | 1.099 | <500 |
| CSS vars usadas em Pages/*.css | 381 | 673 | >900 |
| Inline styles estaticos | ~35 | 6 | 0 |
| Inline styles dinamicos c/ hardcoded | ~5 | ~14 | <5 |
| @media fora do padrao | muitos | 0 | 0 |
