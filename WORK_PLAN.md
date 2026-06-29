# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 9 + revisao Senior) | Ciclo 10 ATIVO
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
- **CSS Fases 1-6**: vars mortas removidas, rgba scoped parcial, events.css/identity.css/site.css convertidos
- **UX Sessoes 1-2**: 36 melhorias de UI em escalacao, pagamentos, grupos, admin, navegacao
- **Estabilidade**: Eliminacao completa de `Virtualize` (3 tabelas admin -> paginacao manual)
- **Problemas**: `--red-opacity-lg` removida e reusada (Senior corrigiu), 5 vars mortas (Senior removeu)

### Ciclo 9 (Pleno Local): rgba Scoped + Decomposicao de Paginas
- Branch: `refactor/ciclo9-vars-decomp` | PR #38
- **Fase 1**: 11 novas vars rgba adicionadas ao `:root` (accent-opacity-xs/2xs, green-opacity-xl/2xl, shadow-3xl, blue300-opacity-xs/sm, red-opacity-sm/md, amber-opacity-sm)
- **Fase 2**: 53 rgba convertidos em 8 arquivos scoped (de ~112 planejados)
- **Fase 3**: 17 rgba convertidos em events.css (de ~254)
- **Fase 4**: 16 rgba convertidos em site.css fora do `:root` (de ~154)
- **Fase 5**: Groups/Detail.razor decomposto (826L -> 297L template + 407L code-behind + 2 sub-componentes)
- **Fase 6**: Payment/Payment.razor decomposto (626L -> 87L template + 554L code-behind + 2 sub-componentes)
- **Detalhes da revisao Senior**: ver secao abaixo

---

## Revisao Senior do Ciclo 9

### Veredicto: DECOMPOSICAO EXCELENTE, CONVERSAO RGBA INSUFICIENTE

| Metrica | C8 | C9 | Meta C9 | Status |
|---------|----|----|---------|--------|
| Hardcoded hex scoped | 0 | **0** | 0 | Atingido |
| rgba() hardcoded scoped | 531 | **478** | <350 | **NAO atingido** |
| CSS vars usadas (scoped) | 1.998 | **2.051** | >2.200 | Parcial |
| `!important` scoped | 1 | **1** | 1 | Atingido |
| `!important` global | 12 | **11** | — | Estavel |
| Hardcoded hex global | 166 | **166** | — | Nao era meta |
| rgba() hardcoded global | 446 | **413** | — | Nao era meta |
| Vars no `:root` | 158 | **168** | ~169 | Atingido |
| Vars indefinidas | 0 | **0** | 0 | Atingido |
| Vars mortas | 0 | **1** (--entity-bg, scoped) | 0 | Aceitavel |
| Build warnings | 0 | **0** | 0 | Atingido |
| Fallbacks | 0 | **4** | 0 | Regressao menor |
| Groups/Detail.razor | 826L | **297L** | <400L | **Superado** |
| Payment/Payment.razor | 626L | **87L** | <400L | **Superado** |

### O que deu CERTO

**Decomposicao (Fases 5-6) — EXCELENTE**:
- `Groups/Detail.razor`: 826L -> 297L template + 407L code-behind. Padrao correto: `partial class`, `IAsyncDisposable`, `IDbContextFactory`, sub-componentes com `[Parameter]` e `EventCallback`
- `Payment/Payment.razor`: 626L -> 87L template + 554L code-behind. Sub-componentes `PaymentProductSummary` e `PaymentCheckoutPanel` com tipagem limpa
- `GroupDetailEvents` e `GroupDetailPendingRequests` extraidos com parametros bem tipados
- Groups/Detail usa `IDbContextFactory` (padrao correto), Payment usa `AppDbContext` direto (pre-existente, nao introducido pelo Pleno)

**Vars adicionadas (Fase 1)**: Todas 11 novas vars estao em uso (verificado por grep). Regra 13 respeitada.

**Commits**: 6 commits limpos, 1 por fase, branch unica. Regra 9 e 14 respeitadas.

### O que NAO deu certo

**Conversao rgba (Fases 2-4) — INSUFICIENTE**:
- Fase 2 converteu apenas 53 de ~112 rgba convertiveis em scoped CSS
- Restam **123 rgba que TEM var existente** e NAO foram convertidas:
  - `rgba(0,0,0,0.3)` -> `var(--shadow-md)`: **16 usos restantes**
  - `rgba(0,0,0,0.4)` -> `var(--shadow-lg)`: **19 usos restantes**
  - `rgba(79,156,248,0.1)` -> `var(--accent-opacity-xs)`: **16 usos restantes**
  - `rgba(79,156,248,0.3)` -> `var(--accent-opacity-md)`: **13 usos restantes**
  - `rgba(79,156,248,0.5)` -> `var(--accent-opacity-xl)`: **12 usos restantes**
  - `rgba(79,156,248,0.4)` -> `var(--accent-opacity-lg)`: **9 usos restantes**
  - `rgba(0,0,0,0.6)` -> `var(--shadow-2xl)`: **6 usos restantes**
  - `rgba(0,0,0,0.5)` -> `var(--shadow-xl)`: **4 usos restantes**
  - E mais ~28 usos de green-opacity, red-opacity, accent-opacity
