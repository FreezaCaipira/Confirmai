# Plano de Trabalho - Confirmai

> Atualizado em 14/07/2026 | Base: `main` (pos-Ciclo 12 + investigacao Senior) | Refatoracao CSS CONCLUIDA
> 1.674/1.674 testes passando | 0 erros de build | 0 warnings | 0 AppDbContext direto
> Ciclo 13: Background + Mobile + Navegacao | Ciclo 14: Refresh Token / Sessao Persistente

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
- Hardcoded hex scoped: 0 | rgba scoped: 185 | Vars: 2.344 | Vars no :root: 317
- 12 paginas >400L decompostas com code-behind
- **Problemas**: 9 vars inventadas, 6 vars mortas, encoding corrompido em site.css (corrigido pelo Senior)

### Ciclo 12 (Pleno Local): UX/UI Fixes + Cleanup Tecnico (11 fases + 6 extras)
- Branch: `fix/ciclo12-ux-cleanup` | PR #44
- Causa raiz card grupo: 17 vars auto-referenciais quebrando gradientes/sombras
- Binding placar corrigido com `EventCallback<int?>`
- AppDbContext direto: 3 -> 0 (IDbContextFactory zerado)
- xUnit2013: 49 -> 0 warnings
- **Problema pendente**: background das telas `/eventos`, `/futsal`, `/poker` nao alinhado com `/grupos`
- **Investigacao Senior**: ver secao "Investigacao Senior: Background das telas de eventos"

---

## Revisao Senior do Ciclo 12

### Veredicto: MUITO BOM -- UX fixes corretos, cleanup completo, problema critico encontrado e resolvido pelo Pleno

| Metrica | C11 | C12 | Status |
|---------|-----|-----|--------|
| Build errors | 0 | **0** | Atingido |
| Build warnings | 0 | **0** | Atingido |
| Tests | 1.674 | **1.674** | Atingido |
| Hardcoded hex scoped | 0 | **0** | Atingido |
| rgba() hardcoded scoped | 185 | **189** | Estavel (+4 novos unicos) |
| CSS vars usadas (scoped) | 2.344 | **2.364** | Melhoria |
| `!important` scoped | 1 | **1** | Atingido |
| `!important` global | 11 | **11** | Estavel |
| Vars no `:root` | 317 | **315** (apos fix) | Melhoria |
| Vars indefinidas | 0 | **0** | Atingido |
| Vars mortas `:root` | 0 | **0** (apos fix) | Atingido |
| AppDbContext direto | 3 | **0** | **Zerado** |
| xUnit2013 warnings | 49 | **0** | **Zerado** |
| Encoding | OK | **OK** | Atingido |

### Bloco A -- UX/UI Fixes: 7/8 executados

**Fase 1 (Card grupo)**: Causa raiz identificada -- 17 vars CSS auto-referenciais no `:root` (ex: `--shadow-md: var(--shadow-md)` em vez do valor rgba). Pleno corrigiu TODAS, restaurando gradientes/sombras em todo o app. Fix critico e bem executado.

**Fase 2 (Partidas nova janela)**: PULADA. Decisao correta -- em Blazor Server, `target="_blank"` cria novo circuito SignalR. Manter navegacao interna.

**Fase 3 (Btn comprovante)**: Corrigiu mojibake em `Groups/Payments.razor` + adicionou btn comprovante na aba Pendentes.

**Fase 4 (Caracteres bugados)**: Corrigiu acentos PT-BR/ES-ES em `PaymentTexts.cs`, `UtilityTexts.cs`, `CoreTexts.cs` + adicionou 6 chaves UiText faltantes + fix seletores CSS `.tr--paid`/`.tr--pending`/`.td--date`.

**Fase 5 (Pos-partida)**: Fix REAL do binding placar -- `ScoreInputA`/`ScoreInputB` agora propagam via `EventCallback<int?>` para o parent. Legibilidade CSS melhorada. Btn Editar escondido quando partida encerrada.

**Fase 6 (Edit partida)**: Removeu 45L de codigo duplicado exposto fora do `@code` block. Unificou formato de endereco com Detail.razor.

**Fase 7 (Background eventos)**: Corrigiu background invertido + adicionou btn "Ver Grupos". NOTA: apos essa fase, Pleno fez 5 commits extras tentando alinhar backgrounds de `/futsal` e `/poker` sem sucesso confirmado pelo usuario.

