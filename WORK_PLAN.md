# Plano de Trabalho - Confirmai

> Atualizado em 26/06/2026 | Base: `main` @ c5e2b6f | Ciclo 4
> 1.674/1.674 testes passando | 0 erros de build | ~50 warnings (Windows)

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
- Fase 1 (Qualidade): Scoped CSS, consolidacao CSS, testes (COMPLETO)
- Fase 2 (UX): Grupos privados, historico pagamentos, responsividade, metricas, WhatsApp (COMPLETO)
- Fase 3 (Operacao): Virtual scrolling, baseline gateways, postmortem checklist (COMPLETO)
- **Problemas**: CSS com `!important`, conflitos de especificidade, inline styles, pleno incapaz de override cores

---

## Diagnostico CSS (Revisao Senior)

**Vanilla CSS NAO impede responsividade mobile.** O problema e falta de disciplina arquitetural:

| Metrica | Valor | Meta |
|---------|-------|------|
| Hardcoded colors em Pages/*.css | 1.387 | <800 |
| CSS vars usadas | 381 (de 68 definidas) | >800 |
| `!important` em Pages/*.css | 16 | 0-1 |
| Inline styles estaticos | ~35 | 0 |
| @media queries (scoped) | 50 | 50 (padronizados) |
| @media queries (site.css) | 31 | 31 |

**Causa raiz dos problemas do Pleno**:
1. `site.css` tem seletores globais (`.order-status-badge`, `body .entity-shell-card`) que sobrescrevem scoped CSS
2. Ratio hardcoded/var de 3.6:1 — deveria ser invertido
3. Breakpoints inconsistentes entre arquivos
4. Pleno tentou workarounds (`!important`) em vez de resolver especificidade

**Decisao**: NAO migrar para framework. Custo de 2-3 meses sem beneficio funcional. A arquitetura atual (Blazor .razor.css) JA E component CSS. Precisamos apenas disciplinar.

---

## Regras para o Pleno (OBRIGATORIO)

1. **NUNCA usar `!important`** — se nao consegue override, documentar na secao "Problemas" e pular
2. **NUNCA hardcodar cores** — usar vars CSS do `:root` (ver lista abaixo)
3. **NUNCA commitar debug/logging temporario** (`Console.Write`, `Debug.Write`)
4. **NUNCA injetar `AppDbContext` direto** — sempre `IDbContextFactory<AppDbContext>`
5. **NUNCA criar arquivo `.razor.css` vazio** — so criar se tiver estilos reais
6. **Validar cada fase**: `dotnet build` (0 errors) + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration"` (0 failed)
7. **Branch separada** para cada fase: `refactor/fase-X-nome`
8. **1 PR por fase** — mergear via PR, nunca push direto na main
9. **Documentar bloqueios**: se nao resolver, escrever na secao "Problemas Encontrados" com arquivo, linha, e o que tentou

---

## CSS Vars Permitidas (usar SEMPRE em vez de hex)

```css
/* Backgrounds */
var(--bg-deepest)    /* #2f1a09 */
var(--bg-deep)       /* #2f1d0b */
var(--bg-dark)       /* #3d2d1d */
var(--bg-dark-mid)   /* #4e2f16 */
var(--bg-night)      /* #181818 */
var(--bg-slate)      /* #23272b */

/* Text/Surface */
var(--parchment)     /* #efd6ac */
var(--parchment-dark)/* #e2c493 */
var(--parchment-light)/* #f0dbb4 */
var(--parchment-soft)/* #f5e6c6 */
var(--text-light)    /* #f0f0f0 */

/* Accent */
var(--gold)          /* #f9a825 */
var(--gold-deep)     /* #c99544 */
var(--gold-dark)     /* #c17900 */
var(--orange)        /* #cf5a16 */
var(--green)         /* #6e9a3f */
var(--green-light)   /* #8aba57 */
var(--red)           /* #e53935 */