- Isso NAO e aceitavel — as vars existem, o mapeamento foi fornecido, bastava aplicar
- Fase 3 (events.css): apenas 17 convertidos. O arquivo tem formatacao diferente (`rgba(96,165,250,0.3)` sem espacos) que pode ter confundido o Pleno

**4 fallbacks reintroduzidos**:
- `events.css:2082`: `background: #0e2236;` (hex inline junto com var)
- `identity.css:21,118`: gradientes lineares com hex inline
- `site.css:6211`: comentario com hex (nao e codigo, baixo impacto)

### Nova regra para o Pleno

**Regra 17**: Ao converter rgba -> var, cobrir TODOS os arquivos scoped (.razor.css), nao apenas os 8-10 maiores. Usar o mapeamento fornecido como checklist e verificar cada padrao com grep global.

---

## Metricas Atuais (pos-Ciclo 9)

| Metrica | C3 | C4 | C5 | C6/C7 | C8 | C9 |
|---------|----|----|----|----|-----|-----|
| Warnings (build) | ~50 | 2 | 0 | 0 | 0 | **0** |
| `!important` scoped | 16 | 1 | 1 | 1 | 1 | **1** |
| `!important` global | — | — | — | 17 | 12 | **11** |
| Hardcoded hex scoped | 1.387 | 1.099 | 867 | 0 | 0 | **0** |
| rgba() hardcoded scoped | — | — | — | 645 | 531 | **478** |
| Hardcoded hex global | — | — | — | ~1.317 | 166 | **166** |
| rgba() hardcoded global | — | — | — | ~185 | 446 | **413** |
| CSS vars (scoped) | 381 | 673 | 1.175 | 2.053 | 1.998 | **2.051** |
| CSS vars total | — | — | — | — | 3.513 | **3.609** |
| Inline estaticos | ~35 | 6 | 0 | 0 | 0 | **0** |
| Vars no `:root` | ~68 | ~68 | 105 | 179 | 158 | **168** |
| Vars indefinidas | — | — | 0 | 0 | 0 | **0** |
| Vars mortas `:root` | — | — | — | 18 | 0 | **0** |
| Fallbacks | — | — | 178 | 0 | 0 | **4** |

### Estado dos CSS globais (pos-Ciclo 9)

| Arquivo | Hex hardcoded | rgba hardcoded | `!important` | var() usadas |
|---------|-------------|----------------|-------------|--------------|
| site.css (fora :root) | 4 | 138 | 11 | ~1.100 |
| events.css | 111 | 237 | 0 | ~400 |
| marketplace.css | 43 | 35 | 0 | 1 |
| identity.css | 12 | 3 | 0 | 32 |
| **Total globais** | **170** | **413** | **11** | **~1.533** |

### Paginas grandes (>500 linhas)

| Arquivo | Linhas | Status |
|---------|--------|--------|
| AdminPayments.razor | 1.156 | 4 sub-componentes extraidos |
| Futsal/Detail.razor | 931 | 9 sub-componentes em Components/ |
| Mailbox.razor | 673 | Candidato decomposicao |
| Payment/EventPayment.razor | 667 | 7 sub-componentes em Components/ |
| Futsal/Escalacao.razor | 661 | 6 sub-componentes em Components/ |
| Admin/AdminLogs.razor | 656 | 1 sub-componente (AdminLogsTable) |
| Poker/Detail.razor | 634 | Candidato decomposicao |
| Poker/Create.razor | 613 | Candidato decomposicao |
| Groups/Payments.razor | 560 | Candidato decomposicao |
| Poker/Edit.razor | 547 | Candidato decomposicao |

