# Plano de Trabalho - Confirmai

> **Documento unico e vivo do projeto.** Serve simultaneamente como: (1) canal de comunicacao Senior <-> Pleno; (2) todolist / linha do tempo do desenvolvimento; (3) manual de como o Pleno deve codar, agir e se comunicar; (4) memoria de contexto -- se a sessao do Senior for resetada, este arquivo permite recuperar TUDO que e necessario para continuar.
>
> **Base atual**: `main` pos-Ciclo 22 | **Testes**: 2124 verdes / 2148 (as 24 falhas sao `ProgramConfigurationTests` sem Postgres local = ambiente, NAO regressao) | **Build**: app e testes com **0 warning / 0 erro**.
> **Stack**: .NET 9 (STS -> migrar p/ .NET 10 LTS quando lancar), Blazor Server, EF Core, PostgreSQL, ASP.NET Identity, SignalR, xUnit+Moq, OpenTelemetry/Serilog. Gateways de pagamento: EfiBank (Pix, ativo), AbacatePay, Appmax, BTCPayServer.

---

## Como ler este documento (para um contexto novo apos reset)

Ordem de leitura recomendada quando o contexto do Senior e reiniciado:
1. **Estado Atual do Projeto** (abaixo) -- onde estamos, o que esta pronto, metricas.
2. **Pendencias & Roadmap** (abaixo) -- o que falta, o que depende do Robson.
3. **Linha do Tempo dos Ciclos** (abaixo) -- historico condensado de cada ciclo, PR e veredito da review.
4. **Manual do Pleno** + **Regras para o Pleno (OBRIGATORIO)** -- como o codigo deve ser escrito.
5. **Regras de Comunicacao Senior <-> Pleno** -- como registrar ciclos e reviews.
6. Secoes de referencia (Setup de Ambiente, Politica .NET, CSS Vars, Comandos de Validacao, Troubleshooting Efi) -- consultar sob demanda.

---

## Papeis: Senior, Pleno e Robson

- **Robson (dono do produto)**: levanta requisitos/bugs/melhorias testando o app (isso NAO e scope creep -- ver regra 22), define prioridades, e responsavel por acoes fora do codigo (rotacionar secrets, gerar credenciais OAuth, validar fiscal/juridico, deploy no EasyPanel).
- **Senior (revisor/planejador)**: audita a `main`, revisa PRs do Pleno, planeja ciclos, escreve reviews e planos AQUI no WORK_PLAN, faz PRs de documentacao e correcoes de baixo risco. NAO implementa features grandes sem plano. Verifica build/testes/CI. Nao afirma "mergeado" sem fonte autoritativa.
- **Pleno (executor)**: executa o ciclo descrito no WORK_PLAN, faz refactors incrementais com TDD, escreve testes, entrega **1 PR por ciclo** para o Senior revisar. O Pleno **nao tem acesso direto aos PRs no GitHub** -- por isso ciclos e reviews precisam estar 100% explicitos AQUI.

---

## Manual do Pleno -- como codar, agir e se comunicar

Esta secao complementa a "Regras para o Pleno (OBRIGATORIO)" (detalhada mais abaixo). Aqui esta o **modo de operar** esperado.

### Como codar
- **TDD sempre**: para refatorar, escrever teste de caracterizacao ANTES (fixa o comportamento ATUAL); Red -> Green -> Refactor. Para feature nova, teste primeiro que descreve o comportamento desejado.
- **SOLID / SRP**: code-behind `.razor.cs` fica so com estado de UI + orquestracao; toda logica de dominio/consulta/acao vai para um **service testavel**. Meta: nenhum `.razor.cs` acima de ~250 LOC.
- **Dados**: NUNCA injetar `AppDbContext` direto -- sempre `IDbContextFactory<AppDbContext>`. Registrar services no DI em `Program.cs`. `AsNoTracking` em consultas read-only. `SaveChangesAsync` (nada de sync-over-async).
- **Incremental**: 1 assunto por commit; sem big-bang. Se um passo exige mudar regra de negocio, PARE e registre um achado para o Senior (nao mude comportamento as escondidas).
- **Preservar comportamento** em refactor: o valor cobrado, o payout, a escalacao etc. nao mudam. Interfaces publicas so mudam com plano explicito.
- **CSS**: mobile-first, breakpoint unico **768px** (`@media (max-width:768px)` mobile, `@media (min-width:769px)` desktop), usar **CSS vars** (nunca hex/rgba hardcoded em scoped CSS), sem `!important` novo, modular por dominio. Ver regras 1-2, 18-23 e "CSS Vars Permitidas".
- **Qualidade minima por incremento**: `dotnet build` 0 warning + suite verde (filtrando os testes ambientais de Postgres). Sem `Console.Write`/debug commitado.

