# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 8 + fix Senior) | Ciclo 9 ATIVO
> 1.674/1.674 testes passando | 0 erros de build | 0 warnings

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
- Warnings: ~50 -> 2 | `!important`: 16 -> 1 | Breakpoints: 50 padronizados | Hardcoded: 1.387 -> 1.099 | Inline: ~35 -> 6
- **Problemas**: Pleno introduziu 16 NOVAS cores hardcoded ao converter inline -> CSS

### Ciclo 5 (Pleno Local): Refatoracao CSS - Hardcoded Colors + Inline Styles
- Warnings: 0 | `!important`: 1 | Hardcoded: 1.099 -> 867 | Vars: 673 -> 1.175 | Inline: 0
- **Problemas**: ViewPayment esquecido, 178 fallbacks desnecessarios, 141 nao convertidas

### Ciclo 6 (Pleno Local): Eliminacao de Hardcoded Hex em Scoped CSS
- Hardcoded hex: 867 -> 0 | Vars usadas: 1.175 -> ~1.476 | Fallbacks: 0
- **Problemas CRITICOS**: 18 vars inventadas sem definir (128 usos, UI quebrada) — corrigido pelo Senior. 18 vars mortas no `:root`

### Ciclo 7 (Pleno Local): Legibilidade de Fontes (UX)
- Melhorou contraste de fontes com text-shadow glow
- **Problemas**: Hardcoded hex/rgba em vez de vars existentes, commits bagunçados

### Ciclo 8 (Pleno Local): rgba Cleanup + UX/UI Interativo
- Branch: `refactor/ciclo8-rgba-cleanup` | PR #36
- **CSS Fases 1-6**: vars mortas removidas, rgba scoped parcial, events.css/identity.css/site.css convertidos
- **UX Sessoes 1-2**: 36 melhorias de UI em escalacao, pagamentos, grupos, admin, navegacao
- **Estabilidade**: Eliminacao completa de `Virtualize` (3 tabelas admin → paginacao manual)
- **Detalhes da revisao Senior**: ver secao abaixo

---

## Revisao Senior do Ciclo 8

### Veredicto: MUITO BOM — escopo amplo, 4/6 metas atingidas, 36 melhorias UX

| Metrica | C6/C7 | C8 | Meta C8 | Status |
|---------|-------|----|---------|----|
| Hardcoded hex scoped | 0 | **0** | 0 | Atingido |
| rgba() hardcoded scoped | 645 | **531** | <300 | Parcial |
| CSS vars usadas (scoped) | 2.053 | **1.998** | >2.200 | Nao atingido |
| `!important` scoped | 1 | **1** | 1 | Atingido |
| `!important` global | 17 | **12** | <5 | Parcial |
| Hardcoded hex global | ~1.317 | **166** | <600 | Superado |
| Vars no `:root` | 179 | **158** (apos fix) | ~161 | Atingido |
| Vars indefinidas | 0 | **1** → **0** (fix) | 0 | Corrigido |
| Vars mortas | 18 | **5** → **0** (fix) | 0 | Corrigido |
| Build warnings | 0 | **0** | 0 | Atingido |

### Problemas encontrados e corrigidos pelo Senior

| Problema | Impacto | Fix |
|----------|---------|-----|
| `--red-opacity-lg` removida como "morta" na Fase 1, mas usada na Fase 2 (2 usos) | CRITICO — border/shadow quebrados em Payments | Senior adicionou de volta ao `:root` com `rgba(239, 68, 68, 0.5)` |
| 5 vars mortas restantes (`--bg-dark-soft`, `--font-accent`, `--futsal-text-light`, `--shadow-green-lg`, `--entity-bg`) | BAIXO — `:root` poluido | Senior removeu 4 do `:root` (entity-bg e local no `.entity-shell`, mantida) |

### Analise das 36 melhorias UX

**Categoria A — Estabilidade (EXCELENTE)**:
- Eliminacao de `Virtualize` em 3 tabelas admin → paginacao manual (20/pagina). Fix definitivo para `NullReferenceException` recorrente em `Virtualize.BuildRenderTree`. Decisao arquitetural correta — `Virtualize` no Blazor Server com `ItemsProvider` assíncrono e problemático
- Guard `SemaphoreSlim.Release()` contra disposal em AdminLogs — previne `ObjectDisposedException`
- `StringComparison.OrdinalIgnoreCase` → `ToLower()` para traducao EF Core — bug real corrigido