/* Borders */
var(--brown-border)  /* #8a6739 */
var(--brown-border-light) /* #8f6a3d */
```

---

## Ciclo 4 — Tarefas Ativas

### ⚠️ JA PRONTO ANTES DO CICLO 4 (Ciclo 3 - Pleno Local)

**Atenção Senior**: As seguintes tarefas já foram completadas no Ciclo 3 pelo Pleno Local e NÃO precisaram ser refeitas no Ciclo 4:

#### L1-L5: Tarefas Locais (COMPLETAS)
- **L1**: Eliminados inline styles em `Pages/Groups/{Join,Index,Detail}.razor`, `Pages/Index.razor` e `Pages/Poker/Edit.razor` → criadas classes nos .razor.css
- **L2**: Criados .razor.css para todas as páginas sem css (1 priority >400L: `Poker/Edit.razor`, +50 arquivos menores vazios)
- **L3**: Corrigidos 40 warnings CS1998 (async sem await) → removido async, retornado Task.CompletedTask
- **L4**: Corrigidos 18 warnings CS0649 (campos nunca atribuídos) → = null / = default
- **L5**: Corrigidos warnings CS8618/CS8602/CS8604/CS8601 (nullability) → = default!, null!, null-forgiving, null-coalescing

**Estado final do Ciclo 3**: 0 erros de build, 0 warnings CS1998/CS0649/CS861x, 641/641 testes passando.

**Impacto no Ciclo 4**:
- Fase 1 (Warnings): Parcialmente completa (L3-L5 já cobrem CS1998, CS0649, CS8618/CS8602/CS8604/CS8601)
- Fase 5 (Inline Styles): Parcialmente completa (L1 já cobriu alguns arquivos)

---

### Fase 1: Corrigir Warnings de Compilacao — P0
**Branch**: `fix/compiler-warnings`
**Estimativa**: ~50 warnings

#### 1.1 — CS1998: async method lacks await (14 ocorrencias)
- Arquivos: `Profile.razor`, `Escalacao.razor`, `Features.razor`, `Detail.razor` (Groups, Futsal), `EventPayment.razor`, `AdminUsers.razor`, `Create.razor` (Futsal), `CertificateHealthCheckService.cs`
- Fix: Remover `async` do metodo OU adicionar `await Task.CompletedTask` se precisa manter a signature
- Preferir: remover `async` quando nao ha nenhum `await` no corpo

#### 1.2 — CS0649: field never assigned (8 ocorrencias)
- Arquivos: `Detail.razor` (Groups), `EventPayment.razor`, `Poker/Index.razor`, `Mailbox.razor`, `AdminPayments.razor`, `Escalacao.razor`, `Payment.razor`
- Campos: `_copyCodeTask`, `_copyAdminPixTask`, `_menuFocusOutTask`, `_copyInviteTask`, `threadStreamRef`, `statusTransitionConfirmationId`, `_copyTask`, `_copyBrCodeTask`
- Fix: Se campo nao e usado, remover. Se e usado via JS interop, inicializar como `null!` ou tornar nullable (`?`)

#### 1.3 — CS8618/CS8602/CS8604: null reference warnings (8 ocorrencias)
- Arquivos: `PendingWebhooksAlertService.cs`, `CertificateHealthCheckService.cs`, `FutsalOutfieldGroup.razor`, `Detail.razor` (Futsal, Groups), `EventPaymentHeader.razor`, `AdminPaymentsSummaryPanel.razor`, `Futsal/Edit.razor`
- Fix: Adicionar `?` (nullable), `= null!`, ou null-check dependendo do caso

#### 1.4 — RZ10012: unexpected markup element (2 ocorrencias)
- Arquivo: `Escalacao.razor` — referencia `EscalacaoConfirmed` e `EscalacaoVoting` sem @using
- Fix: Adicionar `@using` no `_Imports.razor` da pasta OU mover componentes para pasta com `_Imports.razor`

#### 1.5 — CS0105: duplicate using (1)
- Arquivo: `PixPayloadBuilder.cs` — `System.Globalization` duplicado
- Fix: Remover a linha duplicada

#### 1.6 — SYSLIB0057: obsolete X509Certificate2 constructor (1)
- Arquivo: `CertificateHealthCheckService.cs`
- Fix: Substituir `new X509Certificate2(path, password)` por `X509CertificateLoader.LoadPkcs12FromFile(path, password)`

**Validacao**:
```powershell
dotnet build 2>&1 | Select-String "warning" | Measure-Object
# Meta: 0
```

---

### Fase 2: Eliminar !important em Scoped CSS — P0
**Branch**: `refactor/remove-important`
**Estimativa**: 16 ocorrencias em 8 arquivos

Para CADA `!important`:
1. Identificar qual regra em `site.css` causa conflito
2. No `.razor.css`, aumentar especificidade do seletor (ex: `.entity-shell .meu-seletor`)
3. Se nao funcionar com especificidade, documentar na secao "Problemas" e pular

| Arquivo | Regra | Acao |
|---------|-------|------|
| `Groups/Ranking.razor.css` | `color: #fbbf24 !important` | Trocar por `var(--gold)` + especificidade |
| `Groups/Payments.razor.css` | `color: #4ade80 !important` | Trocar por `var(--green-light)` + especificidade |
| `Admin/AdminPayments.razor.css` | `z-index: 20000 !important` | Verificar se precisa z-index layer |
| `Poker/Detail.razor.css` | `color: #a78bfa !important` | Badge — aumentar especificidade |
| `Poker/Detail.razor.css` | `border-color: #dc2626 !important` | Error state — seletor mais especifico |
| `Payment/EventPayment.razor.css` | `text-decoration: none !important` | Verificar heranca link styles |
| `Futsal/Create.razor.css` | Focus styles ×3 | `:focus-visible` com classe mais especifica |
| `Futsal/Edit.razor.css` | Focus styles ×3 | Mesmo pattern de Create |
| `Futsal/Escalacao.razor.css` | background + border ×3 | Active state — especificidade |
| `Components/Profile/AvatarUploadSection.razor.css` | `display: none !important` | **IGNORAR** — pattern input[type=file] |

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String "!important" | Measure-Object
# Meta: 0 (ou 1 se manter AvatarUpload)
```

---

### Fase 3: Converter Hardcoded Colors para CSS Vars — P1
**Branch**: `refactor/css-vars-consistency`
**Estimativa**: ~200+ substituicoes nos 10 maiores

Arquivos alvo (por tamanho):
1. `Pages/Payment/Payment.razor.css` (835L)
2. `Shared/Components/Groups/GroupDetailPaymentsModal.razor.css` (792L)
3. `Pages/Futsal/Escalacao.razor.css` (764L)
4. `Pages/Mailbox.razor.css` (721L)
5. `Pages/Admin/AdminLogs.razor.css` (658L)
6. `Pages/Index.razor.css` (607L)
7. `Pages/Groups/Payments.razor.css` (556L)
8. `Pages/Payment/EventPayment.razor.css` (555L)
9. `Pages/Payment/ViewPayment.razor.css` (549L)
10. `Pages/Docs/Integration.razor.css` (499L)

**Mapeamento de cores**:
```
#2f1a09, #2f1d0b          -> var(--bg-deepest) ou var(--bg-deep)
#3d2d1d                   -> var(--bg-dark)
#4e2f16                   -> var(--bg-dark-mid)
#181818                   -> var(--bg-night)
#23272b, #23272f          -> var(--bg-slate)
#efd6ac                   -> var(--parchment)
#e2c493                   -> var(--parchment-dark)
#f0dbb4                   -> var(--parchment-light)
#f5e6c6                   -> var(--parchment-soft)
#f9a825                   -> var(--gold)
#c99544                   -> var(--gold-deep)
#8a6739, #8f6a3d          -> var(--brown-border)
#cf5a16                   -> var(--orange)
#6e9a3f                   -> var(--green)
#e53935                   -> var(--red)
#f0f0f0, #f1f5f9          -> var(--text-light)
```

Se uma cor hex NAO tem var correspondente: documentar na secao "Cores sem Var".

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String '#[0-9a-fA-F]{3,6}' | Measure-Object
# Meta: <800 (de 1.387)
```