### Como agir (fluxo de trabalho)
- **1 branch por ciclo, 1 PR por ciclo** (excecao: mudancas so-de-doc `.md` podem ir direto pra main -- regra 16). Nunca push direto na `main` para codigo.
- Executar as fases do ciclo NA ORDEM descrita no plano do ciclo (secao "Linha do Tempo" aponta o plano vigente).
- Se bloquear, **documentar** na secao "Problemas Encontrados pelo Pleno" (nao deixar silencioso).
- Ao terminar o ciclo, deixar o PR pronto para review e sinalizar (o Robson chama o Senior).

### Como se comunicar (via este documento)
- O Pleno se comunica com o Senior **escrevendo no WORK_PLAN** (achados, duvidas, bloqueios) e via a descricao do PR.
- Toda pergunta ao Senior vai numa subsecao clara (ex.: "Questionamento do Pleno -- Ciclo X"). O Senior responde na mesma regiao.
- Achados de bug durante um ciclo de teste: registrar como achado, NAO consertar no mesmo ciclo (a menos que o plano peca).
- Ao entregar um ciclo, resumir no PR: o que foi feito por fase, contagem de testes antes/depois, e qualquer desvio do plano.

---

## Regras de Comunicacao Senior <-> Pleno (via WORK_PLAN)

Como o Pleno nao ve os PRs no GitHub, o WORK_PLAN e a fonte de verdade. Todo ciclo e toda review devem ficar **explicitos e completos** aqui.

### Ao PLANEJAR um ciclo (Senior escreve)
Criar uma secao `## Ciclo N (Pleno) -- <titulo>` contendo:
- **Objetivo** (1-2 linhas) e por que o ciclo existe.
- **Fases** numeradas, cada uma com procedimento e **criterio de aceitacao**.
- **Meta de saida** mensuravel (ex.: "0 code-behind > 250 LOC", "+X testes", "0 warning").
- **O que NAO fazer** (limites do escopo).
- Referencia aos alvos concretos (arquivos, services).

### Ao REVISAR um ciclo (Senior escreve)
Criar uma secao `## Review Senior do Ciclo N (PR #XX) -- <VEREDITO>` contendo, de forma **explicita** (foi pedido pelo Pleno):
- **Veredito** no titulo: `APROVADO`, `APROVADO com ressalva`, ou `REPROVADO`.
- **PR e branch** auditados + confirmacao de que ja esta na `main` (ou nao).
- **Evidencias**: build (warnings), contagem de testes antes/depois, separando falhas ambientais de regressao.
- **Por fase**: o que foi entregue vs. o que o plano pedia (META ATINGIDA / PARCIAL / FALTOU).
- **Ressalvas** (bloqueante vs. nao-bloqueante) e o que fica pro proximo ciclo.
- **Pendencias do Robson** repetidas (para nao se perderem).

### Marcacao de estado
- Ao concluir um ciclo, marcar o plano com `[EXECUTADO -- ver review]` e adicionar a entrada na "Linha do Tempo".
- Nunca declarar um PR "mergeado" sem fonte autoritativa (`git_view_pr` / historico da `main`).

---

## Estado Atual do Projeto (pos-Ciclo 22)

- **Refatoracao estrutural: CONCLUIDA.** Code-behinds todos < 250 LOC; CSS modularizado por dominio (`site.css` 5.589->1.615, `events.css` 3.679->1.639, + `admin/buttons/entity-shell/event-detail/events-table/event-listing/event-create/identity/tables/escalacao/payments/marketplace.css`, todos linkados em `Pages/_Host.cshtml`); services extraidos e agora **cobertos por teste** (Ciclo 22, +174 testes).
- **Pagamento real + taxa: implementado e revisado.** Modelo = **intermediacao automatica** (site recebe o total na chave Pix central -> webhook confirma -> Envio de Pix automatico do valor base pra chave do organizador, retendo a taxa). Taxa **FIXA**: R$0,50 (plataforma) + R$0,25 (gateway) por cima do valor da partida. Payout com retry/backoff/idempotencia deterministica + alerta admin em falha. Guarda-corpo: bloqueia cobranca se o grupo nao tem chave Pix de repasse. `serviceFeePercentage` (legacy percentual) removido de ponta a ponta.
  - Config: `Fee` = `{ Enabled:true, AppFeeFixed:0.50, GatewayFeeFixed:0.25, SupportedGateways:["EfiBank"], ShowDirectPixToOrganizer:false }`.
  - `GroupPayoutAccount` guarda a chave Pix do organizador (cadastro manual pelo admin do grupo).
