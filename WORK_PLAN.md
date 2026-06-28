# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 6 + fix Senior) | Ciclo 7 ATIVO
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
- Warnings: ~50 -> 2 | `!important`: 16 -> 1 | Breakpoints: 50 padronizados | Hardcoded: 1.387 -> 1.099 | Inline: ~35 -> 6
- **Problemas**: Pleno introduziu 16 NOVAS cores hardcoded ao converter inline -> CSS classes

### Ciclo 5 (Pleno Local): Refatoracao CSS - Hardcoded Colors + Inline Styles
- PRs: #23-#28 (6 fases, 6 branches — deveria ter sido 1 branch)
- Warnings: 0 | `!important`: 1 | Hardcoded: 1.099 -> 867 | Vars: 673 -> 1.175 | Inline: 0
- **Problemas**: ViewPayment esquecido, 178 fallbacks desnecessarios, 141 nao convertidas

### Ciclo 6 (Pleno Local): Eliminacao Total de Hardcoded em Scoped CSS
- Branch: `refactor/ciclo6-css-vars-final`
- **Resultados reportados pelo Pleno**:
  - Hardcoded colors: **0** (meta <550 — SUPERADA)
  - CSS vars usadas: **~1.249** (meta >1.400 — parcialmente atingida)
  - Fallbacks: **0** | `!important`: **1** | Build: 0 erros
  - 4 commits, branch unica, ~74 cores convertidas em 37 arquivos

#### Revisao Senior do Ciclo 6

**Veredicto: EXECUCAO PARCIAL — bloqueio critico corrigido pelo Senior**

| Metrica | Antes | Depois (Pleno) | Depois (com fix Senior) | Meta |
|---------|-------|----------------|------------------------|------|
| Hardcoded em scoped CSS | 867 | 0 | 0 | <550 |
| CSS vars usadas | 1.175 | ~1.249 | 1.954 (Pages 1.639 + Shared 315) | >1.400 |
| Fallbacks | 178 | 0 | 0 | 0 |
| `!important` | 1 | 1 | 1 | 1 |
| Vars no `:root` | 105 | 105 | 123 | — |
| **Vars indefinidas** | 0 | **18** | **0** | 0 |

**Problema critico encontrado**: O Pleno converteu todos os hex para `var(--nome)` mas **inventou 18 nomes de vars que NAO existiam no `:root`**. Resultado: 128 usos de vars que renderizam como vazio (CSS empty/inherit), quebrando cores em toda a UI.

**Vars inventadas (18)**:
```
--white, --green-strong, --amber-mid, --amber-dark, --amber-strong, --amber-lightest,
--ci-bg-mid, --ci-bg-deep, --ci-bg-dark, --red-dark, --red-bg-dark,
--brown-dark, --brown-muted, --poker-border, --pink-mid,
--slate-dark, --slate-pale, --slate-warm
```

**Distribuicao dos 128 usos**:
| Var | Usos | Var | Usos |
|-----|------|-----|------|
| --ci-bg-mid | 22 | --green-strong | 21 |
| --white | 20 | --red-bg-dark | 16 |
| --amber-mid | 15 | --amber-dark | 9 |
| --red-dark | 4 | --poker-border | 3 |
| --ci-bg-deep | 3 | --brown-muted | 3 |
| --slate-dark | 2 | --ci-bg-dark | 2 |
| --brown-dark | 2 | --amber-lightest | 2 |
| --slate-warm | 1 | --slate-pale | 1 |
| --pink-mid | 1 | --amber-strong | 1 |

**Fix aplicado pelo Senior**: Adicionadas as 18 vars ao `:root` em `wwwroot/css/site.css` com valores hex deduzidos dos diffs originais. Total de vars no `:root`: 105 -> 123.

**Nova regra adicionada (regra 12)**: Antes de converter hex -> var(), verificar que a var EXISTE no `:root` ou na lista de vars permitidas.

**Positivo**:
- Conversoes mecanicas feitas corretamente (mapeamento var correto QUANDO a var existia)
- Regra 7 respeitada (0 fallbacks)
- Branch unica (regra 9 respeitada)
- Build e testes continuam passando

---

## Diagnostico CSS Atualizado (Pos-Ciclo 6 + Fix Senior)

