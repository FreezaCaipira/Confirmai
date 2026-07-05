# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 11 + revisao Senior) | Refatoracao CSS CONCLUIDA
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
- PR #10: Auditoria Services -- namespaces, GatewayService:ControllerBase, file-scoped

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
- **Problemas CRITICOS**: 18 vars inventadas sem definir (128 usos, UI quebrada) -- corrigido pelo Senior. 18 vars mortas no `:root`

### Ciclo 7 (Pleno Local): Legibilidade de Fontes (UX)
- Melhorou contraste de fontes com text-shadow glow
- **Problemas**: Hardcoded hex/rgba em vez de vars existentes, commits baguncados

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
- 165 rgba convertidos em 32 arquivos, events.css/identity.css/site.css convertidos, 3 decomposicoes
- **Problemas**: Encoding UTF-8 corrompido em 6 CSS files (corrigido pelo Senior). Regra 18 adicionada

### Ciclo 11 (Pleno Local): CSS Final + Decomposicao Completa (10 fases)
- Branch: `refactor/ciclo11-final-cleanup` | PR #42
- **Detalhes da revisao Senior**: ver secao abaixo

---

## Revisao Senior do Ciclo 11

### Veredicto: EXCELENTE -- Ciclo mais ambicioso, TODAS as 10 fases executadas, metas superadas

| Metrica | C10 | C11 | Meta C11 | Status |
|---------|-----|-----|----------|--------|
| Hardcoded hex scoped | 0 | **0** | 0 | Atingido |
| rgba() hardcoded scoped | 378 | **185** | <300 | **Superado** |
| CSS vars usadas (scoped) | 2.149 | **2.344** | >2.200 | **Superado** |
| `!important` scoped | 1 | **1** | 1 | Atingido |
| `!important` global | 12 | **11** | -- | Estavel |
| events.css hex | 110 | **0** | <50 | **Zerado** |
| events.css rgba | 220 | **161** | <170 | **Atingido** |
| marketplace.css hex | 43 | **0** | <10 | **Zerado** |
| marketplace.css rgba | 35 | **0** | <15 | **Zerado** |
| site.css hex (fora :root) | 6 | **4** | -- | Melhoria |
| site.css rgba (fora :root) | 136 | **53** | <80 | **Atingido** |
| identity.css rgba | 3 | **3** | -- | Estavel |
| Vars no `:root` | 168 | **317** | -- | +149 |
| Vars indefinidas | 0 | **0** (apos fix) | 0 | Atingido |
| Vars mortas `:root` | 0 | **0** (apos fix) | 0 | Atingido |
| Fallbacks | 0 | **0** | 0 | Atingido |
| Build warnings | 0 | **0** | 0 | Atingido |
| Pages >400L sem code-behind | 12 | **0** | 0 | **Zerado** |
| Pages com `AppDbContext` direto | 6 | **3** | -- | Melhoria |

### O que deu CERTO

**Bloco A -- CSS (Fases 1-5) -- EXCELENTE**:
- **Fase 1**: 24 novas vars + 90 conversoes iniciais -- pipeline limpa, vars usadas imediatamente
- **Fase 2**: events.css rgba 197 -> 169 -- meta <170 atingida
- **Fase 3**: events.css hex 125 -> 0 -- ZERADO (meta era <50!)
- **Fase 4**: marketplace.css hex 51 -> 0, rgba 43 -> 0 -- arquivo CSS global mais limpo do projeto
- **Fase 5**: scoped rgba 613 -> 288, site.css rgba reduzido -- metas superadas

**Bloco B -- Decomposicao (Fases 6-10) -- EXCELENTE**:
- **TODAS as 12 paginas** decompostas com code-behind `.razor.cs`:

| Pagina | Antes | Depois (razor) | Code-behind |
|--------|-------|----------------|-------------|
| AdminPayments | 1.156L | 113L | 1.061L |
| Futsal/Detail | 931L | 483L | 422L |
| Groups/Payments | 560L | 264L | 311L |
| Poker/Edit | 547L | 311L | 248L |
| Groups/Features | 461L | 115L | 360L |
| EventPayment | 667L | 198L | 484L |
| Escalacao | 661L | 206L | 471L |
| AdminLogs | 656L | 109L | 556L |
| PaymentsHistory | 419L | 149L | 293L |
| Futsal/Create | 418L | 109L | 325L |
| Admin | 410L | 151L | 276L |
| MyEvents/Index | 402L | 234L | 181L |