- **Login Google (criar-ou-vincular): implementado.** Login externo ja vinculado entra direto; email existente vincula (`AddLoginAsync`); email novo cria conta com `EmailConfirmed=true`. Email real (SMTP + fallback) e confirmacao por link implementados. (Depende do Robson gerar as credenciais OAuth em prod -- ver secao Google OAuth.)
- **Seguranca**: webhooks autenticados (AbacatePay HMAC timing-safe, BTCPay secret timing-safe, EfiBank mTLS por client-cert configuravel); authz admin 17/17 paginas + teste de convencao; CSP com nonce + X-Frame-Options/nosniff/HSTS; credenciais Efi removidas do `appsettings.json` (placeholders `__SET_VIA_USER_SECRETS__`; `IsEnabled` ignora placeholders).
- **Migration de gateways**: default `EnablePaymentGateways=true` so para grupos NOVOS (o `UPDATE` que ligava grupos existentes foi removido -- evita quebra silenciosa no deploy).

---

## Pendencias & Roadmap

### Pendencias do Robson (fora do codigo)
- **Rotacionar o ClientSecret Efi** no painel (o valor antigo ficou no historico do git) e reconfigurar via user-secrets (dev) / env no EasyPanel (prod). **Adiado** -- Robson viajando, sem acesso ao painel Efi.
- **Validar o Envio de Pix Efi em homologacao** (credenciais + certificado .p12 -- ver Troubleshooting; solucao base64 disponivel). Confirmar limites de envio de Pix.
- **Confirmar com contador** a nota fiscal sobre a taxa de servico (o dinheiro passa pela conta do site = intermediacao).
- **Gerar credenciais Google OAuth** em prod (ver secao dedicada) + credenciais SMTP/provedor de email.
- Confirmar `SyncPassword=false` em producao; confirmar mTLS do webhook Efi ativo em prod.

### Candidatos a proximos ciclos (Senior planeja quando priorizado)
- **Login Google em producao** (apos Robson gerar OAuth Client) + testes dos fluxos de login.
- **Mobile UX critico** (prioridade do Robson): fluxos entrar-em-grupo / ver-eventos / confirmar-presenca / pagar Pix/BTC / consultar comprovante / estados vazios.
- **Cobertura crescente** de testes nos demais services; E2E mobile (Playwright 375px/768px/desktop).
- **WhatsApp real + baseline operacional por gateway** -- POSTERGADO (aguardando ideias de outro dev).
- Pen-test financeiro, revisao de CSP, metricas SignalR, alertas operacionais -- antes de producao.
- Redis: so quando houver multi-instancia ou gargalo medido (nao agora).

---

## Linha do Tempo dos Ciclos (todolist / historico condensado)

Historico enxuto. Cada linha: ciclo, entrega, PR e veredito da review. Detalhes antigos foram comprimidos; o backlog vivo esta na "VARREDURA SENIOR" e nas secoes de plano/review dos ciclos recentes (20-22) abaixo.

| Ciclo | Tema | Entrega (resumo) | PRs | Estado |
|---|---|---|---|---|
| 1-14 | Fundacao + docs + CSS vars | Consolidacao de docs (README/ROADMAP/CONTRIBUTING), fix de testes, refatoracao de services (`IDbContextFactory`, authz admin, indices), migracao completa de cores para CSS vars (`:root`), encoding UTF-8, refresh token/sessao persistente. | #6-#50 | Concluidos e revisados |
| 15 | Testes + UX grupos privados + `!important` | Cobertura de 5 services, UX de grupos privados, auditoria de `!important`. | #53 | Concluido |
| 16 | Mobile UX (critico) | Consolidacao do menu/CSS mobile, prioridade mobile estabelecida. | #53 | Concluido |
| 17 | Login Google + email real + mojibake + menu mobile | Criar-ou-vincular Google, SMTP+confirmacao de email, fix de caracteres especiais. | #56, #57 | Concluido |
| Pagamento Real + Taxa | Intermediacao automatica + taxa fixa | `GroupPayoutAccount`, `FeeOptions`, `EfiBankPixPayoutService`, webhook, checkout, relatorio admin. | #58 (plano), #60 (impl), #62 (review) | APROVADO c/ 4 bloqueadores |
| 18 | TDD+SOLID+CSS + 4 bloqueadores | Fix dos 4 bloqueadores financeiros: `PayoutRetryService`+backoff+alerta, guarda-corpo no checkout, payload Efi corrigido, `txId` idempotente. | #61 (plano), #63 (impl), #64 (review) | APROVADO (EXCELENTE) |
| 19 | UX mobile + toggles + gateways default | Badge "Voce", toggle de votacao MVP com cascata, hint Maps. Risco de migration corrigido (nao auto-ligar grupos existentes). | #65 (impl), #66 (review), #67 (fix migration) | APROVADO |
| Varredura Senior | Auditoria 12 eixos + impl P0-P3 | Backlog P0-P3; secret Efi removido do repo, `GroupFeatureRules`+testes, sync-over-async eliminado, `AsNoTracking`, 68 warnings nullable zerados. | #68 (backlog), #69 (impl) | Entregue |
| 20 | SOLID + CSS modular + remover legacy | 7 code-behinds reduzidos (`AdminPayments` 1046->666 etc.), `admin/escalacao/payments.css` extraidos, `serviceFeePercentage` removido. | #70 (plano), #71 (impl), #72 (review) | APROVADO |
| 21 | Fechar refatoracao (code-behinds + CSS + higiene) | 0 code-behind > 250 LOC; `site.css`/`events.css` ao core; 0 warning de analisador nos testes. Ressalva: services extraidos sem teste. | #73 (plano), #74 (impl), #75 (review) | APROVADO c/ 1 ressalva |
| 22 | Cobrir services extraidos com teste | 12/12 services/formatters com teste dedicado (+174 testes; 1950->2124). 0 diff de producao. Senior corrigiu 7 warnings CS8625 em `ProfileServiceTests`. | #76 (plano), #77 (impl), #78 (review) | APROVADO |