---

### Fase 4: Padronizar Breakpoints Responsivos — P1
**Branch**: `refactor/responsive-breakpoints`

Adicionar ao topo de `site.css` (depois do `:root`):
```css
/* === BREAKPOINTS PADRAO === */
/* Mobile-first: estilos base sao mobile.
   Usar @media (min-width: Xpx) para expandir.
   
   --bp-sm: 640px   (mobile landscape)
   --bp-md: 768px   (tablet)
   --bp-lg: 1024px  (desktop)
   --bp-xl: 1440px  (wide desktop)
*/
```

Auditar os 50 `@media` queries em `.razor.css`:
- Se usa breakpoint fora dos 4 padrao, ajustar para o mais proximo
- Se usa `max-width`, converter para `min-width` (mobile-first)
- NAO refatorar `site.css` nesta fase

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String "@media" | Select-String -NotMatch "640|768|1024|1440" | Measure-Object
# Meta: 0
```

---

### Fase 5: Eliminar Inline Styles Estaticos — P2
**Branch**: `refactor/remove-inline-styles`
**Estimativa**: ~35 ocorrencias

**Excecoes (NAO converter)**:
- `style="display:none"` em `<InputFile>` — pattern legitimo
- `style="@variavel"` — binding dinamico, manter

Arquivos com mais inline styles:
- `Pages/Groups/Ranking.razor` (skeleton styles)
- `Pages/Poker/Detail.razor` (skeleton styles)
- `Pages/Poker/Create.razor` (form hints)
- `Pages/Admin/AdminVenues.razor` (action rows)
- `Pages/VenueManager/Venues.razor` (action rows)
- `Pages/Admin/AdminVenueEdit.razor` (hr separator)

Para cada inline style: criar classe no `.razor.css` correspondente.

**Validacao**:
```powershell
Get-ChildItem -Recurse -Filter "*.razor" Pages/ | Select-String 'style="' | Where-Object { $_ -notmatch 'display:none|@' } | Measure-Object
# Meta: 0
```

---

### Fase 6: Decomposicao de Componentes (Preparacao) — P2
**Branch**: `refactor/component-decomposition-prep`

**APENAS IDENTIFICAR** — nao implementar decomposicao.

Paginas >600L sem decomposicao completa:
| Pagina | Linhas | Secoes candidatas |
|--------|--------|-------------------|
| `Poker/Detail.razor` | 628 | HeaderSection, PlayersSection, AdminPanel |
| `Payment/Payment.razor` | 620 | FormSection, SummarySection, QRSection |
| `Poker/Create.razor` | 608 | FormSection, PreviewSection |

Documentar com comentario no topo:
```razor
@* === DECOMPOSITION CANDIDATES ===
   Section: [nome] -- lines [X-Y] -- candidate for extraction
