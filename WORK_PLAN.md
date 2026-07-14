# Plano de Trabalho - Confirmai

> Atualizado em 14/06/2026 | Base: `main` (pos-Ciclo 16) | Refatoracao CSS CONCLUIDA
> 1.694/1.694 testes passando | 0 erros de build | 0 AppDbContext direto | 0 services sem teste
> Ciclo 15 (testes + UX grupos + !important) e Ciclo 16 (mobile UX) -- CONCLUIDOS e revisados
> Proximo: Ciclo 17 -- Login Google (OAuth, criar-ou-vincular) + email real/confirmacao + fix caracteres especiais + consolidar CSS do menu mobile
> Futuros: C18 mobile UX critico | C19 refatoracao TDD+SOLID (inclui fase CSS Web/Mobile) | C20 WhatsApp+baseline (POSTERGADO)

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

### Ciclo 13 (Pleno Local): Background + Navegacao + Reorganizacao UX
- Branch: `fix/ciclo13-ux-layout` | PR #46
- Background fix: moveu `.sports-shell`/`.listing-block` de scoped para global (`events.css`) — elimina risco de CSS isolation
- Navegacao: tela inicial = `/eventos` (antes `/grupos`), nav reordenado, `Dashboard` redireciona para `/eventos`
- Nova pagina `/grupo/{Id}/partidas` extraida de `Groups/Detail.razor` com `IDbContextFactory`
- SportCard redesenhado com gradientes futsal/poker + emoji watermarks decorativos
- `body` background: `var(--ci-bg)` -> `var(--ci-bg-card)`, `.main-content` -> `transparent` (hierarquia visual)
- Melhorias pagamento: QR ampliado, comprovante centralizado, nomes null-safe
- **Problemas**: docs separado (regra 20), arquivo vazio commitado, 1 rgba com var existente

### Ciclo 14 (Pleno Local): Refresh Token / Sessao Persistente
- Branch: `feat/ciclo14-refresh-token` | PR #48 + PR #49 (docs)
- `SessionTimeoutMinutes`: 60->720 (dev, 12h), 30->480 (prod, 8h)
- `PingController.cs` (novo): endpoint `[Authorize] GET /api/ping` -> `200 OK`
- `session-keepalive.js` (novo): `fetch('/api/ping')` a cada 15min com `credentials: same-origin`
- `Program.cs`: `AddControllers()` + `MapControllers()` (nao existiam)
- `_Host.cshtml`: script registrado antes de `blazor.server.js`
- Testes: 4 asserts atualizados (SecurityPolicyDefaults + ConfigurationDefaults)
- Fase 2 validada manualmente: ping 200 OK, cookie renovado, sessao ativa >15min
- **Problemas**: nenhum

### Ciclo 15 (Pleno Local): Testes + UX Grupos Privados + Cleanup !important
- Branch: `fix/ciclo15-tests-ux-cleanup` | PR #51 (merged)
- Fases 1-5: 20 novos testes (PingController, GroupMetricsService, CityService, WhatsAppNotificationService, LocationService) -- 5 services sem teste ZERADO
- Fase 6: Botao "Aprovar todos" na listagem de grupos com `@onclick:stopPropagation`
- Fase 7: Auditoria `!important` no site.css (11/11 legitimos, nenhum removido)
- Fase 8 (EXTRA, pedido do Robson): 4 novos esportes visuais (Volleyball/BeachTennis/Footvolley/Chess, cards "Em breve")
- 1.694/1.694 testes passando (+20 vs C14), 0 erros de build
- **Problemas**: 3 hardcoded hex + 8 vars mortas na Fase 8 (Senior corrigiu). Nao foi scope creep -- esportes pedidos pelo Robson (regra 22)

### Ciclo 16 (Pleno Local): Mobile UX -- Menu Hamburger + Header Consistente
- Branch: `fix/ciclo16-mobile-ux` | PR #52 (merged)
- DESVIO: ciclo planejado era WhatsApp+Baseline (postergado para Ciclo 20). Pleno repriorizou para mobile UX.
- Fase 1: Implementação do menu hamburger (MainLayout.razor + MainLayout.razor.css)
- Fase 2: Correção do z-index (site.css + MainLayout.razor.css)
- Fase 3: Correção do city selector (CitySelector.razor.css)
- Fase 4: Reorganização do menu mobile (internacionalização primeiro)
- Fase 5: Ajuste do botão "Meus Eventos" (Index.razor.css)
- Fase 6: Linhas separatorias e layout (MainLayout.razor.css)
- Fase 7: Background da internacionalização (MainLayout.razor.css)
- Fase 8: Textos em mensagens e perfil (MainLayout.razor + MainLayout.razor.css)
- Fase 9: Correção de cor/fonte (MainLayout.razor.css)
- Fase 10: Tela de login mobile (identity.css + _Layout.cshtml viewport meta tag)
- Fase 11: Menu mobile colapsar automaticamente ao clicar (MainLayout.razor HandleLocationChanged)
- Fase 12: Badge de aprovar no card do grupo (Index.razor.css position top)
- Fase 13: Div de código para entrar em novo grupo mobile (events.css groups-block padding)
- Fase 14: Scroll horizontal desnecessário na página de grupos (events.css box-sizing)
- Fase 15: Botão de pagamentos no mobile (BLOQUEADO - NECESSITA DIRECIONAMENTO DO SENIOR)
- **Status**: Menu mobile funcional, estrutura CSS estabelecida
- **Problemas**: Breakpoints inconsistentes (700px vs 768px), elementos fora do media query
- **PROBLEMA CRÍTICO FASE 15**: Botão de pagamentos não acompanha os demais botões no mobile. Nenhuma das tentativas funcionou.

## Documentação Completa das Tentativas - Fase 15

### Tentativa 1: Adicionar classe específica ao HTML
- **Arquivo**: MainLayout.razor
- **Mudança**: Adicionada classe `oldsite-top-nav-payments-link` ao link de pagamentos
- **Arquivo**: MainLayout.razor.css
- **Mudança**: Criados estilos mobile específicos para `.oldsite-top-nav-payments-link`
- **Resultado**: Sem efeito visual

### Tentativa 2: Usar seletor de atributo
- **Arquivo**: MainLayout.razor
- **Mudança**: Removida classe `oldsite-top-nav-payments-link`
- **Arquivo**: MainLayout.razor.css
- **Mudança**: Usar seletor de atributo `a[href="/payments"]` para estilos mobile
- **Resultado**: Sem efeito visual

### Tentativa 3: Usar !important para forçar estilos
- **Arquivo**: MainLayout.razor.css
- **Mudança**: Adicionar `!important` a todos os estilos mobile de `body .oldsite-top-nav > a` e `a[href="/payments"]`
- **Resultado**: Sem efeito visual

### Tentativa 4: Adicionar classe compartilhada a todos os links
- **Arquivo**: MainLayout.razor
- **Mudança**: Adicionar classe `oldsite-top-nav-link` a todos os links de navegação (grupos, pagamentos, integration, admin)
- **Arquivo**: MainLayout.razor.css
- **Mudança**: Criar estilos mobile específicos para `.oldsite-top-nav-link` com `!important`
- **Resultado**: Sem efeito visual

### Tentativa 5: Modificar site.css diretamente
- **Arquivo**: site.css
- **Mudança**: Alterar media query `@media (max-width: 700px)` para `body .oldsite-top-nav a`:
  - width 100%, padding 0.6rem 0.8rem, text-align left, border-bottom, border-right none, min-height auto, box-sizing border-box
  - Remover flex: 1 1 auto, min-width 132px, min-height 44px
- **Resultado**: Sem efeito visual mesmo após dotnet clean + build

### Tentativa 6: Remover !important do MainLayout.razor.css
- **Arquivo**: MainLayout.razor.css
- **Mudança**: Remover `!important` para manter consistência com site.css
- **Resultado**: Sem efeito visual

## Análise Técnica

### Estrutura CSS
- site.css é carregado globalmente via _Host.cshtml
- MainLayout.razor.css é carregado como scoped CSS com atributos `b-xxx`
- site.css tem estilos `body .oldsite-top-nav a` com alta especificidade
- MainLayout.razor.css tem estilos mobile em media query `@media (max-width: 700px)`

### Possíveis Causas
1. **Cache do navegador**: Hot reload não aplicando scoped CSS corretamente (regra 21)
2. **Blazor CSS isolation**: Scoped CSS pode não estar aplicando corretamente
3. **Specificidade**: Estilos globais do site.css podem estar prevalecendo
4. **Media query não sendo ativada**: Breakpoint pode não estar sendo atingido
5. **Estrutura HTML**: Link de pagamentos está dentro de AuthorizeView, pode afetar aplicação de estilos

### Testes Realizados
- dotnet clean + dotnet build
- Ctrl+F5 (hard refresh) no navegador
- Verificação de media query em devtools
- Verificação de estilos aplicados no elemento

## Solicitação ao Senior -- RESPONDIDA
Ver **Revisao Senior do Ciclo 16 > Fase 15 BLOQUEADA** abaixo. Causa raiz: CSS do `oldsite-top-nav` fragmentado/duplicado entre `site.css` (global) e `MainLayout.razor.css` (scoped) -- mesmo padrao do bug de background dos Ciclos 12/13. Direcao: consolidar tudo em `site.css`, remover bloco mobile duplicado do scoped. Detalhado no Ciclo 17.

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

## Revisao Senior do Ciclo 13