- Todas usam `partial class` (correto)
- `IDbContextFactory` usado em todas as paginas decompostas (correto)
- Encoding fix commit incluido (7 arquivos .razor corrigidos)
- 11 commits limpos (1 por fase + 1 encoding fix), branch unica, 1 PR -- regras 9, 14 respeitadas

### Problemas encontrados e corrigidos pelo Senior

**1. 9 vars CSS inventadas/indefinidas (critico)**:
O Pleno usou `var(--nome)` em 9 nomes que NAO estavam definidos:
- `--ci-text-link-bright` (2 usos, site.css) -- original era `#bae6fd`
- `--yellow-dark` (1 uso), `--yellow-bright` (1 uso), `--yellow-strong` (1 uso) -- originais: `#1c1508`, `#fbbf24`, `#78350f`
- `--mk-brown2-opacity-xs` (1 uso, ParchmentLab), `--mk-brown6-opacity-xs` (2 usos, ParchmentLab)

**Fix**: Senior adicionou as 9 vars ao `:root` com valores hex deduzidos dos diffs originais.

**2. 6 vars mortas no `:root`**:
`--white-opacity-lg`, `--white-opacity-xl`, `--white-opacity-2xl`, `--white-opacity-3xl`, `--white-opacity-4xl`, `--white-opacity-5xl` -- definidas mas zero usos em qualquer arquivo.

**Fix**: Senior removeu as 6 vars mortas.

**3. Encoding UTF-8 corrompido em site.css (61 linhas)**:
Apesar da regra 18 e do encoding fix commit do Pleno, `site.css` teve 61 linhas com mojibake nos comentarios (`--` -> `ΓöÇ`, `--` -> `ΓÇö`). O Pleno corrigiu .razor mas ignorou o site.css.

**Fix**: Senior restaurou encoding UTF-8 em todas as 61 linhas.

---

## Metricas Atuais (pos-Ciclo 11 + fix Senior)

| Metrica | C4 | C5 | C6/C7 | C8 | C9 | C10 | C11 |
|---------|----|----|-------|----|----|-----|-----|
| Warnings (build) | 2 | 0 | 0 | 0 | 0 | 0 | **0** |
| `!important` scoped | 1 | 1 | 1 | 1 | 1 | 1 | **1** |
| `!important` global | -- | -- | 17 | 12 | 11 | 12 | **11** |
| Hardcoded hex scoped | 1.099 | 867 | 0 | 0 | 0 | 0 | **0** |
| rgba() hardcoded scoped | -- | -- | 645 | 531 | 478 | 378 | **185** |
| Hardcoded hex global | -- | -- | ~1.317 | 166 | 166 | 153 | **4** |
| rgba() hardcoded global | -- | -- | ~185 | 446 | 413 | 394 | **217** |
| CSS vars (scoped) | 673 | 1.175 | 2.053 | 1.998 | 2.051 | 2.149 | **2.344** |
| CSS vars total | -- | -- | -- | 3.513 | 3.609 | 3.739 | **4.246** |
| Vars no `:root` | ~68 | 105 | 179 | 158 | 168 | 168 | **317** |
| Vars indefinidas | -- | 0 | 0 | 0 | 0 | 0 | **0** |
| Vars mortas `:root` | -- | -- | 18 | 0 | 0 | 0 | **0** |
| Fallbacks | -- | 178 | 0 | 0 | 4 | 0 | **0** |
| Pages >400L sem code-behind | -- | -- | -- | -- | 12 | 9 | **0** |

### Estado dos CSS globais (pos-Ciclo 11)

