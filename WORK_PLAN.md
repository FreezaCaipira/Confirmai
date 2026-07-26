# Plano de Trabalho - Confirmai

> Atualizado em 15/07/2026 | Base: `main` (pos-Ciclo 17) | Refatoracao CSS CONCLUIDA
> 1.700/1.700 testes passando | 0 erros de build | 0 AppDbContext direto | 0 services sem teste | 0 mojibake
> Ciclo 15 (testes+UX+!important), Ciclo 16 (mobile UX) e Ciclo 17 (Login Google+email real+mojibake+menu mobile) -- CONCLUIDOS e revisados
> Proximo: **Ciclo TDD + SOLID + CSS Refatoration** (detalhado pelo Senior) -- ver "Ciclo 18: TDD + SOLID + CSS Refatoration"
> Postergado: Pagamento Real + Taxa de Servico (implementado, aguardando review Senior quando tokens resetarem) | WhatsApp real + Baseline por gateway (aguardando ideias de outro dev)

Este documento e o unico plano de trabalho ativo. Ele e atualizado a cada ciclo pelo Senior e executado pelo Pleno.

---

## VARREDURA SENIOR COMPLETA (14/06/2026) -- backlog priorizado P0-P3

Auditoria dos 12 eixos pedidos pelo Robson sobre a `main` (HEAD `9edd89b`). Panorama: 665 arquivos `.cs`, 144 `.razor` (99 com code-behind), 97 services, 88 migrations, 218 arquivos de teste, 102 CSS. Build do app **0 warnings**; **1889** testes verdes / 1913 (24 falhas sao `ProgramConfigurationTests` sem Postgres -- ambiente, nao regressao).

**Pontos fortes ja consolidados (creditos ao Pleno)**: webhooks autenticados (AbacatePay HMAC+secret timing-safe, BTCPay secret timing-safe, EfiBank mTLS por client-cert); authz admin completa (17/17 paginas guardadas + teste de convencao `AdminAuthorizationConventionsTests`); 66 `HasIndex` incluindo `EventConfirmations(EventId, UserId)` e `PixTxId` (**a "decisao #7" do Pleno ja esta feita**); `IDbContextFactory` 100%; CSP com nonce por request + `X-Frame-Options`/`nosniff`/HSTS; payout com retry/backoff/idempotencia deterministica; `async void`=0; `StateHasChanged`=16 (baixo).

### P0 -- CRITICO (fazer ja, antes de qualquer deploy)
1. **SECRET REAL COMMITADO** -- `appsettings.json:22-23` tem `EfiBank:ClientId` e `EfiBank:ClientSecret` em texto puro (`Client_Id_11a0...`, `Client_Secret_10e8...`), diferente de todos os outros provedores que usam `__SET_VIA_USER_SECRETS__`. Mesmo sendo homologacao (`Sandbox:true`), esta versionado no git. **Acao**: (a) **rotacionar** o ClientSecret no painel Efi; (b) trocar por `__SET_VIA_USER_SECRETS__` no `appsettings.json`; (c) injetar via User Secrets (dev) / env var no EasyPanel (prod); (d) considerar limpar do historico do git se o secret rotacionado nao bastar. O `CertificatePassword` vazio tambem deve vir de secret.

### P1 -- ALTO (proximo ciclo)
2. **Path de certificado hardcoded** -- `appsettings.json:24` `CertificatePath: C:\FreezaSSD\...p12` e especifico de maquina. Mover para env var (`EfiBank:CertificatePath` / `CertificateBase64` ja suportado no service) e deixar placeholder no repo.
3. **Cobertura de testes baixa + gap de TDD** -- ~9,9% (metrica do Pleno). Features recentes (`EnableBestPlayerVoting`, default de gateways) entraram sem teste, contra a regra do Ciclo 18. **Acao**: teste do toggle e cascata (desligar gateways -> desliga ranking/votacao) e do guarda-corpo do checkout; meta de cobertura crescente por ciclo focando services sem teste.
4. **Monolitos CSS** -- `site.css` (7.096 linhas) e `events.css` (5.013). Modularizar por dominio (`futsal.css`, `poker.css`, `admin.css`) **incremental** (1 extracao por commit, com verificacao visual). So no ciclo de CSS.

### P2 -- MEDIO
5. **Sync-over-async no `AppDbContext`** -- `SaveChanges()`/`SaveChanges(bool)` (`AppDbContext.cs:38-52`) chamam `EnsureGroupInviteCodesAsync(...).GetAwaiter().GetResult()` e `ValidateEventCollisionsAsync(...).GetAwaiter().GetResult()`. Em Blazor Server isso arrisca thread-pool starvation/deadlock sob carga. **Acao**: garantir que os callers usem `SaveChangesAsync` (o override async ja e correto) e, idealmente, tornar o `SaveChanges` sincrono um caminho sem I/O async (ou lancar se usado).
6. **Code-behinds grandes (SRP)** -- `AdminPayments.razor.cs` (1.046), `Mailbox.razor.cs` (614), `Payment.razor.cs` (562), `Features.razor.cs` (528), `EventPayment.razor.cs` (476), `Escalacao.razor.cs` (471). Continuar a extracao de services testaveis iniciada no Ciclo 18.
7. **`AsNoTracking` subutilizado** -- ~37 usos em 165 queries. Aplicar em paginas read-only (listagens/detalhes que nao salvam) para reduzir overhead de tracking.
8. **mTLS do webhook Efi e opcional** -- so valida client-cert se `WebhookClientCertSubject` estiver setado. Confirmar que esta configurado em prod, senao o webhook Pix fica so com secret de query-string.

### P3 -- BAIXO (higiene)
9. **68 warnings de nullable no projeto de testes** (CS8625/8604/8601/8602) -- app tem 0. Limpar junto ao ciclo de testes.
10. **`serviceFeePercentage`** (`EventPaymentService`) e derivado do total, nao da base -- so "legacy compat" com taxa fixa; remover para reduzir confusao.
11. **29 `!important`** (auditados como majoritariamente legitimos em ciclos anteriores) e **7 `catch {}`** (clipboard/JS dispose, benignos) -- revisar caso a caso quando tocar nos arquivos.
12. **Cache distribuido (Redis)** -- NAO agora; so justifica com multi-instancia ou gargalo medido.

**Sequencia recomendada de ciclos**: (1) P0 secret Efi -> (2) Ciclo Testes+TDD gap (P1.3) -> (3) Ciclo CSS modular (P1.4 + P3.11) -> (4) Ciclo SOLID code-behinds (P2.6) + `SaveChanges` (P2.5) -> (5) higiene (P2.7, P3). Login Google em prod e mobile UX continuam no roadmap conforme prioridade do Robson.

## Padrão de Referência de PRs (NOVO)

Para manter rastreamento claro do desenvolvimento e permitir revisões posteriores, todos os PRs devem ser documentados no WORK_PLAN.md com:

- **Número do PR** (ex: #56)
- **Título do PR** (ex: feat(pagamento): Sistema de pagamento com taxas fixas)
- **Link do PR** (ex: https://github.com/FreezaCaipira/Confirmai/pull/56)

**Formato de documentação:**
```markdown
### Ciclo X (Nome do Ciclo)
- PR #[numero]: [Título do PR] - [Link]
```

**Benefícios:**
- Breadcrumb completo do desenvolvimento
- Identificação clara de PRs não revisados pelo Senior
- Suporte a desenvolvimento em paralelo (múltiplos PRs)
- Liberdade para desenvolvimento a nível Pleno com revisões posteriores

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

## Revisao Senior do Ciclo 17 (Login Google + Email Real + Mojibake + Menu Mobile)

**Data**: 14/06/2026 | **Base auditada**: `main` (PR #54 `feat/ciclo17-google-login` + PR #55 `fix/ciclo17-test-feedback`)
**Build**: 0 erros | **Testes**: 1.700/1.700 passando (+6 vs C16) | **Mojibake**: 0 remanescente

### Veredicto: EXCELENTE -- todas as frentes planejadas entregues com qualidade de producao

| Fase | Planejado | Entregue | Status |
|------|-----------|----------|--------|
| 1. Menu mobile CSS | Consolidar `oldsite-top-nav` em `site.css`, remover scoped duplicado | Regras mobile do nav removidas de `MainLayout.razor.css`; 2 blocos base colapsados em 1 | **OK** |
| 2. Mojibake | Corrigir 4 arquivos | 0 ocorrencias de dupla-codificacao em todo o codigo | **Zerado** |
| 3. Google OAuth backend | Pacote + `AddGoogle` guardado por config | `Microsoft.AspNetCore.Authentication.Google` 9.0.5 + `AddGoogle` so quando ClientId/Secret presentes | **OK** |
| 4. OAuth criar-ou-vincular | Login direto se vinculado; senao busca por email, vincula ou cria | `ExternalLogin.cshtml.cs` implementa exatamente o fluxo, com audit trail em cada caminho | **OK** |
| 5. Email real + confirmacao | `IEmailSender` real + `RequireConfirmedAccount` | `IdentityEmailSender` (SMTP real + fallback em disco p/ dev) + `RequireConfirmedEmail` gated em `emailEnabled` | **OK** |
| 6. Testes | Cobrir fluxos de auth | +6 testes, suite 1.700 verde | **OK** |

### Destaques de qualidade

- **OAuth criar-ou-vincular** (`ExternalLogin.cshtml.cs`): fluxo correto e seguro -- `ExternalLoginSignInAsync` primeiro; se nao vinculado, busca `FindByEmailAsync` -> **vincula em conta existente sem duplicar** (`AddLoginAsync`) ou **cria com `EmailConfirmed=true`** + role `user`. Rate limiting (`auth`), `[AllowAnonymous]`, i18n e audit em todos os caminhos.
- **Config defensiva**: botao Google so aparece quando o provider esta registrado (`ExternalLogins.Count > 0`); `AddGoogle` so registra com ClientId/Secret presentes. Sem credenciais no git -> compila e roda mesmo sem OAuth configurado.
- **Email**: `RequireConfirmedEmail = securityPolicy.RequireConfirmedEmail && emailEnabled` -- confirmacao so e exigida quando o email esta realmente habilitado (nao trava dev/testes). `IdentityEmailSender` tem SMTP real + fallback que persiste o email em `wwwroot/uploads/dev-emails` para o dev ler o link de confirmacao localmente.
- **Processo**: TODAS as mudancas extras (schedule/horario, goleiro, meus-eventos mobile, dropdown de cidades IBGE, upload, fluxo grupos<->eventos) sao requisitos levantados pelo Robson testando -> **legitimas pela regra 22, NAO scope creep**.

### Correcao aplicada pelo Senior nesta PR (higiene CSS)

- **1 var morta removida**: `--shadow-red-lg` (rgba(239,68,68,0.5)) -- adicionada por mim na PR #53 antecipando uso, mas nunca referenciada. `--shadow-red-md` e `--red-light` (irmas) estao em uso. Removida do `:root` de `site.css`.
- Verificado: 0 hardcoded hex em scoped CSS, 0 var indefinida real (as `--identity-rhythm-*` sao definidas em `identity.css`; `--accent-preview`/`--item-accent-border` tem fallback).

### Observacoes (nao-bloqueantes, para o Pleno)

- **Breakpoint inconsistente 700px vs 768px**: o menu mobile e varios blocos ainda usam `@media (max-width: 700px)` enquanto o resto migrou para 768px. O Pleno ja documentou isso (regra 23 + fase CSS do C19) -- consolidar num unico breakpoint fica para o **Ciclo 19** (auditoria CSS Web/Mobile).
- **49 warnings de build** (num build limpo `--no-incremental`): predominantemente nullable (CS86xx) no projeto de testes, pre-existentes; ~4 em codigo de app (`Login.cshtml`, `GroupDetailEvents.razor`, `GroupDetailPendingRequests.razor`, campos nao usados em `EventPayment.razor.cs`). Baixa prioridade -- limpar junto ao C19.

### Proximo: Ciclo 18 -- Mobile UX critico

Auditar e garantir os fluxos principais dos stakeholders 100% OK no mobile (375px/414px): entrar em grupo via convite -> ver eventos -> confirmar presenca -> pagar (Pix/BTC) -> ver comprovante. Toque >=44px, sem scroll horizontal, forms/modais usaveis. Detalhado abaixo no plano do Ciclo 18.

---

## Questionamento do Pleno ao Senior -- Ciclo 17 (UX nao revisada + perguntas nao respondidas)

**Data**: 14/07/2026 | **Branch**: `fix/ciclo17-test-feedback` (PR #55, 101 commits, 41 arquivos, +1456/-453)

### 1. UX extensiva nao revisada

A revisao do senior (acima) cobre apenas o PR #54 (`feat/ciclo17-google-login`) e menciona o PR #55 como "test-feedback". Porem, o PR #55 contem **101 commits de refinamentos de UX/UI e bugfixes** baseados em testes manuais do Robson, incluindo:

- **Menu de navegacao**: "Ola" -> "Bem-vindo", separador, badge reposicionada, texto "Menu" no hamburger
- **Partidas Semanais**: movida de /meus-eventos para /grupo/{id}/partidas (correcao semantica), filtragem por groupId, redirects ajustados
- **Bugfixes**: goleiro confirmado como linha, botao "Editar partida" removido, redirect do "Nova Partida", horario errado (.ToLocalTime subtraindo 3h), header "Data" -> "Data/Hora"
- **Mobile**: inputs desalinhados (breakpoint 480px -> 768px), badges ocultos no mobile, botao Voltar em row separada
- **Payments**: botao Pendentes (contraste), Recarregar discreto, notify actions empilhados, legibilidade
- **Futsal Create**: botao Voltar dinamico, dropdown de cidades IBGE (84KB JSON)
- **Onboarding**: 4 passos explicando fluxo Grupo -> Convidar -> Partida -> Confirmacoes
- **Grupo/Index**: botao "Ver Partidas" verde

Estas mudancas sao legitimas pela regra 22 (requisitos do Robson testando). Porem, a revisao do senior nao menciona ou avalia nenhuma delas. **Solicita-se revisao especifica do PR #55.**

### 2. Perguntas nao respondidas (docs/review-onboarding-hint.md)

O arquivo `docs/review-onboarding-hint.md` (commitado na branch) contem 4 perguntas para o senior que nao foram respondidas:

1. Concorda com a abordagem de faixa contextual vs texto fixo para onboarding?
2. O fluxo de 3 passos (Grupo -> Convidar -> Partida -> Confirmacoes) esta correto/faltando algo?
3. Vale a pena fazer o mesmo na tela de partidas vazias (/meus-eventos)?
4. O botao "entendi" (dispensavel) e necessario ou pode ser sempre visivel para users sem grupos?

### 3. Sugestoes CSS (C19) nao respondidas

As metodologias CSS propostas pelo Pleno (ITCSS, BEM, CSS Layers, mobile-first, design tokens, TDD visual) nas linhas 1438-1492 permanecem marcadas como "sugestoes de nivel Pleno -- aguardando revisao do Senior". Nenhuma resposta foi dada.

### 4. Sobreposicao Ciclo 17 <-> Ciclo 18

O Ciclo 18 planeja "auditar e garantir os fluxos principais dos stakeholders 100% OK no mobile (375px/414px)". O Pleno ja executou extenso trabalho de UX mobile no PR #55:

- Inputs desalinhados corrigidos (breakpoint movido para 768px)
- Badges ocultos no mobile para reduzir ruido visual
- Botoes com toque adequado (Voltar, Acessar, Semanais)
- Sem scroll horizontal em /grupos (box-sizing corrigido)
- Menu hamburger colapsa ao navegar
- Forms utilizaveis em mobile (criacao de grupo, upload, datetime selector)

**Questao**: o trabalho ja realizado no PR #55 cobre parcialmente o escopo do C18. Solicita-se que o senior avalie:
- Quais fluxos especificos ainda precisam de auditoria mobile?
- Ha fluxos que o Pleno ja testou e corrigiu que podem ser marcados como concluidos?
- O C18 deve ser refinado para focar apenas no que falta, ou re-auditar tudo do zero?

---

## Respostas do Senior ao Questionamento do Ciclo 17

**Data**: 14/07/2026 | Questionamento procedente -- a revisao anterior focou o PR #54 e tratou o PR #55 apenas como "test-feedback". Corrigido abaixo.

### R1. Revisao especifica do PR #55 (UX -- 101 commits)

Auditei os 101 commits do PR #55. **Veredicto: APROVADO.** Sao refinamentos legitimos vindos dos testes do Robson (regra 22), coerentes e sem regressao (build 0 erros, 1.700 testes verdes). Destaques por area:

- **Navegacao/Menu**: "Ola" -> "Bem-vindo", separador, badge unread reposicionada sobre o icone da cartinha, texto "Menu" no hamburger. **OK** -- consistencia visual.
- **Semantica (Partidas Semanais)**: mover de `/meus-eventos` para `/grupo/{id}/partidas` + filtrar por `groupId` + ajustar redirects. **OK e importante** -- corrige acoplamento semantico (partidas pertencem ao grupo, nao ao usuario global). Boa decisao.
- **Bugfixes**: horario errado (`.ToLocalTime()` subtraindo 3h -- confirmei que foi removido de `GroupDetailEvents.razor` e `Schedule/`), header "Data" -> "Data/Hora", goleiro confirmado como linha, botao "Editar partida" removido do `DetailAdminPanel`, redirect do "Nova Partida" passando `groupId`. **OK** -- todos corrigem comportamento incorreto real.
- **Mobile**: breakpoint de inputs 480px -> 768px (alinhamento), badges ocultos <=768px (reduz ruido), botao Voltar em row separada, `box-sizing`/`overflow` defensivo (sem scroll horizontal em `/grupos`). **OK** -- ver R4 sobre C18.
- **Payments**: contraste do botao Pendentes, Recarregar discreto (icone only), notify actions empilhados, legibilidade. **OK.**
- **Onboarding**: banner contextual em `/grupos` quando `groups.Count == 0`, 3 passos. **OK** -- ver R2.
- **Dropdown cidades IBGE**: `cities.json` (84KB, gerado por `gen-cities.ps1`) lido do filesystem em Blazor Server. **OK** -- abordagem correta (nao HTTP self-call). Ressalva menor: 84KB carregado por request na criacao de grupo; se virar gargalo, cachear em memoria (`IMemoryCache`) no C19.

**Ressalva de processo (nao bloqueia)**: PR #55 tem 101 commits com muitos "tentativa/erro" de cores/alinhamento (regra 14 -- preferir commits consolidados). Nao impacta o resultado, mas idealmente squashar refinamentos iterativos.

### R2. Perguntas de onboarding (`docs/review-onboarding-hint.md`)

1. **Faixa contextual vs texto fixo?** Concordo com faixa contextual (`groups.Count == 0`). Nao polui quem ja conhece o fluxo. Aprovado como implementado.
2. **Fluxo de 3 passos correto?** Correto. A implementacao final ("Crie grupo e convide" -> "Crie partidas semanais/avulsas" -> "Membros pagam e confirmam") esta melhor que a proposta original porque explica o conceito de "grupo = turma/racha" antes dos passos. Aprovado.
3. **Fazer o mesmo em `/meus-eventos` (partidas vazias)?** **Sim, mas no C18**, nao agora. Com a mudanca de partidas para `/grupo/{id}/partidas`, o empty state mais util e o da tela de partidas do grupo ("nenhuma partida ainda -> Crie a primeira"). Documentado como item do C18.
4. **Botao "entendi" (dispensavel) necessario?** **Nao.** A escolha do Pleno (sempre visivel para quem nao tem grupo, sem dismiss) e a correta -- assim que o user cria/entra num grupo o banner some naturalmente. Evita complexidade de `localStorage`. Aprovado.

### R3. Metodologias CSS do C19 (ITCSS/BEM/Layers/mobile-first/tokens/TDD visual)

**Aprovadas como norte do C19**, com priorizacao (o C19 e grande -- nao fazer tudo de uma vez):
- **Fazer primeiro (alto valor, baixo risco)**: (5) Design Tokens + (3) Mobile-first + breakpoint unico 768px + (6) Single Responsibility por scoped. Resolve o bug recorrente e a fragmentacao `oldsite-top-nav`.
- **Fazer em seguida (valor medio)**: (2) BEM em scoped novos/refatorados -- NAO renomear tudo em massa (risco alto, pouco retorno); aplicar em componentes que ja forem tocados.
- **Avaliar com cautela**: (1) ITCSS como organizacao mental do global (bom), mas sem reescrita grande; (4) `@layer` e (7) TDD visual Playwright sao **opcionais/experimentais** -- validar primeiro se o pipeline Blazor Server + CI suportam sem custo alto. Nao bloquear o C19 neles.
- **Regra**: cada metodologia entra como fase testavel e reversivel, 1 por commit, sem tocar testes existentes.

### R4. Sobreposicao C17 <-> C18 (mobile)

O PR #55 de fato **adiantou parte do C18**. Ajuste o escopo do C18 para focar no que falta, sem re-auditar do zero:

**Ja concluido no PR #55 (marcar como feito no C18)**: alinhamento de inputs (breakpoint 768px), reducao de ruido (badges ocultos mobile), sem scroll horizontal em `/grupos`, menu hamburger, forms de criacao de grupo/upload/datetime usaveis.

**Ainda pendente para o C18 (foco)**: auditoria fim-a-fim dos **fluxos criticos** especificamente em 375px/414px que o PR #55 nao cobriu de forma sistematica:
1. Entrar em grupo via invite code (tela de join + validacao mobile)
2. Ver eventos/partidas do grupo (cards, badges, acoes)
3. **Confirmar presenca** (linha/goleiro) -- toque >=44px nos botoes de acao
4. **Pagar Pix/BTC** -- QR, upload de comprovante, selecao de metodo em 375px
5. Ver comprovante/recibo
6. Empty state de partidas em `/grupo/{id}/partidas` (item da R2.3)

**Decisao**: C18 = auditoria dirigida desses 6 fluxos (nao re-auditar telas ja ajustadas). O breakpoint unico 768px sai do C18 e vai para a fase de Design Tokens do C19 (evita retrabalho).

---

## Ciclo 18: TDD + SOLID + CSS Refatoration (PROXIMO CICLO)

**Motivo**: O ciclo de Pagamento Real + Taxa de Servico foi implementado (branch `feat/pagamento-real-split`, PR pendente) mas sera revisado pelo Senior posteriormente quando os tokens semanais resetarem, por questao de limitacao de tokens.

**Foco deste ciclo**: Refatoracao TDD + SOLID + CSS, aplicando disciplinas de qualidade em TODO o codigo, incluindo o que foi desenvolvido no ciclo de pagamento.

**Observacao**: O Pleno ja aplicara TDD e SOLID no desenvolvimento do ciclo de pagamento (conforme solicitado). O Senior revisara tudo (pagamento + refatoracao) quando os tokens resetarem.

### Detalhamento do Senior -- Como o Pleno deve agir neste ciclo

**Regra de ouro**: refatoracao NAO muda comportamento. Antes de refatorar qualquer coisa, tem que existir teste verde cobrindo o comportamento atual. Se nao existe, o Pleno **escreve o teste primeiro** (caracterizacao), ve passar, e so entao refatora. Nada de "refatorar e torcer".

**Branch**: `refactor/ciclo18-tdd-solid-css`. **1 commit por passo pequeno e reversivel**, 1 PR no final. NUNCA misturar refatoracao estrutural com mudanca de comportamento no mesmo commit.

#### Bloco A -- TDD (disciplina em todo o ciclo)
1. Para cada area a mexer: rodar `dotnet test` e garantir baseline verde (1.700+). Anotar o numero.
2. **Red-Green-Refactor**: novo comportamento -> teste que falha -> codigo minimo pra passar -> refatorar com teste verde.
3. **Characterization tests** antes de refatorar codigo legado sem cobertura: capturam o que o codigo FAZ hoje (mesmo que imperfeito), pra detectar regressao.
4. Testes devem ser de **comportamento** (entrada->saida, efeitos observaveis), nao de implementacao. Nao testar detalhes privados.
5. Ao final de cada fase: suite verde + contagem de testes >= baseline. Zero teste ignorado/comentado pra "passar".

#### Bloco B -- SOLID (aplicar onde ha dor, nao dogmaticamente)
- **SRP**: quebrar componentes/services que fazem coisas demais. Alvo prioritario: qualquer page/service com muita logica no code-behind ou no markup. Extrair logica de negocio pra services testaveis.
- **OCP/DIP**: depender de interfaces (ja existe padrao: `IEventPaymentGateway`, `IDbContextFactory`). Novos pontos de extensao via interface + DI, nao `if/switch` de tipo concreto.
- **LSP/ISP**: interfaces pequenas e coesas; nao forcar implementacoes a metodos que nao usam.
- **Blazor especifico**: usar `IDbContextFactory` (nunca `AppDbContext` injetado direto -- metrica ja e 0, manter), minimizar logica em markup, componentizar blocos repetidos, cuidar de `StateHasChanged`/lifecycle e estado de circuito. Extrair chamadas de dados pra services.
- **Restricao**: refatoracao SOLID e **incremental e local**. Nao reescrever modulos inteiros de uma vez. Cada extracao coberta por teste antes e depois.

#### Bloco C -- Refatoracao CSS (seguir a ordem ja aprovada em R3 -- ver "Respostas do Senior ao Questionamento do Ciclo 17")
Prioridade (fazer nesta ordem, cada fase reversivel):
1. **Inventario/auditoria**: mapear CSS global vs scoped, conflitos de especificidade, duplicacao, breakpoints inconsistentes (700 vs 768).
2. **Design Tokens**: toda cor/spacing/shadow/radius = var do `:root`. Zero hardcoded hex em scoped (manter metrica 0). Remover vars mortas/indefinidas.
3. **Mobile-first + breakpoint unico 768px**: base = mobile; `@media (min-width:769px)` para desktop. Padronizar o breakpoint.
4. **Single Responsibility por `.razor.css`**: cada scoped estiliza so o seu componente; compartilhado vai pro global. Resolver fragmentacao tipo `oldsite-top-nav`.
5. **BEM apenas em scoped novo/refatorado** (nao renomear tudo em massa).
6. **Opcionais/experimentais** (so se sobrar folga e sem risco): `@layer` num spike pequeno; TDD visual Playwright nos fluxos criticos. Nao bloquear o ciclo nisso.

#### Criterios de aceitacao (o Senior vai cobrar na review)
- Suite de testes verde, contagem >= baseline; nenhum teste desabilitado.
- 0 erros de build; warnings nullable nao aumentam (idealmente reduzem).
- 0 hardcoded hex em scoped; 0 var CSS indefinida; 0 var morta.
- 0 `AppDbContext` direto em componentes (manter).
- Sem regressao visual/funcional nos fluxos principais.
- Commits pequenos, 1 responsabilidade cada; refatoracao separada de mudanca de comportamento.
- Encoding UTF-8 em todos os CSS (regra 18/19).

#### O que NAO fazer
- Nao refatorar sem teste cobrindo antes.
- Nao renomear classes CSS em massa sem beneficio concreto.
- Nao reescrever modulo inteiro num commit gigante.
- Nao mexer no comportamento a pretexto de "limpar".

### Formalizacao: TDD + SOLID como regra going forward (Ciclo 18 -- executado)

**Data**: 22/07/2026

A partir do Ciclo 18, TDD + SOLID sao regras permanentes de desenvolvimento, nao apenas deste ciclo. O seguinte foi estabelecido e executado:

#### TDD -- Regras permanentes
1. **Baseline verde obrigatorio**: antes de qualquer mudanca, `dotnet test` deve passar. Anotar contagem.
2. **Characterization tests antes de refatorar**: codigo legado sem cobertura recebe characterization test primeiro (captura comportamento atual), depois refatora com teste verde.
3. **Red-Green-Refactor**: novo comportamento -> teste que falha -> codigo minimo -> refatorar.
4. **Testes de comportamento, nao de implementacao**: entrada->saida, efeitos observaveis. Reflexao para metodos privados e aceitavel em characterization tests de legacy, mas novos testes devem cobrir a API publica.
5. **Zero teste ignorado/comentado pra "passar"**.

#### SOLID -- Regras permanentes
1. **SRP**: code-behinds nao contem logica de negocio. Extrair para services testaveis. Alvo: qualquer metodo com logica de negocio em `.razor.cs` e candidato a extracao.
2. **DIP**: depender de interfaces via DI. Novos pontos de extensao via interface + DI.
3. **Extracao incremental**: cada extracao coberta por teste antes e depois. Nao reescrever modulos inteiros.
4. **Padrao de extracao**: criar service + testes (TDD) -> substituir no code-behind -> build + suite verde.

#### Metricas do Ciclo 18 (executado)
- **Baseline**: 1799 testes
- **Final**: 1876 testes (+77)
- **Characterization tests (reflection)**: 54 (AdminPayments 30, EventPayment 16, Profile 8)
- **Services extraidos**: 3 (EventPaymentChargeCalculator, ReconciliationSeverityEvaluator, PixStaticPayloadGenerator)
- **Service unit tests**: 41 (8 + 17 + 16)
- **CSS audit**: 25 arquivos vazios removidos, fragmentacao auditada, BEM formalizado
- **Build**: 0 erros, 0 hardcoded hex em scoped

#### Ferramentas e padroes estabelecidos
- **Reflection para characterization tests**: `BindingFlags.NonPublic | BindingFlags.Static` para metodos privados estaticos em code-behinds Blazor
- **Unicode escapes em testes**: usar `\u00E7` etc. em string literals para evitar problemas de encoding
- **@inject via .razor**: quando `[Inject]` no `.razor.cs` nao e reconhecido pelo compilador Blazor, usar `@inject` no `.razor`
- **BEM em novos componentes**: `block__element--modifier` (ver css-audit.md Fase 7)
- **PR body como documentacao**: todo PR deve incluir body descritivo com resumo, metricas, criterios de aceitacao e notas para o revisor. PR sem body nao e aceito. O body serve como documentacao permanente do que foi feito e por que. O body vai na descricao do PR no GitHub, nao como arquivo commitado no repo.
- **Politica de versionamento .NET**: ver secao "Politica de Versionamento .NET" abaixo.

---

## Politica de Versionamento .NET (regra permanente -- estabelecida Ciclo 18)

**Data**: 23/07/2026

### STS vs LTS
- **LTS (Long Term Support)**: 36 meses de suporte (ex: .NET 8, .NET 10, .NET 12). **Preferir em producao.**
- **STS (Standard Term Support)**: 18 meses de suporte (ex: .NET 9, .NET 11). Usar apenas se uma feature critica for necessaria e nao estiver em LTS.
- Site de referencia: https://dotnet.microsoft.com/platform/support/policy/dotnet-core

### Regras
1. **Producao roda em LTS**: projetos em producao devem rodar em versao LTS. STS apenas para experimentacao ou necessidade critica.
2. **Janela de migracao**: planejar migracao **6 meses antes** do fim do suporte da versao atual.
   - .NET 9 (STS): suporte ate maio/2026 -> migrar ate dezembro/2025 (ou antes)
   - .NET 10 (LTS): lancamento nov/2025, suporte ate nov/2028
   - **Estrategia atual**: migrar de .NET 9 STS para .NET 10 LTS quando lancar (nov/2025).
3. **`global.json`**: sempre fixar a versao do SDK para build reproduzivel. Atualizar junto com a migracao.
4. **CI primeiro**: migracao comeca pelo CI -- garantir que o pipeline passa na nova versao antes de tocar no codigo.
5. **Dependencias**: verificar compatibilidade de todos os NuGets (EF Core, Moq, xUnit, etc.) antes de migrar. Rodar `dotnet list package --outdated`.
6. **TDD protege**: a suite de testes e a rede de segurança -- se passar na nova versao, a migracao e segura. Se falhar, investigar breaking changes e corrigir.
7. **Ciclo de migracao**: quando uma nova LTS lancar, criar um ciclo dedicado:
   - Atualizar `global.json` -> nova versao
   - Atualizar `TargetFramework` nos `.csproj` -> `netXX.0`
   - Rodar `dotnet test` -- se verde, migracao pronta
   - Se vermelho, investigar breaking changes e corrigir
   - Commit unico: `chore: migrar .NET X -> .NET Y (LTS)`

---

## Setup de Ambiente de Desenvolvimento (regra permanente -- estabelecida Ciclo 18)

**Data**: 23/07/2026

### Multi-machine development
O projeto pode ser desenvolvido em maquinas diferentes (PC principal, notebook de viagem). O Setup de deploy para producao (Easy Panel, Docker, certificados, secrets de prod) fica apenas no PC principal. As maquinas de apoio devem conseguir:

1. **Desenvolver normalmente** -- build, run, debug
2. **Testar localmente** -- `dotnet test`, `dotnet build`, Postgres local
3. **Subir para main** -- commits, push, PRs

O que **nao e necessario** em maquinas de apoio:
- Easy Panel / deploy pipeline
- Certificados de producao (EfiBank homologacao)
- Secrets de producao (API keys reais, webhook secrets)
- Docker (se Postgres nativo estiver instalado)

### Requisitos minimos para desenvolver
1. **.NET SDK** -- versao conforme `global.json` (atualmente 9.0.100+)
2. **PostgreSQL** -- instalado localmente (versao 16+)
   - Usuario: `freeza`
   - Senha: via User Secrets (nao commitada)
   - Banco: `Confirmai`
3. **User Secrets** -- configurados via `dotnet user-secrets`:
   - `ConnectionStrings:DefaultConnection` -- obrigatorio
   - Demais secrets (EfiBank, AbacatePay, etc.) -- opcionais em dev (app usa valores default/sandbox do `appsettings.json`)
4. **EF Migrations** -- `dotnet ef database update` para criar o schema

### Padrao de secrets em dev
- **User Secrets** e a fonte de truth para dev local (nao commitado, nao no appsettings.json)
- **Easy Panel** e a fonte de truth para prod (environment variables no container)
- **appsettings.json** contem apenas valores nao-sensiveis e placeholders `__SET_VIA_USER_SECRETS__`
- Nunca commitar senhas, API keys ou certificados no repo

### Procedimento de setup em nova maquina
1. Instalar .NET SDK (versao do `global.json`)
2. Instalar PostgreSQL 16+
3. Criar usuario `freeza` e banco `Confirmai`
4. `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=Confirmai;Username=freeza;Password=..." --project .\Confirmai.csproj`
5. `dotnet ef database update --project .\Confirmai.csproj`
6. `dotnet build` -- deve passar com 0 erros
7. `dotnet test` -- deve passar (exceto ProgramConfigurationTests se sem Postgres rodando)
8. `./dev.ps1` -- app deve iniciar

### Super user de dev (padronizado)
- **Credenciais de dev** (idênticas em qualquer maquina):
  - Email: `admin@confirmai.app`
  - Senha: `Admin123!Aa`
  - Role: `admin`
- **Fonte**: `appsettings.Development.json` (commitado no repo, so vale em ambiente Development)
- **User Secrets** nao sao mais necessarios para AdminSeed em dev -- o `appsettings.Development.json` ja tem os valores
- **Prod**: o Easy Panel injeta `AdminSeed:Email`, `AdminSeed:Password` e `AdminSeed:FullName` via environment variables, sobrescrevendo o `appsettings.json` (que tem placeholders `__SET_VIA_USER_SECRETS__`)
- **SyncPassword**: em Development, a senha do admin e sincronizada a cada startup (garante consistencia). Em prod, `SyncPassword` deve ser `false` ou nao configurado.

---

## Review Senior -- Ciclo 19 (Polimento UX mobile + toggles + gateways default) -- ANALISADO

**Data**: 14/06/2026 (Senior). **PR revisado**: #65 (`review/senior-overview`, mergeado na main). Build OK. Testes: **1889 passando** / 1913 (as 24 falhas sao apenas `ProgramConfigurationTests` sem Postgres -- ambiente, nao regressao).

**Veredito: BOM no UX, mas 1 RISCO DE PRODUCAO que exige decisao antes de deploy.**

### RISCO ALTO (decisao do Robson antes de deploy) -- default de gateways
A migration `20260725191038_EnablePaymentGatewaysDefaultTrue` faz `UPDATE "Groups" SET "EnablePaymentGateways"=true WHERE ...=false` -- ou seja, **liga gateways para TODOS os grupos existentes** e muda o default do modelo para `true` (`Group.cs`). Combinando com:
- `Fee.Enabled=true` + guarda-corpo do checkout (`EventPaymentService.cs:143-154`): se o grupo **nao tem `GroupPayoutAccount` ativa**, a geracao de cobranca e **bloqueada** ("O organizador nao configurou a chave PIX para repasse");
- `Fee.ShowDirectPixToOrganizer=false` (appsettings.json e Production): **sem fallback de Pix direto** quando gateways estao ligados.

**Consequencia em prod**: no deploy, todo grupo existente que **ainda nao cadastrou chave PIX de repasse** deixa de conseguir receber pagamento -- o jogador so ve o gateway EfiBank e a cobranca falha. **Recuperavel** pelo admin do grupo (cadastrar a chave PIX **ou** desligar gateways -> volta ao Pix direto manual), mas quebra silenciosa ate essa acao.

**Opcoes (o Robson escolhe)**:
1. **Nao auto-ligar grupos existentes**: remover o `UPDATE` da migration -- default `true` vale so para **grupos novos**; existentes seguem como estao. (mais seguro)
2. **Manter flip + `ShowDirectPixToOrganizer=true`**: grupos sem chave caem no Pix direto em vez de bloquear.
3. **Manter como esta + acao operacional**: avisar todos os organizadores para cadastrarem a chave PIX antes do deploy. (so viavel com poucos grupos conhecidos)

### BOM (aprovado)
- **UX mobile "Voce"**: badge compacto substituindo o nome/email em telas <=768px, em 9 componentes; regra `:has()` que oculta tags durante confirmacao ficou **dentro** da media query (corrige o vazamento pro desktop). Correto.
- **Toggles**: novo `EnableBestPlayerVoting` + logica de dependencia (`Features.razor.cs`) -- ao desligar gateways, ranking pos-partida e votacao sao desligados juntos ("incentivo" ao fluxo com taxa). Bem implementado, com audit trail (`AuditEvents.GroupFeatureToggled`) e migrations corretas.
- **Venue Edit**: hint/placeholder do Google Maps condicionais a `hasMapsApiKey`. Correto.
- **Limpeza**: removidos usuarios fake hardcoded (`gm@teste.com`, `gm2@teste.com`) do `AppInitializationService`. Bom.
- **Simulacao de dados**: inserida **via SQL direto no Postgres**, nao no codigo -- nao polui o repo. OK.

### Correcoes de higiene aplicadas nesta PR de review
- **Doc solto removido (regra 20)**: `docs/SENIOR_REVIEW_CICLO19.md` -- conteudo (progresso + pedido de overview + 7 decisoes) consolidado aqui.
- **Nota**: `appsettings.Development.json` commita credencial de admin de dev (`admin@confirmai.app` / `Admin123!Aa`). Aceitavel por ser **so ambiente Development** e documentado, mas confirmar que em prod o `SyncPassword=false` e o AdminSeed vem por env var (esta assim). Nao commitar credencial em `appsettings.json`/`Production.json`.
- **Gap de TDD**: a nova feature `EnableBestPlayerVoting` e a mudanca de default de gateways entraram **sem testes novos** (contraria a regra permanente do proprio Ciclo 18). Pedir ao Pleno: teste do toggle (liga/desliga + cascata que desliga ranking/votacao) e teste do guarda-corpo com gateways ligados sem payout account.

### Respostas do Senior as 7 decisoes pendentes do doc do Pleno
1. **Modularizar `events.css` (~5.000 linhas)**: SIM, mas **incremental** e so no ciclo de CSS -- extrair por dominio (`futsal.css`, `poker.css`, `admin.css`) com characterization visual, uma extracao por commit. Nao fazer num commit gigante.
2. **Cache distribuido (Redis)**: NAO agora. So justifica com multi-instancia ou gargalo medido. Prematuro.
3. **Cobertura 9,9% -> 15%+**: SIM, focar nos 5 services sem cobertura + o gap de TDD acima. Meta por ciclo, nao de uma vez.
4. **WhatsApp real**: manter **postergado** (decisao do Robson -- aguardando ideias do amigo dev).
5. **E2E mobile (Playwright viewport)**: adiar; primeiro estabilizar o pagamento em homologacao. Vale como experimento depois.
6. **Consolidar 4 gateways**: NAO consolidar codigo (a factory ja e plugavel). So EfiBank cobra taxa/repassa hoje; os outros ficam sem taxa. Reavaliar quando houver volume real.
7. **Indices PostgreSQL em `EventConfirmations(EventId, UserId)`**: SIM, baixo custo e alto retorno -- criar via migration dedicada. Bom candidato pro proximo ciclo.

**Conclusao**: ciclo de UX aprovado. **Bloqueio para deploy**: resolver a decisao do default de gateways (risco acima) antes de subir para prod, senao grupos existentes sem chave PIX ficam sem receber pagamento.

---

## Review Senior -- Ciclo Pagamento Real + Taxa de Servico (analisado)

**Status**: Implementado (PR #60 mergeado) e revisado pelo Senior. Build OK, testes unitarios novos (FeeCalculatorTests, PayoutServiceTests) verdes. As 24 falhas locais sao apenas `ProgramConfigurationTests` que precisam de Postgres (ambiente sem DB) -- nao sao regressao; passam no CI.

**Veredito**: arquitetura correta e todas as 8 fases entregues (GroupPayoutAccount, FeeOptions, PayoutStatus/EndToEndId, EfiBankPixPayoutService, integracao no webhook, checkout transparente, guarda-corpos, relatorio admin). Idempotencia do **webhook** esta correta (curto-circuito em `PaymentStatus == Paid`, evita repasse duplicado no fluxo normal -- `WebhookPaymentMarker.cs:48-49`). Porem ha **pontos que o Pleno precisa corrigir antes de prod** (financeiro, alto risco):

### BLOQUEADORES / correcoes para o Pleno

1. **DECISAO TRAVADA (Robson, 17/07/2026) -- taxa FIXA** R$0,50 (app) + R$0,25 (gateway), por cima. RESOLVIDO nesta PR de review: removido o `FeeCalculator` percentual (codigo morto) + `FeeCalculatorTests`, removido o campo `_feeCalculator` de `PayoutService` e o `PercentBps` de `FeeOptions`; testes do `PayoutService` migrados para taxas fixas. O modelo percentual fica descartado.

2. **Falha de repasse NUNCA e retentada (plano pedia retry+backoff+alerta admin)**: em `PayoutService.cs:155-165`, ao falhar o Envio de Pix, marca `PayoutStatus.Failed` e incrementa `PayoutRetryCount`, mas **nada reprocessa**. Como o webhook idempotente so chama o payout na 1a transicao para Paid, webhooks repetidos nao retentam. Resultado: jogador pagou, site reteve tudo, organizador nao recebeu, sem retry nem alerta. Falta job de retry (backoff) + alerta admin em falha persistente.

3. **Sem guarda-corpo efetivo no checkout sem chave de repasse**: se `Fee.IsConfigured` e o grupo **nao tem `GroupPayoutAccount` ativa**, o checkout ainda cobra base+taxa (`EventPayment.razor.cs:174`) e o `PayoutService` depois **pula** o repasse (`PayoutService.cs:76-82`). Dinheiro do organizador fica retido no site. Deve **bloquear a cobranca** (ou exigir cadastro da chave) antes de gerar o QR.

4. **Payload do Envio de Pix EfiBank a validar (critico p/ prod)**: `EfiBankPixPayoutService.cs:90-102` monta `PUT /v2/gn/pix/{txId}` com `{ valor, chave, infoPagador:{nome,cpf:"00000000000"} }`. Nao bate com o schema documentado do "Envio de Pix" da Efi (usa `pagador`/`favorecido`); o CPF placeholder `00000000000` provavelmente e rejeitado. Validar contra doc/homologacao antes de prod, senao todo repasse falha.

5. **Idempotencia do envio**: cada `SendPayoutAsync` gera `txId` aleatorio (`GenerateTxId()`). Se o payout for chamado 2x pro mesmo pagamento, gera 2 envios (duplo repasse). Usar chave de idempotencia deterministica (derivada do `confirmationId`/`PaymentId`) como `{txId}` do PUT.

### Nao-bloqueantes (higiene)

6. **Hardcode no breakdown da taxa** -- RESOLVIDO nesta PR: `EventPaymentGateways.razor` agora le `FeeOptions.AppFeeFixed/GatewayFeeFixed` em vez de `0.50m`/`0.25m` hardcoded.
7. **`pr-body.md`** foi commitado na raiz (viola regra 20 de doc centralizada) -- removido nesta PR de review.
8. **`serviceFeePercentage`** em `EventPayment.razor.cs:166` e calculado sobre o total (base+taxa), nao sobre a base -- so "legacy compatibility", mas confuso; remover se a taxa e fixa.

---

## Review Senior -- Ciclo 18 (TDD + SOLID + CSS + correcao dos 4 bloqueadores) -- ANALISADO

**Data**: 14/06/2026 (Senior). **PR revisado**: #63 (`refactor/ciclo18-tdd-solid-css`, mergeado na main). Build OK. Testes: **1889 passando** / 1913 (as 24 falhas sao apenas `ProgramConfigurationTests` que precisam de Postgres -- ambiente sem DB, nao sao regressao; passam no CI).

**Veredito: EXCELENTE.** O Pleno entregou a refatoracao TDD+SOLID+CSS **e**, no mesmo ciclo, **corrigiu os 4 bloqueadores financeiros** que eu havia levantado na review do ciclo de pagamento. Todos verificados:

1. **Retry de repasse -- RESOLVIDO.** Novo `PayoutRetryService` (`IHostedService`, timer a cada 5min via `IServiceScopeFactory`) + `PayoutService.RetryFailedPayoutsAsync()`: busca `Failed`/`Retrying`, aplica **backoff exponencial** (`5 * 2^retryCount` min), teto de **5 tentativas** e, ao esgotar, loga em nivel `Error` "REPASSE MANUAL NECESSARIO" com valor base e chave PIX (alerta admin). Registrado no DI (`Program.cs:166 AddHostedService<PayoutRetryService>`). Ref: `PayoutService.cs:183-270`, `PayoutRetryService.cs`.

2. **Guarda-corpo no checkout -- RESOLVIDO.** A logica de checkout foi extraida para `EventPaymentService` (SRP) e agora, quando `Fee.IsConfigured` e o grupo **nao tem `GroupPayoutAccount` ativa**, retorna erro e **nao gera a cobranca** ("O organizador nao configurou a chave PIX para repasse..."). Ref: `EventPaymentService.cs:143-154`.

3. **Payload do Envio de Pix EfiBank -- CORRIGIDO (validar homologacao).** O corpo agora usa o schema correto `{ valor, pagador:{ chave, infoPagador }, favorecido:{ chave } }` e o **CPF placeholder foi removido**. A resposta trata `e2eId`/`endToEndId`. Estruturalmente correto; **o Pleno/usuario ainda deve validar em homologacao Efi** (credenciais + certificado) antes de prod. Ref: `EfiBankPixPayoutService.cs:95-131`.

4. **Idempotencia do envio -- RESOLVIDO.** `SendPayoutAsync` passou a receber `idempotencyKey` deterministica (`payout-{confirmationId}-{txId}`) e o `txId` do PUT vira `SHA256(idempotencyKey)[..16]` (32 hex, determinstico) -- Efi reconhece `idEnvio` duplicado. Alem disso `ProcessPayoutAsync` faz curto-circuito se `PayoutStatus == Sent|Confirmed`. Ref: `EfiBankPixPayoutService.cs:256-260`, `PayoutService.cs:106-114,149`.

### TDD + SOLID + CSS (o ciclo em si)
- **SOLID**: 3 services testaveis extraidos de code-behinds (`EventPaymentChargeCalculator`, `ReconciliationSeverityEvaluator`, `PixStaticPayloadGenerator`) + varios `.razor` decompostos em `.razor.cs`. Alem dos services de pagamento (`EventPaymentService`, `AdminPaymentsQueryService`, `AdminPaymentsSummaryService`).
- **TDD**: +77 testes (54 characterization + 41 unit). Baseline 1799 -> 1876 (metrica do Pleno; local 1889 verdes com os testes de env excluidos).
- **CSS**: breakpoints padronizados em 768px, 25 `.razor.css` vazios removidos, BEM formalizado, 0 hex hardcoded em scoped.
- **Regra permanente**: TDD+SOLID formalizados como regra going forward. Aprovado.

### Correcoes de higiene aplicadas nesta PR de review
- **Docs soltos na raiz removidos (regra 20)**: `pr-body.md` (voltou no #63), `REFACTORING_PROGRESS.md`, `css-audit.md`. O conteudo de valor (metricas, BEM) ja esta consolidado neste WORK_PLAN.
- **Ajuste da "regra de PR body"**: manter body descritivo e obrigatorio -- **mas na descricao do PR no GitHub**, nao como arquivo commitado na raiz. Arquivo `.md` de PR body no repo viola a regra 20.

### Pendencias reais antes de prod (nao-codigo)
- Validar o Envio de Pix em **homologacao Efi** (credenciais/certificado fora do git) -- unico item tecnico que resta confirmar.
- Confirmar com contador a nota fiscal sobre a taxa de servico (site no fluxo do dinheiro).

**Conclusao**: o ciclo de pagamento esta **tecnicamente pronto para teste em homologacao**. Nao ha mais bloqueadores de codigo.

---

## Ciclo Pagamento Real + Taxa de Servico (implementado)

**Status**: Implementado (PR #60 mergeado). Revisado -- ver "Review Senior" acima.

**Branch**: `feat/pagamento-real-split`

**Link para criar PR**: https://github.com/FreezaCaipira/Confirmai/pull/new/feat/pagamento-real-split

**Título do PR**: feat(pagamento): Sistema de pagamento com taxas fixas e repasse automático

**Resumo das alterações**:
- 32 arquivos alterados: +5491 linhas, -10 linhas
- 10 commits organizados por fases de implementação
- Taxas fixas: R$ 0,50 (app) + R$ 0,25 (gateway)
- Sistema de repasse automático via PayoutService
- Conta de repasse para grupos (GroupPayoutAccount)
- Relatório de receita para admin
- Configuração de produção (webhook, certificado, variáveis de ambiente)

**Configuração de produção**:
- Certificado de produção EfiBank carregado no EasyPanel
- Variáveis de ambiente configuradas
- Webhook URL: `https://confirmai.m2gpju.easypanel.host/api/webhooks/efibank/pix`

---

## Troubleshooting: Certificado EfiBank em Produção (15/07/2026)

**Problema Original:**
- Erro ao tentar gerar cobrança Pix em produção: `System.Security.Cryptography.CryptographicException: ASN1 corrupted data.`
- O certificado não estava sendo carregado corretamente pelo EasyPanel

**Tentativas de Solução:**

1. **File Mount (arquivo único)**
   - Tentativa: Upload do arquivo via EasyPanel File Mount em `/app/certs/efibank.p12`
   - Problema: Arquivo corrompido (tamanho diferente: 3544 bytes vs 2657 bytes original)
   - Resultado: Falha - EasyPanel corrompeu o arquivo binário durante upload

2. **File Mount (pasta com estrutura JSON)**
   - Tentativa: Upload de pasta `/app/certs` com estrutura JSON contendo o arquivo
   - Problema: EasyPanel não conseguiu processar a estrutura corretamente
   - Resultado: Falha - Arquivo não foi criado

3. **Terminal SSH**
   - Tentativa: Acesso via SSH para criar arquivo manualmente
   - Problema: Permissões insuficientes para criar arquivos em `/app/certs`
   - Resultado: Falha - Sem permissão de escrita

4. **FileBrowser**
   - Tentativa: Instalação e uso do FileBrowser do EasyPanel
   - Problema: FileBrowser acessava sistema de arquivos diferente do container Confirmai
   - Resultado: Falha - Pasta `/app` não visível no FileBrowser

5. **Caminho alternativo (/tmp/)**
   - Tentativa: Upload via FileBrowser em `/tmp/efibank.p12`
   - Problema: Container Confirmai não conseguia acessar `/tmp/` do host
   - Resultado: Falha - Arquivo não encontrado pelo container

**Solução Final: Certificado via Base64**

Implementação de suporte a certificado via variável de ambiente em base64:

- **Arquivos modificados:**
  - `Configuration/EfiBankOptions.cs`: Adicionada propriedade `CertificateBase64`
  - `Services/Payment/EfiBankPixService.cs`: Modificado `CreateProductionHandler()` para tentar carregar de base64 se arquivo falhar

- **Lógica de fallback:**
  1. Tenta carregar de arquivo (`CertificatePath`)
  2. Se falhar, tenta carregar de base64 (`CertificateBase64`)
  3. Logging detalhado para diagnóstico de ambos os métodos

- **Configuração:**
  - Variável de ambiente: `EfiBank__CertificateBase64`
  - Valor: Conteúdo base64 do arquivo .p12 (3544 caracteres)
  - Vantagem: Evita completamente problemas de sistema de arquivos

**Lições Aprendidas:**

1. **EasyPanel File Mount não é confiável para arquivos binários**
   - Corrompeu o certificado .p12 durante upload
   - Tamanho do arquivo mudou de 2657 para 3544 bytes

2. **Isolamento de containers**
   - FileBrowser acessa sistema de arquivos do host, não do container específico
   - Diretórios como `/app` do container não são visíveis externamente
   - Permissões são restritas por segurança

3. **Base64 como alternativa robusta**
   - Variáveis de ambiente são mais confiáveis que file mounts
   - Evita problemas de permissão e isolamento de containers
   - Fácil de configurar e debugar

4. **Logging é essencial**
   - Logging detalhado permitiu identificar o problema rapidamente
   - Mostrou tamanho do arquivo, existência e detalhes do erro criptográfico

**Ferramentas Usadas:**
- EasyPanel (File Mount, Environment Variables, FileBrowser)
- SSH (Termius)
- Git (versionamento e deploy)
- Base64 (codificação/decodificação)
- Serilog (logging detalhado)

**Status:** Solução implementada e aguardando análise do Senior para definir próximo passo.

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
16. **Mudanças apenas em documentação vão direto para main** -- arquivos .md, README, etc. podem ser commitados e pushados diretamente para main sem criar branch separada
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

## Analise: Pagamento Real + Taxa de Servico (PROXIMO CICLO)

**Pergunta do Robson**: "cobrar uma taxa sobre cada pagamento -- ele paga 15,60 e 0,60 fica pro site. Existe como fazer?"

**Resposta: SIM, e viavel.** Duas arquiteturas possiveis:

### Modelo A -- Intermediacao (o que o codigo JA faz hoje)
Hoje TODA cobranca usa a **chave PIX unica do site** (`chave = _options.PixKey` em `EfiBankPixService.CreateChargeAsync`). Ou seja, o site ja recebe 100% do dinheiro de todos os jogadores. Consequencia:
- **Taxa = trivial de implementar**: o site cobra o valor cheio e simplesmente "guarda" a taxa; o restante e devido ao organizador.
- **O que FALTA**: o **repasse ao organizador** (payout). Hoje nao existe fluxo de saque/repasse (confirmado: 0 services de payout/saque/carteira no codigo). O organizador nao tem como receber o dinheiro que esta na conta do site.
- **Risco regulatorio**: reter dinheiro de terceiros torna o site um **intermediador de pagamento / subadquirente** -- implica responsabilidade fiscal (emissao de nota da taxa), possiveis limites de PIX, e obrigacao de repasse. Precisa validar com contador/juridico.

### Modelo B -- Split de Pagamento (marketplace nativo)
O gateway divide o valor no momento do pagamento: X vai direto pro organizador, Y (a taxa) vai pro site. Nenhum dinheiro de terceiro passa pela conta do site.
- **EfiBank/Gerencianet**: suporta **Split de Pagamento** para PIX (requer cada organizador ter conta/recebedor cadastrado + KYC).
- **AbacatePay / Appmax**: verificar suporte a split na API (varia por gateway).
- **BTCPay (Bitcoin)**: NAO tem split nativo -- taxa em BTC teria que ser contabilizada manualmente ou via segunda saida.
- **Vantagem**: sem risco de intermediacao; cada um recebe o seu. **Custo**: onboarding/KYC de cada organizador como recebedor.

### Mecanica da taxa (independe do modelo)
Duas formas de cobrar:
1. **Por cima (repassada ao jogador)** -- jogador paga 15,60, dos quais 0,60 e a taxa. Organizador recebe 15,00. **(e o exemplo do Robson)**
2. **Embutida (descontada do organizador)** -- jogador paga 15,00, site fica com 0,60, organizador recebe 14,40.

Recomendacao: taxa **configuravel** (percentual + opcional fixo), arredondada a centavos, e **transparente** ao pagador ("taxa de servico: R$ 0,60"). Ja existe a string `PaymentDetails.Fee` = "Taxa de processamento" no i18n -- reutilizar.

**Atencao**: o proprio gateway ja cobra uma tarifa (ex: PIX ~0,99%). A taxa liquida do site = taxa cobrada - tarifa do gateway. Considerar no calculo do lucro real.

### DECISOES DO ROBSON (14/07/2026 -- TRAVADAS)
- **Modelo B -- Split de pagamento** (cada um recebe direto; site recebe so a taxa; exige recebedor/KYC por organizador)
- **Taxa por cima** (jogador paga 15,60; organizador recebe 15,00; site fica com 0,60)
- **Percentual** (ex: 4%). Formula: `total = round(base * (1 + feePct))` a centavos; `feeAmount = total - base`; split envia `base` ao organizador e `feeAmount` ao site (deriva feeAmount do total arredondado p/ evitar divergencia de 1 centavo).

### Contexto tecnico do split (verificar na implementacao)
- Gateways sao selecionados dinamicamente (`EventPaymentGatewayFactory` + toggle admin via `GatewayService`; default = primeiro habilitado). Ha 3 gateways PIX (EfiBank, AbacatePay, Appmax) + BTCPay (Bitcoin).
- **EfiBank/Gerencianet**: tem "Split de Pagamento" para PIX -- confirmar na doc a estrutura exata (recebedores precisam de conta/identificacao no proprio EfiBank; validar se aceita split por chave PIX de terceiros ou exige conta interna).
- **AbacatePay / Appmax**: verificar suporte a split na API antes de prometer. Se nao suportarem, ficam sem taxa (ou desabilitados para eventos com taxa).
- **BTCPay (Bitcoin)**: sem split nativo -- decisao MVP: NAO cobrar taxa em pagamentos BTC (contabilizar manualmente depois, se preciso).
- Hoje o `chave = _options.PixKey` e do site. No split, `base` vai pro recebedor do organizador e a taxa pro site -- inverte quem e o "dono" do dinheiro.

### Escopo detalhado do Ciclo "Pagamento Real + Taxa (Split)"

**Branch sugerida**: `feat/pagamento-real-split`. **TDD**: escrever teste antes de cada calculo/regra. 1 commit por fase, 1 PR no final.

1. **Recebedor do organizador (KYC)**: entidade nova (ex: `GroupPayoutAccount`) vinculada ao grupo/organizador -- guarda os dados de recebedor exigidos pelo gateway (chave PIX do organizador / id de recebedor). Tela no perfil do grupo para o admin cadastrar. Migration.
2. **Config + calculo de taxa**: `FeeOptions` (`PercentBps` inteiro p/ evitar float -- ex 400 = 4%, `Enabled`, gateways suportados) + `FeeCalculator` **puro e testavel** (TDD): dado `base` e `feePct`, retorna `(total, feeAmount)` por cima, arredondado a centavos.
3. **Split na cobranca**: estender `IEventPaymentGateway.CreateChargeAsync` (e os gateways que suportam) para receber o recebedor do organizador + a taxa e montar o payload de split. EfiBank primeiro; AbacatePay/Appmax conforme suporte.
4. **Registro por transacao**: campos `BaseAmount`, `FeeAmount`, `PayoutRecipient` no `PaymentRecord` (ou tabela `PlatformRevenue`). Webhook confirma o valor liquido.
5. **Checkout transparente**: jogador ve o breakdown ("Valor: R$ 15,00 + Taxa de servico: R$ 0,60 = R$ 15,60"). Reutilizar string `PaymentDetails.Fee`.
6. **Guarda-corpos**: grupo sem recebedor cadastrado -> bloquear/avisar na criacao de partida paga; gateway sem suporte a split -> nao aplicar taxa / esconder opcao.
7. **Relatorio admin de receita** da plataforma (soma de `FeeAmount` por periodo/gateway) -- conecta com o "Baseline por gateway" postergado.
8. **Testes**: `FeeCalculator` (arredondamento, 0%, valores quebrados), payload de split por gateway, registro por transacao, guarda-corpos (sem recebedor / gateway sem split), webhook confirmando liquido.

**Pre-requisito do Robson**: credenciais reais do provedor escolhido, fora do git. **ATENCAO**: a premissa "cadastrar so a chave PIX do organizador" NAO se confirmou na pesquisa -- ver "ACHADO DA PESQUISA" e "FORK REAL" abaixo. Decisao de provedor/modelo reaberta.

**Nota de risco**: mesmo no split, confirmar com contador a emissao de nota fiscal sobre a taxa de servico do site.

### Provedores de split/marketplace (avaliacao Senior -- 14/07/2026)

O Robson pesquisou e uma IA sugeriu **Iugu, Mercado Pago ou PagSeguro** para o modelo marketplace (muitos vendedores, plataforma fica com comissao). **Concordo -- e a abordagem correta para escalar.** Motivo: provedores de marketplace/split resolvem o ponto mais dificil do Modelo B -- o **onboarding + KYC dos organizadores** e a **responsabilidade regulatoria** -- que com EfiBank ficaria por nossa conta.

Como a arquitetura de gateway ja e plugavel (`IEventPaymentGateway` + `EventPaymentGatewayFactory`), adicionar um provedor de split e um novo gateway, sem reescrever o resto.

Comparativo (verificar detalhes/precos atualizados na contratacao):

| Provedor | Split nativo | Onboarding do organizador | PIX | Observacao |
|----------|-------------|---------------------------|-----|------------|
| **Mercado Pago** | Sim (`application_fee` + OAuth Connect) | **Organizador so faz login com a conta MP dele** -- MP cuida do KYC | Sim | Menor atrito; enorme adocao no BR; modelo "marketplace connect" |
| **Iugu** | Sim (subcontas) | Criar subconta por organizador (KYC via API) | Sim | Focado em SaaS/marketplace; split flexivel |
| **Asaas** | Sim (split + subconta/"wallet") | Subconta por organizador | Sim | Popular no BR para marketplace pequeno/medio; API simples |
| **PagBank/PagSeguro** | Sim (split de recebedores) | Recebedores cadastrados | Sim | Grande, mas API de split mais burocratica |
| **EfiBank (atual)** | Sim (split PIX) | **Por nossa conta** (mais manual) | Sim | Ja integrado, mas onboarding/compliance fica conosco |

**Recomendacao Senior**: para marketplace escalavel, priorizar **Mercado Pago (modelo Connect/OAuth)** ou **Asaas/Iugu (subcontas)** em vez de estender o EfiBank. O MP tem o menor atrito de onboarding (organizador so autoriza a conta dele), o que e critico se voce quer muitos organizadores. A comissao entra como `application_fee` (taxa por cima, percentual -- exatamente o modelo escolhido).

**Atencao a margem** (ponto levantado pela IA): a comissao do site tem que cobrir o custo do provedor. Ex.: se o MP cobra ~0,99% no PIX e voce cobra 4% por cima, sua margem liquida e ~3%. Ajustar o percentual para o custo do provedor nao "engolir" o lucro.

**Impacto no plano acima**: se escolher um provedor Connect (MP), a **Fase 1 (recebedor/KYC)** muda de "cadastrar dados manualmente" para "fluxo OAuth de autorizacao da conta do organizador" -- mais simples e seguro. As demais fases (FeeCalculator, registro, checkout, relatorio, testes) permanecem.

### ACHADO DA PESQUISA (14/07/2026 -- doc oficial EfiBank) -- premissa do "so a chave PIX" NAO se confirma

Fonte: https://dev.efipay.com.br/docs/api-pix/split-de-pagamento-pix
> "**O Split de pagamento Pix so pode ser realizado entre contas Efi**, com limite maximo de 20 contas para o repasse."
> "No processo de split de pagamento, e essencial fornecer uma **conta digital EFI valida**. ... se nao possuir uma conta valida para os repasses, sera necessario **criar uma subconta**."

**Conclusao**: o Split PIX do EfiBank NAO repassa para uma chave PIX de terceiro qualquer. O recebedor (organizador) **precisa ter uma conta Efi** (ou uma subconta criada por nos). Ou seja, a ideia de "so cadastrar a chave PIX do organizador, sem ele ter conta" **nao existe em produto de split** (nenhum provedor de split compliant repassa a chave solta -- MP/Asaas/Iugu tambem exigem conta/subconta, por causa de KYC).

**O unico jeito de aceitar "so a chave PIX do organizador" e o Modelo A (intermediacao)**, NAO split: o site recebe 100% na sua conta e depois faz um **Envio de Pix** (API "Envio e Pagamento Pix" do EfiBank) para a chave do organizador, retendo a taxa. Simples tecnicamente, mas o site fica no fluxo do dinheiro (retem valor de terceiro) -> intermediacao, com risco fiscal/regulatorio.

### DECISAO DO ROBSON (14/07/2026 -- TRAVADA): Opcao 2 -- Intermediacao Automatica (chave PIX solta + repasse via Envio de Pix)

O organizador informa apenas a **chave PIX** (sem precisar de conta Efi). Fluxo 100% automatico, sem clique manual:
1. Jogador paga o `total` (base + taxa) -> cai na conta PIX do site (`chave = _options.PixKey`, o que ja acontece hoje).
2. **Webhook** do EfiBank confirma o pagamento (webhook ja implementado no projeto).
3. Sistema dispara automaticamente um **Envio de Pix** (API "Envio e Pagamento Pix" do EfiBank -- manda PIX para QUALQUER chave) no valor `base` para a chave do organizador; o site **retem** a `feeAmount`.

**Nao e split** -- sao duas operacoes (recebe + reenvia). Consequencia: o dinheiro passa pela conta do site por alguns segundos -> o site figura como **intermediador** (risco fiscal/regulatorio; confirmar nota fiscal da taxa com contador). Tecnicamente exige tratar **falha de repasse** (chave invalida, PIX recusado, saldo) com retry + alerta admin.

**Plano ajustado (Opcao 2 automatica)**:
- **Fase 1 (recebedor)**: `GroupPayoutAccount` guarda a **chave PIX do organizador** (tipo + valor), cadastro manual pelo admin do grupo. Sem conta/subconta/OAuth.
- **Fase 3 (repasse)**: no handler do **webhook de confirmacao**, apos marcar o pagamento como pago, disparar **Envio de Pix** do `base` para a chave do organizador. Registrar estado do repasse (`PayoutStatus`: Pendente/Enviado/Falhou) + `EndToEndId`. **Retry** com backoff + **alerta admin** em falha persistente. Idempotencia (nao repassar duas vezes o mesmo pagamento).
- **Fases 2, 4-8 (FeeCalculator, registro, checkout, guarda-corpos, relatorio, testes)**: inalteradas. Guarda-corpo extra: bloquear/avisar criacao de partida paga se o grupo nao tem chave PIX de repasse cadastrada. Testes cobrindo falha/retry do repasse.
- **So EfiBank** cobra taxa+repasse por ora (tem Envio de Pix). AbacatePay/Appmax/BTC ficam sem taxa.

**Pre-requisito do Robson**: conta EfiBank com **API Pix (envio) habilitada** + credenciais (ClientId/Secret/certificado PIX) fora do git. Confirmar limites de envio de Pix da conta.

**Compliance**: confirmar com contador/juridico a responsabilidade de intermediacao + emissao de nota da taxa de servico.

---

## Ciclo 18 (FUTURO) -- Mobile UX Critico: Fluxos Principais

Auditar e garantir 100% no mobile os fluxos que os stakeholders mais usam:
entrar em grupo (invite code) → ver eventos → confirmar presenca → pagar (Pix/BTC) → ver comprovante.
Foco: alvos de toque >=44px, sem scroll horizontal, formularios/modais utilizaveis em 375px/414px, header consistente.

**Escopo REFINADO (ver "Respostas do Senior ao Questionamento do Ciclo 17 > R4")**: o PR #55 ja adiantou parte do mobile. NAO re-auditar do zero.

**Ja concluido no PR #55 (nao refazer)**: alinhamento de inputs (breakpoint 768px), badges ocultos no mobile, sem scroll horizontal em `/grupos`, menu hamburger, forms de criacao de grupo/upload/datetime.

**Fluxos criticos a auditar (foco do C18, 375px/414px)**:
1. Entrar em grupo via invite code (tela de join + validacao)
2. Ver eventos/partidas do grupo (cards, badges, acoes)
3. Confirmar presenca (linha/goleiro) -- toque >=44px
4. Pagar Pix/BTC (QR, upload comprovante, selecao de metodo)
5. Ver comprovante/recibo
6. Empty state de partidas em `/grupo/{id}/partidas` (banner "nenhuma partida -> crie a primeira", analogo ao onboarding de `/grupos`)

**Nota**: breakpoint unico 768px NAO entra aqui -- vai para a fase Design Tokens do C19 (evita retrabalho).

### Fases do Ciclo 18

**Branch sugerida**: `fix/ciclo18-mobile-ux`

#### Fase 1 -- Entrar em grupo via invite code (`/convite/{Code}`)

**Arquivos**: `Pages/Groups/Join.razor`, `Pages/Groups/Join.razor.css` (se existir), `wwwroot/css/events.css`

**Problemas identificados na auditoria**:
- A tabela de partidas (`events-table`) na pagina de convite usa `.ToLocalTime()` nas linhas 109-110 -- mescla bug de fuso horario do C17
- A tabela nao tem view mobile (sem `mobile-only` / `web-only` como em `GroupDetailEvents.razor`) -- em 375px a tabela vai estourar
- Botao "Entrar no grupo" (`.confirm-btn`) -- verificar se padding da >=44px de altura em mobile
- Botao "Entrar para participar" (login redirect) -- mesmo check

**Acoes**:
1. Remover `.ToLocalTime()` das linhas 109-110 (mesmo fix do C17)
2. Adicionar view mobile para a tabela de partidas (card layout analogo ao `GroupDetailEvents.razor`)
3. Garantir `.confirm-btn` com `min-height: 44px` em mobile (`@media max-width: 768px`)
4. Verificar `detail-card` padding em 375px (atualmente `1.75rem 1.5rem` -- pode esmagar)

#### Fase 2 -- Ver eventos/partidas do grupo (`/grupo/{id}/partidas`)

**Arquivos**: `Pages/Groups/Partidas.razor`, `Shared/Components/Groups/GroupDetailEvents.razor`, `wwwroot/css/events.css`

**Problemas identificados**:
- `GroupDetailEvents.razor` ja tem web/mobile split -- bom
- Tabs "Proximas/Realizadas" (`.detail-events-tab`) -- verificar toque >=44px em mobile
- Botoes "Semanais" e "Nova partida" (`.groups-create-btn`) -- verificar toque e wrapping em 375px
- Botao "Acessar" (`.event-access-btn`) -- so existe na view web (`web-only`); no mobile o card inteiro e clicavel (onclick na `<tr>`) -- confirmar que area de toque e adequada
- `events-table` em mobile: checar overflow horizontal, `et-cell` padding

**Acoes**:
1. Garantir `.detail-events-tab` com `min-height: 44px` em mobile
2. Garantir `.groups-create-btn` com `min-height: 44px` e flex-wrap em mobile
3. Verificar `.et-cell` e `.mobile-only` em 375px (padding, font-size, overflow)
4. Adicionar `box-sizing: border-box` defensivo se faltar

#### Fase 3 -- Confirmar presenca (`/futsal/{id}`)

**Arquivos**: `Pages/Futsal/Detail.razor`, `Pages/Futsal/Components/FutsalOutfieldGroup.razor`, `Pages/Futsal/Components/FutsalGoalkeeperGroup.razor`, `wwwroot/css/events.css`

**Problemas identificados**:
- Botao "Confirmar presenca" (`.player-slot-btn`) -- verificar altura em mobile
- Botoes de acao do admin (`.player-tag--admin-pay`, `.player-tag--admin-remove`, etc.) -- sao `player-tag` que pode ser muito pequeno para toque
- Botao "cancelar" (`.player-tag--cancel`) -- mesmo problema
- Botoes de confirmacao sim/nao (`.player-tag--cancel-yes`, `.player-tag--cancel-no`, `.player-tag--admin-pay-yes`, `.player-tag--admin-remove-yes`) -- icones soltos, provavelmente <44px
- Botao "Entrar na lista de espera" (`.waitlist-cta`) -- verificar
- Slot controls admin (`.slot-ctrl-btn`, `.add-slot-row`) -- verificar
- `.confirmed-inline-banner` com botoes de pagamento -- verificar wrapping

**Acoes**:
1. Garantir `.player-slot-btn` com `min-height: 44px` em mobile
2. Aumentar area de toque dos `.player-tag` interativos (admin pay/remove/cancel) em mobile -- `min-height: 44px`, `min-width: 44px`, padding adequado
3. Garantir `.waitlist-cta` com `min-height: 44px`
4. Verificar `.slot-ctrl-btn` e `.add-slot-row` em mobile
5. Verificar `.confirmed-inline-banner` wrapping em 375px (flex-wrap ja existe, mas confirmar)

#### Fase 4 -- Pagar Pix/BTC (`/pagamento/evento/{id}`)

**Arquivos**: `Pages/Payment/EventPayment.razor`, `Pages/Payment/Components/EventPaymentQr.razor`, `Pages/Payment/Components/EventPaymentGateways.razor`, `Pages/Payment/Components/EventPaymentPixAdmin.razor`, `Pages/Payment/Components/EventPaymentProof.razor`, `wwwroot/css/events.css`

**Problemas identificados**:
- QR code area (`.evpay-qr-image`) -- verificar se o QR e legivel em 375px (minimo ~200px)
- Botao "Copiar codigo Pix" (`.evpay-action-btn--copy`) -- verificar toque
- `evpay-brcode` (codigo copia e cola) -- texto longo, verificar overflow/word-break em mobile
- Selecao de gateway (`.evpay-method-option`) -- verificar toque e layout em mobile
- Botao "Gerar QR Code" (`.evpay-action-btn--pay`) -- verificar toque
- Upload de comprovante (`.evpay-proof-btn`) -- label com InputFile, verificar toque
- QR do Pix direto admin (`.evpay-admin-pix-qr`) -- mesmo check de legibilidade
- Chave pix (`.evpay-admin-pix-code`) -- verificar overflow em mobile
- `@media (max-width: 480px)` na linha 180 -- usar 768px (regra 23)
- Admin review: botoes confirmar/cancelar (`.evpay-admin-confirm-btn`) -- verificar toque

**Acoes**:
1. Garantir QR code com `min-width: 200px` em mobile
2. Garantir todos os botoes (`.evpay-action-btn`, `.evpay-method-option`, `.evpay-proof-btn`, `.evpay-admin-confirm-btn`) com `min-height: 44px` em mobile
3. Adicionar `word-break: break-all` no `.evpay-brcode` e `.evpay-admin-pix-code` para mobile
4. Mover `@media (max-width: 480px)` para `@media (max-width: 768px)` (regra 23)
5. Verificar `.evpay-method-list` layout em 375px (flex-direction column?)

#### Fase 5 -- Ver comprovante/recibo

**Arquivos**: `Pages/Payment/EventPayment.razor` (admin view), `wwwroot/css/events.css`

**Problemas identificados**:
- Admin ve comprovante (`.evpay-admin-proof-img`) -- verificar se imagem e legivel em 375px
- Botoes "Confirmar pagamento" / "Marcar como pago" (`.evpay-admin-confirm-btn`) -- verificar toque
- Botoes sim/nao (`.evpay-admin-confirm-btn--yes`, `.evpay-admin-confirm-btn--no`) -- verificar toque e layout empilhado em mobile
- `.evpay-admin-confirm-row` -- verificar wrapping

**Acoes**:
1. Garantir `.evpay-admin-proof-img` com `max-width: 100%` e `overflow-x: auto` se necessario
2. Garantir botoes admin com `min-height: 44px` em mobile
3. Garantir `.evpay-admin-confirm-actions` empilha verticalmente em 375px (`flex-direction: column` no mobile)

#### Fase 6 -- Empty state de partidas em `/grupo/{id}/partidas`

**Arquivos**: `Shared/Components/Groups/GroupDetailEvents.razor`, `wwwroot/css/events.css`

**Problemas identificados**:
- Empty state atual (linha 23-27) e minimal: so um icone + texto "Nenhuma partida proxima."
- Senior pediu banner analogo ao onboarding de `/grupos`: "nenhuma partida -> crie a primeira"
- So faz sentido para admin (quem pode criar partida)

**Acoes**:
1. Adicionar banner contextual no empty state quando `!ShowPastEvents` e admin: "Nenhuma partida agendada. Crie a primeira partida ou configure partidas semanais."
2. Incluir botoes/links para "Nova partida" e "Semanais" (se futsal) no banner
3. Para non-admin: manter texto simples "Nenhuma partida agendada."
4. Estilizar banner analogo ao onboarding de `/grupos` (fundo sutil + icone info)

#### Fase 7 -- Validacao final

**Acoes**:
1. `dotnet build` -- 0 erros
2. `dotnet test --filter "FullyQualifiedName!~ProgramConfiguration&FullyQualifiedName!~AdminLogsQueryString"` -- 0 failed
3. Testar manualmente em 375px (iPhone SE) e 414px (iPhone 12):
   - Fluxo completo: convite -> ver partidas -> confirmar -> pagar -> ver comprovante
   - Fluxo admin: criar partida -> ver confirmacoes -> marcar pago -> ver comprovante
4. Verificar: sem scroll horizontal em todas as telas, alvos de toque >=44px, forms utilizaveis

## Ciclo 19 (FUTURO) -- Refatoracao Completa: TDD + SOLID (inclui fase CSS Web/Mobile)

Refatoracao ampla aplicando TDD e SOLID (+ padroes pertinentes de Blazor Server: separacao de logica em services testaveis, `IDbContextFactory`, componentizacao, evitar logica no markup, gestao de circuito/estado).
Sera um ciclo grande — provavelmente subdividido. Detalhar escopo e ordem quando priorizado.

### Fase CSS: Refatoracao de Boas Praticas e Isolamento Web/Mobile

**Motivacao**: Bug recorrente onde afinamento de layout (width 57%) aplicado sem media query quebrou o mobile (campos esmagados). Necessario repassar TODO o CSS do projeto garantindo isolamento Web/Mobile e boas praticas.

**Branch sugerida**: `refactor/css-isolation`

**Metodologias CSS como norte (APROVADAS pelo Senior com priorizacao -- ver "Respostas do Senior ao Questionamento do Ciclo 17 > R3")** (equivalentes a SOLID/TDD para codigo funcional):

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

**Fases de Execucao (APROVADAS pelo Senior -- priorizar Design Tokens + Mobile-first + Single Responsibility primeiro; `@layer` e TDD visual sao opcionais/experimentais, ver R3)**:
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