**Categoria B — UX Features (BOM)**:
- Badge de horario semanal nos cards de grupo (Segundas, Tercas, etc.) — boa feature visual
- Badge de partidas recorrentes no detail do grupo
- Admin pode ver e confirmar comprovante de pagamento (confirmacao em 2 passos)
- Breadcrumb fix (`StateHasChanged` apos `LocationChanged`) — bug real corrigido
- QR code reduzido ~40% — melhoria de proporcao
- Renomeacao "Esportes" → "Eventos" + card shell na pagina
- SportCard poker com tema roxo + contador no body

**Categoria C — CSS/Styling (BOM com ressalvas)**:
- Escalacao: centralizacao de nomes, botoes metalicos, modal tema metalico — visual limpo
- Cores distinctivas para botoes admin (gold, teal, blue, green, amber, red) — boa UX
- **Ressalva 1**: 8 commits de tentativa/erro na centralizacao da escalacao (feceb2d→46e3d61). Regra 14 violada
- **Ressalva 2**: Metallic gradients usam rgba hardcoded (4 text colors + ~20 gradient stops em site.css). O Pleno documentou isso como "Known Issue #2"
- **Ressalva 3**: CSS movido para global (events.css, site.css) por causa de CSS isolation do Blazor. Decisao tecnicamente correta, mas o projeto deveria considerar `::deep` como alternativa antes de globalizar

**Categoria D — Conversao CSS vars (PARCIAL)**:
- Fases 1-3 (scoped rgba): 645 → 531 (apenas 114 convertidos de meta 345). Pleno documentou extensa lista de rgba sem var equivalente — correto em nao inventar vars (regra 12)
- Fase 4 (events.css): 403 → 111 hex, !important 4 → 0 — bom trabalho
- Fase 5 (identity.css): 29 → 12 hex. marketplace.css praticamente intocavel (paleta unica)
- Fase 6 (site.css): 749 → 4 hex fora do `:root` — excelente reducao

### Notas sobre o escopo expandido

O Ciclo 8 foi planejado como "rgba cleanup" mas expandiu para incluir 36 melhorias UX interativas. Isso e aceitavel porque:
1. As melhorias UX foram feitas na mesma branch (1 branch, 1 PR — regra 9 respeitada)
2. Build e testes continuam passando
3. Todas as melhorias sao incrementais (nao quebram funcionalidades existentes)

Porem, para ciclos futuros: separar tarefas de CSS refactoring e features UX em branches/PRs diferentes para facilitar review e rollback.

---

## Metricas Atuais (pos-Ciclo 8 + fix Senior)

| Metrica | C3 | C4 | C5 | C6/C7 | C8 |
|---------|----|----|----|----|-----|
| Warnings (build) | ~50 | 2 | 0 | 0 | **0** |
| `!important` scoped | 16 | 1 | 1 | 1 | **1** |
| `!important` global | — | — | — | 17 | **12** |
| Hardcoded hex scoped | 1.387 | 1.099 | 867 | 0 | **0** |
| rgba() hardcoded scoped | — | — | — | 645 | **531** |
| Hardcoded hex global | — | — | — | ~1.317 | **166** |
| rgba() hardcoded global | — | — | — | ~185 | **446** |
| CSS vars (scoped) | 381 | 673 | 1.175 | 2.053 | **1.998** |
| CSS vars (global) | — | — | — | — | **1.515** |
| CSS vars total | — | — | — | — | **3.513** |
| Inline estaticos | ~35 | 6 | 0 | 0 | **0** |
| Vars no `:root` | ~68 | ~68 | 105 | 179 | **158** |
| Vars indefinidas | — | — | 0 | 0 | **0** |
| Vars mortas `:root` | — | — | — | 18 | **0** |

### Estado dos CSS globais (pos-Ciclo 8)