*Groups/Detail (297L) e Payment/Payment (87L) saíram da lista — decomposicao Ciclo 9 bem-sucedida.*

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
17. **Cobrir TODOS os arquivos ao converter rgba** — nao pular arquivos. Usar grep global para encontrar todos os usos de cada padrao e converter todos de uma vez

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
var(--shadow-3xl)        /* rgba(0,0,0,0.7) */

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
var(--accent-opacity-xs) /* rgba(79,156,248,0.1) */
var(--accent-opacity-2xs)/* rgba(79,156,248,0.15) */
var(--accent-opacity-sm) /* rgba(79,156,248,0.2) */
var(--accent-opacity-md) /* rgba(79,156,248,0.3) */
var(--accent-opacity-lg) /* rgba(79,156,248,0.4) */
var(--accent-opacity-xl) /* rgba(79,156,248,0.5) */
var(--accent-opacity-2xl)/* rgba(79,156,248,0.6) */

/* Green opacity */
var(--green-opacity-sm)  /* rgba(74,222,128,0.2) */
var(--green-opacity-md)  /* rgba(74,222,128,0.3) */
var(--green-opacity-lg)  /* rgba(74,222,128,0.35) */
var(--green-opacity-xl)  /* rgba(74,222,128,0.5) */
var(--green-opacity-2xl) /* rgba(74,222,128,0.6) */

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
var(--red-opacity-sm)    /* rgba(239,68,68,0.2) */
var(--red-opacity-md)    /* rgba(239,68,68,0.4) */

/* Blue-300 opacity (96,165,250) */
var(--blue300-opacity-xs) /* rgba(96,165,250,0.1) */
var(--blue300-opacity-sm) /* rgba(96,165,250,0.2) */

/* Amber opacity */
var(--amber-opacity-sm)  /* rgba(251,191,36,0.3) */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--ci-font)           /* system sans-serif stack */
```

---

## Ciclo 10 — Tarefas Ativas

**Foco**: Completar conversao rgba scoped (que ficou pendente no Ciclo 9) + iniciar decomposicao de paginas grandes

**Branch**: `refactor/ciclo10-rgba-complete-decomp`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Converter 123 rgba com vars existentes em scoped CSS — P0
**Estimativa**: ~2h

O Ciclo 9 deixou 123 rgba que TEM var equivalente sem converter. Usar o mapeamento abaixo e converter **TODOS** os arquivos scoped (Pages/ e Shared/ *.razor.css).

**Procedimento**:
1. Para cada padrao na tabela, executar: `grep -rn 'PADRAO' Pages/ Shared/ --include="*.css"`
2. Substituir cada ocorrencia pela var correspondente
3. Verificar com `dotnet build` apos cada arquivo

**Mapeamento COMPLETO** (mesma lista do Ciclo 9 — TODOS devem ser convertidos):
```
rgba(0, 0, 0, 0.3)         -> var(--shadow-md)          [16 usos]
rgba(0, 0, 0, 0.4)         -> var(--shadow-lg)          [19 usos]
rgba(0, 0, 0, 0.5)         -> var(--shadow-xl)          [4 usos]
rgba(0, 0, 0, 0.6)         -> var(--shadow-2xl)         [6 usos]
rgba(0, 0, 0, 0.7)         -> var(--shadow-3xl)         [1 uso]
rgba(79, 156, 248, 0.1)    -> var(--accent-opacity-xs)  [16 usos]
rgba(79, 156, 248, 0.15)   -> var(--accent-opacity-2xs) [7 usos]
rgba(79, 156, 248, 0.2)    -> var(--accent-opacity-sm)  [5 usos]
rgba(79, 156, 248, 0.3)    -> var(--accent-opacity-md)  [13 usos]
rgba(79, 156, 248, 0.4)    -> var(--accent-opacity-lg)  [9 usos]
rgba(79, 156, 248, 0.5)    -> var(--accent-opacity-xl)  [12 usos]
rgba(74, 222, 128, 0.3)    -> var(--green-opacity-md)   [5 usos]
rgba(74, 222, 128, 0.35)   -> var(--green-opacity-lg)   [4 usos]
rgba(239, 68, 68, 0.2)     -> var(--red-opacity-sm)     [1 uso]
rgba(239, 68, 68, 0.3)     -> var(--shadow-red-sm)      [1 uso]
rgba(239, 68, 68, 0.5)     -> var(--red-opacity-lg)     [1 uso]
rgba(251, 191, 36, 0.3)    -> var(--amber-opacity-sm)   [2 usos]
rgba(147, 197, 253, 0.2)   -> var(--inset-accent-sm)    [1 uso]
```

**ATENCAO**: Alguns arquivos usam formatacao sem espacos (`rgba(0,0,0,0.3)`) e outros com espacos (`rgba(0, 0, 0, 0.3)`). Buscar AMBOS os formatos:
```bash
grep -rn 'rgba(0, 0, 0, 0.3)\|rgba(0,0,0,0.3)\|rgba(0,0,0,.3)' Pages/ Shared/ --include="*.css"
```

**Validacao**:
```bash
dotnet build
# Verificar que TODOS os padroes acima tem 0 ocorrencias restantes:
grep -rn 'rgba(0, 0, 0, 0.3)\|rgba(0,0,0,0.3)' Pages/ Shared/ --include="*.css" | wc -l
# Meta: 0 para cada padrao da tabela
```

---

### Fase 2: Converter rgba com vars existentes em events.css — P0
**Estimativa**: ~1.5h

events.css tem 237 rgba. Muitos usam formatacao SEM espacos. Converter usando o mesmo mapeamento da Fase 1, mas tambem incluir padroes com formatacao compacta:

```
rgba(0,0,0,.3)             -> var(--shadow-md)
rgba(0,0,0,.4)             -> var(--shadow-lg)
rgba(0,0,0,.5)             -> var(--shadow-xl)
rgba(96,165,250,0.3)       -> var(--blue300-opacity-sm) [NOTA: 96,165,250 = blue-300, nao accent]
rgba(96,165,250,0.5)       -> var(--sky-blue-md)        [NOTA: reutilizar se for 125,211,252 — senao DOCUMENTAR]
rgba(74,222,128,0.35)      -> var(--green-opacity-lg)
rgba(74,222,128,0.3)       -> var(--green-opacity-md)
rgba(239,68,68,0.35)       -> DOCUMENTAR (sem var existente)
rgba(239,68,68,0.08)       -> DOCUMENTAR (sem var existente)
rgba(251,191,36,0.08)      -> DOCUMENTAR (sem var existente)
```

**IMPORTANTE**: Para cores sem var correspondente, NAO inventar vars. Documentar na secao "Cores sem Var" no fim deste arquivo.

**Validacao**:
```bash
dotnet build
grep -c 'rgba(' wwwroot/css/events.css
# Meta: <180 (muitos serao unicos sem var)
```

---

### Fase 3: Converter rgba com vars existentes em site.css (fora do :root) — P1
**Estimativa**: ~1h

site.css tem 138 rgba fora do `:root`. Converter os padroes com var existente.

Os 4 hex restantes em site.css (`#e0f7f4`, `#ffffff`, `#ecfdf5`, `#fffbeb`) sao text colors de botoes metalicos. Converter:
```
#ffffff  -> var(--white)
#fffbeb  -> var(--amber-lightest)
#e0f7f4  -> DOCUMENTAR (sem var)
#ecfdf5  -> DOCUMENTAR (sem var)
```