| Arquivo | Hex hardcoded | rgba hardcoded | `!important` | var() usadas |
|---------|-------------|----------------|-------------|--------------|
| site.css (fora :root) | 4 | 53 | 11 | ~1.900 |
| events.css | 0 | 161 | 0 | ~598 |
| marketplace.css | 0 | 0 | 0 | ~79 |
| identity.css | 0 | 3 | 0 | ~32 |
| **Total globais** | **4** | **217** | **11** | **~2.609** |

### Paginas grandes -- TODAS decompostas

Nenhuma pagina >400L sem code-behind. Unica pagina >400L com code-behind: `Futsal/Detail.razor` (483L) -- aceitavel dado que ja tem 9+ sub-componentes.

### AppDbContext direto restante (3 paginas)

| Pagina | Status |
|--------|--------|
| Payment/ViewPayment.razor | AppDbContext direto (nao decompostas neste ciclo) |
| Payment/Payment.razor | AppDbContext direto (ja decomposta em Ciclo 9) |
| Payment/PaymentDetails.razor | AppDbContext direto (nao decompostas neste ciclo) |

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** -- se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores em scoped CSS** -- usar vars do `:root`. Se nao existe var, documentar na secao "Cores sem Var"
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** -- sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** -- so criar se tiver estilos reais
6. **NUNCA introduzir novas cores hardcoded** ao converter inline -> classe CSS
7. **NUNCA usar fallback em var()** -- usar `var(--nome)` sem fallback hex
8. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"` (0 failed)
9. **1 branch unica por ciclo** -- commits por fase dentro dela. NAO criar branches separadas
10. **1 PR por ciclo** -- mergear via PR, nunca push direto na main
11. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados"
12. **NUNCA inventar nomes de var()** -- so usar vars que JA existem na lista "CSS Vars Permitidas" abaixo
13. **NUNCA adicionar var ao `:root` sem uso imediato** -- so adicionar vars que serao usadas no mesmo commit
14. **Commits limpos** -- 1 commit por fase, sem commits de tentativa/erro/reversao. Testar ANTES de commitar
15. **NUNCA remover var do `:root` sem verificar uso em TODOS os arquivos** -- usar `grep -rn 'var(--nome)' Pages/ Shared/ wwwroot/css/` antes de remover. Se tem uso, NAO remover
16. **Separar CSS refactoring de features UX** -- nao misturar os dois no mesmo ciclo/PR
17. **Cobrir TODOS os arquivos ao converter rgba** -- grep global para cada padrao, converter todos de uma vez
18. **NUNCA salvar CSS com encoding diferente de UTF-8** -- verificar encoding antes de commitar. Se o editor corromper acentos em comentarios, reverter a linha com `git checkout -- arquivo` antes de commitar
19. **Verificar encoding em TODOS os CSS apos cada fase** -- rodar script de verificacao UTF-8 (ver secao Comandos de Validacao). Inclui site.css, events.css, marketplace.css, identity.css e TODOS os scoped CSS

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
var(--yellow-dark)       /* #1c1508 */
var(--yellow-bright)     /* #fbbf24 */
var(--yellow-strong)     /* #78350f */

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
var(--ci-text-link-bright) /* #bae6fd */
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

> **NOTA**: Existem ~130 vars adicionais de opacity/marketplace no `:root` (mk-brown*, mk-gold*, mk-cream*, pay-cream*, etc.) que nao estao listadas aqui por brevidade. Consultar o `:root` em `site.css` para a lista completa.

---

## Estado Final da Refatoracao CSS

A refatoracao CSS iniciada no Ciclo 4 esta **essencialmente concluida**. Os numeros restantes sao predominantemente padroes unicos (1-2 usos) que nao justificam novas vars:

- **185 rgba scoped**: 183 padroes unicos -- genuinamente diferentes opacidades por contexto
- **161 rgba events.css**: 130 padroes unicos -- opacidades especificas por tipo de evento
- **53 rgba site.css**: quase todos 1-off (metallic gradients, specific opacity)
- **4 hex site.css**: 2 mint green (`#e0f7f4`, `#ecfdf5`) sem var equivalente

**Criar vars para esses padroes seria contraproducente**: inflaria o `:root` (ja com 317 vars) sem beneficio real de reuso.

### Trabalho restante (nao-CSS)

