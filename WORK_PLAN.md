# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 7 + fix Senior) | Ciclo 8 ATIVO
> 1.674/1.674 testes passando | 0 erros de build

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
- Branch: `refactor/ciclo6-css-vars-final` | PR #32
- Convertidos ~74 hardcoded colors em 37 arquivos + adicionadas 57 vars rgba/shadow/opacity ao `:root`
- Hardcoded hex: 867 -> 0 | Vars usadas: 1.175 -> ~1.476 | Fallbacks: 0

#### Revisao Senior do Ciclo 6

**Veredicto: PROBLEMAS CRITICOS corrigidos pelo Senior**

| Problema | Impacto | Fix |
|----------|---------|-----|
| **18 vars inventadas sem definir no `:root`** — 128 usos renderizando como vazio | CRITICO — UI quebrada | Senior adicionou 18 vars com hex deduzidos dos diffs |
| **18 vars rgba/opacity MORTAS** — definidas mas nunca usadas em nenhum arquivo | MEDIO — :root poluido | Documentado para limpeza |
| **4 hardcoded hex introduzidos na PR #33** — `#deeeff` hardcoded | BAIXO — deveria ser var() | Senior converteu para `var(--ci-text-blue)` |

**18 vars inventadas pelo Pleno (agora definidas pelo Senior)**:
```
--white (#ffffff), --green-strong (#22c55e), --amber-mid (#d97706),
--amber-dark (#b45309), --amber-strong (#e65100), --amber-lightest (#fffbeb),
--ci-bg-mid (#112240), --ci-bg-deep (#0d1825), --ci-bg-dark (#0a1f35),
--red-dark (#dc2626), --red-bg-dark (#290f0f), --brown-dark (#4a2c15),
--brown-muted (#8b7355), --poker-border (#2d1a4a), --pink-mid (#f472b6),
--slate-dark (#334155), --slate-pale (#e2e8f0), --slate-warm (#78716c)
```

**18 vars rgba/opacity MORTAS (definidas mas nao usadas)**:
```
--shadow-sm, --shadow-3xl, --shadow-4xl, --text-shadow-md,
--text-shadow-lg, --text-shadow-xl, --inset-accent-md,
--overlay-md, --overlay-xl, --overlay-2xl,
--green-opacity-xl, --green-opacity-2xl,
--red-opacity-sm, --red-opacity-md, --red-opacity-lg,
--slate-border-xl, --green-dark-sm, --green-dark-xl
```

**Positivo**: Conversoes mecanicas corretas, 0 fallbacks, branch unica, 57 vars rgba organizadas por categoria

### Ciclo 7 (Pleno Local): Legibilidade de Fontes (UX)
- Branch: `improve/font-contrast` | PR #33
- Substituiu `#8aacc8` (ci-text-muted) por cores mais claras com text-shadow glow em ~18 seletores no site.css
- Melhorou contraste de: tagline, nav links, sidebar links, news ticker, entity-shell-subtitle, admin labels, admin buttons

#### Revisao Senior do Ciclo 7

**Veredicto: BOM trabalho visual, problemas tecnicos**

| Aspecto | Avaliacao |
|---------|-----------|
| Decisao de design (clarear fontes) | Correta — melhora real de legibilidade |
| Execucao tecnica | Problematica |

**Problemas**:
1. **Hardcoded hex**: Usou `#deeeff` e `#fffaf0` hardcoded em 20+ lugares em vez de `var(--ci-text-blue)` e vars existentes (CORRIGIDO pelo Senior em scoped CSS, ~20 em site.css restantes)
2. **rgba() hardcoded**: Adicionou `rgba(79, 156, 248, 0.3)` e `rgba(255, 230, 182, 0.4)` hardcoded em ~26 text-shadows (deveria usar `var(--shadow-accent-sm)`)
3. **Regressao**: Substituiu `var(--entity-muted)` por `#deeeff` no EntityProfileShell.razor.css — removeu referencia a var funcional (CORRIGIDO pelo Senior)
4. **Commits bagunçados**: 8 commits de tentativa/erro/reversao em vez de 1-2 commits limpos

---

## Diagnostico CSS Atualizado (Pos-Ciclo 7 + Fix Senior)