> As secoes detalhadas de **plano** e **review** dos Ciclos 20, 21 e 22 seguem logo abaixo (mantidas na integra por serem recentes). Ciclos anteriores foram condensados nesta tabela.

---

# Detalhes dos Ciclos Recentes (planos + reviews na integra)

---

## Review Senior do Ciclo 22 (PR #77) -- APROVADO

Auditoria da PR #77 (`test/ciclo22-service-tests`, ja na `main`). Ciclo de testes entregue: **12/12** services/formatters alvo agora tem arquivo de teste dedicado, com casos significativos (feliz + ramos), nao smoke test. **+174 testes** (1950 -> 2124 verdes); as 24 falhas seguem `ProgramConfigurationTests` sem Postgres (ambiente). **0 diff de codigo de producao** (verificado: so arquivos em `Confirmai.Tests/` mudaram) -- respeitou a regra "ciclo so de testes".

Cobertura por alvo: `GroupDetailService` 23, `MailboxFormatter` 28, `SummaryAgeTracker` 18, `EscalacaoTextFormatter` 17, `AdminPaymentsCommandService` 15, `GroupPaymentsService` 14, `PokerCreateService` 12, `FutsalCreateService` 11, `AdminLogsExportCommandService` 10, `ReconciliationHealthService` 10, `AdminUsersQueryService` 9, `ProfileService` 7. Padrao correto: DB in-memory via `TestDbContextFactory` + Moq so onde necessario (UserManager/auth/JS).

**Correcao do Senior nesta review**: o `ProfileServiceTests` reintroduziu **7 warnings CS8625** (nulls do construtor de `UserManager`), violando a meta "0 warning". Corrigido com `null!` (padrao ja usado no projeto). Projeto de testes volta a **0 warning**.

**Pendente do Robson (inalterado)**: rotacao do ClientSecret Efi no painel + reconfigurar credenciais via user-secrets/env (adiado -- sem acesso ao painel em viagem).

Com isso, a divida de refatoracao dos Ciclos 20/21 esta **fechada** (code-behinds < 250 LOC, CSS modular, services extraidos e agora cobertos por teste).

---

---

## Ciclo 22 (Pleno) -- Cobrir com testes os services extraidos (fechar divida de cobertura) [EXECUTADO -- ver review acima]

**Objetivo**: fechar a ressalva do Ciclo 21 -- os services extraidos nos Ciclos 20/21 nao ganharam teste. Este ciclo e SO de testes: **nenhuma mudanca de codigo de producao** (se um service estiver dificil de testar, isso e sinal de refactor -- registrar e trazer pro Senior, nao mudar comportamento as escondidas). Regra de ouro do TDD segue: teste de caracterizacao que fixa o comportamento ATUAL.

### Services/formatters SEM teste (alvo do ciclo)

Com DB (usar `TestDbContextFactory.CreateInMemoryFactory` + `IDbContextFactory`, padrao ja usado em `GroupFeaturesServiceTests`):
- `GroupDetailService`, `GroupPaymentsService`, `FutsalCreateService`, `PokerCreateService`, `ProfileService`, `AdminUsersQueryService`, `ReconciliationHealthService`.

Com dependencias externas (mockar via Moq -- gateway/JS/auth), sem DB ou com DB in-memory:
- `AdminPaymentsCommandService` (delega a reconciliation/status/query -- mockar essas deps e assertar orquestracao/mensagens de retorno), `AdminLogsExportCommandService`.

Puros (teste unitario simples, sem DB nem mock):
- `EscalacaoTextFormatter`, `MailboxFormatter`, `SummaryAgeTracker`.