| Metrica | Ciclo 3 | Pos-C4 | Pos-C5 | Pos-C6 | Meta C6 |
|---------|---------|--------|--------|--------|---------|
| Hardcoded scoped CSS | 1.387 | 1.099 | 867 | **0** | <550 |
| CSS vars usadas | 381 | 673 | 1.175 | **1.954** | >1.400 |
| `!important` scoped | 16 | 1 | 1 | **1** | 1 |
| Inline estaticos | ~35 | 6 | 0 | **0** | 0 |
| Warnings projeto | ~50 | 2 | 0 | **0** | 0 |
| Fallbacks | — | — | 178 | **0** | 0 |
| Vars no `:root` | ~68 | ~68 | 105 | **123** | — |
| Vars indefinidas | — | — | 0 | **0** (fix) | 0 |

### Estado atual dos CSS globais (wwwroot/css/)

| Arquivo | Hardcoded | `!important` | Fallbacks |
|---------|-----------|-------------|-----------|
| site.css | 858 (incl. :root defs) | 13 | — |
| events.css | 403 | 4 | sim |
| marketplace.css | 44 | 0 | — |
| identity.css | 29 | 0 | — |
| **Total globais** | **~1.334** | **17** | **sim** |

### Paginas grandes (candidatas a decomposicao)

| Arquivo | Linhas |
|---------|--------|
| AdminPayments.razor | 1.151 |
| Futsal/Detail.razor | 933 |
| Groups/Detail.razor | 818 |
| Mailbox.razor | 673 |
| Admin/AdminLogs.razor | 662 |
| Futsal/Escalacao.razor | 661 |
| Poker/Detail.razor | 634 |
| Payment/Payment.razor | 626 |
| Poker/Create.razor | 613 |
| Groups/Payments.razor | 560 |

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** — se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores em scoped CSS** — usar vars do `:root`. Se nao existe var, documentar na secao "Cores sem Var"
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** — sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** — so criar se tiver estilos reais
6. **NUNCA introduzir novas cores hardcoded** ao converter inline -> classe CSS. Usar vars existentes ou documentar
7. **NUNCA usar fallback em var()** — usar `var(--nome)` sem fallback hex
8. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"` (0 failed)
9. **1 branch unica por ciclo** — commits por fase dentro dela. NAO criar branches separadas
10. **1 PR por ciclo** — mergear via PR, nunca push direto na main
11. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados" com arquivo, linha, e o que tentou
12. **NUNCA inventar nomes de var()** — so usar vars que JA existem na lista "CSS Vars Permitidas" abaixo. Se a cor nao tem var, deixar hardcoded e documentar

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
var(--brown-dark)        /* #4a2c15 - Ciclo 7 fix */
var(--brown-muted)       /* #8b7355 - Ciclo 7 fix */

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
var(--green-strong)      /* #22c55e - Ciclo 7 fix */
var(--red)               /* #e53935 */
var(--red-soft)          /* #f87171 */
var(--red-strong)        /* #ef4444 */
var(--red-dark)          /* #dc2626 - Ciclo 7 fix */
var(--red-border)        /* #7f1d1d */
var(--red-text-light)    /* #fca5a5 */
var(--red-pale)          /* #fecaca */
var(--red-lightest)      /* #fef2f2 */
var(--red-bg-dark)       /* #290f0f - Ciclo 7 fix */
var(--amber)             /* #fbbf24 */
var(--amber-light)       /* #fcd34d */
var(--amber-warm)        /* #fde68a */
var(--amber-mid)         /* #d97706 - Ciclo 7 fix */
var(--amber-dark)        /* #b45309 - Ciclo 7 fix */
var(--amber-strong)      /* #e65100 - Ciclo 7 fix */
var(--amber-lightest)    /* #fffbeb - Ciclo 7 fix */

/* Neutral text */
var(--text-light)        /* #f0f0f0 */
var(--text-muted-gray)   /* #888 */
var(--white)             /* #ffffff - Ciclo 7 fix */
var(--link-blue)         /* #8ab4f8 */
var(--link-info)         /* #93c5fd */
var(--border-slate)      /* #314454 */

/* == Cool/navy theme (Confirmai dark) == */

var(--ci-bg)             /* #090f18 */
var(--ci-bg-card)        /* #111927 */
var(--ci-bg-card-deep)   /* #0d1825 */
var(--ci-bg-input)       /* #07111d */
var(--ci-bg-mid)         /* #112240 - Ciclo 7 fix */
var(--ci-bg-deep)        /* #0d1825 - Ciclo 7 fix */
var(--ci-bg-dark)        /* #0a1f35 - Ciclo 7 fix */
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
var(--poker-border)      /* #2d1a4a - Ciclo 7 fix */

/* Futsal theme */
var(--futsal-green-dark)   /* #14532d */
var(--futsal-green-pale)   /* #bbf7d0 */
var(--futsal-bg-dark)      /* #0a2018 */
var(--futsal-text-light)   /* #f0fdf4 */