| Metrica | C3 | C4 | C5 | C6/C7 | Meta C8 |
|---------|----|----|----|----|---------|
| Hardcoded hex scoped | 1.387 | 1.099 | 867 | **0** | 0 |
| rgba() hardcoded scoped | — | — | — | **645** | <300 |
| CSS vars usadas (scoped) | 381 | 673 | 1.175 | **2.053** | >2.200 |
| `!important` scoped | 16 | 1 | 1 | **1** | 1 |
| Inline estaticos | ~35 | 6 | 0 | **0** | 0 |
| Vars no `:root` | ~68 | ~68 | 105 | **179** | 179+ |
| Vars indefinidas | — | — | 0 | **0** (fix) | 0 |
| Vars mortas (no :root, sem uso) | — | — | — | **18** | 0 |

### Estado dos CSS globais (wwwroot/css/)

| Arquivo | Hardcoded hex | rgba hardcoded | `!important` |
|---------|-------------|----------------|-------------|
| site.css | ~841 (incl. :root defs) | ~100 | 13 |
| events.css | 403 | ~50 | 4 |
| marketplace.css | 44 | ~30 | 0 |
| identity.css | 29 | ~5 | 0 |
| **Total globais** | **~1.317** | **~185** | **17** |

### Paginas grandes (candidatas a decomposicao)

| Arquivo | Linhas |
|---------|--------|
| AdminPayments.razor | 1.151 |
| Futsal/Detail.razor | 933 |
| Groups/Detail.razor | 818 |
| Mailbox.razor | 673 |
| AdminLogs.razor | 662 |
| Futsal/Escalacao.razor | 661 |
| Poker/Detail.razor | 634 |
| Payment/Payment.razor | 626 |

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

---

## CSS Vars Permitidas (usar SEMPRE em vez de hex/rgba)

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
var(--futsal-text-light)   /* #f0fdf4 */

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

/* Accent shadows */
var(--shadow-accent-sm)  /* rgba(79,156,248,0.3) */
var(--shadow-accent-md)  /* rgba(79,156,248,0.4) */
var(--shadow-accent-lg)  /* rgba(79,156,248,0.5) */

/* Green shadows */
var(--shadow-green-sm)   /* rgba(74,222,128,0.3) */
var(--shadow-green-lg)   /* rgba(74,222,128,0.6) */

/* Red shadows */
var(--shadow-red-sm)     /* rgba(239,68,68,0.3) */

/* Text shadows */
var(--text-shadow-sm)    /* rgba(0,0,0,0.3) */

/* Inset shadows */
var(--inset-accent-sm)   /* rgba(147,197,253,0.2) */

/* Overlays */
var(--overlay-sm)        /* rgba(0,0,0,0.3) */
var(--overlay-lg)        /* rgba(0,0,0,0.55) */