| Item | Descricao | Prioridade |
|------|-----------|------------|
| AppDbContext -> IDbContextFactory | 3 paginas restantes: ViewPayment, Payment, PaymentDetails | P1 |
| !important site.css | 11 ocorrencias, maioria legitima (autofill, accessibility, modals) | P3 |
| Build warnings (Tests) | 49 warnings de xUnit2013 no projeto de testes (nao afetam producao) | P3 |

---

## Ciclo 12 -- UX/UI Fixes + Cleanup Tecnico

> Ciclo misto: 8 pontos UX/UI levantados em testes do sistema + 3 tarefas de cleanup tecnico.
> Pontos 9-11 do backlog (mobile, refresh token, repensamento de fluxo) ficam para o Ciclo 13 por serem mudancas arquiteturais maiores.

**Branch**: `fix/ciclo12-ux-cleanup`
**1 commit por fase** dentro da branch. **1 PR** no final.

---

### BLOCO A -- FIXES UX/UI (Fases 1-8)

#### Fase 1: Card do grupo perdeu efeito de estilizacao -- P0
**Estimativa**: ~30 min

O card de grupo em `Pages/Groups/Index.razor` perdeu efeito visual. O styling esta em `wwwroot/css/events.css` (classes `.group-card`, `.group-card-overlay`, `.group-card:hover`).

**Procedimento**:
1. Verificar se `.group-card` em `events.css` (linha ~2957) tem gradientes/sombras/hover corretos
2. Comparar com versao anterior (`git diff HEAD~15 -- wwwroot/css/events.css | grep group-card`)
3. Restaurar efeitos que foram perdidos na conversao hex -> var do Ciclo 11
4. Testar visualmente: o card deve ter overlay com gradiente, hover com elevacao, sombra sutil

**Validacao**: `dotnet build` + verificar visualmente em `/grupos`

---

#### Fase 2: Partidas em nova janela na tela do grupo -- P1
**Estimativa**: ~30 min

Na tela `Pages/Groups/Detail.razor`, avaliar se links para partidas devem abrir em nova aba.

**Procedimento**:
1. Identificar os links de partida no `Detail.razor`
2. Se o link navega para `/futsal/{id}`, considerar se `target="_blank"` faz sentido no contexto SPA
3. **DECISAO**: Em Blazor Server (SPA), abrir nova janela quebra o circuito SignalR. Manter navegacao interna a menos que o dono do projeto insista.
4. Se decidir abrir nova aba, usar `<a href="/futsal/{id}" target="_blank">` e documentar que cria novo circuito

**Validacao**: `dotnet build` + testar navegacao em `/grupo/{id}`

---

#### Fase 3: Pagamentos pendentes -- btn comprovante + btns email/zap -- P1
**Estimativa**: ~1h

Na tela de pagamentos pendentes, avaliar:
1. **Btn comprovante**: verificar se esta posicionado de forma logica. Se nao, mover para posicao mais intuitiva
2. **Btns email e WhatsApp**: verificar se existem e estao funcionais. Se faltam, adicionar botoes para enviar lembrete por email/WhatsApp

**Arquivos**: `Pages/Payment/PaymentsHistory.razor`, `Pages/Payment/PaymentsHistory.razor.cs`, `Pages/Payment/PaymentsHistory.razor.css`

**Validacao**: `dotnet build` + testar visualmente nos pagamentos pendentes

---

#### Fase 4: Pagamentos historico -- caracteres bugados + valor verde -- P0
**Estimativa**: ~1h

Duas questoes na tela `Pages/Payment/PaymentsHistory.razor`:
1. **Caracteres bugados**: Verificar encoding de strings exibidas (pode ser UiText com encoding errado ou dados do banco com acentos corrompidos). Verificar `PaymentsHistory.razor.css` por mojibake nos comentarios
2. **Valor verde**: O CSS mostra `color: var(--green-bright)` nas linhas 85 e 408 do `.razor.css`. Avaliar se o valor monetario deve ser verde (pode confundir com "pago" quando o pagamento esta pendente). Se sim, usar cor neutra (`var(--ci-text)`) para valores e manter verde so para status "Pago"