### Veredicto: EXCELENTE -- Abordagem de background SUPERIOR ao planejado, navegacao reestruturada, UX melhorada

| Metrica | C12 | C13 | Status |
|---------|-----|-----|--------|
| Build errors | 0 | **0** | Atingido |
| Tests | 1.674 | **1.674** | Atingido |
| Hardcoded hex scoped | 0 | **0** | Atingido |
| rgba() hardcoded scoped | 189 | **189** | Estavel |
| CSS vars (scoped) | 2.364 | **2.360** | Estavel (-4 movidas p/ global) |
| `!important` scoped | 1 | **1** | Atingido |
| Vars no `:root` | 315 | **315** | Atingido |
| Vars indefinidas | 0 | **0** | Atingido |
| Vars mortas | 0 | **0** | Atingido |
| Tela inicial | /grupos | **/eventos** | **Atualizado** |

### Fase 1 (Background fix): EXECUTADA -- abordagem MELHOR que a documentada

O Senior documentou fix pontual em `.sports-shell` no scoped CSS. O Pleno tomou decisao SUPERIOR:
**moveu `.sports-page`, `.sports-shell`, `.listing-page`, `.listing-block` de scoped CSS para global `events.css`**.
Isso elimina completamente o risco de CSS isolation que era a causa raiz dos problemas de cache.

CSS final alinhado com referencia `.groups-block`:
```css
background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
border: 1px solid var(--ci-border);
border-radius: 14px;
box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
```