### Procedimento por service
1. Criar `Confirmai.Tests/<Service>Tests.cs` seguindo o padrao de `GroupFeaturesServiceTests` (helper `Setup()` com `TestDbContextFactory.CreateInMemoryFactory($"prefix-{Guid.NewGuid()}")`, seed via `factory.CreateDbContext()`).
2. Cobrir os caminhos publicos: caso feliz + principais ramos (nao encontrado, vazio, permissao/guarda, erro esperado). Formatters: cobrir formatacao de cada caso/edge (nulo, vazio, plural/singular, datas).
3. Assertar o **comportamento atual** (caracterizacao) -- se algo parecer bug, NAO "consertar" aqui; registrar como achado pro Senior.
4. Build 0 warnings + suite verde a cada arquivo; commits pequenos (1 service por commit, ou agrupamento coeso).

### Meta de saida
- Os 12 tipos acima com arquivo de teste dedicado e casos significativos (feliz + ramos), nao so smoke test.
- Contagem total de testes sobe de forma relevante (referencia: hoje 1974; cada service deve somar varios testes).
- 0 warning de analisador nos testes novos (seguir o padrao ja limpo do projeto).
- Build do app inalterado (0 diffs em codigo de producao).

### O que NAO fazer
- Nao alterar codigo de producao (nem "pequenos ajustes") -- ciclo e so de teste.
- Nao mascarar comportamento: se um teste revela bug, deixa o teste refletir o real e abre achado; nao adapta o teste pra passar escondendo problema.
- Nao mockar o que da pra testar de verdade com DB in-memory (preferir integracao leve via `TestDbContextFactory`).
- Nao tocar na rotacao do secret Efi (pendencia do Robson).

---

## Review Senior do Ciclo 21 (PR #74) -- APROVADO com 1 ressalva de processo

Auditoria da PR #74 (`refactor/ciclo21-codebehinds-css-higiene`, ja na `main`). Build do app **0 warnings**; build do projeto de testes **0 warnings**; **1950** testes verdes / 1974 -- as 24 falhas continuam sendo `ProgramConfigurationTests` sem Postgres (ambiente, nao regressao). As 3 fases foram entregues, sem mudanca de regra de negocio.

**Fase 1 (code-behinds / SRP) -- META ATINGIDA**: **0 code-behind `.razor.cs` acima de 250 LOC** (era 16). Novos services extraidos e **todos registrados em DI** (`Program.cs`): `AdminPaymentsCommandService`, `AdminLogsExportCommandService`, `AdminUsersQueryService`, `ReconciliationHealthService`, `GroupDetailService`, `GroupPaymentsService`, `FutsalCreateService`, `PokerCreateService`, `ProfileService` + formatters (`EscalacaoTextFormatter`, `MailboxFormatter`). Verificado: **0 uso de `AppDbContext` direto** nos services novos (usam `IDbContextFactory` ou delegam a services que ja o usam).

**Fase 2 (CSS modular) -- META ATINGIDA**: `site.css` 5.589 -> 1.615 e `events.css` 3.679 -> 1.639. Dominios extraidos: `buttons.css`, `entity-shell.css`, `event-detail.css`, `events-table.css`, `event-listing.css`, `event-create.css`, `identity.css`, `tables.css` (+ `admin`/`escalacao`/`payments`/`marketplace` dos ciclos anteriores). Todos com `<link>` em `Pages/_Host.cshtml`; `site.css` tem preload+stylesheet (intencional). Sem duplicacao de regra base (os `.entity-shell` que restam em `site.css` sao overrides `body`-prefixed de maior especificidade, nao a base).

**Fase 3 (higiene) -- META ATINGIDA**: projeto de testes agora com **0 warning** de analisador (xUnit1012/1026/2000, BL0005, CA2022 zerados) + warnings CS do app zerados.

**RESSALVA (processo, nao-bloqueante)**: a Fase 1 pedia **teste unitario por service extraido**, mas o ciclo **nao adicionou nenhum arquivo de teste novo** para os ~11 services/formatters extraidos (total de testes caiu 1976 -> 1974). A extracao foi "mover codigo" (baixo risco, suite existente cobre o comportamento via os fluxos originais), entao nao ha regressao -- mas a divida de cobertura desses services segue aberta. **Fechada no Ciclo 22** (ver review acima).

**Observacao menor**: comentario em `_Host.cshtml` ainda diz "site.css ~125 KB" (desatualizado apos a modularizacao) -- ajustar quando tocar no arquivo.

---

## Ciclo 21 (Pleno) -- Fechar toda a refatoracao restante (code-behinds + CSS + higiene) [EXECUTADO -- ver review acima]

**Objetivo**: concluir a divida tecnica de refatoracao aberta na varredura Senior. Ciclo MAIOR, mas executado em **incrementos pequenos (1 assunto por commit/PR)**. Regra de ouro do Ciclo 18 continua valendo: **teste de caracterizacao ANTES de refatorar**, comportamento identico, `IDbContextFactory` (nunca `AppDbContext` direto), 0 warning no app, suite verde. **Nenhuma mudanca de regra de negocio.**