**Validacao**: `dotnet build` + testar visualmente em `/pagamentos-historico`

---

#### Fase 5: Pos-partida -- legibilidade + btn confirmar placar + btn edit -- P1
**Estimativa**: ~1.5h

Na tela de `Pages/Futsal/Detail.razor` apos a partida terminar:
1. **Legibilidade**: Textos com baixo contraste contra fundo escuro. Verificar cores de texto no CSS scoped (`Detail.razor.css`) e global (`events.css`). Usar vars de texto legiveis (`var(--ci-text)`, `var(--ci-text-blue)`)
2. **Btn confirmar placar nao faz nada**: O componente `EscalacaoScoreEditor.razor` pode ter evento `@onclick` sem handler funcional ou handler que falha silenciosamente. Verificar binding e logica no code-behind
3. **Btn edit**: Avaliar se faz sentido ter btn de editar na tela pos-partida. Se a partida ja terminou, editar nao deveria ser permitido. Esconder btn quando `ev.Status == EventStatus.Finished`

**Validacao**: `dotnet build` + `dotnet test` + testar visualmente apos uma partida encerrada

---

#### Fase 6: Edit partida -- endereco diferente + texto exposto -- P1
**Estimativa**: ~1h

Na tela `Pages/Futsal/Edit.razor`:
1. **Endereco**: O campo de endereco nao exibe o endereco da mesma forma que na tela da partida (`Detail.razor`). Verificar como o endereco e renderizado em ambas as telas e unificar o formato
2. **Texto exposto no fim da pagina**: Pode ser um bloco `@code` mal fechado, texto de debug ou conteudo HTML fora de tag. Inspecionar o final de `Edit.razor` e remover/encapsular texto exposto

**Validacao**: `dotnet build` + testar visualmente em `/futsal/edit/{id}`

---

#### Fase 7: Tela eventos -- background invertido + btn criar partida -- P1
**Estimativa**: ~1h

Na tela `Pages/MyEvents/Index.razor`:
1. **Background invertido**: O CSS (`Index.razor.css`) pode ter cores de fundo trocadas (ex: fundo escuro onde deveria ser claro ou vice-versa). Verificar classes `entity-shell` e background vars
2. **Btn criar partida**: Avaliar se faz sentido ter btn "Criar Partida" aqui. Se o fluxo ideal e Grupos -> Eventos, considerar trocar por "Ver Grupos" (`/grupos`) ou mover btn de criacao para dentro da tela do grupo
3. Se decidir manter os dois, documentar o racional

**Validacao**: `dotnet build` + testar visualmente em `/meus-eventos`

---

#### Fase 8: Layout profile vs resto das telas -- P2
**Estimativa**: ~1.5h

A tela `Pages/Profile.razor` usa layout diferente das demais (nao usa `entity-shell` padrao). Avaliar:
1. Verificar se `Profile.razor` usa `entity-shell` ou wrapper customizado
2. Comparar com layout de outras telas (ex: `Groups/Detail`, `Futsal/Detail`)
3. Se divergir, padronizar para usar `entity-shell` com os mesmos tokens de background/border
4. Verificar tambem se `ProfileHeaderCard.razor`, `ProfileEditForm.razor` seguem o mesmo pattern

**Validacao**: `dotnet build` + comparar visualmente Profile vs outras telas

---

### BLOCO B -- CLEANUP TECNICO (Fases 9-11)

#### Fase 9: Migrar 3 paginas de AppDbContext -> IDbContextFactory -- P1
**Estimativa**: ~1h