**Fase 8 (Profile layout)**: Padronizou Profile com tokens entity-shell (border-radius, border-top, box-shadow, transitions).

### Bloco B -- Cleanup: 3/3 executados

**Fase 9 (IDbContextFactory)**: Migrou ViewPayment, Payment, PaymentDetails. `grep '@inject AppDbContext'` retorna 0 resultados. Meta ZERADA.

**Fase 10 (!important)**: Removeu 1 `!important` desnecessario em `oldsite-payment-chip span`. 10/11 restantes sao legitimos.

**Fase 11 (xUnit2013)**: Substituiu `Assert.Equal(1, .Count)` -> `Assert.Single()` em 2 arquivos. 0 warnings xUnit2013.

### Commits extras (fora do escopo do ciclo)

O Pleno fez 6 commits adicionais apos as 11 fases, tentando alinhar backgrounds das telas `/eventos`, `/futsal`, `/poker` ao padrao de `/grupos`. Tres tentativas, nenhuma confirmada visualmente pelo usuario. O Pleno documentou tudo em `docs/ciclo12-revisao-senior.md` (removido pelo Senior -- consolidar em WORK_PLAN.md).

Mudancas incluem:
- Gradient+overlay nos event-card de `/futsal` e `/poker`
- Degrade no `.sports-shell` de `/eventos`
- Container `.listing-block` no `EventListingShell` compartilhado
- Botao "Meus Eventos" na tela `/eventos`
- Remocao de ~130L CSS duplicado em `Poker/Index.razor.css`

**Analise Senior**: O problema de background pode ser cache do navegador ou hot reload nao aplicando scoped CSS. Recomendacao: rebuild limpo (`dotnet clean && dotnet build`) + Ctrl+F5 (sem cache) para confirmar. Se persistir, investigar se o Blazor CSS isolation esta aplicando corretamente os atributos `b-xxx` no `EventListingShell`.

### Problemas encontrados e corrigidos pelo Senior

**1. 2 vars mortas no `:root`** (`--purple-md`, `--entity-bg`):
Definidas mas sem nenhum `var(--purple-md)` ou `var(--entity-bg)` em qualquer arquivo. Senior removeu.

**2. `--entity-bg` e `--entity-card` mortas em scoped CSS**:
Definidas em `Profile.razor.css`, `EntityProfileShell.razor.css` e `site.css` mas nunca consumidas via `var()`. Senior removeu de todos os 3 arquivos.

**3. Arquivo docs separado**:
Pleno criou `docs/ciclo12-revisao-senior.md` em vez de consolidar no WORK_PLAN.md. Senior removeu o arquivo (regra: docs no WORK_PLAN.md unico).

### Positivo
- 17 vars auto-referenciais identificadas e corrigidas -- excelente diagnostico da causa raiz
- Binding placar corrigido com `EventCallback` -- fix tecnico correto
- IDbContextFactory migrado em todas as 3 paginas restantes -- meta historica ZERADA
- Commits de fase limpos (1 por fase), branch unica, 1 PR -- regras 9, 14 respeitadas
- Documentou detalhadamente as tentativas de fix do background -- transparencia

### Ressalvas
- Regra 16 violada: misturou CSS refactoring com feature UX (gradient nos event-cards, botao Meus Eventos) no mesmo PR
- 5 commits extras de tentativa/erro no background -- regra 14 parcialmente violada
- Arquivo docs separado criado -- deve consolidar em WORK_PLAN.md

---

## Metricas Atuais (pos-Ciclo 12 + fix Senior)