**Validacao**:
```bash
dotnet build
```

---

### Fase 4: Decomposicao de Mailbox.razor (673L -> <400L) — P2
**Estimativa**: ~2h

Extrair sub-componentes seguindo o padrao de Groups/Detail (Ciclo 9):

1. `MailboxConversationList` — lista lateral de conversas
2. `MailboxMessageThread` — thread de mensagens da conversa selecionada
3. `MailboxComposePanel` — formulario de nova mensagem/resposta

**REGRAS de decomposicao**:
- Componente filho recebe dados via `[Parameter]`
- Eventos de volta via `[Parameter] EventCallback`
- CSS vai no `.razor.css` do componente filho (NAO global)
- Se o CSS precisa estilizar netos, usar `::deep` (NAO mover para global)
- O `.razor` pai fica como orquestrador (lifecycle, data loading, routing)
- Usar `partial class` com code-behind `.razor.cs` para logica C#

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Mailbox.razor
# Meta: <400
```

---

### Fase 5: Decomposicao de Poker/Detail.razor (634L -> <400L) — P2
**Estimativa**: ~1.5h

Similar a Fase 4. Seguir o padrao de Futsal/Detail que ja tem 9 sub-componentes:

1. `PokerDetailHeader` — cabecalho + badges + status
2. `PokerDetailPlayers` — lista de jogadores + confirmacoes
3. `PokerDetailPayments` — resumo de pagamentos + acoes admin

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Poker/Detail.razor
# Meta: <400
```

---

### Fase 6: Decomposicao de Poker/Create.razor (613L -> <400L) — P2
**Estimativa**: ~1.5h

Extrair formulario de criacao:

1. `PokerCreateForm` — campos do formulario de criacao
2. `PokerCreatePreview` — preview do evento antes de criar

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Poker/Create.razor
# Meta: <400
```

---

## Ordem de Execucao

```
Fase 1 (rgba scoped TODOS) -> Fase 2 (events.css rgba) -> Fase 3 (site.css rgba+hex) -> Fase 4 (decomp Mailbox) -> Fase 5 (decomp Poker/Detail) -> Fase 6 (decomp Poker/Create)
```

**Meta Ciclo 10**: rgba scoped <350, rgba events <180, 3 paginas decompostas (<400L cada)

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