Bonus: mudou `body` background de `var(--ci-bg)` para `var(--ci-bg-card)` e `.main-content` para `transparent`,
criando hierarquia visual entre body (#111927, claro) e blocos de conteudo (gradient #0a1928->#090f18, escuro).

### Fase 2 (Mobile): PARCIAL

Ajustes pontuais (`.sports-tabs` position static em 768px, `.my-events-link-btn` responsive),
mas sem teste sistematico documentado. Aceitavel dado que o foco era a Fase 1 e 3.

### Fase 3 (Navegacao): EXECUTADA CORRETAMENTE

- `Index.razor`: `@page "/"` + `@page "/eventos"` (home = eventos)
- `Groups/Index.razor`: removeu `@page "/"`
- `Dashboard.razor`: redireciona para `/eventos` em vez de `/grupos`
- `MainLayout.razor`: Eventos primeiro na nav, Grupos atras de `AuthorizeView`
- `Futsal/Index.razor`: "Criar Partida" -> "Criar Grupo" (`/grupos/criar`)
- Teste atualizado: `FutsalIntegrationTests` espera `/grupos/criar`

### Mudancas extras (fora do escopo planejado)

1. **Nova pagina `/grupo/{Id}/partidas`**: Extraiu secao de partidas de `Groups/Detail.razor` para pagina dedicada.
   Usa `IDbContextFactory` (correto), `partial class`, code-behind limpo (79L). Boa decomposicao.

2. **SportCard redesenhado**: Gradientes `futsal-green-dark`/`poker-bg-dark` no body, emoji watermarks via `::after`,
   header centralizado. Mudanca visual significativa.

3. **Pagamento**: QR ampliado (170px -> 384px), comprovante centralizado, nomes null-safe (`FullName ?? UserName ?? "Jogador"`).

4. **`.detail-card` atualizado**: Mesmo gradient/border/shadow da referencia.

5. **Readability**: `--slate-muted` -> `--ci-text-muted`, `--ci-text-muted` -> `--ci-text-blue` em pagamento.

### Problemas encontrados e corrigidos pelo Senior

**1. `rgba(79, 156, 248, 0.10)` hardcoded com var existente**:
Em `events.css` `.detail-admin-btn--partidas:hover`. Convertido para `var(--accent-opacity-xs)`.

**2. `docs/ciclo13-auditoria-css.md` (470L) criado**:
Viola regra 20 (consolidar tudo no WORK_PLAN.md). Senior removeu o arquivo.

**3. `.devin/workflows/meus-eventos.md` (0 bytes) commitado**:
Arquivo vazio sem utilidade. Senior removeu.

### Positivo
- Decisao de mover CSS de scoped para global e SUPERIOR ao fix pontual -- elimina classe inteira de bugs
- Navegacao reestruturada conforme solicitacao do dono do produto
- Partidas extraida para pagina dedicada -- boa separacao de concerns
- Nomes null-safe em pagamento -- fix defensivo correto
- IDbContextFactory usado na nova pagina (regra 4 respeitada)
- 0 vars indefinidas, 0 vars mortas -- disciplina mantida

### Ressalvas
- Regra 20 violada novamente (docs separado)
- Regra 16 parcialmente violada (background fix + feature UX no mesmo PR)
- `rgba(79, 156, 248, 0.08)` em `Create.razor.css` sem var (nenhuma var 0.08 existe, aceitavel)

---

## Revisao Senior do Ciclo 14

### Veredicto: PERFEITO -- Implementacao exata conforme documentado, zero problemas

| Metrica | C13 | C14 | Status |
|---------|-----|-----|--------|
| Build errors | 0 | **0** | Atingido |
| Tests | 1.674 | **1.674** | Atingido |
| SessionTimeoutMinutes (dev) | 60 | **720** | Atualizado |
| SessionTimeoutMinutes (prod) | 30 | **480** | Atualizado |
| PingController | -- | **Criado** | Novo |
| session-keepalive.js | -- | **Criado** | Novo |
| AddControllers/MapControllers | -- | **Registrado** | Novo |

### O que o Pleno fez

**Fase 1 (Refresh Token)**: Implementacao EXATA conforme orientacao do Senior:
1. `SecurityPolicyDefaults.cs`: `SessionTimeoutMinutes` dev 60→720, prod 30→480
2. `Controllers/PingController.cs`: endpoint `[Authorize] GET /api/ping` → `200 OK`
3. `wwwroot/js/session-keepalive.js`: `fetch('/api/ping')` a cada 15min com `credentials: same-origin`
4. `Program.cs`: `AddControllers()` + `MapControllers()` registrados
5. `Pages/_Host.cshtml`: script antes de `blazor.server.js`
6. Testes: 4 asserts atualizados (SecurityPolicyDefaultsTests + ConfigurationDefaultsTests)

**Fase 2 (Validacao manual)**: Confirmada pelo Pleno -- ping 200 OK, cookie renovado, sessao ativa >15min

### Problemas encontrados pelo Senior
Nenhum. Codigo limpo, commits limpos, todas as regras respeitadas.

### Positivo
- Seguiu instrucoes do Senior ao pe da letra -- melhor ciclo em termos de aderencia
- 1 commit de implementacao + 2 commits de docs (aceitavel)
- Branch unica, PRs limpas (regras 9, 10, 14 respeitadas)
- Testes atualizados ANTES de commitar (regra 8 respeitada)

---

## Revisao Senior do Ciclo 15

### Veredicto: MUITO BOM -- 20 testes limpos, UX grupos correta, !important auditado. Scope creep na Fase 8 (novos esportes)

| Metrica | C14 | C15 | Status |
|---------|-----|-----|--------|
| Build errors | 0 | **0** | Atingido |
| Tests | 1.674 | **1.694** (+20) | Melhoria |
| Test files | 192 | **197** (+5) | Melhoria |
| Services sem teste | 5 | **0** | **Zerado** |
| Hardcoded hex scoped | 0 | **0** (apos fix) | Atingido |
| Vars indefinidas | 0 | **0** | Atingido |
| Vars mortas `:root` | 0 | **0** (apos fix) | Atingido |

### Bloco A -- Testes (Fases 1-5): 5/5 executados, cobertura ZERADA

Os 5 services sem teste agora tem cobertura. 20 novos testes, todos passando:
- **`PingControllerTests`** (2): auth 200 + anon 401/302 via `IntegrationTestWebAppFactory`. Padrao correto.
- **`GroupMetricsServiceTests`** (5): snapshot com dados, grupo inexistente, grupo vazio, multi-grupo, payment rate 70%. Usa `IDbContextFactory` mock + `TestDataFactory`. Excelente.
- **`CityServiceTests`** (5): cidades distintas, sem grupos, inativos excluidos, edge cases IBGE (vazio/null sem chamada HTTP). Seguiu a nota do Senior (nao mockar IBGE real).
- **`WhatsAppNotificationServiceTests`** (5): sem config, com config, erro API 500, formatacao lembrete/pagamento. `MockHttpMessageHandler` proprio -- abordagem correta.
- **`LocationServiceTests`** (3): `GetStateCode` puro via reflection (`BindingFlags.NonPublic | Static`). Testou so a logica pura como planejado.

### Bloco B -- UX + Cleanup (Fases 6-7): executados

**Fase 6 (Aprovar todos)**: `ApproveAllPending(groupId)` em `Groups/Index.razor` com `@onclick:stopPropagation`, botao so aparece quando `hasPending = isAdmin && pendingCount > 0`, spinner de processamento, mailbox preservada, `LoadGroups()` no fim. Reutilizou o padrao de `Detail.razor.cs`. Bom.

**Fase 7 (!important)**: Auditoria confirmada -- 11/11 legitimos (utility, acessibilidade W3C, Google Maps z-index, autofill Chrome). Nenhum removido. Correto.

### Fase 8 (EXTRA) -- Novos esportes visualmente (PEDIDO PELO ROBSON)

O Pleno adicionou 4 esportes ao `Enums/Sport.cs` (Volleyball, BeachTennis, Footvolley, Chess) com cards "Em breve", temas de cor e watermarks. **Requisito do Robson** (nao foi iniciativa do Pleno) -- portanto NAO conta como scope creep (regra 22). Cards sao placeholders (`ComingSoon=true`, so Futsal ativo), sem risco de fluxo quebrado. Unica ressalva e a higiene de CSS (ver problemas abaixo), padrao normal de cleanup Senior.

### Problemas encontrados e corrigidos pelo Senior

**1. 3 hardcoded hex em `SportCard.razor.css`** (Fase 8): `#fff`, `#93c5fd`, `#99f6e4`. Senior converteu para `var(--white)`, `var(--link-info)` e nova var `--beachtennis-cta`. Disciplina "0 hardcoded hex scoped" restaurada.

**2. 4 vars mortas no `:root`** (Fase 8): `--volleyball-text`, `--beachtennis-text`, `--chess-text`, `--footvolley-text` -- definidas mas nunca usadas (so os `-text-pale` sao consumidos). Senior removeu (regra 13).

**3. 4 vars `-accent-deep` mortas** (Fase 8): `--volleyball/beachtennis/footvolley/chess-accent-deep` -- 0 usos. Senior removeu.

### Ressalvas
- Fase 8 (esportes, pedido do Robson) introduziu 3 hardcoded hex + 8 vars mortas -- higiene de CSS a observar em features com tema de cor, mas nao e scope creep

---

## Revisao Senior do Ciclo 16

### Veredicto: BOM em UX mobile, mas DESVIO DE ESCOPO -- nao implementou o planejado (WhatsApp + Baseline). Fase 15 bloqueada.

| Metrica | C15 | C16 | Status |
|---------|-----|-----|--------|
| Build errors | 0 | **0** | Atingido |
| Tests | 1.694 | **1.694** | Atingido |
| Hardcoded hex scoped | 0 | **0** (apos fix) | Atingido |
| Vars indefinidas | 0 | **0** (apos fix) | Atingido |
| WhatsApp real | pendente | **pendente** | NAO feito |
| Baseline por gateway | pendente | **pendente** | NAO feito |

### DESVIO DE ESCOPO (importante)

O Ciclo 16 planejado era **Integracao WhatsApp Real + Baseline Operacional por Gateway**. O Pleno em vez disso executou um ciclo de **UX Mobile** (menu hamburger + padrao de header consistente em ~8 telas). O WhatsApp/Baseline foi movido para o **Ciclo 17** no WORK_PLAN.md.

Isto foi uma repriorizacao (mobile e o publico predominante -- justificativa valida), mas o trabalho planejado do Ciclo 16 continua **pendente**. Registrado como Ciclo 17.

### O que o Pleno fez (Mobile UX)

- **Menu hamburger mobile** em `MainLayout.razor` + `MainLayout.razor.css` (nav vira coluna em `<700px`, colapsa ao navegar via `HandleLocationChanged`)
- **Padrao de header consistente** (btn voltar / badge / titulo) em Pagamentos, Ranking, Futsal, Poker
- Cores semanticas dos botoes Pendentes (vermelho) / Historico (accent), badge de valor verde metalico
- Correcao de botoes extrapolando div, scroll horizontal em /grupos, viewport meta tag no login
- Restaurou envio de email em `NotifyDelinquencyAsync` (commit 63be464)

### Problemas encontrados e corrigidos pelo Senior

**1. 3 vars indefinidas em `Groups/Payments.razor.css`** (commit 2f803da, cor do botao Pendentes): `--red-light`, `--shadow-red-lg`, `--shadow-red-md` usadas SEM fallback -> renderizam vazio (gradiente/sombra quebrados no botao "Pendentes" e badge). Mesmo problema recorrente da regra 12. Senior adicionou as 3 ao `:root` seguindo a escala red existente (sm=0.3, md=0.4, lg=0.5).

**2. 3 arquivos .md soltos na raiz** (regra 20): `CSS_MOBILE_WEB_ISSUES.md`, `CSS_PATTERNS_HEADER.md`, `MOBILE_UX_CYCLE_16.md`. Senior removeu -- docs so no WORK_PLAN.md.

### Fase 15 BLOQUEADA -- Direcao do Senior (botao Pagamentos no mobile)

O Pleno tentou 6 abordagens para alinhar o link de pagamentos no menu mobile e todas falharam "sem efeito visual". **Causa raiz: CSS do `oldsite-top-nav` esta FRAGMENTADO e DUPLICADO entre global e scoped** -- exatamente o mesmo padrao que causou o bug de background dos Ciclos 12/13.

Evidencia:
- `site.css` tem **2 blocos base** `body .oldsite-top-nav {` (linhas ~3543 e ~5332) que se sobrescrevem
- `site.css` tem regras mobile `body .oldsite-top-nav a` em `@media (max-width: 700px)` (~4225)
- `MainLayout.razor.css` tem OUTRO `@media (max-width: 700px)` com `> a` E `.oldsite-top-nav-link` (scoped, ganha `[b-xxx]`)

Com duas fontes de verdade competindo (uma global descendente `a`, outra scoped `> a` + classe), o cascade/especificidade fica fragil e o hot-reload nao reflete mudancas -- foi por isso que "nenhuma tentativa teve efeito".

**Direcao para o Pleno (Ciclo 17, Fase mobile-nav)**: aplicar a solucao JA PROVADA no Ciclo 13 (background) -- **consolidar TODO o CSS do `oldsite-top-nav` numa unica fonte global (`site.css`) e REMOVER o bloco mobile duplicado de `MainLayout.razor.css`**. Passos:
1. Mover as regras mobile de `MainLayout.razor.css` (`@media max-width:700px` do nav) para `site.css`
2. Colapsar os 2 blocos base `body .oldsite-top-nav {` em um so
3. Usar UM unico seletor para todos os links (`body .oldsite-top-nav a` OU `.oldsite-top-nav-link`, nao os dois) para o link de pagamentos herdar identico aos demais
4. `dotnet clean && dotnet build` + Ctrl+F5 (regra 21) antes de validar

Nota: as 6 tentativas falhas ja foram revertidas (nao ha seletores `payments-link`/`a[href="/payments"]` residuais no codigo atual -- confirmado pelo Senior).

### Ressalvas
- Desvio de escopo: ciclo planejado (WhatsApp/Baseline) nao foi feito -> Ciclo 17
- Muitos commits de tentativa/erro nas cores dos botoes (regra 14): ef6c854, c0861de, 1aa0b94, 76d24fe, 2f803da... ~10 commits so ajustando o vermelho
- 3 vars indefinidas reintroduzidas (regra 12 de novo)
- 3 docs soltos (regra 20 de novo)

---

## Metricas Atuais (pos-Ciclo 16)

| Metrica | C10 | C11 | C12 | C13 | C14 | C15 | C16 |
|---------|-----|-----|-----|-----|-----|-----|-----|
| Warnings (build) | 0 | 0 | 0 | 0 | 0 | 0 | **0** |
| Tests | 1.674 | 1.674 | 1.674 | 1.674 | 1.674 | 1.694 | **1.694** |
| Test files | -- | -- | -- | -- | 192 | 197 | **197** |
| Services sem teste | -- | -- | -- | -- | 5 | 0 | **0** |
| `!important` scoped | 1 | 1 | 1 | 1 | 1 | 1 | **2** (2x `display:none`, legitimos) |
| `!important` global | 12 | 11 | 11 | 11 | 11 | 11 | **11** |
| Hardcoded hex scoped | 0 | 0 | 0 | 0 | 0 | 0 | **0** (apos fix) |
| rgba() hardcoded scoped | 378 | 185 | 189 | 189 | 189 | 189 | **~189** |
| Vars no `:root` | 168 | 317 | 315 | 315 | 315 | 337 | **337** (apos fix) |
| Vars indefinidas | 0 | 0 | 0 | 0 | 0 | 0 | **0** (apos fix) |
| Vars mortas `:root` | 0 | 0 | 0 | 0 | 0 | 0 | **0** (apos fix) |
| Pages >400L sem code-behind | 9 | 0 | 0 | 0 | 0 | 0 | **0** |
| AppDbContext direto | -- | 3 | 0 | 0 | 0 | 0 | **0** |
| xUnit2013 warnings | -- | 49 | 0 | 0 | 0 | 0 | **0** |
| SessionTimeoutMinutes (dev/prod) | -- | -- | -- | 60/30 | 720/480 | 720/480 | **720/480** |

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
22. **Requisitos vindos dos testes do Robson sao legitimos** -- bugs/melhorias/requisitos que o Robson levanta testando o app NAO sao "scope creep" e devem entrar na "Fase padrao de melhorias UX" do ciclo. A regra 16 (separar features) so se aplica a adicoes que o proprio Pleno inventa sem pedido (ex: novos esportes). Documentar cada item vindo do Robson na fase de melhorias antes de implementar
23. **ISOLAMENTO WEB/MOBILE EM CSS** -- NUNCA aplicar estilos de layout (width, max-width, padding, gap, font-size) sem media query de isolamento. Estilos desktop devem usar `@media (min-width: 769px)`. Estilos mobile devem usar `@media (max-width: 768px)`. O base (sem media query) deve ser mobile-first ou neutro. SEMPRE testar em ambas as viewports apos mudancas de layout. Bug recorrente: afinamento de width 57% aplicado sem media query quebrou o mobile (campos esmagados) -- corrigido envolvendo em `@media (min-width: 769px)`
24. **Pasta default para prints**: `C:\Users\FreezaPC\Desktop\devin-prints` -- sempre procurar nesta pasta quando o usuario mencionar "ver print na pasta"

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

### Trabalho restante (nao-CSS) -- TODOS CONCLUIDOS

| Item | Descricao | Prioridade | Status |
|------|-----------|------------|--------|
| AppDbContext -> IDbContextFactory | 3 paginas restantes: ViewPayment, Payment, PaymentDetails | P1 | **ZERADO (C12)** |
| !important site.css | 11 ocorrencias, maioria legitima (autofill, accessibility, modals) | P3 | **Reducao parcial (C12)** |
| Build warnings (Tests) | 49 warnings de xUnit2013 no projeto de testes (nao afetam producao) | P3 | **ZERADO (C12)** |

---

## Estado Final pos-Ciclo 14

Refatoracao CSS, cleanup tecnico e infraestrutura de sessao **completos**. Marcos:
- 0 hardcoded hex em scoped CSS (desde C6)
- 0 `AppDbContext` direto (desde C12)
- 0 paginas >400L sem code-behind (desde C11)
- 0 vars indefinidas, 0 vars mortas (desde C13)
- 315 vars no `:root`, 4.244 usos de `var()` total
- Tela inicial = `/eventos` (desde C13)
- Background `.sports-shell` / `.listing-block` alinhados com `.groups-block` (global em events.css)
- 1 `!important` scoped (AvatarUpload pattern legitimo)
- Refresh token via keep-alive JS (15min ping) + session timeout 720min dev / 480min prod (desde C14)

---

## Ciclo 15 -- Testes + UX Grupos Privados + Cleanup !important

**Branch**: `fix/ciclo15-tests-ux-cleanup`
**1 commit por fase** dentro da branch. **1 PR** no final.
Validar cada fase com `dotnet build` + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"`.

### Fase 1 — Testes do PingController (P1)

Criar `Confirmai.Tests/PingControllerTests.cs`. Usar `IntegrationTestWebAppFactory` (ja existe no projeto, ver `AdminLogsQueryServiceIntegrationTests.cs` como referencia).

```csharp
// PingControllerTests.cs
public class PingControllerTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    public PingControllerTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_WithAuthenticatedUser_Returns200()
    {
        var client = _factory.CreateClient();
        // Autenticar (ver padrao em FullFlowAuthenticationIdentityScenariosIntegrationTests.cs)
        var response = await client.GetAsync("/api/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/api/ping");
        // Blazor Server redireciona para login (302) ou retorna 401
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized
                 || response.StatusCode == HttpStatusCode.Redirect);
    }
}
```

**Arquivo**: `Confirmai.Tests/PingControllerTests.cs`
**Referencia**: `Confirmai.Tests/AdminLogsQueryServiceIntegrationTests.cs` (padrao IClassFixture)
**Referencia**: `Confirmai.Tests/FullFlowAuthenticationIdentityScenariosIntegrationTests.cs` (padrao de auth)

### Fase 2 — Testes do GroupMetricsService (P2)

Criar `Confirmai.Tests/GroupMetricsServiceTests.cs`. Usar `TestDataFactory.CreateDbContext()` (in-memory EF Core).

```csharp
// Testes a implementar:
[Fact] GetSnapshot_WithEventsAndMembers_CalculatesCorrectly()
  // Criar grupo com 5 membros, 3 eventos (2 passados, 1 futuro)
  // Criar confirmacoes e pagamentos
  // Assert: TotalMembers=5, TotalEvents=3, UpcomingEvents=1, PaymentCompletionRate>0

[Fact] GetSnapshot_GroupNotFound_ReturnsEmptySnapshot()
  // groupId inexistente → snapshot com zeros

[Fact] GetSnapshot_EmptyGroup_ReturnsZeros()
  // grupo sem membros/eventos → TotalMembers=0, TotalEvents=0, etc.

[Fact] GetMultipleSnapshots_ReturnsAllGroups()
  // 2 grupos → dictionary com 2 entries

[Fact] GetSnapshot_PaymentRate_CalculatesCorrectly()
  // 10 confirmacoes, 7 pagas → PaymentCompletionRate=70.0
```

**Arquivo**: `Confirmai.Tests/GroupMetricsServiceTests.cs`
**Referencia**: `Services/Groups/GroupMetricsService.cs` (95 linhas)
**Modelo**: `GroupMetricsSnapshot` — 8 propriedades (TotalMembers, TotalEvents, UpcomingEvents, AverageAttendanceRate, PendingJoinRequests, TotalConfirmations, PaidConfirmations, PaymentCompletionRate)

### Fase 3 — Testes do CityService (P2)

Criar `Confirmai.Tests/CityServiceTests.cs`.

```csharp
// Testes a implementar:
[Fact] GetCityOptions_WithActiveGroups_ReturnsDistinctCities()
  // Criar 3 grupos ativos em 2 cidades → 2 CityOption
  // Assert: format "Cidade|UF" no Value, "Cidade - UF" no Label

[Fact] GetCityOptions_NoActiveGroups_ReturnsEmptyList()
  // Sem grupos ativos → lista vazia

[Fact] GetCityOptions_InactiveGroupsExcluded()
  // Grupo com IsActive=false → nao aparece

[Fact] GetIbgeMunicipios_EmptyStateCode_ReturnsEmpty()
  // stateCode="" → lista vazia (sem chamada HTTP)

[Fact] GetIbgeMunicipios_NullStateCode_ReturnsEmpty()
  // stateCode=null → lista vazia
```

**NOTA sobre `GetIbgeMunicipiosAsync`**: Depende de API externa (IBGE). Testar apenas os edge cases (string vazia/null). NAO mockar HttpClient para a chamada real — baixo ROI.

**Arquivo**: `Confirmai.Tests/CityServiceTests.cs`
**Referencia**: `Services/CityService.cs` (64 linhas)
**Modelo**: `CityOption` (Value, Label, City, StateCode)

### Fase 4 — Testes do WhatsAppNotificationService (P2)

Criar `Confirmai.Tests/WhatsAppNotificationServiceTests.cs`.

```csharp
// Testes a implementar (com HttpClient mockado):
[Fact] SendNotification_NoConfig_ReturnsFalse()
  // Config sem WhatsApp:ApiKey → false (sem chamada HTTP)

[Fact] SendNotification_WithConfig_CallsApi()
  // Config com ApiKey+ApiUrl + mock HttpMessageHandler → true

[Fact] SendNotification_ApiError_ReturnsFalse()
  // Mock retorna 500 → false (catch block)

[Fact] SendEventReminder_FormatsMessageCorrectly()
  // Verificar que a mensagem contem nome do evento e data

[Fact] SendPaymentReminder_FormatsAmountCorrectly()
  // Verificar formato "R$ 25,00"
```

**Arquivo**: `Confirmai.Tests/WhatsAppNotificationServiceTests.cs`
**Referencia**: `Services/Notification/WhatsAppNotificationService.cs` (55 linhas)
**Mock**: Usar `MockHttpMessageHandler` (ver padrao em `BitcoinPaymentFactoryTests.cs` se existir, ou criar custom handler)

### Fase 5 — Testes do LocationService (P3)

Criar `Confirmai.Tests/LocationServiceTests.cs`.

```csharp
// Testes do metodo estatico GetStateCode (via reflection ou por ReverseGeocodeAsync):
[Fact] GetStateCode_ValidStates_ReturnsCorrectCodes()
  // "Sao Paulo" → "SP", "Rio de Janeiro" → "RJ", etc.

[Fact] GetStateCode_CaseInsensitive()
  // "sao paulo" → "SP"

[Fact] GetStateCode_InvalidState_ReturnsEmpty()
  // "XYZ" → ""
```

**NOTA**: `LocationService` depende de `IJSRuntime` (browser API) e `HttpClient` (Nominatim). Testar apenas o metodo puro `GetStateCode` — para isso, extrair como `internal static` OU testar via `ReverseGeocodeAsync` com mock. Se for muito complexo, pular e documentar o motivo.

**Arquivo**: `Confirmai.Tests/LocationServiceTests.cs`
**Referencia**: `Services/LocationService.cs` (109 linhas)

### Fase 6 — UX Grupos Privados: Atalhos de aprovacao/rejeicao

**Problema**: Na `/grupos` (Index), admin ve badge "3 pendentes" mas precisa entrar na pagina do grupo para aprovar/rejeitar. Nao ha atalho direto.

**Solucao**: Adicionar swipe actions ou botoes inline na listagem de grupos.

**Implementacao**:

1. Em `Pages/Groups/Index.razor`, adicionar botoes "Aprovar todos" e "Ver pendentes" no card do grupo quando `hasPending == true`:

```razor
@* Dentro do foreach, apos o badge de pendentes *@
@if (hasPending)
{
    <div class="group-card-pending-actions" @onclick:stopPropagation="true">
        <button class="btn btn--sm btn--success" @onclick="() => ApproveAllPending(g.Id)"
                title="Aprovar todas as solicitações pendentes">
            <i class="fas fa-check-double"></i> Aprovar @pendingCount
        </button>
    </div>
}
```

2. No code-behind (`Index.razor` `@code`), adicionar o metodo `ApproveAllPending(int groupId)`:

```csharp
private async Task ApproveAllPending(int groupId)
{
    await using var db = await DbFactory.CreateDbContextAsync();
    var pendingRequests = await db.GroupJoinRequests
        .Where(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending)
        .Include(r => r.User)
        .ToListAsync();

    var group = await db.Groups.FindAsync(groupId);
    if (group == null) return;

    foreach (var req in pendingRequests)
    {
        var alreadyMember = await db.GroupMembers
            .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
        if (!alreadyMember)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = req.GroupId, UserId = req.UserId,
                Role = GroupMemberRole.Member, CreatedAt = DateTime.UtcNow,
            });
        }
        req.Status = JoinRequestStatus.Approved;
        req.RespondedAt = DateTime.UtcNow;
        req.RespondedByUserId = currentUserId;

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = null, RecipientUserId = req.UserId,
            RecipientDisplayName = req.User?.UserName,
            Subject = $"Voce foi aprovado em \"{group.Name}\"",
            Body = $"Sua solicitacao para entrar no grupo **{group.Name}** foi aprovada!",
            CreatedAt = DateTime.UtcNow,
        });
    }
    await db.SaveChangesAsync();
    await LoadGroups(); // recarrega a lista
}
```

3. CSS para `.group-card-pending-actions` em `Index.razor.css`:
   - Posicao absoluta no canto inferior direito do card
   - Botao compacto com icone + numero
   - Prevenir propagacao do click (nao navegar para o grupo)

**Arquivos**:
- `Pages/Groups/Index.razor` (template + code-behind)
- `Pages/Groups/Index.razor.css` (estilo do botao)
**Referencia**: `Pages/Groups/Detail.razor.cs:245-296` (logica de `ApproveSelected` — copiar padrao)

### Fase 7 — Reduzir !important em site.css

**Analise do Senior — 11 ocorrencias em site.css**:

| Linha | Contexto | Veredicto |
|-------|----------|-----------|
| 882 | `.is-hidden { display: none !important }` | **LEGITIMO** — utility class precisa sobrescrever qualquer display |
| 1254 | `@media (prefers-reduced-motion) animation-duration` | **LEGITIMO** — acessibilidade, padrao W3C |
| 1255 | `@media (prefers-reduced-motion) animation-iteration-count` | **LEGITIMO** — acessibilidade |
| 1256 | `@media (prefers-reduced-motion) transition-duration` | **LEGITIMO** — acessibilidade |
| 6462 | `.pac-container { z-index: 9999 }` | **LEGITIMO** — Google Maps dropdown precisa ficar acima de modals |
| 6547 | `input:-webkit-autofill box-shadow` | **LEGITIMO** — unico jeito de mudar cor de autofill no Chrome |
| 6548 | `input:-webkit-autofill box-shadow (vendor prefix)` | **LEGITIMO** — idem |
| 6549 | `input:-webkit-autofill -webkit-text-fill-color` | **LEGITIMO** — idem |
| 6550 | `input:-webkit-autofill color` | **LEGITIMO** — idem |
| 6553 | `input:-webkit-autofill outline: none` | **LEGITIMO** — idem |

**Conclusao**: TODOS os 11 `!important` em site.css sao **LEGITIMOS**. Nao devem ser removidos.
- `.is-hidden` — padrao utility-first (Bootstrap, Tailwind usam o mesmo)
- `prefers-reduced-motion` — padrao de acessibilidade W3C
- `.pac-container` — necessario para sobrescrever z-index inline do Google Maps JS
- `input:-webkit-autofill` — UNICO jeito de corrigir o fundo azul/amarelo do Chrome autofill

**Acao**: Documentar no commit que todos foram auditados e sao legitimos. NAO remover nenhum.

### Fase 8 — Melhorias UX vindas de testes do app (NOVA FASE PADRAO)

**Contexto**: Durante testes manuais do app apos conclusao das fases 1-7, foram identificadas oportunidades de melhoria UX nos cards de esporte.

**Implementacao**:

1. **Adicionar novos esportes visualmente** (Volleyball, Beach Tennis, Footvolley, Chess):
   - `Enums/Sport.cs`: Adicionar valores Volleyball=3, BeachTennis=4, Footvolley=5, Chess=6
   - `Shared/Components/SportCard.razor`: Adicionar switch expressions para icons, names, CSS classes dos novos esportes
   - `Pages/Index.razor`: Adicionar SportCards para novos esportes, opcoes no filtro dropdown, hrefs, e queries de match count em `LoadMatchCountsAsync`

2. **Temas visuais consistentes** (cores, gradientes, marcas d'água):
   - `wwwroot/css/site.css`: Adicionar CSS variables para novos esportes (accent, accent-deep, text, text-pale, bg-dark, bg-deepest, border)
   - `Shared/Components/SportCard.razor.css`: Adicionar estilos header (gradient, border, box-shadow), body (gradient, border, watermark), label color, CTA color, event icon color para cada esporte

3. **Badge "Em breve"**:
   - `Shared/Components/SportCard.razor`: Adicionar parametro `ComingSoon` e badge no header
   - `Shared/Components/SportCard.razor.css`: Estilizar badge com fundo translúcido e borda branca; mudar header para `justify-content: space-between`
   - `Pages/Index.razor`: Adicionar `ComingSoon="true"` para todos exceto Futsal

4. **Correcoes de cor**:
   - Bordas do Volleyball e BeachTennis: usar `--border` (tom compatível com card) em vez de `--accent`
   - CTA do Volleyball e BeachTennis: usar tons claros do tema (#93c5fd azul, #99f6e4 teal)
   - Labels: usar `--text-pale` (tons muito claros) para todos os esportes

5. **Marca d'água**:
   - Volleyball: mudar de pessoa na praia para bola de vôlei (🏐)
   - Footvolley: usar bola de futebol (⚽)

6. **Titulo do Futsal**:
   - Mudar de "Futsal" para "Futebol/Futsal"

7. **Seta no CTA**:
   - Adicionar "→" ao CtaText dos novos esportes via texto (padrao existente: "Ver partidas →", "Ver torneios →")

**Arquivos**:
- `Enums/Sport.cs`
- `Shared/Components/SportCard.razor`
- `Shared/Components/SportCard.razor.css`
- `Pages/Index.razor`
- `wwwroot/css/site.css`

**NOTA**: A partir deste ciclo, todos os ciclos terão uma fase dedicada a "Melhorias UX vindas de testes do app" para capturar correções e refinamentos identificados durante testes manuais. Esta fase deve ser documentada no PR com solicitação de documentação formal da expectativa deste tipo de fase.

---

## Ciclo 16 -- Mobile UX Fix (PRIORIDADE CRÍTICA)

**Branch**: `fix/ciclo16-mobile-ux`
**1 commit por fase** dentro da branch. **1 PR** no final.
Validar cada fase com `dotnet build` + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"`.

### Contexto e Importância

**Público predominante mobile**: Este projeto terá a maioria dos usuários acessando via dispositivos móveis. A experiência mobile não é apenas uma melhoria UX, mas um requisito crítico de negócio.

**Problema identificado**: O layout mobile está totalmente quebrado. O comportamento foi replicado no Chrome DevTools (F12) ao alterar para mobile e diminuir a largura da tela. O layout estava ok em telas maiores, mas conforme a largura diminui, ele começa a se comportar da mesma forma que no celular pessoal.

**Objetivo**: Corrigir todos os problemas de layout mobile para garantir uma experiência consistente e profissional em dispositivos móveis, que é o principal ponto de acesso dos usuários.

**Prioridade**: CRÍTICA - Este ciclo deve receber máxima atenção e recursos, pois impacta diretamente a experiência do público principal do produto.

### Fase 1 — Auditoria de problemas mobile

**Ação**: Identificar todos os componentes e páginas com problemas de layout mobile.

**Metodologia**:
1. Usar Chrome DevTools (F12) em modo mobile (iPhone SE, iPhone 12 Pro, iPad)
2. Navegar por todas as páginas principais:
   - `/eventos` (home)
   - `/grupos`
   - `/grupo/{Id}` (detail)
   - `/futsal`
   - `/poker`
   - `/meus-eventos`
   - `/profile`
   - `/admin`
3. Documentar cada problema encontrado com:
   - Página/componente
   - Largura de tela onde o problema ocorre
   - Descrição do comportamento incorreto
   - Screenshot (opcional)

**Arquivo de documentação**: `docs/ciclo16-mobile-auditoria.md` (criado para listar todos os problemas)

**Status**: ✅ Concluído
- Auditoria inicial baseada em 6 screenshots da página /eventos
- Identificados problemas gerais: sidebar, grid, overflow, transições
- Documentação criada e mantida atualizada com cada correção

### Fase 2 — Correção de problemas mobile (prioridade alta)

**Ação**: Corrigir os problemas identificados na Fase 1, começando pelos mais críticos.

**Abordagem**:
1. Revisar breakpoints existentes em `site.css` e arquivos scoped
2. Adicionar/ajustar media queries para telas mobile
3. Corrigir overflow, wrapping, spacing e sizing
4. Testar cada correção em múltiplos tamanhos de tela

**Arquivos a modificar** (dependente da auditoria):
- `wwwroot/css/site.css` (breakpoints globais)
- `Pages/Index.razor.css` (home)
- `Pages/Groups/Index.razor.css`
- `Pages/Groups/Detail.razor.css`
- `Pages/Futsal/Index.razor.css`
- `Pages/Poker/Index.razor.css`
- Outros arquivos CSS conforme necessário

**Status**: ✅ Concluído (todas as páginas principais)
- ✅ `/eventos` (Index.razor.css + site.css) - Commit 46b7be7
- ✅ `/grupos` (events.css) - Commit 077aea9
- ✅ `/grupo/{Id}` (events.css) - Commit aa41b06
- ✅ `/futsal` (Futsal/Index.razor.css) - Commit 7cd83c4
- ✅ `/poker` (Poker/Index.razor.css) - Commit a1022db
- ✅ `/meus-eventos` (MyEvents/Index.razor.css) - Commit 94a11c7
- ✅ `/profile` (Profile.razor.css + componentes) - Commit 08e7e57
- ✅ `/admin` (Admin.razor.css) - Commit 9d81c28

**Correções aplicadas**:
- Adicionado breakpoint <375px para telas muito pequenas em todos os arquivos
- Expandido breakpoints existentes (<640px, <760px, <560px, <480px)
- Reduzido padding, fontes e heights para economizar espaço
- Ajustado layouts para flex-direction: column em mobile
- Ajustado grids para grid-template-columns: 1fr em mobile
- Centralizado botões e ações em mobile
- Ajustado avatares, badges e ícones para mobile pequeno

### Fase 3 — Testes mobile cross-browser

**Ação**: Validar correções em múltiplos browsers e dispositivos.

**Metodologia**:
1. Testar em Chrome, Safari, Firefox (mobile)
2. Validar em diferentes tamanhos de tela (320px, 375px, 414px, 768px)
3. Verificar orientação portrait e landscape
4. Testar scroll, zoom e interações touch

**Status**: ⏳ Em andamento (instruções documentadas, aplicação rodando)

**Aplicação rodando em**: http://localhost:5000

**Instruções de teste documentadas em**: `docs/ciclo16-mobile-auditoria.md`

**Checklist por página** (para testes manuais):
- `/eventos` - Sidebar, sports tabs, event cards, city selector
- `/grupos` - Grid de grupos, cards, botões
- `/grupo/{Id}` - Header, admin bar, events tabs, pending requests
- `/futsal` - Event cards, time badges, meta chips
- `/poker` - Event cards, dropdown menu, type badges
- `/meus-eventos` - Header, view tabs, create buttons, event cards
- `/profile` - Profile page, avatar, edit form, chat thread
- `/admin` - Settings card, security panels, KPI grid, nav

**Browsers para testar**: Chrome, Firefox, Edge, Safari (se disponível)

**Tamanhos de tela**: 320px, 375px, 414px, 768px, 1024px

### Fase 4 — Melhorias UX vindas de testes do app

**Contexto**: Durante testes manuais do app após conclusão das fases 1-3, capturar correções e refinamentos identificados.

**Implementação**: Documentar e implementar melhorias adicionais identificadas durante testes mobile.

---

## Ciclo 17 -- Login Google (OAuth) + Fix Caracteres Especiais + Consolidar Menu Mobile

**Branch sugerida**: `feat/ciclo17-google-login`
**1 commit por fase** dentro da branch. **1 PR** no final.
Validar cada fase com `dotnet build` + `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"`.

Ordem sugerida: Fase 1 (mobile nav, rapida) → Fase 2 (mojibake, rapida) → Fases 3-6 (Google OAuth, foco principal).

### Fase 1 — Consolidar CSS do menu mobile (resolve Fase 15 do Ciclo 16)

**Problema**: link de pagamentos nao alinha com os demais no menu mobile. Causa raiz: CSS do `oldsite-top-nav` duplicado entre global e scoped (mesmo padrao do bug de background dos Ciclos 12/13).

**Procedimento** (aplicar a solucao provada no Ciclo 13):
1. Mover as regras mobile do nav (`@media (max-width: 700px)` que estilizam `.oldsite-top-nav`) de `Shared/Components/MainLayout.razor.css` para `wwwroot/css/site.css`
2. Remover essas regras do `MainLayout.razor.css` (elimina competicao scoped vs global)
3. Colapsar os 2 blocos base `body .oldsite-top-nav {` de `site.css` (linhas ~3543 e ~5332) em um so
4. Usar UM unico seletor de link (`body .oldsite-top-nav a` OU `.oldsite-top-nav-link`, nao ambos) para que o link de pagamentos herde identico aos demais
5. `dotnet clean && dotnet build` + Ctrl+F5 antes de validar (regra 21)

**Arquivos**: `wwwroot/css/site.css`, `Shared/Components/MainLayout.razor.css`
**Nota**: nao ha seletores residuais `payments-link`/`a[href="/payments"]` (tentativas ja revertidas).

### Fase 2 — Corrigir caracteres especiais (mojibake) — LOCAIS EXATOS

**Problema**: texto com dupla codificacao UTF-8 (bytes UTF-8 lidos como Latin-1 e re-salvos). Ex: `ConfiguraÃ§Ãµes` deveria ser `Configurações`, `â€”` deveria ser `—`, `nÃ£o` → `não`, `EndereÃ§o` → `Endereço`, `Â·` → `·`, `â”€` → `─`.

**4 arquivos afetados** (o Pleno deve reescrever essas strings em UTF-8 correto):

1. **`Pages/Groups/Features.razor`** (linhas ~21, 49, 71):
   - `ConfiguraÃ§Ãµes` → `Configurações`
   - `â€”` → `—` | `Â·` → `·`
   - `nÃ£o autorizado` → `não autorizado`

2. **`Pages/Poker/Edit.razor`** (linhas ~32, 79, 81, 101, 110, 121, 125, 155, 175, 182):
   - `nÃ£o encontrado` → `não encontrado`
   - `EndereÃ§o` → `Endereço` | `nÃºmero` → `número`
   - `HorÃ¡rio` → `Horário` | `inÃ­cio` → `início`
   - `â€”` → `—` | `â”€` (box drawing nos comentarios `@* ─── *@`) → `─`
   - `mÃ¡ximo` → `máximo` | `PreÃ§os` → `Preços`

3. **`Pages/Payment/Payment.razor.cs`** (linhas ~195, 249, 260):
   - `mÃ­nimo` → `mínimo`
   - `EndereÃ§o/invoice` → `Endereço/invoice` (2x)

4. **`Shared/Components/UserSummaryCard.razor`** (linhas ~15, 24, 59):
   - `UsuÃ¡rio` → `Usuário`
   - `NÃ£o informada` → `Não informada` | `NÃ£o informado` → `Não informado`

**Procedimento seguro**: editar cada string manualmente (NAO usar sed em massa — risco de corromper mais). Salvar SEMPRE em UTF-8 sem BOM (regra 18/19). Verificar apos: `grep -rlP 'Ã©|Ã£|Ã§|Ã¡|Ã³|Ã­|â€|â”€' --include="*.cs" --include="*.razor" .` deve retornar 0 arquivos.

**Nota Senior**: esses textos deveriam idealmente estar nos arquivos de UiText (i18n) e nao hardcoded no markup. Migrar para UiText fica como oportunidade futura (nao obrigatorio neste ciclo).

### Fase 3 — Google OAuth: pacote + configuracao (backend)

**Contexto**: o projeto usa ASP.NET Identity com paginas Razor em `/Identity/Account/*` e cookie auth. Google login usa o fluxo de external login padrao do Identity.

1. Adicionar pacote: `dotnet add package Microsoft.AspNetCore.Authentication.Google`
2. Em `Program.cs`, apos `AddIdentity`/`AddAuthentication`, registrar o provider:
   ```csharp
   builder.Services.AddAuthentication()
       .AddGoogle(options =>
       {
           options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
           options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
           // callback padrao: /signin-google
       });
   ```
3. Adicionar secao em `appsettings.json` (valores VAZIOS — segredos reais via user-secrets/env, NUNCA commitados):
   ```json
   "Authentication": { "Google": { "ClientId": "", "ClientSecret": "" } }
   ```
4. So habilitar o botao na UI quando `ClientId` estiver preenchido (evita erro em dev sem credenciais).

### Fase 4 — Google OAuth: paginas de external login (UI) + comportamento CRIAR-OU-VINCULAR

Verificar se existem as paginas scaffolded do Identity. Se nao, criar:
- `Areas/Identity/Pages/Account/ExternalLogin.cshtml` (+ `.cs`): recebe o callback.
- Em `Login.cshtml`: renderizar os botoes de provider externo (`Model.ExternalLogins`) — botao "Entrar com Google".

**Comportamento exigido pelo Robson (criar-ou-vincular por email)** no `ExternalLogin.OnGetCallbackAsync`:
1. `signInManager.ExternalLoginSignInAsync(...)` — se ja existe o login externo vinculado, **loga direto**.
2. Se nao ha login externo vinculado, pegar o `email` do claim do Google e `userManager.FindByEmailAsync(email)`:
   - **Se JA existe usuario com esse email** → `userManager.AddLoginAsync(existingUser, info)` (vincula o Google a conta existente) e loga. Nao criar conta duplicada.
   - **Se NAO existe** → criar novo `ApplicationUser { Email, UserName = email, EmailConfirmed = true }` + `AddLoginAsync` + login.
3. Apos criar, redirecionar para completar perfil se campos obrigatorios do dominio estiverem vazios (`BirthDate`, cidade, etc.).

Pontos de atencao:
- `ApplicationUser` tem campos custom — no primeiro login Google ficam nulos; garantir que o fluxo nao quebra (opcionais) e completar depois.
- Email do Google ja vem verificado → `EmailConfirmed = true` (nao exigir novo email de confirmacao para contas Google).

### Fase 5 — Emails reais + confirmacao de email (para cadastro tradicional)

**Contexto**: hoje o cadastro por email/senha (nao-Google) provavelmente nao envia email real de confirmacao. O Robson quer emails reais + confirmacao funcionando.

1. Implementar um `IEmailSender` real (SMTP ou provedor transacional — ex: SendGrid/Mailgun/SMTP do dominio). Config via `appsettings` + segredos fora do git (mesma abordagem do OAuth).
2. Ativar `options.SignIn.RequireConfirmedAccount = true` no Identity (verificar valor atual em `Program.cs`).
3. Fluxo de registro: enviar link de confirmacao (`/Identity/Account/ConfirmEmail`), bloquear login ate confirmar.
4. Contas via Google entram com `EmailConfirmed = true` e nao passam por esse fluxo (ja verificado).
5. Verificar/ajustar as paginas `Register.cshtml`, `RegisterConfirmation.cshtml`, `ConfirmEmail.cshtml`, `ResendEmailConfirmation.cshtml`.

**Acao do Robson**: fornecer as credenciais SMTP/API do provedor de email (host, porta, usuario, senha/API key) — guardadas fora do git.

### Fase 6 — Testes dos fluxos de login

- `AddGoogle` so registrado quando ha `ClientId` (config guard).
- Login expondo `ExternalLogins` renderiza o botao Google quando configurado.
- `ExternalLogin` callback: (a) email existente → vincula sem criar duplicata; (b) email novo → cria com `EmailConfirmed = true`.
- Cadastro tradicional exige confirmacao de email antes do login (`RequireConfirmedAccount`).
- Usar `IntegrationTestWebAppFactory` seguindo o padrao dos testes existentes.

### Fase 7 — Melhorias UX vindas de testes do app (FASE PADRAO)

Reservada para requisitos/bugs/melhorias que o Robson levantar testando o app durante o ciclo. Documentar cada item aqui antes de implementar. (Ver regra 22.)

---

## O que o Robson precisa gerar para o Google OAuth (acao do usuario)

Antes/durante o Ciclo 17, para o login funcionar em prod e dev:
1. Google Cloud Console → **APIs & Services → Credentials → Create OAuth client ID** (tipo: Web application).
2. **Authorized redirect URIs**: adicionar `https://<seu-dominio-prod>/signin-google` e `https://localhost:xxxx/signin-google` (porta do dev).
3. Configurar a **OAuth consent screen** (nome do app, email de suporte, dominios autorizados).
4. Copiar **Client ID** e **Client Secret**.
5. Guardar os segredos FORA do git:
   - Dev: `dotnet user-secrets set "Authentication:Google:ClientId" "..."` e idem para o secret
   - Prod: variaveis de ambiente `Authentication__Google__ClientId` / `Authentication__Google__ClientSecret`

Para email real + confirmacao (Fase 5): fornecer credenciais do provedor de email (SMTP host/porta/usuario/senha OU API key de SendGrid/Mailgun), tambem guardadas fora do git (user-secrets em dev, env vars em prod).

---

## Ciclo 18 (FUTURO) -- Mobile UX Critico: Fluxos Principais

Auditar e garantir 100% no mobile os fluxos que os stakeholders mais usam:
entrar em grupo (invite code) → ver eventos → confirmar presenca → pagar (Pix/BTC) → ver comprovante.
Foco: alvos de toque >=44px, sem scroll horizontal, formularios/modais utilizaveis em 375px/414px, header consistente.
Detalhar em fases proprias quando iniciarmos o ciclo.

## Ciclo 19 (FUTURO) -- Refatoracao Completa: TDD + SOLID (inclui fase CSS Web/Mobile)

Refatoracao ampla aplicando TDD e SOLID (+ padroes pertinentes de Blazor Server: separacao de logica em services testaveis, `IDbContextFactory`, componentizacao, evitar logica no markup, gestao de circuito/estado).
Sera um ciclo grande — provavelmente subdividido. Detalhar escopo e ordem quando priorizado.

### Fase CSS: Refatoracao de Boas Praticas e Isolamento Web/Mobile

**Motivacao**: Bug recorrente onde afinamento de layout (width 57%) aplicado sem media query quebrou o mobile (campos esmagados). Necessario repassar TODO o CSS do projeto garantindo isolamento Web/Mobile e boas praticas.

**Branch sugerida**: `refactor/css-isolation`

**Metodologias CSS como norte (sugestoes de nivel Pleno -- aguardando revisao do Senior)** (equivalentes a SOLID/TDD para codigo funcional):

1. **ITCSS (Inverted Triangle CSS)** -- hierarquia de especificidade em camadas:
   - **Settings layer**: CSS custom properties (vars do `:root`) -- design tokens
   - **Tools layer**: mixins/functions (nao aplicavel em CSS puro, mas conceitual)
   - **Generic layer**: reset/normalize (ja em site.css)
   - **Elements layer**: estilos de elementos base (body, a, input)
   - **Objects layer**: classes utilitarias (.btn, .input, .form-group)
   - **Components layer**: scoped .razor.css (estilos especificos de cada pagina)
   - **Utilities layer**: overrides pontuais (.text-center, .mt-1)
   - Regra: camada superior NUNCA pode depender de camada inferior

2. **BEM (Block Element Modifier)** -- convencao de nomenclatura para scoped CSS:
   - `.block` -- componente independente (ex: `.upload-section`)
   - `.block__element` -- parte do bloco (ex: `.upload-section__preview`)
   - `.block--modifier` -- variacao (ex: `.upload-section--compact`)
   - Elimina ambiguidade de seletor e reduz conflitos de especificidade

3. **Mobile-First obrigatorio** -- toda regra base serve mobile, desktop e enhancement:
   - Base (sem media query) = mobile
   - `@media (min-width: 769px)` = desktop enhancements
   - `@media (max-width: 768px)` = apenas para overrides que diferem do base mobile
   - NUNCA aplicar regra desktop no base (causa do bug recorrente)

4. **CSS Layers (`@layer`)** -- isolamento explicito de cascade:
   - `@layer reset, tokens, base, objects, components, utilities;`
   - Garante que scoped CSS nao override global indevidamente
   - Reduz necessidade de `!important` e especificidade alta

5. **Design Tokens como single source of truth**:
   - Toda cor, espacamento, border-radius, shadow, transition = var do `:root`
   - NUNCA hardcodar valores em scoped CSS
   - Vars semanticas > vars literais (ex: `--ci-accent` > `#4f9cf8`)
   - Breakpoints como vars: `--bp-mobile: 768px` (quando CSS.supports)

6. **Single Responsibility por arquivo scoped**:
   - Cada `.razor.css` estiliza APENAS o componente da pagina
   - NUNCA estilizar elementos de outras paginas via scoped
   - `::deep` apenas para componentes filhos renderizados pelo Blazor
   - Estilos compartilhados entre paginas = global (events.css/site.css)

7. **Testes Visuais (TDD para CSS)**:
   - Playwright: snapshot por pagina em 2 viewports (375px e 1280px)
   - Testar: overflow horizontal, alinhamento, contraste, alvos de toque >=44px
   - Rodar antes e apos cada fase para detectar regressoes
   - CI gate: falhar se snapshot diff > threshold

**Fases de Execucao (sugestoes de nivel Pleno -- aguardando revisao do Senior)**:
1. **Auditoria**: Mapear todos os arquivos CSS (globais e scoped), identificar conflitos de especificidade, overrides desnecessarios, regras de layout sem media query, breakpoints inconsistentes (700px vs 768px), CSS duplicado entre arquivos
2. **Isolamento Web/Mobile**: Aplicar mobile-first em TODO o CSS existente. Toda regra de layout sem media query deve ser analisada: se e desktop-only, envolver em `@media (min-width: 769px)`. Se e mobile-only, envolver em `@media (max-width: 768px)`. Se e neutro, manter como base. Regra 23 aplicada sistematicamente
3. **Consolidacao ITCSS**: Organizar CSS global em camadas ITCSS. Eliminar duplicacao entre `site.css`, `events.css` e scoped `.razor.css`. Definir fronteira clara: global = objects + base, scoped = components. Resolver fragmentacao historica (ex: `oldsite-top-nav` split entre global/scoped -- causa raiz do bug do Ciclo 16 Fase 15)
4. **BEM em scoped CSS**: Renomear classes scoped para convencao BEM onde fizer sentido. Eliminar seletores fragoris (`body .oldsite-top-nav > a` em scoped). Padronizar `::deep` apenas para filhos Blazor
5. **Design Tokens**: Auditar todas as vars do `:root`, eliminar vars mortas, consolidar vars semanticas. Garantir 0 hardcoded hex/rgba em scoped. Padronizar breakpoints (768px unico)
6. **TDD Visual**: Criar testes Playwright para desktop (1280px) e mobile (375px) de cada pagina. Snapshot testing para detectar regressoes de layout. Validar: sem overflow horizontal, alvos >=44px, contraste AA
7. **CSS Layers (opcional)**: Se Suporte Blazor permitir, adicionar `@layer` para isolamento explicito de cascade. Reduzir `!important` restantes

**Premissas**:
- Nunca modificar testes existentes
- Commits pequenos e frequentes (1 por fase)
- Branch separada: `refactor/css-isolation`
- Testar em ambas viewports (desktop 1280px e mobile 375px) apos cada mudanca
- `dotnet clean && dotnet build` + Ctrl+F5 apos mudancas CSS (regra 21)
- 0 hardcoded hex/rgba em scoped CSS (regra 2)
- 0 `!important` desnecessario (regra 1)

---

## Ciclo 20 (POSTERGADO) -- Integracao WhatsApp Real + Baseline Operacional por Gateway

**POSTERGADO** a pedido do Robson (aguardando ideias/sugestoes de outro dev). Conteudo mantido abaixo para referencia; revisar quando reativado.

### Fase 1 — Configuracao WhatsApp: appsettings + opt-in

**O que existe hoje**:
- `WhatsAppNotificationService.cs` (55L) — pronto, le `WhatsApp:ApiKey` e `WhatsApp:ApiUrl` da config
- `ApplicationUser.WhatsAppNumber` (string?) e `ApplicationUser.WhatsAppOptIn` (bool) — campos no modelo
- Botoes "Cobrar via WhatsApp" nas paginas de pagamentos → abrem `wa.me` link (URL no browser, nao API)
- Compartilhar escalacao no WhatsApp → `wa.me` link

**O que falta**:
1. Adicionar secao `WhatsApp` no `appsettings.json` e `appsettings.Development.json`:
   ```json
   "WhatsApp": {
     "Enabled": false,
     "ApiUrl": "",
     "ApiKey": "",
     "Provider": "evolution-api"
   }
   ```

2. Criar tela de opt-in no perfil (`Pages/Profile.razor`):
   - Campo "Numero WhatsApp" (input tel com mascara +55)
   - Toggle "Receber notificacoes via WhatsApp"
   - Salvar em `ApplicationUser.WhatsAppNumber` e `WhatsAppOptIn`

3. Registrar `WhatsAppNotificationService` no DI (`Program.cs`):
   ```csharp
   builder.Services.AddHttpClient<WhatsAppNotificationService>();
   ```

**Arquivos**:
- `appsettings.json`, `appsettings.Development.json`
- `Pages/Profile.razor` (adicionar secao WhatsApp)
- `Program.cs` (registrar no DI)
- `Services/Notification/WhatsAppNotificationService.cs` (adicionar check de `Enabled`)

### Fase 2 — Envio real de notificacoes WhatsApp

**Substituir links `wa.me` por chamadas reais via API**:

1. Em `Pages/Groups/Payments.razor.cs`, metodo `OpenWhatsAppDelinquency`:
   - Se `WhatsApp:Enabled == true` e usuario tem `WhatsAppOptIn == true`:
     → Chamar `WhatsAppNotificationService.SendPaymentReminderAsync()`
   - Se nao: manter comportamento atual (abrir `wa.me` link)

2. Em `Pages/Futsal/Escalacao.razor.cs`, metodo `ShareOnWhatsApp`:
   - Se `WhatsApp:Enabled == true`:
     → Enviar para todos os jogadores confirmados com opt-in
   - Se nao: manter `wa.me` link

3. Adicionar notificacoes automaticas (novos triggers):
   - Evento criado → notificar membros do grupo com opt-in
   - Pagamento confirmado → notificar jogador
   - Solicitacao aprovada → notificar solicitante

**Arquivos**:
- `Pages/Groups/Payments.razor.cs`
- `Pages/Futsal/Escalacao.razor.cs`
- `Services/Notification/WhatsAppNotificationService.cs` (adicionar metodos de template)

### Fase 3 — Admin: toggle WhatsApp + log de envios

1. Em `Pages/Admin/AdminSettings.razor` (ou criar se nao existir):
   - Toggle para habilitar/desabilitar WhatsApp
   - Campo para ApiUrl e ApiKey (masked)
   - Botao "Testar envio" → envia mensagem de teste para o admin

2. Audit trail:
   - Adicionar `AuditEvents.WhatsAppNotificationSent` e `AuditEvents.WhatsAppNotificationFailed`
   - Logar cada envio no `LogService` com entityType=Notification

**Arquivos**:
- `Pages/Admin/AdminSettings.razor` (ou secao em AdminGateways)
- `Services/Core/AuditEvents.cs` (novos eventos)
- `Services/Notification/WhatsAppNotificationService.cs` (adicionar logging)

### Fase 4 — Baseline Operacional por Gateway

**O que e isso**: Um painel que mostra metricas semanais por gateway de pagamento. Serve para detectar degradacao (ex: EfiBank comecar a falhar mais) antes que vire incidente.

**O que ja existe**:
- `AdminPayments.razor.cs` L459-492 — `gatewayTelemetry` com Pending/StalePending/PaidTotal por gateway
- `PendingWebhooksAlertService` — alerta quando webhooks ficam pendentes >1h
- `DashboardMetricsService` — conta users e quote queries (basico)

**O que criar**:

1. Criar `Services/Payment/GatewayBaselineService.cs`:
   ```csharp
   public class GatewayBaselineSnapshot
   {
       public string GatewayName { get; set; }
       public int TransactionsLast7Days { get; set; }
       public int TransactionsLast30Days { get; set; }
       public int SuccessCount { get; set; }
       public int FailureCount { get; set; }
       public int PendingCount { get; set; }
       public decimal SuccessRate { get; set; }      // SuccessCount / Total * 100
       public TimeSpan AverageConfirmationTime { get; set; }  // tempo medio entre criacao e pagamento
       public DateTime? LastSuccessfulPayment { get; set; }
       public string TrendLabel { get; set; }        // "estavel", "melhorando", "degradando"
   }
   ```

2. Implementar consulta que agrupa por `PaymentGatewayName` e calcula:
   - Volume (7d e 30d) via `ConfirmedAt` timestamp
   - Taxa de sucesso: `Paid / (Paid + Failed + Pending) * 100`
   - Tempo medio de confirmacao: `AVG(PaidAt - ConfirmedAt)` para pagamentos Paid
   - Trend: comparar taxa de sucesso semana atual vs semana anterior
   - Ultimo pagamento bem-sucedido: `MAX(PaidAt) WHERE Status=Paid`

3. Criar componente `Shared/Components/GatewayBaselinePanel.razor`:
   - Tabela com colunas: Gateway | Volume 7d | Volume 30d | Taxa Sucesso | Tempo Medio | Ultimo OK | Trend
   - Cores: verde (>95% sucesso), amarelo (80-95%), vermelho (<80%)
   - Trend com seta: ↑ melhorando, → estavel, ↓ degradando

4. Adicionar painel em `Pages/Admin/AdminPayments.razor`:
   - Nova secao "Baseline por Gateway" abaixo da telemetria existente
   - Carregar dados do `GatewayBaselineService` no `OnInitializedAsync`

**Arquivos**:
- `Services/Payment/GatewayBaselineService.cs` (novo)
- `Shared/Components/GatewayBaselinePanel.razor` + `.razor.css` (novo)
- `Pages/Admin/AdminPayments.razor` + `.razor.cs` (adicionar secao)
- `Program.cs` (registrar `GatewayBaselineService` como Scoped)

### Fase 5 — Testes do GatewayBaselineService

```csharp
// Confirmai.Tests/GatewayBaselineServiceTests.cs
[Fact] GetBaseline_WithPayments_CalculatesRates()
[Fact] GetBaseline_NoPayments_ReturnsEmptyList()
[Fact] GetBaseline_MultipleGateways_ReturnsAll()
[Fact] GetBaseline_TrendCalculation_DetectsDegradation()
```

Usar `TestDataFactory.CreateDbContext()` com pagamentos em diferentes datas/estados.

---

## Mapeamento de Testes -- Gaps e Oportunidades

### Estado atual
- **1.674 testes** em **192 arquivos** de teste
- **Cobertura**: 9.9% (11.733/118.427 linhas)
- **Meta**: 15%+

### Services SEM cobertura de testes (5/87)

| Service | Dominio | Prioridade | Justificativa |
|---------|---------|------------|---------------|
| `PingController` | Controllers | **P1** | Novo no C14, endpoint critico de keep-alive, facil de testar |
| `CityService` | Utility | P2 | CRUD de cidades, depende de DB |
| `GroupMetricsService` | Groups | P2 | Calculos de metricas por grupo |
| `LocationService` | Utility | P3 | Servico auxiliar de localizacao |
| `WhatsAppNotificationService` | Notification | P3 | Depende de API externa |

### Testes recomendados para o Pleno

**P1 — PingController (novo, facil, alto valor)**:
```csharp
// PingControllerTests.cs
[Fact] Ping_Authorized_Returns200()    // GET /api/ping com user autenticado → 200
[Fact] Ping_Anonymous_Returns401()     // GET /api/ping sem auth → 401/redirect
```
Usar `IntegrationTestWebAppFactory` + `HttpClient` com/sem auth.

**P2 — GroupMetricsService**:
```csharp
[Fact] GetMetrics_WithEvents_CalculatesCorrectly()
[Fact] GetMetrics_EmptyGroup_ReturnsZeros()
```

**P2 — CityService**:
```csharp
[Fact] GetCities_ByState_FiltersCorrectly()
[Fact] GetCity_ById_ReturnsExpected()
```

### O que NAO precisa de teste (baixo ROI)
- Componentes Blazor (sub-componentes de UI) — cobertura minima, alto custo
- Models/DTOs sem logica — triviais
- `LocationService` e `WhatsAppNotificationService` — dependencias externas

---

## Ciclo 14 -- Refresh Token / Sessao Persistente (CONCLUIDO)

> Ciclo separado por ser mudanca arquitetural que requer orientacao detalhada do Senior.

**Branch**: `feat/ciclo14-refresh-token`
**1 commit por fase** dentro da branch. **1 PR** no final.

### Resultado
- Fase 1 executada: 7 files changed, +29 -6
- Fase 2 validada manualmente: `GET /api/ping` 200 OK, cookie renovado, keep-alive a cada 15min
- 1.674/1.674 testes passando, 0 erros de build

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