| Metrica | C4 | C5 | C6/C7 | C8 | C9 | C10 | C11 | C12 |
|---------|----|----|-------|----|----|-----|-----|-----|
| Warnings (build) | 2 | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| `!important` scoped | 1 | 1 | 1 | 1 | 1 | 1 | 1 | **1** |
| `!important` global | -- | -- | 17 | 12 | 11 | 12 | 11 | **11** |
| Hardcoded hex scoped | 1.099 | 867 | 0 | 0 | 0 | 0 | 0 | **0** |
| rgba() hardcoded scoped | -- | -- | 645 | 531 | 478 | 378 | 185 | **189** |
| Hardcoded hex global | -- | -- | ~1.317 | 166 | 166 | 153 | 4 | **~103** |
| rgba() hardcoded global | -- | -- | ~185 | 446 | 413 | 394 | 217 | **~360** |
| CSS vars (scoped) | 673 | 1.175 | 2.053 | 1.998 | 2.051 | 2.149 | 2.344 | **2.364** |
| CSS vars total | -- | -- | -- | 3.513 | 3.609 | 3.739 | 4.246 | **4.690** |
| Vars no `:root` | ~68 | 105 | 179 | 158 | 168 | 168 | 317 | **315** |
| Vars indefinidas | -- | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Vars mortas `:root` | -- | -- | 18 | 0 | 0 | 0 | 0 | **0** |
| Fallbacks | -- | 178 | 0 | 0 | 4 | 0 | 0 | **0** |
| Pages >400L sem code-behind | -- | -- | -- | -- | 12 | 9 | 0 | **0** |
| AppDbContext direto | -- | -- | -- | -- | -- | -- | 3 | **0** |
| xUnit2013 warnings | -- | -- | -- | -- | -- | -- | 49 | **0** |

### Paginas grandes -- TODAS decompostas

Nenhuma pagina >400L sem code-behind. Unica pagina >400L com code-behind: `Futsal/Detail.razor` (483L) -- aceitavel dado que ja tem 9+ sub-componentes.

### AppDbContext direto -- ZERADO

Todas as paginas agora usam `IDbContextFactory<AppDbContext>`. 0 paginas com `@inject AppDbContext` direto.

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
20. **NUNCA criar arquivos de docs separados** -- consolidar TUDO no WORK_PLAN.md. Nao criar arquivos em `docs/`, `.md` avulsos, etc.
21. **Rebuild limpo antes de testar mudancas visuais** -- ao alterar CSS (especialmente scoped CSS), sempre fazer `dotnet clean && dotnet build` e testar com Ctrl+F5 (hard refresh). Hot-reload pode nao aplicar scoped CSS corretamente

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

## Estado Final pos-Ciclo 12

A refatoracao CSS e cleanup tecnico estao **completos**. Marcos atingidos:
- 0 hardcoded hex em scoped CSS (desde C6)
- 0 `AppDbContext` direto (desde C12)
- 0 paginas >400L sem code-behind (desde C11)
- 0 build warnings (desde C5)
- 0 xUnit2013 warnings (desde C12)
- 315 vars no `:root`, 4.690 usos de `var()` total
- 1 `!important` scoped (AvatarUpload pattern legitimo)

Trabalho restante e predominantemente **arquitetural e de UX** (nao CSS).

---

## Investigacao Senior: Background das telas de eventos

### Problema
A tela inicial (`/grupos`) tem um visual coeso com `.groups-block` (degrade sutil, borda azul, sombra inset).
As telas `/eventos`, `/futsal`, `/poker` deveriam replicar esse padrao mas apresentam visual diferente.

### Diagnostico do Senior (analise de codigo)

**Referencia (padrao correto): `.groups-block` em `events.css` (GLOBAL)**:
```css
background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
border: 1px solid var(--ci-border);        /* #1b3d6c — azul claro */
border-radius: 14px;
box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);  /* brilho sutil no topo */
```

**`/futsal` e `/poker`: `.listing-block` em `EventListingShell.razor.css` (SCOPED)**:
```css
/* IDENTICO ao .groups-block — CSS correto */
background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
border: 1px solid var(--ci-border);
border-radius: 14px;
box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
```
Status: CSS esta correto. O scoped CSS bundle gera `.listing-block[b-7yv3gvojvj]` corretamente.

**`/eventos`: `.sports-shell` em `Pages/Index.razor.css` (SCOPED) — DIFERENTE**:
```css
background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
border: 1px solid var(--ci-border-alt);    /* #1a3a5c — DIFERENTE, mais escuro */
border-top: 2px solid var(--ci-border-dim); /* EXTRA: borda grossa no topo */
border-radius: 14px;
box-shadow: 0 8px 24px var(--shadow-2xl), 0 4px 12px var(--shadow-lg);  /* DIFERENTE: sombra EXTERNA */
```

### Causa raiz: 2 problemas