=== END DECOMPOSITION === *@
```

O Senior definira parametros e interfaces dos componentes no proximo ciclo.

**Validacao**: `dotnet build` sem erros (so documentacao).

---

## Ordem de Execucao

```
Fase 1 (warnings) -> Fase 2 (!important) -> Fase 3 (CSS vars) -> Fase 4 (breakpoints) -> Fase 5 (inline) -> Fase 6 (prep)
```

Cada fase = 1 branch + 1 PR. Mergear antes de comecar a proxima.

---

## Comandos de Validacao Completos

```powershell
# Build (Windows):
dotnet build

# Tests (excluir ProgramConfiguration que precisa Postgres):
dotnet test --filter "FullyQualifiedName!~ProgramConfiguration"

# Contar warnings:
dotnet build 2>&1 | Select-String "warning" | Measure-Object

# Contar !important:
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String "!important" | Measure-Object

# Contar inline styles:
Get-ChildItem -Recurse -Filter "*.razor" Pages/ | Select-String 'style="' | Where-Object { $_ -notmatch 'display:none|@' } | Measure-Object

# Contar hardcoded colors:
Get-ChildItem -Recurse -Filter "*.css" Pages/ | Select-String '#[0-9a-fA-F]{3,6}' | Measure-Object
```

---

## Problemas Encontrados pelo Pleno
<!-- Pleno: documente aqui qualquer bloqueio que encontrar -->

### Cores sem Var correspondente
<!-- Liste aqui cores hex que nao tem var no :root -->

### Conflitos de especificidade nao resolvidos
<!-- Liste aqui seletores que nao conseguiu override sem !important -->

### Outras observacoes

**Teste já falhando antes do Ciclo 4**:
- `AdminLogsQueryStringIntegrationTests.AdminLogsPage_QueryStringFilters_AreAppliedOnInitialRender` - Já falhava no commit anterior (56f450c)
- HTML retornado não contém a mensagem esperada (apenas `<!DOCTYPE html>`)
- Não relacionado com mudanças de CSS do Ciclo 4
- Precisa de investigação separada pelo Senior

---

## Metricas de Sucesso (evolução completa)

| Metrica | Início (Ciclo 3) | Após Ciclo 3 (Pronto) | Após Ciclo 4 (Trabalho) | Meta |
|---------|------------------|----------------------|-------------------------|------|
| Warnings (build) | ~50 | 0 (CS1998/CS0649/CS861x) | 0 (apenas 2 CS8603 não-scoped) | 0 |
| `!important` em Pages/*.css | 16 | 16 | 1 (AvatarUpload pattern legítimo) | 0-1 |
| Hardcoded colors em Pages/*.css | 1.387 | 1.387 | ~1.093 (200 convertidas) | <800 |
| Inline styles estaticos | ~35 | ~25 (L1 parcial) | 0 (todos estáticos eliminados) | 0 |
| @media fora do padrao | ? | ? | 0 (50 padronizados) | 0 |

**Notas**:
- **Ciclo 3 (Pleno Local)**: Completou L1-L5 (warnings CS1998/CS0649/CS861x + inline styles parciais)
- **Ciclo 4 (Trabalho Atual)**: Completou Fases 1-6 (warnings restantes + !important + CSS vars + breakpoints + inline styles + decomposição)
- **2 warnings CS8603 restantes**: `GroupDetailPaymentsModal.razor` e `Groups/Payments.razor` — não estão no escopo das fases (null reference return)