/* Accent opacity */
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
var(--red-opacity-lg)    /* rgba(239,68,68,0.5) */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--font-accent)       /* "MedievalSharp", Georgia, cursive */
var(--ci-font)           /* system sans-serif stack */
```

**NOTA**: Vars mortas (definidas no `:root` mas NAO listadas acima) serao removidas no Ciclo 8 Fase 1.

---

## Ciclo 8 — Tarefas Ativas

**Foco**: Limpar vars mortas + converter rgba hardcoded restantes em scoped CSS + iniciar globais

**Branch**: `refactor/ciclo8-rgba-cleanup`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Remover 18 vars mortas do `:root` — P0
**Estimativa**: ~15 min

Remover do bloco `:root` em `wwwroot/css/site.css` as vars que foram definidas mas NUNCA sao usadas:

```
--shadow-sm, --shadow-3xl, --shadow-4xl,
--text-shadow-md, --text-shadow-lg, --text-shadow-xl,
--inset-accent-md, --overlay-md, --overlay-xl, --overlay-2xl,
--green-opacity-xl, --green-opacity-2xl,
--red-opacity-sm, --red-opacity-md, --red-opacity-lg,
--slate-border-xl, --green-dark-sm, --green-dark-xl
```

**IMPORTANTE**: Antes de remover, confirmar que a var NAO e usada em NENHUM arquivo:
```bash
grep -rn 'var(--shadow-sm)' Pages/ Shared/ wwwroot/css/ --include="*.css"
# Se retornar 0 linhas: pode remover
```

**Validacao**:
```bash
dotnet build
```

---

### Fase 2: Converter rgba() hardcoded no top 5 arquivos scoped — P0
**Estimativa**: ~1.5h

645 rgba() hardcoded restantes em scoped CSS. Converter os 5 maiores para usar vars do `:root`:

1. `Shared/Components/Groups/GroupDetailPaymentsModal.razor.css` (60 rgba)
2. `Pages/Futsal/Escalacao.razor.css` (52 rgba)
3. `Pages/Payment/Payment.razor.css` (48 rgba)
4. `Pages/Payment/EventPayment.razor.css` (42 rgba)
5. `Pages/Groups/Payments.razor.css` (42 rgba)

**Mapeamento** (usar as vars rgba da lista acima):
```
rgba(0, 0, 0, 0.3)       ->  var(--shadow-md) ou var(--overlay-sm)
rgba(0, 0, 0, 0.5)       ->  var(--shadow-xl) ou var(--overlay-lg) depende do contexto
rgba(79, 156, 248, 0.3)  ->  var(--shadow-accent-sm) ou var(--accent-opacity-md)
rgba(74, 222, 128, 0.3)  ->  var(--shadow-green-sm) ou var(--green-opacity-md)
rgba(239, 68, 68, 0.3)   ->  var(--shadow-red-sm)
```

**REGRAS**:
- Se o rgba NAO tem var equivalente na lista, deixar hardcoded e documentar na secao "Cores sem Var"
- NAO inventar vars novas — so usar as que existem
- Verificar que a substituicao faz sentido semanticamente (shadow var para box-shadow, overlay var para backgrounds, opacity var para borders/bg-colors)

**Validacao**:
```bash
dotnet build
grep -c 'rgba(' Shared/Components/Groups/GroupDetailPaymentsModal.razor.css
# Meta: <15
```

---

### Fase 3: Converter rgba() hardcoded nos proximos 5 arquivos scoped — P1
**Estimativa**: ~1.5h

6. `Pages/Product/Marketplace.razor.css` (37 rgba)
7. `Pages/Admin/AdminPayments.razor.css` (34 rgba)
8. `Pages/Admin/ParchmentLab.razor.css` (24 rgba)
9. `Pages/Mailbox.razor.css` (20 rgba)
10. `Pages/Groups/Components/MembersManager.razor.css` (20 rgba)

**Validacao**:
```bash
dotnet build
grep -rn 'rgba(' Pages/ Shared/ --include="*.css" | grep -v 'var(--' | wc -l
# Meta: <300
```

---

### Fase 4: Converter hardcoded em events.css para vars — P1
**Estimativa**: ~1h

`events.css` tem 403 hardcoded hex + ~50 rgba + 4 `!important`.

1. Converter hex -> var() onde a var existe
2. Converter rgba -> var() onde a var existe
3. Remover os 4 `!important` (tentar resolver especificidade)
4. Remover fallbacks `var(--nome, #hex)` -> `var(--nome)`

**Validacao**:
```bash
dotnet build
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/events.css
# Meta: <100
grep -c '!important' wwwroot/css/events.css
# Meta: 0
```

---

### Fase 5: Converter hardcoded em marketplace.css + identity.css — P2
**Estimativa**: ~30min

- `marketplace.css`: 44 hex + ~30 rgba
- `identity.css`: 29 hex + ~5 rgba

**Validacao**:
```bash
dotnet build
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/marketplace.css
# Meta: <10
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/identity.css
# Meta: <10
```

---

### Fase 6: Converter hardcoded em site.css (FORA do :root) — P2
**Estimativa**: ~2h

`site.css` tem ~841 linhas com hex, mas muitas sao definicoes no `:root` (legitimas). Converter FORA do `:root`:

1. Pular todo o bloco `:root { ... }` — essas sao definicoes, NAO converter
2. Para o restante: hex -> var() e rgba -> var() onde equivalente existe
3. Tentar eliminar os 13 `!important`

**Validacao**:
```bash
dotnet build
# Contar hardcoded FORA do :root (excluir definicoes):
awk '/^:root/,/^}/' wwwroot/css/site.css | wc -l  # linhas do :root
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/site.css  # total
# Meta: hardcoded fora do :root < 200
grep -c '!important' wwwroot/css/site.css
# Meta: <5
```

---

## Ordem de Execucao

```
Fase 1 (limpar vars mortas) -> Fase 2 (rgba top 5) -> Fase 3 (rgba next 5) -> Fase 4 (events.css) -> Fase 5 (marketplace+identity) -> Fase 6 (site.css)
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

rgba(96, 165, 250, ...)   | GroupDetailPaymentsModal, Payments, EventPayment | border, bg, shadow
rgba(30, 41, 59, ...)     | GroupDetailPaymentsModal                          | bg, border
rgba(91, 163, 255, ...)   | GroupDetailPaymentsModal                          | border
rgba(251, 191, 36, ...)   | GroupDetailPaymentsModal, Payments, EventPayment, Escalacao | border, bg, text
rgba(245, 158, 11, ...)   | GroupDetailPaymentsModal, Payments                | border, bg
rgba(16, 185, 129, ...)   | GroupDetailPaymentsModal, Payments                | border, bg
rgba(100, 116, 139, ...)  | GroupDetailPaymentsModal, Payments                | border
rgba(161, 118, 24, ...)   | GroupDetailPaymentsModal, Payments                | border
rgba(253, 230, 138, ...)  | GroupDetailPaymentsModal, Payments                | border
rgba(15, 26, 48, ...)     | GroupDetailPaymentsModal, EventPayment            | bg, border
rgba(10, 22, 40, ...)     | Escalacao, EventPayment                           | bg
rgba(51, 76, 120, ...)    | Escalacao                                         | bg, border
rgba(255, 255, 255, ...)  | Escalacao, EventPayment                           | border, bg, text
rgba(34, 197, 94, ...)    | Escalacao, EventPayment                           | bg, border
rgba(59, 130, 246, ...)   | Escalacao, EventPayment                           | bg
rgba(8, 18, 30, ...)      | Escalacao                                         | bg
rgba(7, 20, 36, ...)      | Escalacao                                         | bg
rgba(29, 78, 143, ...)    | Escalacao                                         | shadow
rgba(37, 99, 235, ...)    | Escalacao                                         | shadow
rgba(191, 219, 254, ...)  | Escalacao                                         | shadow
rgba(219, 234, 254, ...)  | Escalacao                                         | shadow
rgba(99, 179, 255, ...)   | EventPayment                                      | bg, border, text
rgba(147, 197, 253, ...)  | EventPayment                                      | text-shadow
rgba(112, 84, 52, ...)    | Payment                                           | bg
rgba(78, 58, 36, ...)     | Payment                                           | bg
rgba(116, 76, 37, ...)    | Payment                                           | border
rgba(255, 251, 241, ...)  | Payment                                           | bg
rgba(242, 226, 191, ...)  | Payment                                           | bg
rgba(123, 79, 35, ...)    | Payment                                           | border
rgba(184, 114, 39, ...)   | Payment                                           | bg
rgba(255, 244, 218, ...)  | Payment                                           | bg
rgba(255, 231, 189, ...)  | Payment                                           | shadow
rgba(130, 87, 42, ...)    | Payment                                           | border
rgba(250, 239, 214, ...)  | Payment                                           | bg
rgba(252, 243, 221, ...)  | Payment                                           | bg
rgba(128, 89, 45, ...)    | Payment                                           | border
rgba(255, 251, 240, ...)  | Payment                                           | bg
rgba(245, 231, 203, ...)  | Payment                                           | bg
rgba(255, 245, 223, ...)  | Payment                                           | shadow
rgba(254, 241, 211, ...)  | Payment                                           | bg
rgba(236, 210, 161, ...)  | Payment                                           | bg
rgba(255, 247, 227, ...)  | Payment                                           | shadow, text
rgba(145, 95, 43, ...)    | Payment                                           | shadow
rgba(95, 57, 23, ...)     | Payment                                           | bg
rgba(93, 55, 22, ...)     | Payment                                           | bg
rgba(238, 210, 162, ...)  | Payment                                           | bg
rgba(224, 191, 138, ...)  | Payment                                           | bg
rgba(148, 98, 43, ...)    | Payment                                           | border
rgba(255, 240, 196, ...)  | Payment                                           | shadow
rgba(79, 41, 14, ...)     | Payment                                           | shadow
rgba(243, 205, 143, ...)  | Payment                                           | bg
rgba(230, 179, 101, ...)  | Payment                                           | bg
rgba(171, 112, 43, ...)   | Payment                                           | border
rgba(255, 232, 193, ...)  | Payment                                           | shadow
rgba(255, 200, 130, ...)  | Payment                                           | shadow
rgba(247, 212, 161, ...)  | Payment                                           | bg
rgba(236, 189, 128, ...)  | Payment                                           | bg
rgba(192, 132, 66, ...)   | Payment                                           | border
rgba(60, 20, 0, ...)      | Payment                                           | text-shadow
rgba(34, 19, 8, ...)      | Payment                                           | shadow
rgba(31, 79, 138, ...)    | GroupDetailPaymentsModal                          | bg
rgba(23, 63, 115, ...)    | GroupDetailPaymentsModal                          | bg
rgba(74, 222, 128, 0.5)   | GroupDetailPaymentsModal, Payments                | border, text-shadow (between --green-opacity-lg 0.35 and --shadow-green-lg 0.6)
rgba(74, 222, 128, 0.15)  | GroupDetailPaymentsModal                          | bg (between --green-dark-lg 0.15 but different base color)
rgba(239, 68, 68, 0.4)    | GroupDetailPaymentsModal, Payments                | border, shadow (between --shadow-red-sm 0.3 and --red-opacity-lg 0.5)
rgba(239, 68, 68, 0.15)   | GroupDetailPaymentsModal                          | bg (no red opacity var at 0.15)
rgba(239, 68, 68, 0.2)    | GroupDetailPaymentsModal, Payments                | border (no red opacity var at 0.2)
rgba(0, 0, 0, 0.7)        | GroupDetailPaymentsModal, Payments                | overlay (between --shadow-2xl 0.6 and no overlay var at 0.7)

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores com !important que nao conseguiu resolver -->

### Problemas de conversao rgba
<!-- Liste aqui rgba() que tem a mesma cor base mas opacidade diferente das vars existentes -->

rgba(74, 222, 128, 0.5)  — base matches --green-opacity-* but opacity 0.5 has no var (closest: --green-opacity-lg 0.35, --shadow-green-lg 0.6)
rgba(239, 68, 68, 0.4)  — base matches --shadow-red-sm but opacity 0.4 has no var (closest: --shadow-red-sm 0.3, --red-opacity-lg 0.5)
rgba(239, 68, 68, 0.15) — base matches --shadow-red-sm but opacity 0.15 has no var
rgba(239, 68, 68, 0.2)  — base matches --shadow-red-sm but opacity 0.2 has no var
rgba(0, 0, 0, 0.7)      — base matches --shadow-* but opacity 0.7 has no var (closest: --shadow-2xl 0.6)

### Outras observacoes
Fase 2: 49 conversoes realizadas nos 5 arquivos. 195 rgba() restantes sem var equivalente — cores base (blue-400, slate-800, amber-400, emerald-500, white, parchment/brown spectrum) nao tem vars rgba definidas no :root. Documentado acima para avaliacao do Senior sobre adicionar novas vars em ciclo futuro.

---

## Metricas de Sucesso (evolucao completa)

| Metrica | C3 | C4 | C5 | C6/C7 | Meta C8 |
|---------|----|----|----|----|---------|
| Warnings (projeto) | ~50 | 2 | 0 | 0 | 0 |
| `!important` scoped | 16 | 1 | 1 | 1 | 1 |
| `!important` global | — | — | — | 17 | <5 |
| Hardcoded hex scoped | 1.387 | 1.099 | 867 | 0 | 0 |
| rgba() hardcoded scoped | — | — | — | 645 | <300 |
| Hardcoded hex global | — | — | — | ~1.317 | <600 |
| CSS vars usadas | 381 | 673 | 1.175 | 2.053 | >2.200 |
| Inline estaticos | ~35 | 6 | 0 | 0 | 0 |
| Vars no `:root` | ~68 | ~68 | 105 | 179 | ~161 |
| Vars mortas | — | — | — | 18 | 0 |