| Arquivo | Hex hardcoded | rgba hardcoded | `!important` | var() usadas |
|---------|-------------|----------------|-------------|--------------|
| site.css (fora :root) | 4 | 154 | 12 (legit) | 1.092 |
| events.css | 111 | 254 | 0 | 390 |
| marketplace.css | 43 | 35 | 0 | 1 |
| identity.css | 12 | 3 | 0 | 32 |
| **Total globais** | **170** | **446** | **12** | **1.515** |

### Paginas grandes (>500 linhas)

| Arquivo | Linhas | Componentes extraidos |
|---------|--------|----------------------|
| AdminPayments.razor | 1.156 | 4 (Table, Filters, Summary, Modal) |
| Futsal/Detail.razor | 931 | 9 em Components/ |
| Groups/Detail.razor | 826 | — |
| Mailbox.razor | 673 | — |
| Payment/EventPayment.razor | 667 | 7 em Components/ |
| Futsal/Escalacao.razor | 661 | 6 em Components/ |
| Admin/AdminLogs.razor | 656 | 1 (AdminLogsTable) |
| Poker/Detail.razor | 634 | — |
| Payment/Payment.razor | 626 | — |
| Poker/Create.razor | 613 | — |
| Groups/Payments.razor | 560 | — |
| Poker/Edit.razor | 547 | — |

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** — se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores em scoped CSS** — usar vars do `:root`. Se nao existe var, documentar na secao "Cores sem Var"
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** — sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** — so criar se tiver estilos reais
6. **NUNCA introduzir novas cores hardcoded** ao converter inline -> classe CSS
7. **NUNCA usar fallback em var()** — usar `var(--nome)` sem fallback hex
8. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"` (0 failed)
9. **1 branch unica por ciclo** — commits por fase dentro dela. NAO criar branches separadas
10. **1 PR por ciclo** — mergear via PR, nunca push direto na main
11. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados"
12. **NUNCA inventar nomes de var()** — so usar vars que JA existem na lista "CSS Vars Permitidas" abaixo
13. **NUNCA adicionar var ao `:root` sem uso imediato** — so adicionar vars que serao usadas no mesmo commit
14. **Commits limpos** — 1 commit por fase, sem commits de tentativa/erro/reversao. Testar ANTES de commitar
15. **NUNCA remover var do `:root` sem verificar uso em TODOS os arquivos** — usar `grep -rn 'var(--nome)' Pages/ Shared/ wwwroot/css/` antes de remover. Se tem uso, NAO remover
16. **Separar CSS refactoring de features UX** — nao misturar os dois no mesmo ciclo/PR

---

## CSS Vars Permitidas (usar SEMPRE em vez de hex/rgba)