Meta de saida do ciclo:
- **nenhum** code-behind `.razor.cs` acima de ~250 LOC (hoje ha 16 acima de 250; ver lista);
- `site.css` e `events.css` reduzidos a um "core" pequeno (layout/base/vars), com os dominios em arquivos dedicados;
- breakpoint padronizado em **768px**;
- projeto de testes com **0 warning de analisador**.

### Fase 1 -- Finalizar extracao de code-behinds (P2.6)

Estado atual (`.razor.cs` > 300 LOC): `AdminPayments` 666, `AdminLogs` 529, `Payment` 426, `Escalacao` 391, `Mailbox` 387, `Groups/Detail` 380, `EventPayment` 372, `Futsal/Create` 329, `Groups/Payments` 311. (Total > 250 LOC: 16 arquivos.)

Procedimento por arquivo (o mesmo do Ciclo 20, que funcionou):
1. Teste de caracterizacao dos fluxos publicos ANTES.
2. Mover a logica de dominio/consulta/acao para um service testavel via DI + `IDbContextFactory`; o code-behind fica so com estado de UI + orquestracao.
3. Reusar services ja existentes quando houver (ex.: `AdminPaymentsQueryService`/`AdminPaymentsSummaryService`, `EventPaymentService`) em vez de criar novos.
4. Build 0 warnings + suite verde; 1 PR por arquivo (ou extracao coesa).

Ordem sugerida: `AdminPayments` (terminar -- extrair as acoes restantes que ainda vivem no code-behind), depois `AdminLogs`, `Payment`, `Groups/Detail`, `Groups/Payments`, `Futsal/Create`, e o restante da lista dos 16 ate todos ficarem < ~250 LOC.

Criterio de aceitacao Fase 1: 0 code-behind > ~250 LOC; cada service extraido com teste unitario; 0 `AppDbContext` direto novo; 0 sync-over-async.

### Fase 2 -- Modularizar o resto do CSS por dominio (P1.4 + P3.11)

Estado atual: `site.css` 5.589 linhas, `events.css` 3.679 (`admin`/`escalacao`/`payments`/`marketplace` ja extraidos). Continuar a mesma tecnica **incremental** (1 dominio por commit, so mover regras -- nao reescrever -- e verificacao visual em 375/768/desktop antes do proximo).

Mapa de extracao sugerido (a partir dos comentarios de secao ja existentes):
- de `site.css`: `buttons.css` (BUTTON SYSTEM / variants / size-shape), `forms.css` (form surfaces/cards/filter-bar), `oldsite.css` (oldsite-page layout, header emblems, language flags, world selection), `marketplace.css` (final market polish -- consolidar com o `marketplace.css` existente, sem duplicar), `entity-shell.css` (entity-shell base) e manter em `site.css` so `:root`/vars + base global + acessibilidade.
- de `events.css`: `event-detail.css` (detail shell/header/info chips/quorum/sections/players list/zebra/slots), `event-admin.css` (admin bar, toggles pago/pendente, add/remove vaga, confirmacoes inline), `events-table.css` (events table group join/detail + visibility classes), mantendo em `events.css` so loading/skeleton + base.

Regras:
1. So mover; nao alterar valores. Confirmar `<link>` no `Pages/_Host.cshtml` a cada arquivo novo.
2. Padronizar breakpoint **768px** nos blocos tocados (ha 1 ocorrencia de `700px` restante + historicos).
3. Refino dos poucos `!important` (site.css=4, events.css=2) so DEPOIS, caso a caso, no arquivo ja modularizado.
4. Corrigir de passagem o mojibake nos comentarios de secao do CSS (ex.: `â€”`) dos blocos que forem movidos -- so nos comentarios, nao mexe em regra.

Criterio de aceitacao Fase 2: `site.css`/`events.css` reduzidos ao core; 0 duplicacao de regra entre core e novos arquivos (somas de linhas batem); nenhuma regressao visual nos 3 breakpoints; breakpoint unico 768px nos blocos tocados.

### Fase 3 -- Higiene final (P3.9 residual + P3.11)

- Zerar os ~7 warnings de analisador do projeto de testes (xUnit1012/1026/2000, BL0005, CA2022) -- ajustes pontuais, sem mascarar falha real.
- Revisar os `catch {}` e `!important` remanescentes caso a caso quando tocar nos arquivos.

Criterio de aceitacao Fase 3: `dotnet build` do projeto de testes com 0 warning.

### O que NAO fazer
- Nao mexer em regra de negocio de pagamento/payout/escalacao -- so mover/extrair.
- Nao introduzir Redis (P3.12) nem novas features.
- Nao tocar na rotacao do secret Efi (pendencia do Robson).
- Nao fazer big-bang: tudo incremental, 1 assunto por commit; parar e pedir review se algum passo exigir mudar comportamento.

---

---

## Review Senior do Ciclo 20 (PR #71) -- APROVADO