Migrar `ViewPayment.razor`, `Payment.razor`, `PaymentDetails.razor`:
1. Substituir `@inject AppDbContext Db` por `@inject IDbContextFactory<AppDbContext> DbFactory`
2. Em cada metodo, usar `await using var db = await DbFactory.CreateDbContextAsync();`
3. Se a pagina nao tem code-behind, criar `.razor.cs` com `partial class`

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
grep -rn '@inject AppDbContext' Pages/ Shared/ --include="*.razor"
# Meta: 0 resultados
```

---

#### Fase 10: Reduzir !important em site.css -- P3
**Estimativa**: ~30 min

Analisar os 11 `!important` em site.css. Os seguintes sao LEGITIMOS e devem ser mantidos:
- Linhas 1255-1257: `prefers-reduced-motion` (padrao de acessibilidade W3C)
- Linhas 6549-6555: Override de autofill do Chrome (necessario)
- Linha 6464: `z-index: 9999` em modal (pattern aceitavel)

Os seguintes PODEM ser removidos via refatoracao de especificidade:
- Linha 883: `display: none !important` -- verificar se cascade resolve
- Linha 4689: `color !important` -- verificar especificidade

**Validacao**: testar visualmente apos cada remocao.

---

#### Fase 11: Eliminar warnings xUnit2013 nos testes -- P3
**Estimativa**: ~30 min

49 warnings `xUnit2013: Do not use Assert.Equal() to check for collection size`. Substituir:
- `Assert.Equal(1, collection.Count)` -> `Assert.Single(collection)`
- `Assert.Equal(0, collection.Count)` -> `Assert.Empty(collection)`

**Validacao**: `dotnet build 2>&1 | grep 'warning' | grep -v 'xUnit'` -> 0 resultados

---

## Ciclo 13 -- Backlog Arquitetural (para ciclo futuro)

> Estes pontos sao mudancas maiores que requerem planejamento e possivelmente mais de 1 ciclo.

### 13.1: Testar versao web mobile -- P1
Verificar responsividade em viewport mobile (375px, 414px). Usar breakpoints ja padronizados (640/768/1024/1440px).
- Testar telas principais: grupos, partida, pagamentos, perfil
- Documentar problemas de overflow, touch targets <44px, texto cortado

### 13.2: Implementar refresh token -- P0
O sistema atual nao renova tokens de autenticacao automaticamente. Implementar:
- Refresh token com `ITicketStore` ou cookie sliding expiration
- Testar sessao longa (>30min) sem perda de autenticacao
- Avaliar impacto em Blazor Server (circuito SignalR ja mantem sessao)

### 13.3: Repensar fluxo de navegacao -- P1
Fluxo atual: tela inicial = Grupos. Proposta:
- Tela inicial = Eventos (proximas partidas)
- Grupos -> ver eventos do grupo -> criar partida dentro do grupo
- Separar criacao de partida da tela geral de eventos
- Requer: alterar `@page "/"` de `Groups/Index.razor` para nova pagina de eventos, criar navegacao coerente

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
grep -rPoh 'var\(--([\w-]+)\)' Pages/ Shared/ wwwroot/css/ --include="*.css" | grep -oP '\-\-([\w-]+)' | sed 's/^--//' | sort -u > /tmp/used.txt
grep -rPoh '^\s*--([\w-]+)\s*:' Pages/ Shared/ wwwroot/css/ --include="*.css" | sed 's/^\s*--//' | sed 's/\s*://' | sort -u > /tmp/local.txt
cat /tmp/defined.txt /tmp/local.txt | sort -u > /tmp/all.txt
comm -23 /tmp/used.txt /tmp/all.txt
# Meta: 0 linhas

# Verificar vars mortas:
while IFS= read -r var; do
  count=$(grep -rPo "var\(--${var}\)" Pages/ Shared/ wwwroot/css/ --include="*.css" | wc -l)
  if [ "$count" -eq 0 ]; then echo "MORTA: --$var"; fi
done < /tmp/defined.txt
# Meta: 0

# Verificar encoding UTF-8 em TODOS os CSS (inclui site.css!):
python3 -c "
import glob
for p in ['Pages/**/*.css','Shared/**/*.css','wwwroot/css/*.css']:
    for f in glob.glob(p, recursive=True):
        with open(f,'rb') as fh:
            try: fh.read().decode('utf-8')
            except: print(f'ENCODING: {f}')
for p in ['wwwroot/css/*.css']:
    for f in glob.glob(p, recursive=True):
        with open(f,'r') as fh:
            for i,line in enumerate(fh,1):
                if 'ΓöÇ' in line or 'ΓÇö' in line:
                    print(f'MOJIBAKE: {f}:{i}')
                    break
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