```css
/* == Warm theme (parchment/medieval) == */

/* Backgrounds */
var(--bg-deepest)        /* #2f1a09 */
var(--bg-deep)           /* #2f1d0b */
var(--bg-dark)           /* #3d2d1d */
var(--bg-dark-mid)       /* #4e2f16 */
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
var(--brown-dark)        /* #4a2c15 */
var(--brown-muted)       /* #8b7355 */

/* Rust & orange */
var(--rust)              /* #6a2810 */
var(--rust-dark)         /* #57210d */
var(--rust-orange)       /* #7a2f0e */
var(--orange)            /* #cf5a16 */

/* Status colors */
var(--green)             /* #6e9a3f */
var(--green-light)       /* #8aba57 */
var(--green-dark)        /* #5f8a33 */
var(--green-bright)      /* #4ade80 */
var(--green-soft)        /* #86efac */
var(--green-strong)      /* #22c55e */
var(--red)               /* #e53935 */
var(--red-soft)          /* #f87171 */
var(--red-strong)        /* #ef4444 */
var(--red-dark)          /* #dc2626 */
var(--red-border)        /* #7f1d1d */
var(--red-text-light)    /* #fca5a5 */
var(--red-pale)          /* #fecaca */
var(--red-lightest)      /* #fef2f2 */
var(--red-bg-dark)       /* #290f0f */
var(--amber)             /* #fbbf24 */
var(--amber-light)       /* #fcd34d */
var(--amber-warm)        /* #fde68a */
var(--amber-mid)         /* #d97706 */
var(--amber-dark)        /* #b45309 */
var(--amber-strong)      /* #e65100 */
var(--amber-lightest)    /* #fffbeb */

/* Neutral text */
var(--white)             /* #ffffff */
var(--text-light)        /* #f0f0f0 */
var(--text-muted-gray)   /* #888 */
var(--link-blue)         /* #8ab4f8 */
var(--link-info)         /* #93c5fd */
var(--border-slate)      /* #314454 */

/* == Cool/navy theme (Confirmai dark) == */

var(--ci-bg)             /* #090f18 */
var(--ci-bg-card)        /* #111927 */
var(--ci-bg-card-deep)   /* #0d1825 */
var(--ci-bg-input)       /* #07111d */
var(--ci-bg-mid)         /* #112240 */
var(--ci-bg-deep)        /* #0d1825 */
var(--ci-bg-dark)        /* #0a1f35 */
var(--ci-bg-deepest)     /* #050810 */
var(--ci-bg-alt)         /* #0a1928 */
var(--ci-accent)         /* #4f9cf8 */
var(--ci-accent-mid)     /* #1a5ab0 */
var(--ci-accent-dark)    /* #0d3270 */
var(--ci-accent-light)   /* #7dd3fc */
var(--ci-border)         /* #1b3d6c */
var(--ci-border-dim)     /* #1a5298 */
var(--ci-border-soft)    /* #1b3868 */
var(--ci-border-alt)     /* #1a3a5c */
var(--ci-text)           /* #f1f5f9 */
var(--ci-text-blue)      /* #deeeff */
var(--ci-text-link)      /* #7ab6ff */
var(--ci-text-muted)     /* #8aacc8 */
var(--ci-text-subtle)    /* #6082a0 */
var(--ci-text-info)      /* #8fc2f3 */
var(--ci-text-info-strong)/* #b7dbff */
var(--ci-surface-muted)  /* #cbd5e1 */
var(--ci-surface-light)  /* #e0f2fe */

/* Poker/violet theme */
var(--poker-accent)      /* #a78bfa */
var(--poker-accent-deep) /* #7c3aed */
var(--poker-text)        /* #c4b5fd */
var(--poker-bg-deepest)  /* #3b0764 */
var(--poker-bg-dark)     /* #4c1d95 */
var(--poker-border)      /* #2d1a4a */

/* Futsal theme */
var(--futsal-green-dark)   /* #14532d */
var(--futsal-green-pale)   /* #bbf7d0 */
var(--futsal-bg-dark)      /* #0a2018 */

/* Neutrals/Slate */
var(--slate-light)       /* #e2e8f0 */
var(--slate-mid)         /* #64748b */
var(--slate-muted)       /* #94a3b8 */
var(--slate-dark)        /* #334155 */
var(--slate-pale)        /* #e2e8f0 */
var(--slate-warm)        /* #78716c */

/* Pink */
var(--pink-mid)          /* #f472b6 */

/* == Shadow/Opacity vars (usar em vez de rgba hardcoded) == */

/* Black shadows */
var(--shadow-md)         /* rgba(0,0,0,0.3) */
var(--shadow-lg)         /* rgba(0,0,0,0.4) */
var(--shadow-xl)         /* rgba(0,0,0,0.5) */
var(--shadow-2xl)        /* rgba(0,0,0,0.6) */

/* Accent shadows (blue-400: 79,156,248) */
var(--shadow-accent-sm)  /* rgba(79,156,248,0.3) */
var(--shadow-accent-md)  /* rgba(79,156,248,0.4) */
var(--shadow-accent-lg)  /* rgba(79,156,248,0.5) */

/* Green shadows */
var(--shadow-green-sm)   /* rgba(74,222,128,0.3) */

/* Red shadows */
var(--shadow-red-sm)     /* rgba(239,68,68,0.3) */
var(--red-opacity-lg)    /* rgba(239,68,68,0.5) */

/* Text shadows */
var(--text-shadow-sm)    /* rgba(0,0,0,0.3) */

/* Inset shadows */
var(--inset-accent-sm)   /* rgba(147,197,253,0.2) */

/* Overlays */
var(--overlay-sm)        /* rgba(0,0,0,0.3) */
var(--overlay-lg)        /* rgba(0,0,0,0.55) */

/* Accent opacity (blue-400: 79,156,248) */
var(--accent-opacity-sm) /* rgba(79,156,248,0.2) */
var(--accent-opacity-md) /* rgba(79,156,248,0.3) */
var(--accent-opacity-lg) /* rgba(79,156,248,0.4) */
var(--accent-opacity-xl) /* rgba(79,156,248,0.5) */
var(--accent-opacity-2xl)/* rgba(79,156,248,0.6) */

/* Green opacity */
var(--green-opacity-sm)  /* rgba(74,222,128,0.2) */
var(--green-opacity-md)  /* rgba(74,222,128,0.3) */
var(--green-opacity-lg)  /* rgba(74,222,128,0.35) */

/* Slate border */
var(--slate-border-sm)   /* rgba(148,163,184,0.1) */
var(--slate-border-md)   /* rgba(148,163,184,0.15) */
var(--slate-border-lg)   /* rgba(148,163,184,0.2) */

/* Green-dark opacity */
var(--green-dark-md)     /* rgba(22,163,74,0.12) */
var(--green-dark-lg)     /* rgba(22,163,74,0.15) */
var(--green-dark-2xl)    /* rgba(22,163,74,0.4) */

/* Purple opacity */
var(--purple-sm)         /* rgba(124,58,237,0.1) */
var(--purple-md)         /* rgba(124,58,237,0.12) */
var(--purple-lg)         /* rgba(124,58,237,0.18) */
var(--purple-xl)         /* rgba(124,58,237,0.3) */

/* Sky/Red opacity */
var(--sky-blue-md)       /* rgba(125,211,252,0.5) */
var(--red-light-sm)      /* rgba(248,113,113,0.08) */
var(--red-light-md)      /* rgba(248,113,113,0.2) */
var(--red-light-lg)      /* rgba(248,113,113,0.5) */
var(--red-dark-md)       /* rgba(183,28,28,0.3) */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--ci-font)           /* system sans-serif stack */
```