Auditoria da PR #71 (`refactor/ciclo20-tdd-solid-css`, ja na `main`). Build do app **0 warnings**; **1952** testes verdes (+57 novos) / 1976 -- as 24 falhas continuam sendo `ProgramConfigurationTests` sem Postgres (ambiente, nao regressao). Os 3 blocos do plano foram entregues sem mudanca de regra de negocio.

**Bloco A (code-behinds / SRP)** -- 7 code-behinds reduzidos extraindo logica para services testaveis via `IDbContextFactory` + DI (todos registrados em `Program.cs:126-132`):
- `AdminPayments.razor.cs` 1046 -> 666 (extraiu `SummaryAgeTracker` e usa `AdminPaymentsQueryService`/`AdminPaymentsSummaryService`);
- `Features.razor.cs` 528 -> 247 (`GroupFeaturesService`, reusa `GroupFeatureRules` do #69 -- cascata preservada);
- `Mailbox.razor.cs` 614 -> 387 (`MailboxQueryService`); `Payment.razor.cs` 562 -> 426 (`PaymentInitializationService`+`PaymentCommandService`); `EventPayment.razor.cs` 476 -> 372 (`EventPaymentService`); `Escalacao.razor.cs` 471 -> 391 (`EscalacaoService`); `Detail.razor.cs` -> 279 (`EventDetailService`); `AdminLogs` (`BuildExportRowsAsync` -> `AdminLogsQueryService`).
- Novos services com teste unitario: `EscalacaoServiceTests`, `EventDetailServiceTests`, `GroupFeaturesServiceTests`, `MailboxQueryServiceTests`, `PaymentCommandServiceTests`.

**Bloco B (CSS modular)** -- extracao sem duplicacao e com `<link>` registrado em `Pages/_Host.cshtml:33-35`:
- `admin.css` (1514) extraido de `site.css` (7096 -> 5589);
- `escalacao.css` (797) + `payments.css` (537) extraidos de `events.css` (5013 -> 3679).
- Somas batem (mover, nao reescrever): 1514 = delta site; 797+537 = 1334 = delta events.

**Bloco C (`serviceFeePercentage` legacy)** -- removido de ponta a ponta: parametro fora de `IEventPaymentGateway.CreateChargeAsync` + 3 gateways + `EfiBankPixService` (incl. stubs `CreateSplitAsync`/`LinkChargeToSplitAsync` que so lancavam `NotImplementedException`), `record EventPaymentFeeCalculation(ChargeAmount)`, `EventPaymentService`. O valor cobrado nao muda (o percentual era derivado e nunca usado no corpo das chamadas) -- coberto por `EventPaymentChargeCalculatorTests`.

**Observacoes nao-bloqueantes (proximo ciclo)**:
- `AdminPayments.razor.cs` ainda em 666 LOC -- reducao boa, mas acima do alvo ~250; continuar a extracao das acoes restantes num proximo passo.
- Projeto de testes ainda tem ~7 warnings de analisador (xUnit1012/1026/2000, BL0005, CA2022) -- pre-existentes, nao introduzidos aqui; limpar em ciclo de higiene.

**Pendente do Robson (inalterado)**: rotacao do ClientSecret Efi no painel + reconfigurar credenciais via user-secrets/env (adiado -- sem acesso ao painel em viagem).

---

---

## Ciclo 20 (Pleno) -- Refatoracao SOLID + CSS modular + limpeza de legado [EXECUTADO -- ver review acima]

**Objetivo**: continuar os itens P1-P3 da varredura que ficaram para ciclo dedicado. Regra de ouro do Ciclo 18 continua valendo: **teste de caracterizacao ANTES de refatorar**, comportamento identico, mudancas incrementais (1 assunto por commit). Nenhuma mudanca de regra de negocio.

### Bloco A -- Refatoracao dos code-behinds grandes (P2.6, SRP)

Alvos por tamanho (LOC): `AdminPayments.razor.cs` (1.046), `Mailbox.razor.cs` (614), `Payment.razor.cs` (562), `Features.razor.cs` (528, ja parcialmente reduzido pela extracao de `GroupFeatureRules`), `EventPayment.razor.cs` (476), `Escalacao.razor.cs` (471).

Procedimento por arquivo:
1. Escrever/rodar testes de caracterizacao cobrindo os fluxos publicos do code-behind (o que ele carrega, salva, valida) ANTES de mexer.
2. Extrair a logica que NAO e de UI para um service testavel (padrao ja usado: `GroupFeatureRules`, `GroupMetricsService`, `EventPaymentService`). O code-behind deve ficar so com estado de UI + orquestracao de chamadas ao service.
3. Injetar o service via DI (`builder.Services.AddScoped<...>`), nunca instanciar `AppDbContext` direto -- usar `IDbContextFactory`.
4. Rodar build (0 warnings) + toda a suite; comportamento deve permanecer identico.
5. **Prioridade**: comecar pelo `AdminPayments.razor.cs` (maior e com mais logica de query/acao) -- extrair query/filtros/acoes para services (`AdminPaymentsQueryService`/`AdminPaymentsSummaryService` ja existem; mover o resto pra la). 1 PR por arquivo (ou por extracao coesa) para facilitar review.

Criterios de aceitacao do Bloco A:
- cada code-behind refatorado perde a logica de dominio (fica < ~250 LOC quando viavel);
- services extraidos tem teste unitario;
- 0 uso novo de `AppDbContext` direto; 0 sync-over-async;
- suite verde, 0 warning no app.

### Bloco B -- Modularizacao CSS incremental (P1.4 + P3.11)

Alvos: `wwwroot/css/site.css` (7.096 linhas) e `wwwroot/css/events.css` (5.013).

Procedimento (MUITO incremental, para nao quebrar layout):
1. Mapear blocos por dominio dentro do arquivo monolitico (ex.: futsal, poker, grupos, admin, pagamento, componentes comuns).
2. Extrair **um dominio por commit** para um arquivo dedicado (ex.: `css/futsal.css`), registrar no carregamento e **verificar visualmente** (o proprio Pleno abrindo as telas afetadas em 375px/768px/desktop) antes do proximo.
3. Nao mudar valores/regras na extracao -- so mover. Refino de `!important` (29 ocorrencias) e feito DEPOIS, caso a caso, so nos arquivos ja modularizados.
4. Padronizar breakpoint unico em **768px** (ha inconsistencia historica 700 vs 768) conforme R3 -- mas so nos blocos que estiver tocando.

Criterios de aceitacao do Bloco B:
- nenhum arquivo novo introduz regressao visual (checagem nos 3 breakpoints);
- `site.css`/`events.css` reduzidos a medida que dominios saem;
- sem duplicacao de regra entre monolito e novo arquivo.

### Bloco C -- Remocao do `serviceFeePercentage` legacy (P3.10)

Contexto: a taxa e FIXA (R$0,50 + R$0,25). `serviceFeePercentage` sobrou como parametro derivado do total em `IEventPaymentGateway.CreateChargeAsync` e implementacoes (`EfiBank`/`Abacate`/`Appmax`), alem de `EventPaymentService`/`EventPaymentChargeCalculator`.

Procedimento (cuidado -- interface publica de gateway):
1. Confirmar por testes que o valor cobrado depende SO da taxa fixa (ja coberto por `EventPaymentChargeCalculatorTests`).
2. Remover o parametro/propriedade `serviceFeePercentage` da interface `IEventPaymentGateway` e propagar a remocao nas 3 implementacoes + chamadas.
3. Ajustar/limpar os testes que passam o parametro.
4. build 0 warnings + suite verde.

Criterio de aceitacao do Bloco C: `serviceFeePercentage` nao existe mais no codigo (0 ocorrencias) e o valor cobrado permanece identico nos testes.

### O que NAO fazer neste ciclo
- Nao mexer em regra de negocio de pagamento/payout (so mover/extrair codigo).
- Nao introduzir Redis (P3.12 -- so quando houver multi-instancia/gargalo medido).
- Nao tocar na rotacao do secret Efi (pendencia do Robson).
- Nao fazer big-bang: refatoracao e CSS sao incrementais, 1 assunto por commit/PR.

---

# Referencia permanente

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

### Status da varredura (atualizado)

Itens ja RESOLVIDOS pelo Senior nas PRs #68 (doc) e #69 (codigo):

- **P0.1 (parcial)** -- credenciais Efi removidas do `appsettings.json` (viraram `__SET_VIA_USER_SECRETS__`); `EfiBankOptions.IsEnabled` passa a ignorar placeholders. **Pendente do Robson**: rotacionar o ClientSecret no painel Efi (adiado -- sem acesso ao painel em viagem; fica pro ciclo posterior).
- **P1.2** -- cert path hardcoded trocado por placeholder no `appsettings.json`.
- **P1.3 (parcial)** -- cascata do toggle de gateways extraida para `GroupFeatureRules` + 4 testes; guarda-corpo do checkout ja tinha teste. **Restante**: teste do toggle `EnableBestPlayerVoting` isolado (ver ciclo abaixo).
- **P2.5** -- sync-over-async eliminado no `AppDbContext.SaveChanges` (helpers sincronos dedicados).
- **P2.7 (parcial)** -- `AsNoTracking` aplicado em `GroupMetricsService` e `AdminRevenueReportService`. **Restante**: demais paginas/servicos read-only.
- **P3.9** -- 68 warnings de nullable nos testes zerados.

PENDENTE do Robson (ciclo posterior): **P0.1 rotacao do ClientSecret Efi** + reconfigurar credenciais reais via user-secrets (dev) / env no EasyPanel (prod).

---

---

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