**Problema 1 — `/eventos` tem CSS divergente**:
`.sports-shell` usa `var(--ci-border-alt)` (#1a3a5c, mais escuro) em vez de `var(--ci-border)` (#1b3d6c, mais claro),
tem borda extra no topo, e usa sombra externa em vez de inset. Isso cria visual diferente.

**Problema 2 — Cache/hot-reload durante testes do Pleno**:
O Pleno fez 3 tentativas de fix (commits `19bf314`, `0763d5a`, `1c311f0`, `d59be9e`) e a CSS final do `.listing-block`
esta correta, mas o resultado "nao vai, desisti" sugere que o browser servia CSS cacheado.
Em Blazor Server, o scoped CSS bundle (`Confirmai.styles.css`) tem fingerprint via `asp-append-version="true"`,
mas durante desenvolvimento com hot-reload, o browser pode manter a versao antiga em cache.

### Fix documentado no Ciclo 13 (Fase 1)

---

## Ciclo 13 -- Background + Mobile + Navegacao

> Ciclo focado em UX/layout. NAO inclui implementacao de features (refresh token fica no Ciclo 14).

**Branch**: `fix/ciclo13-ux-layout`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Fase 1: Corrigir background de `/eventos` + verificar `/futsal` e `/poker` -- P0
**Estimativa**: ~30 min

**Passo 1 — Fix `.sports-shell` em `Pages/Index.razor.css`**:

Alterar de:
```css
.sports-shell {
    /* ... layout props ... */
    background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
    border: 1px solid var(--ci-border-alt);
    border-top: 2px solid var(--ci-border-dim);
    border-radius: 14px;
    box-shadow: 0 8px 24px var(--shadow-2xl), 0 4px 12px var(--shadow-lg);
    /* ... */
}
```

Para (mesmo padrao de `.groups-block`):
```css
.sports-shell {
    /* ... layout props ... */
    background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
    border: 1px solid var(--ci-border);
    border-radius: 14px;
    box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
    /* ... */
}
```

Remover: `border-top: 2px solid var(--ci-border-dim);`
Trocar: `var(--ci-border-alt)` -> `var(--ci-border)`
Trocar: sombra externa -> `inset 0 1px 0 rgba(79, 156, 248, 0.14)`

**Passo 2 — Rebuild limpo**:
```bash
dotnet clean
dotnet build
```

**Passo 3 — Verificar `/futsal` e `/poker`**:
Abrir no browser com Ctrl+F5 (hard refresh). Comparar visualmente com `/grupos`.
Se `.listing-block` renderizar corretamente, esta resolvido.
Se NAO renderizar, inspecionar no DevTools:
1. Clicar com botao direito no `.listing-block` -> Inspecionar
2. Verificar se o elemento tem atributo `b-7yv3gvojvj` (ou similar)
3. Se NAO tem atributo, o problema e CSS isolation do Blazor
4. Nesse caso: mover CSS de `.listing-block` para `events.css` (global, ao lado de `.groups-block`)

**Validacao**: `dotnet build` + comparar visualmente `/eventos`, `/futsal`, `/poker` com `/grupos`

---

### Fase 2: Testar responsividade mobile -- P1
**Estimativa**: ~2h

Verificar layout em viewport 375px e 414px. Breakpoints ja padronizados (640/768/1024/1440px).

**Procedimento**:
1. Abrir DevTools do browser, ativar modo responsivo
2. Testar telas: `/grupos`, `/grupo/{id}`, `/futsal/{id}`, `/pagamentos-historico`, `/perfil`
3. Documentar problemas: overflow horizontal, touch targets <44px, texto cortado, botoes sobrepostos
4. Corrigir o que for possivel via media queries nos scoped CSS
5. Documentar o que precisar de mudanca estrutural na secao "Problemas Encontrados"

**Validacao**: `dotnet build` + verificar visualmente em 375px e 414px

---

### Fase 3: Repensar fluxo de navegacao -- P1
**Estimativa**: ~3h

Fluxo atual: tela inicial = `/grupos`. Proposta do dono:
- Tela inicial = proximas partidas (eventos)
- Grupos -> ver eventos do grupo -> criar partida dentro do grupo
- Separar criacao de partida da tela geral de eventos

**Procedimento**:
1. Criar nova pagina `Pages/Home.razor` com `@page "/"` mostrando proximas partidas
2. Mover `@page "/"` de `Groups/Index.razor` para so `@page "/grupos"`
3. Adicionar navegacao coerente: Home -> Grupos -> Grupo -> Partida
4. Mover btn "Criar Partida" para dentro da tela do grupo (nao faz sentido na tela geral de eventos)
5. Ajustar links no `MainLayout.razor` (nav superior) para refletir novo fluxo

**Validacao**: `dotnet build` + `dotnet test` + verificar navegacao completa

---

## Ciclo 14 -- Refresh Token / Sessao Persistente

> Ciclo separado por ser mudanca arquitetural que requer orientacao detalhada do Senior.

**Branch**: `feat/ciclo14-refresh-token`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Contexto tecnico (analise do Senior)

O sistema JA tem:
- `SlidingExpiration = true` (Program.cs L212)
- `ExpireTimeSpan = 30 min` (producao) / `60 min` (dev) — via `SecurityPolicyDefaults.cs`
- `SecurityStampValidator` a cada 30s (Program.cs L220-223)
- `RevalidatingIdentityAuthenticationStateProvider` que revalida security stamp a cada 30s

**O problema**: Em Blazor Server, o usuario raramente faz requisicoes HTTP apos o carregamento inicial.
A comunicacao passa a ser via WebSocket (SignalR). O `SlidingExpiration` do cookie so e renovado
em requisicoes HTTP. Resultado: o cookie pode expirar enquanto o circuito SignalR esta ativo.
Quando o usuario finalmente recarrega a pagina, o cookie ja expirou e ele perde a sessao.

### Fase 1: Aumentar timeout e adicionar ping periodico -- P0
**Estimativa**: ~1.5h

**Passo 1 — Aumentar `SessionTimeoutMinutes`**:
Em `Configuration/SecurityPolicyDefaults.cs`:
- Produção (L44): `SessionTimeoutMinutes: 30` -> `SessionTimeoutMinutes: 480` (8 horas)
- Desenvolvimento (L31): `SessionTimeoutMinutes: 60` -> `SessionTimeoutMinutes: 720` (12 horas)

Justificativa: usuarios de app esportivo esperam sessao longa (abrem de manha, usam ao longo do dia).

**Passo 2 — Criar endpoint de keep-alive**:
Criar `Controllers/PingController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Confirmai.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
```

Registrar em `Program.cs` (antes de `app.Run()`):
```csharp
app.MapControllers();
```

Verificar se `AddControllers()` ja esta registrado em `builder.Services`. Se nao, adicionar.

**Passo 3 — Criar script JS de keep-alive**:
Criar `wwwroot/js/session-keepalive.js`:
```javascript
(function () {
    var intervalMs = 15 * 60 * 1000; // 15 minutos
    setInterval(function () {
        fetch('/api/ping', { credentials: 'same-origin' })
            .catch(function () { /* silently ignore */ });
    }, intervalMs);
})();
```

**Passo 4 — Registrar o script em `Pages/_Host.cshtml`**:
Adicionar antes de `blazor.server.js`:
```html
<script src="/js/session-keepalive.js" asp-append-version="true"></script>
```

**Validacao**:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"
```
NOTA: o teste `SecurityPolicyDefaultsTests` vai falhar porque espera `SessionTimeoutMinutes: 30`.
Atualizar os testes:
- `Confirmai.Tests/SecurityPolicyDefaultsTests.cs` L23: `Assert.Equal(60, ...)` -> `Assert.Equal(720, ...)`
- `Confirmai.Tests/SecurityPolicyDefaultsTests.cs` L42: `Assert.Equal(30, ...)` -> `Assert.Equal(480, ...)`
- `Confirmai.Tests/ConfigurationDefaultsTests.cs` L22: `Assert.Equal(60, ...)` -> `Assert.Equal(720, ...)`

### Fase 2: Testar sessao longa -- P0
**Estimativa**: ~30 min

1. Rodar o app
2. Fazer login
3. Verificar no DevTools > Network que `/api/ping` e chamado a cada 15 min
4. Verificar que o cookie `Confirmai.session` tem `Expires` atualizado apos cada ping
5. Verificar que o usuario nao perde sessao apos 30+ min de uso

**Validacao**: sessao permanece ativa por >1h sem necessidade de re-login

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