/* Neutrals/Slate */
var(--slate-light)       /* #e2e8f0 */
var(--slate-mid)         /* #64748b */
var(--slate-muted)       /* #94a3b8 */
var(--slate-dark)        /* #334155 - Ciclo 7 fix */
var(--slate-pale)        /* #e2e8f0 - Ciclo 7 fix */
var(--slate-warm)        /* #78716c - Ciclo 7 fix */

/* Pink */
var(--pink-mid)          /* #f472b6 - Ciclo 7 fix */

/* Typography */
var(--font-display)      /* "Cinzel", Georgia, serif */
var(--font-accent)       /* "MedievalSharp", Georgia, cursive */
var(--ci-font)           /* system sans-serif stack */
```

---

## Ciclo 7 — Tarefas Ativas

**Foco**: Converter CSS globais (wwwroot/css/) para usar vars + eliminar `!important` em globais + decomposicao de paginas grandes

**Branch**: `refactor/ciclo7-global-css-decomp`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Converter hardcoded em events.css — P0
**Estimativa**: ~1h

`events.css` tem 403 hardcoded colors e 4 `!important`. Converter para usar vars do `:root`.

**Procedimento**:
1. Abrir `wwwroot/css/events.css`
2. Para cada cor hex, verificar se existe var equivalente na lista acima
3. Se existe: substituir hex por `var(--nome)`
4. Se NAO existe: deixar hardcoded e documentar na secao "Cores sem Var"
5. Para cada `!important`: tentar remover. Se nao funciona sem `!important`, documentar na secao "Problemas"
6. Remover qualquer fallback `var(--nome, #hex)` — usar so `var(--nome)`

**Validacao**:
```bash
dotnet build
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/events.css
# Meta: <100 (maioria das 403 deve ter var equivalente)
grep -c '!important' wwwroot/css/events.css
# Meta: 0
```

---

### Fase 2: Converter hardcoded em site.css (fora do :root) — P0
**Estimativa**: ~2h

`site.css` tem ~858 linhas com hex. Muitas sao definicoes no `:root` (legitimas). As que estao FORA do `:root` devem usar vars.

**Procedimento**:
1. Abrir `wwwroot/css/site.css`
2. Pular todo o bloco `:root { ... }` (linhas 22-209 aprox) — essas sao definicoes, NAO converter
3. Para o restante do arquivo: converter hex -> var() onde possivel
4. NAO tocar no bloco `:root`

**Validacao**:
```bash
dotnet build
# Contar hardcoded FORA do :root:
sed -n '/^}/,$p' wwwroot/css/site.css | grep -c '#[0-9a-fA-F]\{3,8\}'
# Meta: <200 (gradientes e rgba sao legitimos)
```

---

### Fase 3: Converter hardcoded em marketplace.css e identity.css — P1
**Estimativa**: ~30min

- `marketplace.css`: 44 hardcoded
- `identity.css`: 29 hardcoded

**Validacao**:
```bash
dotnet build
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/marketplace.css
# Meta: <10
grep -c '#[0-9a-fA-F]\{3,8\}' wwwroot/css/identity.css
# Meta: <10
```

---

### Fase 4: Eliminar `!important` em CSS globais — P1
**Estimativa**: ~1h

17 ocorrencias de `!important` em `wwwroot/css/` (13 em site.css, 4 em events.css).

**Procedimento**:
1. Para cada `!important`, analisar o seletor e entender POR QUE precisa de `!important`
2. Se e conflito de especificidade: aumentar especificidade do seletor OU mover o estilo para scoped CSS
3. Se NAO consegue resolver: documentar na secao "Problemas" com o seletor, a razao, e o que tentou
4. **NAO simplesmente deletar** `!important` sem verificar que o estilo continua aplicado

**Validacao**:
```bash
dotnet build
grep -rn '!important' wwwroot/css/ | wc -l
# Meta: 0 (ou <3 se houver casos genuinamente necessarios)
```

---

### Fase 5: Decomposicao de AdminPayments.razor (1.151L) — P2
**Estimativa**: ~2h

Extrair sub-componentes do maior arquivo Razor:

1. Identificar blocos de UI independentes (tabelas, modais, sumarios, filtros)
2. Criar componentes em `Pages/Admin/Components/` (se nao existirem)
3. Cada componente recebe `[Parameter]` tipados
4. O arquivo pai fica com orquestracao + estado + callbacks
5. CSS acompanha: se o componente tem estilos, criar `Component.razor.css`

**Regras de decomposicao**:
- Componente extraido DEVE compilar independentemente
- NAO mover logica de negocio para componentes — so UI
- Usar `EventCallback` para comunicacao filho -> pai
- Manter nomes descritivos: `AdminPaymentsSummaryPanel`, `AdminPaymentsFilterBar`, etc.

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Admin/AdminPayments.razor
# Meta: <500L
```

---

### Fase 6: Decomposicao de Futsal/Detail.razor (933L) — P2
**Estimativa**: ~2h

Mesmo processo da Fase 5.

1. Identificar blocos: lista de jogadores, formulario de edicao, modais, metricas
2. Criar componentes em `Pages/Futsal/Components/`
3. Mover UI para componentes, manter estado no pai

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
wc -l Pages/Futsal/Detail.razor
# Meta: <500L
```

---

## Ordem de Execucao

```
Fase 1 (events.css) -> Fase 2 (site.css) -> Fase 3 (marketplace+identity) -> Fase 4 (!important globais) -> Fase 5 (AdminPayments decomp) -> Fase 6 (Futsal/Detail decomp)
```

**Branch unica**: `refactor/ciclo7-global-css-decomp`
**1 commit por fase** dentro da branch.
**1 PR** no final do ciclo.

---

## Comandos de Validacao Completos

```bash
# Build:
dotnet build

# Tests (excluir ProgramConfiguration + AdminLogsQueryString):
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"

# Contar hardcoded em scoped CSS (deve ser 0):
grep -rn '#[0-9a-fA-F]\{3,8\}' Pages/ Shared/ --include="*.css" | wc -l

# Contar hardcoded em CSS globais:
grep -rn '#[0-9a-fA-F]\{3,8\}' wwwroot/css/ | grep -v '^\s*--' | wc -l

# Contar CSS vars usadas (scoped):
grep -rn 'var(--' Pages/ Shared/ --include="*.css" | wc -l

# Contar !important total:
grep -rn '!important' Pages/ Shared/ wwwroot/css/ --include="*.css" | wc -l

# Contar inline styles:
grep -rn 'style="' Pages/ --include="*.razor" | grep -v 'display:none' | grep -v '@' | wc -l

# Verificar vars indefinidas:
# 1. Extrair vars definidas no :root
grep -oP '^\s*--([\w-]+)\s*:' wwwroot/css/site.css | sed 's/^\s*--//' | sed 's/\s*://' | sort -u > /tmp/defined.txt
# 2. Extrair vars usadas
grep -rPoh 'var\(--([\w-]+)\)' Pages/ Shared/ --include="*.css" | grep -oP '\-\-([\w-]+)' | sed 's/^--//' | sort -u > /tmp/used.txt
# 3. Extrair vars definidas localmente em scoped CSS
grep -rPoh '^\s*--([\w-]+)\s*:' Pages/ Shared/ --include="*.css" | sed 's/^\s*--//' | sed 's/\s*://' | sort -u > /tmp/local.txt
# 4. Comparar
cat /tmp/defined.txt /tmp/local.txt | sort -u > /tmp/all.txt
comm -23 /tmp/used.txt /tmp/all.txt
# Meta: 0 linhas (nenhuma var indefinida)
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui cores hex que nao tem var no :root NEM no mapeamento acima -->
<!-- Formato: #hex | arquivo | contexto (ex: gradiente, sombra) -->

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores que nao conseguiu override sem !important -->
<!-- Formato: seletor | arquivo | o que tentou -->

### Problemas de decomposicao
<!-- Liste aqui componentes que nao conseguiu extrair e por que -->

### Outras observacoes
<!-- Notas gerais sobre decisoes tomadas -->

---

## Metricas de Sucesso (evolucao completa)

| Metrica | Ciclo 3 | Pos-C4 | Pos-C5 | Pos-C6 | Meta C7 |
|---------|---------|--------|--------|--------|---------|
| Warnings (projeto) | ~50 | 2 | 0 | 0 | 0 |
| `!important` scoped | 16 | 1 | 1 | 1 | 1 |
| `!important` global | — | — | — | 17 | <3 |
| Hardcoded scoped CSS | 1.387 | 1.099 | 867 | 0 | 0 |
| Hardcoded global CSS | — | — | — | ~1.334 | <400 |
| CSS vars usadas | 381 | 673 | 1.175 | 1.954 | >2.000 |
| Inline estaticos | ~35 | 6 | 0 | 0 | 0 |
| Fallbacks | — | — | 178 | 0 | 0 |
| Vars no `:root` | ~68 | ~68 | 105 | 123 | 123+ |
| AdminPayments.razor | — | — | — | 1.151L | <500L |
| Futsal/Detail.razor | — | — | — | 933L | <500L |
