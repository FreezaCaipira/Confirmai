# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 10 + revisao Senior) | Ciclo 11 ATIVO
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
- 11 novas vars rgba, 53 rgba convertidos em scoped, decomposicao Groups/Detail (297L) e Payment (87L)
- **Problemas**: Conversao rgba insuficiente (123 convertiveis nao convertidos). Senior adicionou regra 17

### Ciclo 10 (Pleno Local): rgba Complete + Decomposicao de Paginas
- Branch: `refactor/ciclo10-rgba-complete-decomp` | PR #40
- **Fase 1**: 165 rgba convertidos em 32 arquivos scoped (regra 17 seguida — TODOS os arquivos)
- **Fase 2**: events.css rgba parcial (237 -> 220) + identity.css hex zerado (12 -> 0) + 4 fallbacks corrigidos
- **Fase 3**: site.css hex convertidos (#ffffff -> var(--white), #fffbeb -> var(--amber-lightest))
- **Fase 4**: Mailbox.razor decomposto (673L -> 66L template + 614L code-behind + 3 sub-componentes)
- **Fase 5**: Poker/Detail.razor decomposto (635L -> 315L template + 194L code-behind + PokerDetailInfo)
- **Fase 6**: Poker/Create.razor decomposto (614L -> 319L template + 276L code-behind)
- **Detalhes da revisao Senior**: ver secao abaixo

---

## Revisao Senior do Ciclo 10

### Veredicto: MELHOR CICLO ATE AGORA — conversao completa, decomposicao excelente

| Metrica | C9 | C10 | Meta C10 | Status |
|---------|----|----|---------|--------|
| Hardcoded hex scoped | 0 | **0** | 0 | Atingido |
| rgba() hardcoded scoped | 478 | **378** | <350 | Proximo |
| rgba convertiveis restantes | 123 | **0** | 0 | **Atingido** |
| CSS vars usadas (scoped) | 2.051 | **2.149** | >2.200 | Proximo |
| `!important` scoped | 1 | **1** | 1 | Atingido |
| `!important` global | 11 | **12** | — | Estavel |
| Hardcoded hex global | 166 | **153** | — | Melhoria |
| identity.css hex | 12 | **0** | 0 | **Zerado** |
| events.css rgba | 237 | **220** | <180 | Parcial |
| site.css rgba | 138 | **136** | — | Estavel |
| Vars no `:root` | 168 | **168** | — | Estavel |
| Vars indefinidas | 0 | **0** | 0 | Atingido |
| Vars mortas | 1 | **1** (--entity-bg) | 0 | Aceitavel |
| Fallbacks | 4 | **0** | 0 | **Corrigido** |
| Build warnings | 0 | **0** | 0 | Atingido |
| Mailbox.razor | 673L | **66L** | <400L | **Superado** |
| Poker/Detail.razor | 635L | **315L** | <400L | **Superado** |
| Poker/Create.razor | 614L | **319L** | <400L | **Superado** |

### O que deu CERTO

**Conversao rgba (Fase 1) — EXCELENTE**:
- 165 rgba convertidos em **32 arquivos** — cobertura total, regra 17 respeitada
- Zero rgba convertiveis restantes (verificado por grep: todos os padroes com var existente foram convertidos)
- Mesmo arquivo `Pages/Docs/Integration.razor.css` (binary) foi incluido

**Decomposicao (Fases 4-6) — EXCELENTE**:
- `Mailbox.razor`: 673L -> 66L. Tres sub-componentes limpos: `MailboxFilters`, `MailboxConversationList`, `MailboxThreadPane`. Usa `IDbContextFactory` (correto)
- `Poker/Detail.razor`: 635L -> 315L. `PokerDetailInfo` extraido com `[Parameter] Event`. Usa `IDbContextFactory` (correto)
- `Poker/Create.razor`: 614L -> 319L. Code-behind com `CreatePokerEventForm` sealed class. Usa `IDbContextFactory` (correto)

**Fallbacks corrigidos**: Os 4 fallbacks de Ciclo 9 foram eliminados (events.css, identity.css)

**Commits**: 6 commits limpos, 1 por fase, branch unica. Regras 9 e 14 respeitadas

### Problemas encontrados

**Encoding corrompido (13 + 7 + 4 = 24 linhas em 6 arquivos)**:
O Pleno corrompeu encoding UTF-8 -> Latin-1 em comentarios CSS ao editar os arquivos:
- `events.css`: 13 linhas (em dashes `—` -> `�`, acentos `á/ó/ã` -> `�`)
- `Pages/Docs/Integration.razor.css`: 7 linhas
- `Pages/Futsal/Create.razor.css`: 1 linha
- `Pages/Product/Products.razor.css`: 1 linha
- `Shared/Components/CookieConsent.razor.css`: 1 linha
- `Shared/Components/MainLayout.razor.css`: 1 linha

**Fix aplicado pelo Senior**: Restaurados os caracteres UTF-8 originais a partir do git history.

**Nova regra 18**: NUNCA salvar arquivos CSS com encoding diferente de UTF-8. Se o editor local nao suporta UTF-8, nao editar linhas com caracteres acentuados.

**events.css conversao parcial**: Apenas 17 de 237 rgba convertidos. Os restantes (220) sao padroes unicos sem var correspondente (opacities especificas de events: `rgba(96,165,250,0.35)`, `rgba(239,68,68,0.08)`, `rgba(251,191,36,0.85)`, etc.).

---

## Metricas Atuais (pos-Ciclo 10 + fix Senior)

| Metrica | C4 | C5 | C6/C7 | C8 | C9 | C10 |
|---------|----|----|----|----|-----|-----|
| Warnings (build) | 2 | 0 | 0 | 0 | 0 | **0** |
| `!important` scoped | 1 | 1 | 1 | 1 | 1 | **1** |
| `!important` global | — | — | 17 | 12 | 11 | **12** |
| Hardcoded hex scoped | 1.099 | 867 | 0 | 0 | 0 | **0** |
| rgba() hardcoded scoped | — | — | 645 | 531 | 478 | **378** |
| Hardcoded hex global | — | — | ~1.317 | 166 | 166 | **153** |
| rgba() hardcoded global | — | — | ~185 | 446 | 413 | **394** |
| CSS vars (scoped) | 673 | 1.175 | 2.053 | 1.998 | 2.051 | **2.149** |
| CSS vars total | — | — | — | 3.513 | 3.609 | **3.739** |
| Vars no `:root` | ~68 | 105 | 179 | 158 | 168 | **168** |
| Vars indefinidas | — | 0 | 0 | 0 | 0 | **0** |
| Vars mortas `:root` | — | — | 18 | 0 | 0 | **0** |
| Fallbacks | — | 178 | 0 | 0 | 4 | **0** |

### Estado dos CSS globais (pos-Ciclo 10)

| Arquivo | Hex hardcoded | rgba hardcoded | `!important` | var() usadas |
|---------|-------------|----------------|-------------|--------------|
| site.css (fora :root) | 2 | 136 | 11 | ~1.100 |
| events.css | 110 | 220 | 0 | ~400 |
| marketplace.css | 43 | 35 | 0 | 1 |
| identity.css | 0 | 3 | 0 | ~32 |
| **Total globais** | **155** | **394** | **11** | **~1.533** |

### Paginas grandes (>500 linhas)

| Arquivo | Linhas | Status |
|---------|--------|--------|
| AdminPayments.razor | 1.156 | 4 sub-componentes ja extraidos |
| Futsal/Detail.razor | 931 | 9 sub-componentes em Components/ |
| Payment/EventPayment.razor | 667 | 7 sub-componentes em Components/ |
| Futsal/Escalacao.razor | 661 | 6 sub-componentes em Components/ |
| Admin/AdminLogs.razor | 656 | 1 sub-componente (AdminLogsTable) |
| Groups/Payments.razor | 560 | Candidato decomposicao |
| Poker/Edit.razor | 547 | Candidato decomposicao |

*Mailbox (66L), Poker/Detail (315L), Poker/Create (319L) saíram da lista — decomposicao Ciclo 10.*

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
17. **Cobrir TODOS os arquivos ao converter rgba** — grep global para cada padrao, converter todos de uma vez
18. **NUNCA salvar CSS com encoding diferente de UTF-8** — verificar encoding antes de commitar. Se o editor corromper acentos em comentarios, reverter a linha com `git checkout -- arquivo` antes de commitar

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

## Ciclo 11 — Tarefas Ativas

**Foco**: Criar vars para rgba frequentes em events.css + marketplace.css refactoring + decomposicao

**Branch**: `refactor/ciclo11-events-marketplace-decomp`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Adicionar ~12 novas vars rgba ao `:root` para events.css — P0
**Estimativa**: ~20 min

events.css tem 220 rgba, muitos com 3-6 usos cada. Adicionar vars para os padroes mais frequentes:

```css
/* Blue-300 mid opacity (96,165,250) */
--blue300-opacity-md:  rgba(96, 165, 250, 0.3);    /* 6 usos */
--blue300-opacity-lg:  rgba(96, 165, 250, 0.35);   /* 4 usos */
--blue300-opacity-xl:  rgba(96, 165, 250, 0.5);    /* 3 usos */
--blue300-opacity-2xl: rgba(96, 165, 250, 0.85);   /* 4 usos */

/* Red event opacity (239,68,68) */
--red-opacity-xs:      rgba(239, 68, 68, 0.08);    /* 6 usos */
--red-opacity-lg-mid:  rgba(239, 68, 68, 0.35);    /* 6 usos */

/* Amber event opacity (251,191,36) */
--amber-opacity-xs:    rgba(251, 191, 36, 0.08);   /* 5 usos */
--amber-opacity-md:    rgba(251, 191, 36, 0.25);   /* 3 usos */
--amber-opacity-lg:    rgba(251, 191, 36, 0.35);   /* 3 usos */
--amber-opacity-2xl:   rgba(251, 191, 36, 0.85);   /* 3 usos */

/* Emerald (52,211,153) */
--emerald-opacity-md:  rgba(52, 211, 153, 0.3);    /* 3 usos */

/* Slate extended (148,163,184) */
--slate-border-xl:     rgba(148, 163, 184, 0.25);  /* 3 usos */
```

**Total**: ~12 novas vars para ~49 usos em events.css

**Validacao**: `dotnet build`

---

### Fase 2: Converter rgba em events.css usando novas + existentes vars — P0
**Estimativa**: ~1.5h

Converter os ~49 rgba que agora tem vars (da Fase 1) + qualquer padrao restante com var existente.

**ATENCAO encoding**: NAO editar linhas com caracteres acentuados nos comentarios. Se precisar editar perto de um comentario com acentos, copiar a linha original sem modificar o comentario.

**Validacao**:
```bash
dotnet build
grep -c 'rgba(' wwwroot/css/events.css
# Meta: <175
```

---

### Fase 3: Refatorar marketplace.css — hex -> vars — P1
**Estimativa**: ~1.5h

marketplace.css tem 43 hex e apenas 1 var usage. E o CSS global menos refatorado. Os hex sao todos tons de brown/parchment que tem vars existentes:

```
#d8c093 -> var(--parchment-dark)    ou similar
#bfa06c -> var(--gold-deep)         ou similar
#7f5e34 -> var(--brown-mid)         ou similar
#3f2914 -> var(--bg-dark)           ou similar
#2f1e0c -> var(--bg-deep)           ou similar
```

**PROCEDIMENTO**: Para cada hex, encontrar a var mais proxima na lista permitida. Se nao existir var proxima, documentar na secao "Cores sem Var".

**Validacao**:
```bash
dotnet build
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/marketplace.css
# Meta: <15
```

---

### Fase 4: Decomposicao de Groups/Payments.razor (560L -> <400L) — P2
**Estimativa**: ~1.5h

Extrair sub-componentes seguindo o padrao de Mailbox (Ciclo 10):

1. `GroupPaymentsSummary` — resumo financeiro do grupo
2. `GroupPaymentsTable` — tabela de pagamentos com filtros

**REGRAS de decomposicao**:
- Componente filho recebe dados via `[Parameter]`
- Eventos de volta via `[Parameter] EventCallback`
- CSS vai no `.razor.css` do componente filho (NAO global)
- Usar `partial class` com code-behind `.razor.cs`
- Usar `IDbContextFactory<AppDbContext>` (NAO AppDbContext direto)

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Groups/Payments.razor
# Meta: <400
```

---

### Fase 5: Decomposicao de Poker/Edit.razor (547L -> <400L) — P2
**Estimativa**: ~1.5h

Similar a Poker/Create. Extrair:

1. Code-behind `Poker/Edit.razor.cs` — logica de edicao
2. Reutilizar `PokerDetailInfo` se aplicavel

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Poker/Edit.razor
# Meta: <400
```

---

### Fase 6: Converter rgba restantes em site.css (fora do :root) — P1
**Estimativa**: ~1h

site.css tem 136 rgba fora do `:root`. Muitos sao padroes unicos de metallic gradients. Converter os que tem var existente e documentar os que nao tem.

Os 2 hex restantes (`#e0f7f4`, `#ecfdf5`) sao mint green sem var — documentar na secao "Cores sem Var".

**Validacao**:
```bash
dotnet build
```

---

## Ordem de Execucao

```
Fase 1 (novas vars) -> Fase 2 (events.css rgba) -> Fase 3 (marketplace.css hex) -> Fase 4 (decomp Groups/Payments) -> Fase 5 (decomp Poker/Edit) -> Fase 6 (site.css rgba)
```

**Meta Ciclo 11**: events.css rgba <175, marketplace.css hex <15, 2 paginas decompostas (<400L cada), site.css rgba <120

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

# Verificar vars mortas:
while IFS= read -r var; do
  count=$(grep -rPo "var\(--${var}\)" Pages/ Shared/ wwwroot/css/ --include="*.css" | wc -l)
  if [ "$count" -eq 0 ]; then echo "MORTA: --$var"; fi
done < /tmp/defined.txt
# Meta: 0

# Verificar encoding UTF-8:
python3 -c "
import glob
for p in ['Pages/**/*.css','Shared/**/*.css','wwwroot/css/*.css']:
    for f in glob.glob(p, recursive=True):
        with open(f,'rb') as fh:
            try: fh.read().decode('utf-8')
            except: print(f'ENCODING: {f}')
"
# Meta: 0 arquivos listados
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui rgba() ou hex que nao tem var equivalente na lista -->
<!-- Formato: valor | arquivo | contexto (shadow, overlay, border, gradient) -->

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores com !important que nao conseguiu resolver -->

### Problemas de decomposicao
<!-- Liste aqui dificuldades na extracao de componentes -->

### Outras observacoes