---

## Ciclo 9 — Tarefas Ativas

**Foco**: Adicionar vars rgba faltantes + converter scoped restante + decomposicao de paginas

**Branch**: `refactor/ciclo9-vars-decomp`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Adicionar novas vars rgba ao `:root` — P0
**Estimativa**: ~20 min

Adicionar ao bloco `:root` em `wwwroot/css/site.css` as vars para os padroes rgba mais comuns SEM var equivalente:

```css
/* Accent subtle (blue-400 low opacity) */
--accent-opacity-xs:  rgba(79, 156, 248, 0.1);   /* 17 usos */
--accent-opacity-2xs: rgba(79, 156, 248, 0.15);   /* 7 usos */

/* Green mid opacity */
--green-opacity-xl:   rgba(74, 222, 128, 0.5);    /* 8 usos */
--green-opacity-2xl:  rgba(74, 222, 128, 0.6);    /* 4 usos */

/* Black heavy */
--shadow-3xl:         rgba(0, 0, 0, 0.7);          /* 3 usos */

/* Blue-300 (96,165,250) — cor diferente de accent (79,156,248) */
--blue300-opacity-xs: rgba(96, 165, 250, 0.1);    /* 5 usos */
--blue300-opacity-sm: rgba(96, 165, 250, 0.2);    /* 9 usos */

/* Red mid opacity */
--red-opacity-sm:     rgba(239, 68, 68, 0.2);     /* 5 usos */
--red-opacity-md:     rgba(239, 68, 68, 0.4);     /* 4 usos */

/* Amber opacity (251,191,36) */
--amber-opacity-sm:   rgba(251, 191, 36, 0.3);    /* 4 usos */
```

**Total**: 11 novas vars para ~66 usos (12% do total scoped)

**IMPORTANTE**: Verificar que cada var sera usada na Fase 2 antes de adicionar:
```bash
grep -rn 'rgba(79, 156, 248, 0.1)' Pages/ Shared/ --include="*.css" | wc -l
# Deve retornar >= 1
```

**Validacao**:
```bash
dotnet build
```

---

### Fase 2: Converter rgba com vars existentes + novas (scoped) — P0
**Estimativa**: ~2h

Converter os ~112 rgba que AGORA tem var equivalente (incluindo as novas da Fase 1) nos 10 maiores arquivos:

1. `Shared/Components/Groups/GroupDetailPaymentsModal.razor.css` (51 rgba total)
2. `Pages/Payment/Payment.razor.css` (46 rgba total)
3. `Pages/Groups/Payments.razor.css` (22 rgba total)
4. `Pages/Payment/EventPayment.razor.css` (19 rgba total)
5. `Pages/Payment/ViewPayment.razor.css` (18 rgba total)
6. `Pages/Futsal/Detail.razor.css` (17 rgba total)
7. `Pages/Groups/Components/MembersManager.razor.css` (16 rgba total)
8. `Pages/Groups/Detail.razor.css` (15 rgba total)
9. `Pages/Admin/AdminLogs.razor.css` (13 rgba total)
10. `Pages/Index.razor.css` (11 rgba total)

**Mapeamento COMPLETO** (vars existentes + novas da Fase 1):
```
rgba(0, 0, 0, 0.3)         → var(--shadow-md)
rgba(0, 0, 0, 0.4)         → var(--shadow-lg)
rgba(0, 0, 0, 0.5)         → var(--shadow-xl)
rgba(0, 0, 0, 0.6)         → var(--shadow-2xl)
rgba(0, 0, 0, 0.7)         → var(--shadow-3xl)           [NOVA]
rgba(79, 156, 248, 0.1)    → var(--accent-opacity-xs)    [NOVA]
rgba(79, 156, 248, 0.15)   → var(--accent-opacity-2xs)   [NOVA]
rgba(79, 156, 248, 0.2)    → var(--accent-opacity-sm)
rgba(79, 156, 248, 0.3)    → var(--accent-opacity-md)
rgba(79, 156, 248, 0.4)    → var(--accent-opacity-lg)
rgba(79, 156, 248, 0.5)    → var(--accent-opacity-xl)
rgba(96, 165, 250, 0.1)    → var(--blue300-opacity-xs)   [NOVA]
rgba(96, 165, 250, 0.2)    → var(--blue300-opacity-sm)   [NOVA]
rgba(74, 222, 128, 0.3)    → var(--green-opacity-md)
rgba(74, 222, 128, 0.35)   → var(--green-opacity-lg)
rgba(74, 222, 128, 0.5)    → var(--green-opacity-xl)     [NOVA]
rgba(74, 222, 128, 0.6)    → var(--green-opacity-2xl)    [NOVA]
rgba(239, 68, 68, 0.2)     → var(--red-opacity-sm)       [NOVA]
rgba(239, 68, 68, 0.3)     → var(--shadow-red-sm)
rgba(239, 68, 68, 0.4)     → var(--red-opacity-md)       [NOVA]
rgba(239, 68, 68, 0.5)     → var(--red-opacity-lg)
rgba(251, 191, 36, 0.3)    → var(--amber-opacity-sm)     [NOVA]
rgba(147, 197, 253, 0.2)   → var(--inset-accent-sm)
rgba(26, 90, 176, 0.25)    → manter (sem var — opacity unica)
```

**REGRAS**:
- Se o rgba NAO esta nesta lista, deixar hardcoded
- NAO inventar vars — so usar as da lista acima
- Verificar contexto (shadow var para box-shadow, opacity var para bg/border)

**Validacao**:
```bash
dotnet build
grep -rn 'rgba(' Pages/ Shared/ --include="*.css" | grep -v 'var(--' | wc -l
# Meta: <400
```

---

### Fase 3: Converter rgba em events.css — P1
**Estimativa**: ~1h

events.css tem 254 rgba hardcoded. Converter usando o mesmo mapeamento da Fase 2 + vars hex existentes para os 111 hex restantes.

Para hex: usar o mapeamento de Fase 4/5/6 do Ciclo 8 (mesmo padrao).

**Validacao**:
```bash
dotnet build
grep -c 'rgba(' wwwroot/css/events.css
# Meta: <150 (muitos sao gradientes unicos)
```

---

### Fase 4: Converter rgba em site.css (fora do :root) — P1
**Estimativa**: ~1h

site.css tem 154 rgba fora do `:root`. Converter usando mapeamento da Fase 2.

Os 4 hex restantes sao text colors de botoes metalicos — podem ser convertidos para `var(--white)`, `var(--amber-lightest)`, etc.

**Validacao**:
```bash
dotnet build
```

---

### Fase 5: Decomposicao de Groups/Detail.razor (826L → <400L) — P2
**Estimativa**: ~2h

Extrair sub-componentes do `Groups/Detail.razor` seguindo o padrao ja usado em `Futsal/Detail`:

1. `GroupDetailMembersSection` — lista de membros + manage
2. `GroupDetailEventsTable` — tabela de partidas + recurring badge
3. `GroupDetailMetrics` — cards de metricas (ja existe `GroupMetrics.razor`?)
4. `GroupDetailHeader` — header + badges

**REGRAS de decomposicao**:
- Componente filho recebe dados via `[Parameter]`
- Eventos de volta via `[Parameter] EventCallback`
- CSS vai no `.razor.css` do componente filho (NAO global)
- Se o CSS precisa estilizar netos, usar `::deep` (NAO mover para global)
- O `.razor` pai fica como orquestrador (lifecycle, data loading, routing)

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Groups/Detail.razor
# Meta: <400
```

---

### Fase 6: Decomposicao de Payment/Payment.razor (626L → <400L) — P2
**Estimativa**: ~1.5h

Similar a Fase 5. Extrair:

1. `PaymentMethodSelector` — selecao de metodo (PIX, BTC, etc.)
2. `PaymentQrDisplay` — QR code display (similar ao EventPaymentQr)
3. `PaymentConfirmationSection` — confirmacao + status

**REGRAS**: mesmas da Fase 5.

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Payment/Payment.razor
# Meta: <400
```

---

## Ordem de Execucao

```
Fase 1 (novas vars) → Fase 2 (converter rgba scoped) → Fase 3 (events.css rgba) → Fase 4 (site.css rgba) → Fase 5 (decomp Groups/Detail) → Fase 6 (decomp Payment)
```

---

## Comandos de Validacao Completos

```bash
# Build:
dotnet build

# Tests:
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"

# Contar hardcoded hex em scoped CSS (deve ser 0):
grep -rn '#[0-9a-fA-F]\{3,8\}' Pages/ Shared/ --include="*.css" | wc -l

# Contar rgba hardcoded em scoped CSS:
grep -rn 'rgba(' Pages/ Shared/ --include="*.css" | grep -v 'var(--' | wc -l

# Contar CSS vars usadas (scoped):
grep -rn 'var(--' Pages/ Shared/ --include="*.css" | wc -l

# Contar !important total:
grep -rn '!important' Pages/ Shared/ wwwroot/css/ --include="*.css" | wc -l

# Verificar vars indefinidas:
grep -oP '^\s*--([\w-]+)\s*:' wwwroot/css/site.css | sed 's/^\s*--//' | sed 's/\s*://' | sort -u > /tmp/defined.txt
grep -rPoh 'var\(--([\w-]+)\)' Pages/ Shared/ --include="*.css" | grep -oP '\-\-([\w-]+)' | sed 's/^--//' | sort -u > /tmp/used.txt
grep -rPoh '^\s*--([\w-]+)\s*:' Pages/ Shared/ --include="*.css" | sed 's/^\s*--//' | sed 's/\s*://' | sort -u > /tmp/local.txt
cat /tmp/defined.txt /tmp/local.txt | sort -u > /tmp/all.txt
comm -23 /tmp/used.txt /tmp/all.txt
# Meta: 0 linhas

# Verificar vars mortas (definidas mas nao usadas):
while IFS= read -r var; do
  count=$(grep -rPo "var\(--${var}\)" Pages/ Shared/ wwwroot/css/ --include="*.css" | wc -l)
  if [ "$count" -eq 0 ]; then echo "MORTA: --$var"; fi
done < /tmp/defined.txt
# Meta: 0
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui rgba() que nao tem var equivalente na lista -->
<!-- Formato: rgba(r,g,b,a) | arquivo | contexto (shadow, overlay, border) -->

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores com !important que nao conseguiu resolver -->

### Problemas de decomposicao
<!-- Liste aqui dificuldades na extracao de componentes -->

### Outras observacoes
