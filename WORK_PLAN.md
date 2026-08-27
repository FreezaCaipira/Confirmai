# Plano de Trabalho - Confirmai

> **Documento unico e vivo do projeto.** Serve simultaneamente como: (1) canal de comunicacao Senior <-> Pleno; (2) todolist / linha do tempo do desenvolvimento; (3) manual de como o Pleno deve codar, agir e se comunicar; (4) memoria de contexto -- se a sessao do Senior for resetada, este arquivo permite recuperar TUDO que e necessario para continuar. **Comece pela secao "Deveres do Senior x Deveres do Pleno"** -- ela define quem executa o que.
>
> **Base atual**: `main` pos-Ciclo 28 | **Testes**: 2290 verdes / 2314 (as 24 falhas sao `ProgramConfigurationTests` sem Postgres local = ambiente, NAO regressao) | **Build**: app e testes com **0 warning / 0 erro**.
> **Stack**: .NET 9 (STS -> migrar p/ .NET 10 LTS quando lancar), Blazor Server, EF Core, PostgreSQL, ASP.NET Identity, SignalR, xUnit+Moq, OpenTelemetry/Serilog. Gateways de pagamento: EfiBank (Pix, ativo), AbacatePay, Appmax, BTCPayServer.

---

## Como ler este documento (para um contexto novo apos reset)

Ordem de leitura recomendada quando o contexto do Senior e reiniciado:
0. **Deveres do Senior x Deveres do Pleno** (logo abaixo) -- **ler primeiro**: define quem executa o que. Se um prompt novo comecar rodando build/suite completa durante uma review, esta violando esta secao.
1. **Mapa de Progresso e Proximos Passos** (abaixo) -- progresso por eixo + a fila ordenada do que vem depois. Esta e a visao rapida de "onde estamos".
2. **Estado Atual do Projeto** (abaixo) -- detalhe tecnico do que esta pronto.
3. **Pendencias & Roadmap** (abaixo) -- o que falta, o que depende do Robson.
4. **Linha do Tempo dos Ciclos** (abaixo) -- historico condensado de cada ciclo, PR e veredito da review.
5. **Manual do Pleno** + **Regras para o Pleno (OBRIGATORIO)** -- como o codigo deve ser escrito.
6. **Regras de Comunicacao Senior <-> Pleno** -- como registrar ciclos e reviews.
7. Secoes de referencia (Setup de Ambiente, Politica .NET, CSS Vars, Comandos de Validacao, Troubleshooting Efi) -- consultar sob demanda.

---

## Deveres do Senior x Deveres do Pleno (LER ANTES DE QUALQUER COISA)

> Esta secao existe para que **qualquer prompt/contexto futuro** execute exatamente como operamos hoje. Ela tem precedencia sobre habitos anteriores: se uma instrucao antiga sugerir que o Senior rode a suite completa, vale o que esta aqui.

**Principio**: o **Pleno EXECUTA** (codigo, teste, build, evidencia). O **Senior ORGANIZA, DIRECIONA, ORIENTA e REVISA**. Processamento pesado (build, suite completa, servidor, navegador) fica **do lado do Pleno**; se migrar pro Senior, a cota evapora em tarefa que nao e de revisao.

### Papeis
- **Robson (dono do produto)**: levanta requisitos/bugs/melhorias testando o app (isso NAO e scope creep -- ver regra 22), define prioridades e decisoes de negocio, e responsavel por acoes fora do codigo (rotacionar secrets, credenciais OAuth, fiscal/juridico, deploy no EasyPanel). Chama o Senior para planejar ciclo ou revisar entrega. Merge dos PRs e dele.
- **Senior (revisor/planejador/orientador)**: audita a `main`, planeja ciclos, revisa a entrega do Pleno, escreve planos e reviews AQUI no WORK_PLAN, responde questionamentos do Pleno, abre PR de documentacao e de correcao pontual de baixo risco.
- **Pleno (executor)**: executa o ciclo descrito no WORK_PLAN com TDD, escreve testes, roda build e suite completa, entrega **1 PR por ciclo**. O Pleno **nao tem acesso direto aos PRs no GitHub** -- por isso ciclo e review precisam estar 100% explicitos AQUI.

### Deveres do PLENO (executa)
1. Implementar as fases do ciclo **na ordem** descrita no plano, 1 assunto por commit, com **TDD** (teste antes/junto).
2. **Rodar `dotnet build --no-incremental`** e garantir **0 warning / 0 erro**.
3. **Rodar a suite completa (`dotnet test`)** -- e o Pleno, nunca o Senior. Classificar falhas: as 24 `ProgramConfigurationTests` sem PostgreSQL local sao **ambientais**; qualquer outra vermelha impede abrir o PR.
4. **Colar os numeros no corpo do PR** (regra 28): linha de warnings/erros do build + linha final do `dotnet test` (`Failed/Passed/Total`) + o que e ambiental. Sem isso o ciclo **nao esta entregue**.
5. Verificar visualmente mudancas de UI/CSS e listar no PR quais telas verificou (regra 26).
6. Aplicar i18n (regra 25) e as regras de CSS (mobile-first, 768px, CSS vars) no mesmo incremento.
7. Registrar achados, duvidas e bloqueios **escrevendo no WORK_PLAN** (secao "Problemas Encontrados pelo Pleno"), sem consertar fora do escopo do ciclo.
8. Entregar o PR com title + body + link de criacao (regra 27).

### Deveres do SENIOR (organiza, direciona, orienta, revisa)
1. **Planejar o ciclo**: objetivo, fases numeradas com criterio de aceitacao, meta mensuravel, "o que NAO fazer", arquivos/services alvo.
2. **Revisar a entrega lendo o diff**: arquitetura, SOLID, **caminho do dinheiro**, seguranca/autorizacao, migrations e reversibilidade, i18n, aderencia ao plano, higiene (catch mudo, endpoint aberto, artefato commitado).
3. **Confiar nos numeros do PR + no CI** para build/testes. Rodar, no maximo, `dotnet test --filter "FullyQualifiedName~<Area>"` quando precisar **provar** um furo especifico, e `dotnet build` quando ele mesmo alterar codigo na PR de review.
4. **Escrever a review no WORK_PLAN** com veredito no titulo (`APROVADO` / `APROVADO com ressalva` / `REPROVADO`), resultado **por fase**, ressalvas (bloqueante vs. nao-bloqueante) e o que vai pro proximo ciclo.
5. **Corrigir na PR de review** apenas o que e pequeno e critico (seguranca, dinheiro, warning); o resto vira fase do proximo ciclo.
6. **Planejar o ciclo seguinte** a partir das ressalvas, no mesmo PR de review.
7. Verificar o **CI** do proprio PR e nunca afirmar "mergeado" sem fonte autoritativa.
8. Reportar ao Robson: veredito, link do PR, status real do CI, pendencias dele.

### O que o SENIOR NAO faz
- **Nao roda a suite completa** (~2.300 testes) para revisar -- isso e do Pleno (regra 28).
- Nao sobe app/Postgres/navegador para E2E por conta propria; **E2E so quando o Robson pedir explicitamente**.
- Nao implementa feature grande, nao refatora em massa, nao "termina o ciclo" pelo Pleno.
- Nao muda regra de negocio ja ratificada sem decisao do Robson.
- Nao devolve review vaga: sem numeros no PR, **devolve pedindo os numeros** em vez de executar a suite.

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
- **i18n sempre (nova regra -- ver regra 25)**: toda string visivel ao usuario passa pelo `UiTextService`/`@Ui[...]` (chave por dominio). Proibido texto hardcoded em markup `.razor` ou em mensagens de retorno de service. i18n faz parte do "done" do incremento, junto com o TDD.
- **Qualidade minima por incremento**: `dotnet build` 0 warning + suite verde (filtrando os testes ambientais de Postgres). Sem `Console.Write`/debug commitado. Sem string de UI hardcoded nova.
- **Quem roda build e teste (regra 28)**: o **Pleno** roda `dotnet build --no-incremental` e a **suite completa** antes de abrir o PR e **cola os numeros no corpo do PR** (`0 Warning(s)`, `Failed/Passed/Total`, quais falhas sao ambientais). O **Senior nao repete a suite completa** na review -- ele revisa diff/arquitetura/seguranca/dinheiro e confia nesses numeros + no CI. Suite completa rodada no lado do Senior queima cota que deveria ir para revisao e direcionamento.

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
- **Sem os numeros de build/teste no PR, o ciclo nao esta entregue** (regras 27 + 28): o Senior devolve o PR pedindo os numeros em vez de rodar a suite por conta propria.

---

## Regras de Comunicacao Senior <-> Pleno (via WORK_PLAN)

Como o Pleno nao ve os PRs no GitHub, o WORK_PLAN e a fonte de verdade. Todo ciclo e toda review devem ficar **explicitos e completos** aqui.

### Ao PLANEJAR um ciclo (Senior escreve)
Criar uma secao `## Ciclo N (Pleno) -- <titulo>` contendo:
- **Objetivo** (1-2 linhas) e por que o ciclo existe.
- **Fases** numeradas, cada uma com procedimento e **criterio de aceitacao**.
- **Meta de saida** mensuravel (ex.: "0 code-behind > 250 LOC", "+X testes", "0 warning").
- **Evidencia exigida na entrega**: numeros de build + suite completa colados no PR (regra 28) -- e o Pleno que executa.
- **O que NAO fazer** (limites do escopo).
- Referencia aos alvos concretos (arquivos, services).

### Ao REVISAR um ciclo (Senior escreve)
Criar uma secao `## Review Senior do Ciclo N (PR #XX) -- <VEREDITO>` contendo, de forma **explicita** (foi pedido pelo Pleno):
- **Veredito** no titulo: `APROVADO`, `APROVADO com ressalva`, ou `REPROVADO`.
- **PR e branch** auditados + confirmacao de que ja esta na `main` (ou nao).
- **Evidencias**: build (warnings) e contagem de testes antes/depois **conforme reportado pelo Pleno no PR** + status do CI, separando falhas ambientais de regressao. O Senior **nao** roda a suite completa (regra 28); se precisar provar um furo, roda apenas `dotnet test --filter` da area.
- **Por fase**: o que foi entregue vs. o que o plano pedia (META ATINGIDA / PARCIAL / FALTOU).
- **Ressalvas** (bloqueante vs. nao-bloqueante) e o que fica pro proximo ciclo.
- **Pendencias do Robson** repetidas (para nao se perderem).

### Marcacao de estado
- Ao concluir um ciclo, marcar o plano com `[EXECUTADO -- ver review]` e adicionar a entrada na "Linha do Tempo".
- Nunca declarar um PR "mergeado" sem fonte autoritativa (`git_view_pr` / historico da `main`).

---

## Mapa de Progresso e Proximos Passos (atualizado pos-Ciclo 29)

### Progresso por eixo

| Eixo | Estado | O que falta |
|---|---|---|
| Refatoracao estrutural (SOLID, code-behinds, CSS modular) | **CONCLUIDO** (C20-C22) | Nada. Manutencao natural. |
| Cobertura de testes | **BOM** -- 2315 verdes (C29, com Postgres local disponivel), +350 testes desde o C21 | Sem teste de **render** das telas -- **decidido pular** (C29 Fase E): a logica esta em helpers/services cobertos; bUnit nao entra sem necessidade. |
| i18n (regra 25) | **CONCLUIDO** (C25-C29) -- ~450 strings migradas, PT/EN/ES, teste anti-hardcode com/sem acento validado empiricamente, **allowlist zerada** e formatos de data i18n-aware | Nada aberto. Manutencao: feature nova nasce com i18n (o teste barra). |
| Pagamento V1 manual (jogador) | **CONCLUIDO** -- grupo nasce manual, Pix do organizador + QR com valor total, comprovante, confirmacao do organizador, rejeicao com motivo | Validacao em navegador e feita **manualmente pelo Robson** (F12/mobile). Nao ha gravacao/E2E automatizado do caminho do dinheiro. |
| Taxa da plataforma R$ 0,75 (futsal, modo manual) | **CONCLUIDO** -- snapshot no pagamento, taxa discriminada (`15,00 + 0,75 = 15,75`) inclusive no QR, ledger por partida, residuo por valor | Idem. |
| Repasse organizador -> plataforma | **CONCLUIDO** -- aba do organizador (selecao explicita de partidas, somatorio, Pix da plataforma, comprovante, historico), fila do admin (confirmar/rejeitar com motivo), endpoint do comprovante autorizado, sem autoquitacao | Idem. |
| Consolidacao tecnica do repasse | **CONCLUIDO** (C29) -- `DelinquencyService` morto removido, `PlatformFeeCoverage` extraido, allowlist i18n zerada, FK `Restrict` avaliada, cobertura de render decidida | Nada. A divida tecnica aberta na review do C28 esta encerrada. |
| Seguranca | **BOM** -- webhooks autenticados, authz admin 17/17 + teste de convencao, CSP/HSTS, secret Efi fora do repo, teste que impede endpoint de seed/debug aberto, autorizacao do repasse no service | Pen-test financeiro antes de producao; pendencias de prod do Robson. |
| Login/identidade | **Implementado** (Google criar-ou-vincular, SMTP, confirmacao de email) | **Nunca exercitado de fato.** Decisao do Robson: validar **direto em producao** (Ciclo 30). Bloqueado no OAuth Client de prod + env vars no EasyPanel. |
| Mobile UX | **Em andamento, validado pelo Robson tela por tela** (F12/emulacao mobile) -- navegacao, estados vazios (`NoGroupsHint`), UX do Profile refeita | Continuar o modelo atual: Robson aponta a tela, o ciclo corrige. **Nao** transformar em varredura sistematica sem ele pedir. |
| Pix automatico (V2) | **Codigo pronto e preservado atras do toggle** | Bloqueado nas pendencias do Robson (rotacao Efi, homologacao, nota fiscal). |
| Observabilidade / operacao | Serilog + OpenTelemetry + alerta de payout | Metricas SignalR, alertas operacionais, revisao de CSP -- antes de producao. |
| E2E automatizado em navegador | **NUNCA EXECUTADO** pelo Senior/Pleno (2 tentativas cairam por cota) | Nao e bloqueio: o Robson valida manualmente. Fica como reforco opcional do caminho do dinheiro. |
| WhatsApp | **NAO INICIADO** -- ultima feature do escopo | Decisao pendente do Robson: **Cloud API oficial** (Business verificado + numero dedicado + templates aprovados) vs. **deep link `wa.me`** (sem custo/aprovacao, usuario clica pra enviar). O esforco muda radicalmente. |

### Proximos passos, em ordem

**O V1 esta funcionalmente completo e a divida tecnica do repasse esta encerrada (C29).** Daqui pra frente o caminho critico nao e mais codigo de feature -- e **operacao**: o que falta depende de credenciais/decisoes do Robson.

1. **Mergear a PR de review do C29** (fix de canonicalizacao do idioma + review + este mapa).
2. **Ciclo 30 -- Login Google + email validados em PRODUCAO** (ja planejado abaixo; decisao do Robson). **Bloqueado no Robson**: OAuth Client de prod + `ClientId`/`ClientSecret`/SMTP como env vars no EasyPanel. Sem isso o ciclo nao comeca -- e o unico item que separa o app de aceitar usuario novo por Google.
3. **Ciclo 31 -- WhatsApp** (ultima feature do escopo): **bloqueado** no nome da ferramenta que o Robson recebeu de indicacao. Nao planejar antes -- provider de terceiro muda custo, lock-in, quem e o numero remetente e para onde vao os contatos dos jogadores.
4. **Ciclo 32 -- pre-producao**: revisao de CSP, metricas SignalR/alertas, checklist de deploy (EasyPanel), `SyncPassword=false`, mTLS do webhook, pen-test financeiro do caminho manual.
5. **V2 (Pix automatico)** -- so depois do go-live do V1 e das pendencias Efi/fiscal do Robson.

**Se o C30 e o C31 seguirem bloqueados**, o unico ciclo de codigo executavel agora e o **C32 (pre-producao)** -- ele nao depende de ninguem e e pre-requisito de go-live de qualquer forma. E a recomendacao do Senior enquanto as credenciais nao chegarem.

**Fora da fila**: E2E automatizado em navegador (opcional -- o Robson valida manualmente); varredura mobile sistematica (o modelo atual, tela por tela apontada pelo Robson, esta funcionando); Redis (so com multi-instancia ou gargalo medido); .NET 10 (quando o LTS sair).

### Google OAuth e email: onde validar

**Decisao do Robson: validar tudo direto em PRODUCAO** (Google Auth + email real). Justificativa: o deploy no EasyPanel e simples, o dominio real com HTTPS e exatamente o que o Google espera (some o atrito do `localhost`), e o app ainda nao tem base de usuarios. Dev com `localhost`/catcher local fica disponivel como alternativa, mas nao e o caminho escolhido.

O que isso exige e como reduzir o risco:

1. **Credenciais**: `ClientId`/`ClientSecret` e SMTP **somente como env vars no EasyPanel**, nunca em `appsettings*.json` (regra ja vigente desde a Efi).
2. **Consent screen**: usamos apenas escopos basicos (`email`, `profile`), portanto publicar dispensa verificacao da Google -- mas exige URL de politica de privacidade. Em modo *Testing* so os emails cadastrados como test users conseguem logar.
3. **Risco principal nao e a credencial, e o `criar-ou-vincular` contra dados reais.** A logica atual: login externo ja vinculado entra; email existente **vincula** (`AddLoginAsync`); email novo cria conta com `EmailConfirmed=true`. Um bug ai nao aparece como erro -- ele funde contas ou da acesso a conta errada, e em banco de producao isso e irreversivel. **Protocolo obrigatorio**: exercitar os 3 caminhos com a conta do Robson + uma conta de teste **antes** de divulgar o login a qualquer usuario.
4. **Email real**: manter o volume baixo no inicio e conferir SPF/DKIM/DMARC do dominio -- dominio novo sem esses registros cai em spam e queima reputacao de envio. Testar o link de confirmacao com o email do Robson primeiro.
5. **Rollback**: qualquer problema no fluxo de login = desligar o botao do Google (configuracao) e voltar ao login por email/senha, que ja esta em uso.

---

## Estado Atual do Projeto (pos-Ciclo 29)

- **Refatoracao estrutural: CONCLUIDA.** Code-behinds todos < 250 LOC; CSS modularizado por dominio (`site.css` 5.589->1.615, `events.css` 3.679->1.639, + `admin/buttons/entity-shell/event-detail/events-table/event-listing/event-create/identity/tables/escalacao/payments/marketplace.css`, todos linkados em `Pages/_Host.cshtml`); services extraidos e agora **cobertos por teste** (Ciclo 22, +174 testes).
- **Pagamento real + taxa: implementado e revisado.** Modelo = **intermediacao automatica** (site recebe o total na chave Pix central -> webhook confirma -> Envio de Pix automatico do valor base pra chave do organizador, retendo a taxa). Taxa **FIXA**: R$0,50 (plataforma) + R$0,25 (gateway) por cima do valor da partida. Payout com retry/backoff/idempotencia deterministica + alerta admin em falha. Guarda-corpo: bloqueia cobranca se o grupo nao tem chave Pix de repasse. `serviceFeePercentage` (legacy percentual) removido de ponta a ponta.
  - Config: `Fee` = `{ Enabled:true, AppFeeFixed:0.50, GatewayFeeFixed:0.25, SupportedGateways:["EfiBank"], ShowDirectPixToOrganizer:false }`.
  - `GroupPayoutAccount` guarda a chave Pix do organizador (cadastro manual pelo admin do grupo).
- **Login Google (criar-ou-vincular): implementado.** Login externo ja vinculado entra direto; email existente vincula (`AddLoginAsync`); email novo cria conta com `EmailConfirmed=true`. Email real (SMTP + fallback) e confirmacao por link implementados. (Depende do Robson gerar as credenciais OAuth em prod -- ver secao Google OAuth.)
- **Seguranca**: webhooks autenticados (AbacatePay HMAC timing-safe, BTCPay secret timing-safe, EfiBank mTLS por client-cert configuravel); authz admin 17/17 paginas + teste de convencao; CSP com nonce + X-Frame-Options/nosniff/HSTS; credenciais Efi removidas do `appsettings.json` (placeholders `__SET_VIA_USER_SECRETS__`; `IsEnabled` ignora placeholders).
- **Migration de gateways**: default `EnablePaymentGateways=true` so para grupos NOVOS (o `UPDATE` que ligava grupos existentes foi removido -- evita quebra silenciosa no deploy).
- **Estrategia de pagamento V1 vs V2 (decisao Robson, Ciclo 23)**: o **V1 (go-live) usa o fluxo MANUAL** -- jogador paga na chave Pix de um admin do grupo, envia comprovante, organizador confirma. O **Pix automatico** (gateway + `PayoutService`/`EfiBankPixPayoutService` + taxa) fica como **V2**, atras do toggle `EnablePaymentGateways`, **codigo preservado** (nada removido). O Ciclo 23 vai inverter o default para manual em grupos novos (**Robson confirmou** o flip). Motivo: simplicidade para lancar logo; a complexidade do Pix automatico + rotacao/homologacao Efi vira desenvolvimento V2.

---

## Pendencias & Roadmap

> **Antes de qualquer go-live**: ver "CHECKLIST OBRIGATORIO DE RESET PRE-PRODUCAO REAL" no fim
> deste documento. O ambiente atual (`confirmai.m2gpju.easypanel.host`) e de teste, com
> credenciais conscientemente expostas -- todas precisam ser rotacionadas antes da producao real.

### Pendencias do Robson (fora do codigo)
- **Rotacionar o ClientSecret Efi** no painel (o valor antigo ficou no historico do git) e reconfigurar via user-secrets (dev) / env no EasyPanel (prod). **Adiado** -- Robson viajando, sem acesso ao painel Efi.
- **Validar o Envio de Pix Efi em homologacao** (credenciais + certificado .p12 -- ver Troubleshooting; solucao base64 disponivel). Confirmar limites de envio de Pix.
- **Confirmar com contador** a nota fiscal sobre a taxa de servico (o dinheiro passa pela conta do site = intermediacao).
- **Gerar o OAuth Client de PROD** (redirect `https://<dominio>/signin-google`) + publicar a consent screen (escopos basicos email/profile dispensam verificacao; exige URL de politica de privacidade) e configurar `ClientId`/`ClientSecret` + SMTP como **env vars no EasyPanel** -- desbloqueia o Ciclo 30. Client de dev nao e mais necessario (decisao: validar direto em prod).
- **Decidir o modelo do WhatsApp**: Cloud API oficial (Meta) vs. deep link `wa.me` vs. **uma ferramenta de terceiro recomendada a ele** (nome a confirmar) -- desbloqueia o Ciclo 31. Nao planejar o ciclo antes dessa informacao: o desenho muda completamente (provider externo exige avaliar custo, lock-in, dados de contato saindo da plataforma e se o numero e da plataforma ou do organizador).
- Confirmar `SyncPassword=false` em producao; confirmar mTLS do webhook Efi ativo em prod.
- (SMTP/provedor de email em prod: **nao bloqueia dev** -- em dev usa-se catcher local ou Gmail App Password.)

### Candidatos a proximos ciclos (Senior planeja quando priorizado)
- **Login Google + email em PROD** (Ciclo 30) -- ver secao "Google OAuth e email: onde validar".
- **WhatsApp** (Ciclo 31, ultima feature do escopo) -- sai do status POSTERGADO; aguarda apenas a decisao Cloud API vs. `wa.me`.
- **Mobile UX**: segue no modelo incremental (Robson testa com F12, aponta a tela, o ciclo corrige) -- nao ha ciclo de varredura planejado.
- **Cobertura crescente** de testes nos demais services; E2E automatizado (Playwright 375/768/desktop) como reforco opcional.
- Pen-test financeiro, revisao de CSP, metricas SignalR, alertas operacionais -- antes de producao.
- Redis: so quando houver multi-instancia ou gargalo medido (nao agora).

### Aparato Geral do Senior (pos-Ciclo 23) -- backlog priorizado para o Ciclo 24 [HISTORICO -- em grande parte JA ENTREGUE nos Ciclos 24-28; a fila viva esta no "Mapa de Progresso e Proximos Passos"]

Varredura Senior focada em melhorias/adicoes, **separando codigo (Pleno/Senior) das pendencias do Robson**. Base: `main` @ `cb6c89a`.

**P1 -- alto valor para o go-live V1 (fluxo manual como default):**
- **i18n ainda incompleto (regra 25)**: ~185 strings acentuadas hardcoded em `Pages/**/*.razor` contra apenas ~35 usos de `@Ui[...]` -> i18n esta em ~15-20%. Continuar a migracao priorizando os fluxos mobile principais restantes: `Pages/Payment/EventPayment.razor` (+ componentes), `Pages/Groups/Detail.razor`, `Pages/Groups/Payments.razor`, `Pages/MyEvents/Index.razor`. Meta incremental por tela; PT-BR baseline. Alvo: derrubar os 185 para <50 no fim do ciclo.
- **UX do pagamento V1 quando gateways OFF (default agora)**: em `Pages/Payment/EventPayment.razor:164-193`, com `!groupGatewaysEnabled` ainda renderiza o seletor `EventPaymentGateways` ACIMA do Pix manual -- no V1 manual isso confunde. Esconder o bloco de gateways quando desabilitado e exibir so `EventPaymentPixAdmin` + `EventPaymentProof`. + teste.
- **Cobertura do fluxo manual V1 ponta a ponta**: e o fluxo do dinheiro no V1. Teste de integracao do caminho manual: exibicao da chave/QR do organizador, upload de comprovante (`PixProofUploadService`), e confirmacao do organizador em `/grupo/{id}/pagamentos` (`AdminMarkPaid`). Caracterizar estados (sem comprovante / comprovante enviado / confirmado).

**P2 -- qualidade / observabilidade / UX:**
- **`catch {}` que engolem tudo**: `Pages/Admin/Admin.razor.cs:125,127`, `Pages/Groups/Detail.razor.cs:61,82` e os `catch { }` de modal JS em `Pages/Admin/AdminPayments.razor.cs:95,168,235` -> logar via `ILogger` em vez de silenciar. **Manter** os `JSDisconnectedException`/`OperationCanceledException`/`TaskCanceledException` no dispose/navegacao (esses sao aceitaveis).
- **Estados vazios padronizados**: reaproveitar o padrao do `NoGroupsHint` para os vazios de `Pages/MyEvents/Index.razor` (o plano do C23 citou `/meus-eventos` e ficou de fora), pagamentos e ranking -- sempre com CTA.
- **Consistencia de idioma no `UiTextService`**: ha chaves ja em ES/EN misturadas com PT-BR (ex.: `AuthTexts.cs:232`, `UtilityTexts.cs:333-334,484-486` em espanhol) -> hoje o baseline esta inconsistente. Decidir: (a) manter so PT-BR e normalizar as chaves espanholas para PT, ou (b) assumir multi-idioma de verdade e completar. Recomendo (a) para o V1.

**P3 -- higiene / futuro:**
- **Teste de convencao anti-hardcode i18n** nas telas-alvo (proposto no C23, nao feito): falhar se aparecer literal acentuado em `.razor` dos fluxos principais.
- **WhatsApp** (`Services/Events/EventNotificationService.cs:194` TODO) -- promovido a Ciclo 31 (ultima feature do escopo).
- E2E mobile (Playwright 375/768/desktop) dos fluxos principais -- quando priorizado.

**Pendencias do Robson (fora do codigo) -- NAO sao do proximo ciclo de codigo:**
- Rotacionar ClientSecret Efi + reconfigurar credenciais (so afeta o **V2**).
- Validar Envio de Pix Efi em homologacao + limites (V2). Nota fiscal da taxa com contador (V2).
- Gerar OAuth Google prod + SMTP/provedor de email prod.
- Confirmar `SyncPassword=false` e mTLS do webhook Efi em prod.

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
| 23 | UX navegacao + Pagamento V1 manual + i18n | Botao Grupos na row do Voltar (Partidas/Features), `NoGroupsHint` reutilizavel nos estados vazios (+botao), fix do link Pix (`/perfil`->`/profile/{id}`), default `EnablePaymentGateways=false` p/ grupos novos (migration so-default, sem UPDATE), i18n dos fluxos principais via `UiTextService` + regra 25. Senior fechou a ressalva na #81 (+3 testes). | #79 (plano), #80 (impl), #81 (review + testes) | APROVADO |

| 24 | Pagamento V1 usavel + i18n do pagamento + cobertura do fluxo manual | Seletor de gateways escondido quando OFF (`ShouldShowGateways`/`ShouldShowManualPix` extraidos e testados); i18n do fluxo de pagamento migrada (`PaymentTexts`); +6 testes de logica do ramo (fluxo manual ponta a ponta ja coberto por `PixManualPaymentFlowTests`). 2127->2133. | #83 (plano), #84 (impl), #85 (review) | APROVADO |

| 25 | i18n completo (zerar debito tecnico) | ~450 strings migradas em ~70 arquivos `.razor` (Futsal/Poker/Groups/Admin/Payment/Venue/Profile/Docs); 4 dominios novos (`FutsalTexts`, `PokerTexts`, `GroupTexts`, `UtilityTexts`) + `AdminTexts`/`CoreTexts`/`PaymentTexts` estendidos, PT-BR/EN-US/ES-ES; +32 testes de completude/paridade de chaves; extras: fix NullRef em `BuildPixStaticPayload`, aviso "Pix nao configurado", padronizacao `btn-view-groups`, cascata CSS `detail-admin-btn`, remocao de debug write em teste. 2133->2165. | #86 (plano+impl), #87 (review) | APROVADO c/ ressalvas |

| 26 | Taxa acumulada p/ repasse manual + UX Profile + privacidade do Pix + fechar i18n | Fases 0/1/2/4/5 entregues (privacidade do Pix, UX do Profile + deep link `?intent=pix`, `PlatformFeeAmount` + `PlatformFeeLedgerService` + migration, breakdown `Partida + Taxa = Total`, Pix como pre-requisito no `FutsalCreateService`); Fase 3 so **backend** (services sem UI/endpoint/FIFO); Fase 6 **nao entregue** (so paridade de chaves de novo). 2165->2223. Senior corrigiu 3 itens (QR cobrando valor errado, autorizacao ausente, `catch{}` mudo). | #88 (impl), #89 (review) | APROVADO PARCIAL -- Fase 3 incompleta, Fase 6 nao entregue |

| 27 | Fechar a Fase 3 (UI do repasse) + Fase 6 (anti-hardcode) | 6 fases entregues: (A) UI do repasse lado organizador em `Payments.razor` (aba "Taxa da plataforma", lista por partida, somatório, Pix da plataforma via config + QR, enviar comprovante, histórico); (B) UI do repasse lado admin em `AdminRevenue.razor` (grupos com saldo, fila de lotes, confirmar/rejeitar com motivo); (C) endpoint `GET /api/fee-settlement-proof/{id}` com autorização extraída para `PlatformFeeSettlementProofAuthorizer` + 10 testes de autorização; (D) baixa FIFO por partida em `PlatformFeeLedgerService.GetGroupFeeBreakdownByMatchAsync` + 10 testes; (E) teste anti-hardcode de verdade (`AntiHardcodeI18nTests` varre 143 `.razor`, allowlist explícita) + 7 residuais migrados + removido `if (enUs.Count == 0) return;` do `I18nKeyParityTests`; (F) limpeza C26: `eval`→`site.js` função nomeada, `StateHasChanged` após `_highlightPix`, 11 mensagens de service via `UiTextService`, indentação `Program.cs`. 2223→2261 (+38). | #90 (impl), #91 (review) | APROVADO c/ ressalvas -- Senior removeu endpoint de seed aberto e fechou 3 furos financeiros |

| 28 | Fechar as ressalvas do C27 | 5 fases entregues: (A) `AntiHardcodeI18nTests` agora detecta tambem palavra PT **sem** acento (lista de ~150 palavras) -- validado empiricamente pelo Senior; (B) `Shared/Components/**` migrado, allowlist de ~24 entradas reduzida a 2 (formato de data com "as") + marca; (C) CSV `SelectedEventIds` -> tabela `PlatformFeeSettlementItem` com FK + indice unico + migration com **backfill** e `Down` reversivel; (D) `PlatformFeeSelectionState` (helper puro) + rejeicao exige motivo **no service**; (E) higiene do `Payments` (`catch` loga, metodos duplicados unificados). Extras achados pelo Pleno testando: `MarkPaidAsync` nao carimbava a taxa (repasse nunca fechava pela aba Comprovantes) e telas do admin mostravam R$ 15,00 enquanto o jogador pagava R$ 15,75. 2261->2287 (+26). | #92 (impl), #93 (review) | APROVADO -- Senior fechou 1 furo de residuo de taxa |

| 29 | Consolidacao pos-repasse | 3 fases de codigo + 2 decisoes: (A) `DelinquencyService` (codigo morto, logica duplicada) **removido** do DI e do repo, records movidos para `Services/Groups/PaymentRecords.cs`, precedido de 7 testes de caracterizacao no `GroupPaymentsService`; (B) `GetCoveredFeeByEventAsync` extraido do ledger para `PlatformFeeCoverage` (query identica, +5 testes diretos), os dois services passam a depender dele; (C) `ResidualAllowlist` **zerada** -- os 2 formatos de data com "as" viraram `DateTimeFull`/`DateTimeFullLong` i18n-aware no `UiTextService` (PT/EN/ES + `CultureInfo` do idioma no weekday); (D) exclusao de partida: nao existe fluxo (`Events.Remove` inexistente) -> decisao registrada, FK `Restrict` fica defensiva; (E) cobertura de render: bUnit **pulado** (logica ja em helpers/services cobertos). 2287->2315 (com Postgres local disponivel; +12 novos, -11 removidos). Senior corrigiu a canonicalizacao do codigo de idioma (`?uiLang=en-us` dava texto EN com data PT). | #94 (impl), #95 (review) | APROVADO |

> As secoes detalhadas de **plano** e **review** dos Ciclos 20, 21 e 22 seguem logo abaixo (mantidas na integra por serem recentes). Ciclos anteriores foram condensados nesta tabela.

---

# Detalhes dos Ciclos Recentes (planos + reviews na integra)

---

## Review Senior do Ciclo 29 (PR #94, mergeada na `main`) -- APROVADO

**Escopo revisado**: commits `b9ae3a5`..`858b919` (3 commits, 20 arquivos, +467/-793), mergeados via `64c0d99`.

**Build (numeros do Pleno, regra 28 cumprida)**: `dotnet build --no-incremental` -> **0 warning / 0 error**.
**Testes (numeros do Pleno)**: `Failed: 0, Passed: 2315, Total: 2315`. Nesta execucao o Postgres local estava disponivel, entao as 24 falhas ambientais historicas (`ProgramConfigurationTests`) passaram. Liquido: +12 testes novos (7 caracterizacao + 5 `PlatformFeeCoverage`), -11 removidos com o `DelinquencyServiceTests`.
**O Senior nao rodou a suite completa** (regra 28); rodei apenas `--filter LanguagePreferenceServiceTests` para provar o achado abaixo (9 verdes).

### Resultado por fase

| Fase | Veredito | Observacao |
|---|---|---|
| A -- resolver o `DelinquencyService` | **OK, caminho (b), justificado** | Service removido do repo e do DI; records movidos para `Services/Groups/PaymentRecords.cs` (namespace `Confirmai.Services.Groups`). Conferi que nao sobrou referencia viva (`grep`: so comentario, `WORK_PLAN` e `Tests/README`). O ponto que importava -- a armadilha do `enablePaymentGateways = false` **default** -- morreu junto: `GroupPaymentsService.LoadPaymentsDataAsync` le `group.EnablePaymentGateways` do proprio grupo, nao ha parametro para esquecer. Os 7 testes de caracterizacao foram escritos **antes** da remocao (eventos futuros, confirmacoes pagas, preco zero/nulo, ordenacao por total, nao-membros, historico so manual, ordenacao do historico) -- ordem correta. |
| B -- extrair `PlatformFeeCoverage` | **OK** | `Services/Payment/PlatformFeeCoverage.cs` (static, 1 metodo, 1 responsabilidade); ledger e settlement service agora dependem dele em vez de um chamar o `internal static` do outro. **A query e byte-a-byte a mesma** (mesmo filtro `Status == Pago`, mesmo `GroupBy`, mesmo `AsNoTracking`) -- a regra financeira do residual nao mudou, que era a condicao da fase. +5 testes diretos (sem settlement, so aprovados contam, multiplos lotes no mesmo evento, separacao por grupo, separacao por evento). |
| C -- zerar allowlist i18n | **OK** | `ResidualAllowlist` de fato vazia (so comentario). `UiTextService.FormatDateTime` ganhou `DateTimeFull` e `DateTimeFullLong` com padrao por idioma (PT `'às'` / EN `'at'` + `MM/dd` / ES `'a las'`) e, no caso `FullLong`, `CultureInfo` do idioma para o nome do dia da semana -- o `new CultureInfo("pt-BR")` cravado no `EventPaymentHeader` saiu. 3 `.razor` migrados. A regra 25 agora nao tem excecao declarada. |
| D -- exclusao de partida com repasse | **OK -- decisao aceita** | Conferi de novo: nao existe `Events.Remove` em nenhum fluxo. Nada a implementar; a FK `Restrict` fica como guarda defensiva. Registrado como decisao, e o certo (implementar mensagem de erro para um fluxo que nao existe seria codigo morto novo). |
| E -- cobertura de render | **OK -- decisao aceita, com 1 correcao de registro** | Pular bUnit e a decisao correta (dependencia nova sem necessidade; a logica esta em `PlatformFeeSelectionState`/`PlatformFeeCoverage`/ledger/settlement, todos cobertos). **Ressalva de processo**: o PR diz "confirmada pelo Robson", e o Robson nunca opinou sobre bUnit -- o que ele confirmou foi que **E2E/validacao visual e feita por ele, manualmente**. O Pleno nao deve atribuir confirmacao ao Robson por inferencia (regra 16: duvida de desenho vira pergunta no WORK_PLAN). Decisao mantida, atribuicao corrigida. |

### Correcao aplicada pelo Senior nesta review

**P2 -- o formato de data novo caia para PT-BR quando o idioma vinha com casing diferente.** `FormatDateTime` decide o padrao com `_language.SelectedLanguage switch { "en-US" => ..., "es-ES" => ..., _ => PT }` -- comparacao **case-sensitive**. Mas `SelectedLanguage` guarda o valor **como o caller mandou**: `LanguagePreferenceService.SetLanguage` validava contra um `HashSet` `OrdinalIgnoreCase` e atribuia a string original. E os callers reais nao sao controlados -- `MainLayout` le o idioma do **query string `?uiLang=`**, de cookie e do localStorage. Com `?uiLang=en-us` o dicionario de textos (que e `OrdinalIgnoreCase`) devolvia **ingles**, e a data saia `dd/MM/yyyy 'às' HH:mm` -- exatamente a mistura que a Fase C existia para eliminar.
- Fix no lugar certo (a fonte, nao os consumidores): `SetLanguage` agora **canonicaliza** o codigo (`en-us` -> `en-US`) antes de atribuir, entao qualquer comparacao exata a jusante passa a funcionar. +5 testes (4 de canonicalizacao + 1 que formata `DateTimeFull` apos `SetLanguage("en-us")` e espera `03/15/2026 at 14:30`).

### Ressalvas nao bloqueantes (nao viram ciclo)

- **Os padroes de formato de data vivem em `switch` no codigo, nao no dicionario de i18n.** Coerente com os outros `formatKey`s ja existentes e sem string visivel hardcoded em `.razor`, entao a regra 25 esta cumprida -- mas um 4o idioma exigira editar `UiTextService` em vez de so adicionar chaves. Aceitavel enquanto forem 3 idiomas.
- **`PaymentRecords.cs` agrupa 4 records num arquivo** (incluindo `PendingProofEntry`). Segue a convencao de records do projeto; so registrando.
- **Sem cobertura de render** das telas -- segue valendo desde o C27, por decisao.

**Divida tecnica do repasse: encerrada.** As 4 ressalvas abertas na review do C28 estao todas fechadas (codigo morto, cobertura extraida, allowlist zerada, FK avaliada). Nao ha Ciclo de consolidacao pendente.

---

## Review Senior do Ciclo 28 (PR #92, mergeada na `main`) -- APROVADO

**Escopo revisado**: commits `f030cd7`..`cbb6331` (8 commits, 54 arquivos, +3126/-208), mergeados via `b972a75`.

**Build**: `dotnet build --no-incremental` -> **0 warning / 0 error** (meta do ciclo cumprida).
**Testes**: **2287 verdes** (era 2267, +20), 24 falhas = `ProgramConfigurationTests` sem PostgreSQL em `127.0.0.1:5432` -- **ambientais**, mesmas de sempre.

### Resultado por fase

| Fase | Veredito | Observacao |
|---|---|---|
| A -- anti-hardcode alem do acento | **OK, verificado empiricamente** | `PtUnaccentedWords` com ~150 palavras PT comuns + strip de expressoes Blazor (`@(...)`, `@Ui["..."]`, `@Variavel`) antes de avaliar. Nao aceitei so a leitura: injetei `<span>Enviar comprovante</span>` num componente de `Shared` e o teste **falhou apontando arquivo e linha** (143 arquivos / 13.713 linhas varridos); revertido depois. A regra 25 agora tem guarda de verdade. |
| B -- migrar `Shared/Components/**` | **OK** | A allowlist saiu de ~24 entradas de `Shared` para **2** (`EventPayment|as` e `EventPaymentProof|as`, ambas o "as" **dentro de format string de data**, nao texto visivel) + marca (`Confirmai`, `Confirma Ai!`, `Bora jogar!?`). 12 componentes de `Shared` migrados, ~130 chaves novas em `CoreTexts`/`GroupTexts`/`FutsalTexts`/`PokerTexts`/`AdminTexts`. |
| C -- CSV -> tabela | **OK, migration bem feita** | `PlatformFeeSettlementItem` (`SettlementId`, `EventId`, `FeeAmount`) com FK `Cascade` p/ settlement, `Restrict` p/ evento e indice **unico** `(SettlementId, EventId)`. A migration cria a tabela, **faz backfill** do CSV (`string_to_array`/`unnest`, `FeeAmount` = soma de `PlatformFeeAmount` do evento) e **so depois** dropa a coluna -- ordem correta, nada de dado perdido; o `Down` reconstroi o CSV a partir dos itens. Ledger e service passaram a consultar a tabela em vez de parsear texto. |
| D -- cobertura das telas | **OK** | `PlatformFeeSelectionState` (`IsSelectable`, `SelectedAmount`, `CanSubmit`) extraido puro e coberto por 9 testes, com a pagina delegando; rejeicao **sem motivo** agora e recusada **no service** (`ReviewSettlementAsync`) e tambem na UI (defense in depth) -- o certo, ja que a UI nao e fronteira de seguranca. |
| E -- higiene | **OK** | `catch (Exception)` do `ConfirmSettlementSubmit` agora usa `LogError`; `RefreshFeeOverview`/`LoadFeeOverviewAsync` (identicos) unificados. |

### Dois bugs de dinheiro que o Pleno achou testando (fora do plano, com razao)

1. **`MarkPaidAsync` nao carimbava a taxa.** Existem dois caminhos para o organizador confirmar o pagamento do jogador: `AdminConfirmationService.TogglePaidAsync` (que chamava `StampFeeOnPaidAsync`) e `GroupPaymentsService.MarkPaidAsync`, usado pela aba **Comprovantes** de `/grupo/{id}/pagamentos` -- que **nao chamava**. Ou seja: quem confirmava pela aba de comprovantes marcava o jogador como pago e a taxa **nunca entrava no ledger**; a partida nao aparecia na aba "Taxa da plataforma" e o repasse nunca fechava. Corrigido com o mesmo padrao do outro caminho (try/catch com `LogError`, non-blocking mas nunca silencioso) + teste.
2. **Admin via R$ 15,00 onde o jogador pagava R$ 15,75.** `GroupPaymentsService` e `DelinquencyService` montavam `DelinquencyEntry`/`PendingProofEntry`/`PaymentHistoryEntry` com `Event.Price` cru, sem a taxa manual -- o comprovante dizia 15,75 e a tela de revisao dizia 15,00. Agora os tres pontos usam `ManualPlatformFee.TotalToPay(...)`, a mesma fonte do QR e da tela do jogador (+4 testes).

Ambos sao exatamente o tipo de achado que eu quero do Pleno: bug real no caminho do dinheiro, com teste. Tambem entrou um `ManualPlatformFeeFlowE2ETests` costurando a cadeia inteira (comprovante do jogador -> confirmacao -> carimbo da taxa -> lote -> aprovacao -> saldo zerado).

### Correcao aplicada pelo Senior nesta review (PR #93)

**P1 -- taxa residual ficava impossivel de quitar.** Com a selecao explicita, o status por partida era booleano: `EventId` presente em algum lote `Pago` => partida `Pago`. Cenario real: o organizador envia o repasse na quarta cobrindo a partida de segunda; na quinta um **jogador atrasado paga a mesma partida** e o `StampFeeOnPaidAsync` acumula outros R$ 0,75. Resultado antes do fix: a partida aparecia `Pago` (nao selecionavel), mas o saldo agregado do grupo (`accrued - settled`) continuava acusando R$ 0,75 de divida -- **divida visivel que o organizador nao tinha como pagar**, e que o admin cobraria sem contrapartida na tela.
- `GetGroupFeeBreakdownByMatchAsync` passou a comparar **valores**: `covered` = soma do `FeeAmount` dos itens em lotes `Pago`; a partida so e `Pago` quando `covered >= accrued`, senao continua `Pendente` **exibindo o residual**.
- `SubmitSettlementAsync` acompanha: o bloqueio de reenvio agora e (a) partida em lote `EmAnalise` **ou** (b) residual `<= 0`; e o `expected` do `amount` (e o `FeeAmount` do item) passou a ser o **residual**, nao o acumulado -- senao o organizador pagaria a partida duas vezes inteira.
- +3 testes: residual pendente no breakdown, submit do residual aceito, partida totalmente coberta ainda recusada.

### Ressalvas que ficam para o Ciclo 29

- **`DelinquencyService` e codigo morto.** Esta registrado no DI, mas `grep` confirma que **nenhuma pagina ou service o chama** -- a producao usa `GroupPaymentsService`, que tem a mesma logica duplicada. O fix da taxa do item 2 foi aplicado nos dois (correto, por seguranca), mas a duplicacao e a armadilha do parametro `enablePaymentGateways = false` **default** (um futuro caller que esquecer o argumento passa a mostrar taxa em grupo com gateway ligado) pedem consolidacao: ou o service passa a ser usado, ou sai.
- **`internal static GetCoveredFeeByEventAsync` mora no ledger e e usado pelo settlement service.** Foi o menor acoplamento possivel para nao duplicar a regra do residual, mas o lugar natural e um `PlatformFeeCoverage` proprio.
- **FK `Restrict` em `PlatformFeeSettlementItem.EventId`**: hoje nao existe exclusao de `Event` em nenhum fluxo (`grep` por `Events.Remove` nao acha nada), entao nao ha regressao; se um dia entrar "excluir partida", ela vai falhar em partida com repasse -- o que e o comportamento certo, mas precisa de mensagem tratada.
- **Sem teste de UI das telas** (segue valendo do C27): a logica esta em helpers/services testados, os componentes nao tem cobertura de render.

---

## Review Senior do Ciclo 27 (PR #90, mergeada na `main`) -- APROVADO com ressalvas

**Escopo revisado**: commits `e37baf2`..`eb6f704` (7 commits, 53 arquivos, +5629/-67), mergeados via `22d0e5d`.

**Build**: `dotnet build --no-incremental` -> **1 warning** (CS0169, campo `settlementAmount` nunca usado em `Payments.razor.cs`) / 0 error. Meta do ciclo era 0 warning -- corrigido pelo Senior.
**Testes**: **2261 verdes** (era 2223, +38), 24 falhas = `ProgramConfigurationTests` sem PostgreSQL em `127.0.0.1:5432` -- **ambientais**, mesmas de sempre.

### Resultado por fase

| Fase | Veredito | Observacao |
|---|---|---|
| A -- UI do organizador | **OK** | Aba de taxa em `Pages/Groups/Payments.razor` com lista por partida (data, local, pagantes, taxa, status), resumo (acumulado / em analise / repassado / a repassar), Pix da plataforma via `FeeOptions.PlatformPixKey` + QR, selecao de partidas por checkbox, envio de comprovante com modal de confirmacao e historico dos lotes com motivo da rejeicao. **Sem** botao de autoquitacao, como a regra exige. |
| B -- UI do admin do sistema | **OK com lacuna corrigida** | Fila em `AdminRevenue.razor` (grupos com saldo + lotes `EmAnalise` + confirmar/rejeitar com motivo). Lacuna: a lista de grupos vinha de `GroupBy` sobre `PlatformFeeSettlements`, entao **grupo que deve e nunca enviou repasse nao aparecia** -- exatamente o inadimplente que o admin precisa ver. Corrigido pelo Senior (uniao com os grupos que tem taxa acumulada) + teste. |
| C -- Endpoint do comprovante | **OK** | `GET /api/fee-settlement-proof/{id}` com a regra extraida para `PlatformFeeSettlementProofAuthorizer` (testavel): `Unauthorized` sem login, `NotFound` sem imagem, `Forbid` para terceiro, liberado ao submitter / admin do grupo / admin do sistema. 10 testes. |
| D -- Baixa por partida | **ENTREGUE COM DESVIO DE DESENHO (aceito)** | O plano pedia FIFO por valor; o Pleno trocou por **selecao explicita de partidas** (`SelectedEventIds` CSV + migration). Ver analise abaixo -- o desvio e **melhor** que o pedido, mas veio sem validacao server-side, o que abriu 3 furos financeiros. |
| E -- Anti-hardcode i18n | **OK, na terceira tentativa** | `AntiHardcodeI18nTests` varre 143 `.razor` de `Pages/**` e `Shared/**` (conteudo de elemento + `title`/`placeholder`/`alt`/`aria-label`), com allowlist **explicita e comentada** em vez de auto-skip; 7 residuais migrados; o `if (enUs.Count == 0) return;` do `I18nKeyParityTests` foi removido. Ressalva de cobertura abaixo. |
| F -- Limpeza da divida do C26 | **OK** | `eval` -> `ConfirmaiScrollToElement` no `site.js`, `StateHasChanged` apos `_highlightPix`, 11 mensagens de service via `UiTextService`, 5 mensagens de `Profile.razor.cs` migradas, indentacao do `Program.cs` corrigida. |

### Sobre o desvio da Fase D (FIFO -> selecao explicita)

**Aceito e preferivel.** Com FIFO por valor, um lote de R$ 3,00 quita "as 4 partidas mais antigas" por inferencia; com selecao explicita, o organizador marca **quais** partidas esta quitando e o lote guarda os `EventId`s. Fica auditavel, casa com o comprovante e elimina a baixa parcial ambigua (partida coberta pela metade). O Pleno deveria ter registrado a mudanca de desenho como pergunta antes de implementar (regra 16), mas o resultado e o certo -- fica **ratificado**: a regra vigente e selecao explicita, e o `FIFO` sai do vocabulario do projeto.

### Correcoes aplicadas pelo Senior nesta review (PR #91)

**1. P0 -- endpoint de seed anonimo commitado na `main`.** O commit da Fase A deixou `app.MapGet("/api/seed-fee-test", ...)` **no nivel raiz** do `Program.cs` (fora do bloco `if (app.Environment.IsDevelopment() || ...)`), **sem `RequireAuthorization()`**. Ou seja: em producao, qualquer visitante anonimo poderia chamar `/api/seed-fee-test?groupId=N` e (a) carimbar `PlatformFeeAmount` em confirmacoes pagas do grupo e (b) criar um `PlatformFeeSettlement` com `Status = Pago` de R$ 3,00 -- **zerando a divida do grupo com a plataforma sem ninguem pagar nada**. Um comentario `// TEMPORARY: remove after testing` nao e controle de acesso.
   - Endpoint **removido**. Adicionado `NoUnauthenticatedSeedEndpointsTests`, teste de convencao que falha se um endpoint com `seed`/`debug` na rota voltar a ser mapeado fora do bloco de desenvolvimento.

**2. P0 -- o repasse podia quitar mais do que pagava.** `SubmitSettlementAsync` gravava `SelectedEventIds` **sem validar nada**: o `amount` vinha do cliente e nao era conferido contra a taxa das partidas selecionadas. Como a aprovacao do admin marca **todas** as partidas selecionadas como `Pago`, um lote de R$ 0,75 selecionando 10 partidas (R$ 7,50) quitaria as 10 -- perda direta de receita. Tambem era possivel selecionar partidas **de outro grupo** e reenviar as **mesmas partidas** que ja estavam em outro lote (o organizador pagaria duas vezes, ou o admin aprovaria dois lotes cobrindo a mesma divida).
   - `SubmitSettlementAsync` agora exige selecao nao-vazia e valida, server-side: (a) toda partida selecionada pertence ao grupo e tem taxa acumulada; (b) nenhuma delas esta em lote `Pago` ou `EmAnalise`; (c) `amount` == soma das taxas selecionadas. +5 testes.

**3. P1 -- `Due` com duas definicoes diferentes.** No painel do organizador `Due` = soma das taxas das partidas ainda `Pendente`; na fila do admin `Due` = `accrued - settled`. Com a validacao do item 2 as duas convergem, mas o admin continuava sem ver quem deve e nunca enviou nada -- corrigido junto (item da Fase B acima).

**4. Higiene** -- warning CS0169 (`settlementAmount` morto) removido; docstring do ledger que ainda dizia "FIFO settlement allocation" corrigida para descrever a selecao explicita; 8 mensagens PT cruas restantes no `PlatformFeeSettlementService` migradas para `PaymentTexts` (PT/EN/ES) -- a Fase F migrou o `FutsalCreateService` mas deixou este de fora.

### Ressalvas que ficam para o Ciclo 28

- **O teste anti-hardcode so pega literal acentuado.** `AntiHardcodeI18nTests` detecta por caractere acentuado, entao string visivel sem acento (`"Voltar"`, `"Grupos"`, `"Pix"`, `"Total"`) passa livre. E um avanco real (guarda contra o caso comum em PT), mas nao fecha a regra 25. Evolucao natural: heuristica de palavra PT sem acento, ou lista de palavras conhecidas.
- **`Shared/Components/**` foi allowlistado, nao migrado.** A Fase E pedia migrar os 16 residuais de `Shared`; o Pleno migrou 7 de `Pages` e colocou ~24 entradas de `Shared` na allowlist ("C28 target"). A allowlist e honesta e comentada -- aceitavel como divida declarada --, mas a divida existe e precisa encolher no C28.
- **`SelectedEventIds` como CSV em coluna de texto.** Funciona e evita tabela nova, mas nao tem integridade referencial e obriga parsing em memoria. Se o volume crescer, virar tabela `PlatformFeeSettlementItem` (settlementId, eventId, feeAmount) fica melhor -- e permite guardar a taxa por partida no momento do lote.
- **Sem teste de UI dos fluxos novos** (Fase A/B): a logica esta em services testados, mas as duas telas nao tem cobertura de componente. Consistente com o resto do projeto, so registrando.
- **`RejectProofAsync`** (rejeicao do comprovante do jogador, nivel 1) entrou junto no ultimo commit sem estar no plano do ciclo. E coerente com a simetria dos dois niveis e tem valor, mas foi escopo extra.

---

## Review Senior do Ciclo 26 (PR #88, mergeada na `main`) -- APROVADO PARCIAL

**Escopo revisado**: commits `c5d81ae`..`fdbdd87` (8 commits, 39 arquivos, +3567/-66), mergeados via `ac277aa`.

**Build**: `dotnet build --no-incremental` -> **0 warning / 0 error**.
**Testes**: **2223 verdes** (era 2165, +58), 24 falhas = `ProgramConfigurationTests` sem PostgreSQL em `127.0.0.1:5432` -- **ambientais**, mesmas de sempre, nao ha regressao.

### Resultado por fase

| Fase | Veredito | Observacao |
|---|---|---|
| 0 -- Privacidade do Pix | **OK** | `ProfilePixVisibility.ShouldShowPix(isOwnProfile, hasPixKey)` + `IsOwnProfile` no `ProfileContactsDisplay`; guarda tambem a condicao da `<section>` (nao renderiza card vazio p/ visitante). 4 testes. |
| 1 -- UX do Profile | **OK** | Avatar fundido no `ProfileHeaderCard` (o `AvatarUploadSection` solto sumiu), `ProfileEditForm` fora do `<details>`, bloco proprio "Recebimento (Pix)" com ancora `#pix`, e o `?intent=pix` -- que antes era lido e **descartado** -- agora destaca e rola ate o bloco. 4 chaves i18n em PT/EN/ES. |
| 2 -- Ledger da taxa | **OK** | `EventConfirmation.PlatformFeeAmount` (snapshot), `ManualPlatformFeeFixed = 0.75` separado de `AppFeeFixed`/`GatewayFeeFixed` (como o plano exigia), `PlatformFeeLedgerService` com `StampFeeOnPaidAsync` idempotente + `GetGroupBalanceAsync`, migration **so aditiva** (sem `UPDATE` -- licao do C19 respeitada). 10 testes cobrindo poker, gateway ligado, pendente, idempotencia e toggle-back. |
| 3 -- Repasse com comprovante | **INCOMPLETA** | Entregou **so o backend**. Ver abaixo. |
| 4 -- Taxa discriminada | **OK com bug corrigido pelo Senior** | Ver abaixo. |
| 5 -- Pix como pre-requisito | **OK** | `FutsalCreateService.SaveAsync` bloqueia partida com preco em grupo manual sem Pix e devolve link `?intent=pix` (fecha o ciclo com a Fase 1). 4 testes. |
| 6 -- Anti-hardcode i18n | **NAO ENTREGUE** | Ver abaixo. |

### Correcoes aplicadas pelo Senior nesta review (PR #89)

**1. P0 -- o QR cobrava um valor diferente do que a tela mostrava.** A Fase 4 passou a exibir `Partida 15,00 + Taxa 0,75 = Total 15,75` no resumo, mas o `EventPaymentPixAdmin` continuou recebendo `ev.Price!.Value`: tanto o `Price` exibido quanto o `BuildPixStaticPayload(...)` do QR usavam **o preco base**. Resultado pratico: a tela promete R$ 15,75, o QR cobra R$ 15,00, o organizador **nao recebe a taxa** e mesmo assim fica devendo o repasse -- a taxa sairia do bolso dele. Alem disso a mesma condicao (`!gateways && futsal && preco>0 && fee>0`) estava escrita **inline no markup**, sem teste.
   - Extrai `Services/Payment/ManualPlatformFee.cs` (`Applies` / `TotalToPay`), e agora **resumo, valor exibido e payload do QR usam a mesma fonte**. 8 testes novos, incluindo um que confere o campo `54` do BR Code (`540515.75`).

**2. P0 -- `PlatformFeeSettlementService` sem nenhuma autorizacao.** `SubmitSettlementAsync` aceitava qualquer `submittedByUserId` para qualquer `groupId`, e `ReviewSettlementAsync` aceitava **qualquer** `reviewerUserId` -- inclusive o proprio organizador aprovando o proprio repasse, que e exatamente a regra central do plano ("o devedor nao quita a propria divida"). Como ainda nao ha UI, ninguem exercitou o furo; mas a regra tem que morar no service, nao na ausencia de tela.
   - Submit exige `GroupMemberRole.Admin` **naquele grupo**; review exige a role `admin` do sistema (via `db.UserRoles`/`db.Roles`, mesmo criterio do resto do app). +3 testes (`NonAdminMember_IsRejected`, `AdminOfAnotherGroup_IsRejected`, `Organizer_CannotSettleOwnDebt`) e os testes existentes foram corrigidos para semear a autorizacao -- eles estavam **validando o comportamento inseguro**.

**3. P1 -- `catch { }` mudo no caminho do dinheiro.** O `AdminConfirmationService` chamava `StampFeeOnPaidAsync` dentro de `try { } catch { /* best-effort */ }`. Se o carimbo falhar, a taxa daquela confirmacao **nunca mais e cobrada** e nao sobra nem log. Trocado por `LogError`. (E o item P2 "catch{} que engolem excecao" do meu aparato -- aqui aplicado a receita.)

**4. Higiene** -- removido `progress-ciclo26.md` da raiz. O `WORK_PLAN.md` e a fonte unica; docs soltos ja foram removidos nos Ciclos 12/13 e nao devem voltar.

### Ressalvas que ficam para o Ciclo 27

**Fase 3 entregue pela metade.** `PlatformFeeSettlementService` e `PlatformFeeSettlement` existem, estao registrados no DI e tem 8 testes -- mas **nenhuma tela os chama** (`grep` confirma: as unicas referencias fora dos testes sao as duas linhas de `AddScoped` no `Program.cs`). Faltam:
- a aba "Taxa da plataforma" em `Pages/Groups/Payments.razor` (lista por partida, somatorio a repassar, Pix da plataforma, envio do comprovante);
- a fila de revisao em `Pages/Admin/AdminRevenue.razor` (confirmar/rejeitar com visualizacao do comprovante);
- o endpoint `GET /api/fee-settlement-proof/{id}` autorizado (o plano pedia explicitamente, espelhando `/api/pix-proof/{id}`);
- a **baixa FIFO** por partida -- hoje `GetGroupBalanceAsync` so devolve o agregado, entao nao da para dizer quais partidas um repasse quitou, que era o "pagas e pendentes por partida" que o Robson pediu.
Na pratica: **a feature ainda nao existe para o usuario**. O saldo acumula e ninguem consegue ver nem quitar.

**Fase 6 nao entregue -- segunda vez.** O plano pedia um **teste de convencao anti-hardcode** varrendo os `.razor`. O Ciclo 25 entregou paridade de chaves em vez disso; o Ciclo 26 entregou **paridade de chaves de novo** (`I18nKeyParityTests`), que e util mas responde outra pergunta: garante que EN/ES acompanham o PT-BR, e **nao** que a UI parou de ter string crua. Prova: os 8 literais que a review do C25 listou continuam **todos** no lugar (`"Chave Aleatória"`, `"Papéis & Permissões"`, `"Late Reg até ..."`, `"Usuários"`, `"Você"`, placeholders do Poker). Pior: o teste tem `if (enUs.Count == 0) return;` -- os dominios stub (`FutsalTexts`, `GroupTexts`, `PokerTexts`) **desligam o teste sozinhos**, entao ele passa sem verificar nada justamente onde falta traducao.

**Menores** (nao bloqueiam, entram no C27):
- `Profile.razor.cs` usa `JS.InvokeVoidAsync("eval", "...")` para rolar ate a ancora. Funciona, mas `eval` e um cheiro ruim e quebra sob CSP -- deve virar uma funcao nomeada no `site.js`.
- O `_highlightPix = false` acontece em `OnAfterRenderAsync` sem `StateHasChanged`, entao o destaque so some no proximo render espontaneo.
- Strings PT cruas nos servicos novos (`PlatformFeeSettlementService`, mensagem de erro do `FutsalCreateService`) -- a regra 25 tambem vale para mensagem de retorno de service.
- `Program.cs` linhas 124-125 com indentacao fora do padrao do bloco.
- `Profile.razor.cs` ainda tem as 5 mensagens hardcoded que a review do C25 apontou.

---

## Ciclo 32 (Pleno) -- Pre-producao: seguranca operacional, observabilidade e checklist de deploy [PLANEJADO -- EXECUTAVEL AGORA, nao depende do Robson]

**Por que este ciclo e o proximo executavel**: o C30 (Google/email) espera credenciais do Robson e o C31 (WhatsApp) espera a decisao da ferramenta. O C32 nao depende de ninguem e e pre-requisito de go-live de qualquer forma.

**Regra de ouro**: TDD, SOLID, i18n (regra 25), build `--no-incremental` **0 warning**, suite verde, 1 commit por fase, numeros de build/suite colados no PR (regra 28). **Nao** alterar regra financeira, nem o desenho do repasse, nem remover o codigo do V2.

- **Fase A -- pen-test do caminho do dinheiro manual (teste, nao prosa)**: escrever testes **adversariais** dos limites do V1 manual, cada um partindo de "o que um usuario mal-intencionado tentaria": jogador marcando o proprio pagamento como pago; membro comum enviando lote de repasse; organizador aprovando o proprio lote; lote com `amount` adulterado; partida de outro grupo na selecao; comprovante de terceiro via `/api/pix-proof/{id}` e `/api/fee-settlement-proof/{id}`; upload acima do limite / content-type falsificado. Onde o teste passar de primeira, **registrar como coberto**; onde falhar, corrigir no service (nunca so na UI).
- **Fase B -- revisao de CSP e cabecalhos**: auditar a CSP atual (nonce + `X-Frame-Options`/`nosniff`/HSTS) contra o que as paginas realmente carregam (Font Awesome, QR, imagens de comprovante inline). Objetivo: **sem `unsafe-inline`/`unsafe-eval`** e sem console warning. Registrar no PR o que teve que ser liberado e por que.
- **Fase C -- metricas e alertas operacionais**: expor metricas do que da errado em producao sem ninguem ver -- conexoes SignalR ativas/reconexoes, falhas de upload de comprovante, falhas de envio de email, lotes de repasse `EmAnalise` parados ha mais de N dias. Alerta ja existe para payout; alinhar o mesmo padrao.
- **Fase D -- checklist de deploy EasyPanel (documento vivo no repo, nao doc solto na raiz)**: variaveis de ambiente obrigatorias (com o efeito de cada uma faltando), `SyncPassword=false`, mTLS do webhook Efi, `ASPNETCORE_ENVIRONMENT`, aplicacao de migrations, e o **procedimento de rollback**. Um item por linha, verificavel.
- **Fase E -- higiene final pre-go-live**: `grep` por `TODO`/`FIXME` em caminho de producao e classificar (corrigir / virar item de backlog explicito / remover); confirmar que nenhum endpoint de dev/seed/debug esta fora do bloco de desenvolvimento (o teste de convencao ja guarda -- confirmar que continua verde).

### O que NAO fazer

Nao subir dependencia nova sem aprovacao; nao mexer no fluxo do dinheiro do jogador; nao "preparar terreno" para WhatsApp (ferramenta indefinida); nao criar doc solto na raiz.

---

## Ciclo 30 (Pleno + Robson) -- Login Google + email validados em PRODUCAO [PLANEJADO -- BLOQUEADO no Robson]

**Bloqueio**: OAuth Client de prod (redirect `https://<dominio>/signin-google`) + `ClientId`/`ClientSecret`/SMTP como **env vars no EasyPanel**. Enquanto nao existirem, o ciclo nao comeca. Detalhes de risco na secao "Google OAuth e email: onde validar".

**O que e do Pleno (executavel antes das credenciais, sem depender delas)**:
- Cobrir o `criar-ou-vincular` com **teste** nos 3 caminhos, se ainda nao estiver: (a) login externo ja vinculado entra; (b) email existente **vincula** via `AddLoginAsync`; (c) email novo cria conta com `EmailConfirmed=true`. Esse e o codigo que, se errar em prod, **funde contas** -- e irreversivel em banco real.
- Garantir mensagem de erro tratada (i18n) para falha do provider externo e para email ja usado por outro login, em vez de excecao crua.
- Conferir que nenhuma credencial vaza em log (`ClientSecret`, senha SMTP) nem em pagina de erro.

**O que e do Robson (em prod, na ordem)**: criar o OAuth Client -> setar as env vars -> conferir SPF/DKIM/DMARC do dominio -> exercitar os 3 caminhos com a conta dele + uma conta de teste -> so depois divulgar o login. Rollback: desligar o botao do Google e ficar no login por email/senha.

---

## Ciclo 31 (Pleno) -- WhatsApp [NAO PLANEJADO -- aguardando o nome da ferramenta]

Ultima feature do escopo. **Nao sera planejado** antes do Robson informar qual ferramenta foi indicada a ele: o desenho muda completamente entre Cloud API oficial da Meta, deep link `wa.me` e um provider de terceiro (custo, lock-in, contatos dos jogadores saindo da plataforma, numero remetente da plataforma vs. do organizador, templates/aprovacao).

**Trava que entra em qualquer um dos desenhos** (decidida na conversa com o Robson): antes de qualquer envio real, **modo dry-run** (loga a mensagem em vez de enviar) + **allowlist de destinatarios** (so o numero do Robson recebe ate ele liberar). Mensageria quebrada nao tem ctrl+z: manda mensagem errada para o celular de pessoa real, pode duplicar cobranca e, na Cloud API, custa por conversa e gera bloqueio por spam.

---

## Ciclo 29 (Pleno) -- Consolidacao pos-repasse [EXECUTADO -- ver review acima]

**Regra de ouro**: TDD, SOLID, i18n (regra 25), build `--no-incremental` **0 warning**, suite verde, 1 commit por fase. **Nao** reabrir o desenho do repasse (selecao explicita de partidas + residual por valor estao ratificados).

**Novo nesta entrega (regra 28)**: o corpo do PR deve conter, colado do terminal, (a) a linha `0 Warning(s)` / `0 Error(s)` do `dotnet build --no-incremental` e (b) a linha final do `dotnet test` completo (`Failed: N, Passed: N, Total: N`), com as falhas ambientais identificadas. Sem esses dois blocos a review nao comeca -- o Senior nao roda mais a suite completa.

- **Fase A -- resolver o `DelinquencyService`**: hoje e codigo morto com logica duplicada do `GroupPaymentsService`. Escolher **um** dos dois caminhos e justificar no PR: (a) `GroupPaymentsService` passa a delegar nele (removendo a duplicacao e o `default false` do `enablePaymentGateways`, que deve virar parametro obrigatorio), ou (b) o service e removido do DI e do repo, com os testes migrados. Teste de caracterizacao antes de mover qualquer linha.
- **Fase B -- extrair a regra de cobertura da taxa**: `PlatformFeeLedgerService.GetCoveredFeeByEventAsync` e `internal static` e consumida pelo `PlatformFeeSettlementService`. Extrair para um tipo proprio (ex.: `PlatformFeeCoverage`) com testes diretos, e os dois services passam a depender dele.
- **Fase C -- fechar as 2 ultimas entradas da allowlist de i18n**: `EventPayment|as` e `EventPaymentProof|as` sao format strings de data (`"dd/MM 'as' HH:mm"`). Mover o **padrao de formato** para chave de i18n (cada idioma tem o seu) e zerar a `ResidualAllowlist`.
- **Fase D -- exclusao de partida com repasse**: a FK `Restrict` de `PlatformFeeSettlementItem.EventId` faz sentido, mas nao existe fluxo de exclusao de partida. Se/quando entrar, precisa de mensagem tratada em vez de excecao de banco -- **so implementar se o fluxo existir**; senao, registrar como decisao e seguir.
- **Fase E -- cobertura de render das telas do repasse**: avaliar bUnit (o projeto nao tem) em um PR de spike **fechado**, ou entao teste de logica adicional para o que ainda nao esta em helper (ordenacao da lista, formatacao do resumo). Nao introduzir dependencia nova sem aprovacao.

### O que NAO fazer

Nao mexer no fluxo do dinheiro do jogador; nao remover o codigo do V2; nao permitir que o organizador aprove o proprio lote; nao reintroduzir endpoint de seed/debug fora do bloco de desenvolvimento; nao criar doc solto na raiz.

---

## Ciclo 28 (Pleno) -- Fechar as ressalvas do C27 [EXECUTADO -- ver review acima]

**Regra de ouro**: TDD, SOLID, i18n (regra 25), build `--no-incremental` **0 warning**, suite verde, 1 commit por fase. **Nao** reabrir o desenho do repasse (selecao explicita de partidas esta ratificado).

- **Fase A -- anti-hardcode alem do acento**: `AntiHardcodeI18nTests` hoje so detecta literal com caractere acentuado; passar a detectar tambem palavra PT sem acento (lista de palavras comuns: "Voltar", "Grupos", "Total", "Enviar", "Salvar", "Cancelar", "Nome", "Data", "Status"...). O teste deve ficar vermelho antes da migracao.
- **Fase B -- migrar `Shared/Components/**`**: esvaziar as ~24 entradas de `Shared` da allowlist (injetar `UiTextService` nos componentes e criar as chaves). A allowlist final deve conter **so** nomes proprios/marca.
- **Fase C -- `SelectedEventIds` -> tabela**: substituir o CSV por `PlatformFeeSettlementItem` (`SettlementId`, `EventId`, `FeeAmount`) com FK e migration aditiva; guardar a taxa da partida **no momento do lote** (snapshot, mesma logica de `PlatformFeeAmount`). Backfill dos lotes existentes a partir do CSV.
- **Fase D -- cobertura das telas novas**: teste de componente/logica para a aba de taxa (`Payments.razor.cs`) e para a fila do admin (`AdminRevenue.razor.cs`), cobrindo pelo menos: nada selecionado desabilita o envio, valor exibido == soma das taxas selecionadas, partida `Pago` nao e selecionavel, rejeitar exige motivo.
- **Fase E -- higiene**: revisar `catch (Exception)` genericos introduzidos no C27 (`ConfirmSettlementSubmit`) para logar; conferir se `RefreshFeeOverview` e `LoadFeeOverviewAsync` (identicos) podem ser um metodo so.

### O que NAO fazer

Nao mexer no fluxo do dinheiro do jogador; nao remover o codigo do V2; nao permitir que o organizador aprove o proprio lote; nao reintroduzir endpoint de seed/debug fora do bloco de desenvolvimento; nao criar doc solto na raiz.

---

## Ciclo 27 (Pleno) -- Fechar a Fase 3 (o repasse precisa existir na tela) + Fase 6 de verdade [EXECUTADO -- ver review acima]

**Regra de ouro**: TDD, SOLID, i18n (regra 25) inclusive em mensagens de service, build `--no-incremental` 0 warning, suite verde, 1 commit por fase. **Nao** reabrir decisoes ja travadas no Ciclo 26.

### Fase A -- UI do repasse, lado do organizador (prioridade 1)

Aba "Taxa da plataforma" em `Pages/Groups/Payments.razor`, visivel **so** em grupo `Sport.Futsal` em modo manual:
- lista **por partida**: data, nome, n. de pagantes, taxa da partida, status (`Pago` / `Pendente`);
- somatorio **a repassar** em destaque + acumulado + em analise + ja repassado;
- chave Pix da plataforma via **config** (nao hardcoded) + QR, espelhando o card que o jogador ve;
- acao "Enviar comprovante do repasse" (valor + imagem) -> `SubmitSettlementAsync`, criando lote `EmAnalise`;
- historico dos lotes com status e motivo da rejeicao;
- **sem** botao de marcar como pago.

### Fase B -- UI do repasse, lado do admin do sistema

Em `Pages/Admin/AdminRevenue.razor`: grupos com saldo separando **pendente / em analise / quitado**; fila de lotes com **visualizacao do comprovante**; acoes **Confirmar recebimento** e **Rejeitar (com motivo)** -> `ReviewSettlementAsync`.

### Fase C -- Endpoint da imagem do comprovante

`GET /api/fee-settlement-proof/{id}` espelhando `/api/pix-proof/{id}`: autorizado **ao admin do grupo dono do lote ou ao admin do sistema**; `Unauthorized` sem login, `NotFound` sem imagem, `Forbid` para terceiro. **Teste de autorizacao obrigatorio** (o endpoint serve documento financeiro).

### Fase D -- Baixa FIFO por partida

`PlatformFeeLedgerService` passa a projetar o status **por partida**: os lotes `Pago`, em ordem de `SubmittedAt`, cobrem as partidas mais antigas ate esgotar o valor; partida parcialmente coberta continua `Pendente`; sobra vira credito para o proximo. Testes: baixa parcial, exata, maior que o devido, e lote rejeitado nao abate nada.

### Fase E -- Fase 6 pela terceira vez: teste anti-hardcode DE VERDADE

Nao e paridade de chaves. E um **teste de convencao** que varre os arquivos `.razor` do repo e falha quando encontra texto visivel cru:
- varrer `Pages/**` e `Shared/**` procurando literais com acento/palavra em PT em conteudo de elemento e nos atributos `title`, `placeholder`, `alt`, `aria-label`, e em `<PageTitle>`;
- **allowlist explicita e curta** para os casos legitimos (nomes proprios, "Confirmai", simbolos) -- allowlist no arquivo de teste, comentada, nao um `return` que desliga o teste;
- o teste **deve estar vermelho** antes da migracao e verde depois: migrar os 8 residuais de `Pages` (`PayoutAccountEditor` "Chave Aleatória", `AdminUserView` "Papéis & Permissões", `Poker/Create` e `Poker/Edit` placeholders, `Poker/Index` "Late Reg até", `Users.razor` "Usuários", `EscalacaoVoting` "Você" + o `title` do voto) e os 16 de `Shared/Components/**`;
- remover o `if (enUs.Count == 0) return;` do `I18nKeyParityTests`: ou o dominio stub e preenchido, ou o `[Theory]` recebe um `Skip` explicito com o motivo -- teste que se auto-desliga em silencio nao vale como cobertura.

### Fase F -- Limpeza da divida do C26

- `eval` -> funcao nomeada em `site.js`; `StateHasChanged` apos limpar `_highlightPix`.
- Mensagens de service via `UiTextService` (`PlatformFeeSettlementService`, `FutsalCreateService`) e as 5 mensagens hardcoded de `Profile.razor.cs`.
- Indentacao das linhas 124-125 do `Program.cs`.

### O que NAO fazer

Nao mexer no fluxo do dinheiro do jogador (V1 manual continua Pix direto ao organizador); nao remover o codigo do V2; nao permitir que o organizador aprove o proprio lote; nao trocar framework de UI; nao usar `!important`; nao criar doc solto na raiz.

---

## Ciclo 26 (Pleno) -- Taxa acumulada p/ repasse manual + UX do Profile + privacidade da chave Pix + fechar i18n [EXECUTADO -- ver review acima]

**Origem**: decisoes do Robson (mensagem pos-review do C25) + pontos que o Pleno levantou na PR #86 + ressalvas da review do C25.

**Regra de ouro**: TDD (teste antes), SOLID, i18n (regra 25) em tudo que for texto novo, build `--no-incremental` **0 warning**, suite verde. 1 commit por fase. **Nao** mexer no fluxo do dinheiro do V1 manual (o jogador continua pagando direto no Pix do organizador).

### Decisoes do Robson ja travadas (nao reabrir)

1. **Taxa da plataforma no V1 manual = R$ 0,75 fixo por confirmacao paga**, apenas em partidas de **futebol/futsal** (`Sport.Futsal`). Racional do Robson: o jogador de linha paga R$ 10-15, entao R$ 0,75 e irrisorio.
2. **O sistema NAO intermedia o dinheiro**: soma o devido e **mostra no painel do organizador/manager do grupo**, que faz o **repasse manual** a plataforma. Motivacao: menos complexidade de integracao bancaria e menos exposicao de receita.
3. **Transparencia com o usuario**: *"podemos detalhar a taxa sim, prezemos por transparencia com o user"* -- a taxa aparece **discriminada** na tela de pagamento (`Partida + Taxa = Total`), nao embutida no preco (Fase 4).
4. **Os dois lados do repasse, espelhando o fluxo que ja existe**: assim como *o jogador envia comprovante e o organizador confirma o recebimento*, agora **o organizador faz o Pix, envia o comprovante, e o admin do sistema confirma o recebimento** daquele grupo. Mesmo padrao mental, mesma UI, mesmo codigo (Fase 3). O organizador ve as taxas **por partida** (pagas/pendentes) e o **somatorio a pagar**; o admin do sistema ve os grupos com repasses pendentes/realizados.
5. **UX do Profile**: o Robson reportou "tela poluida visualmente, opcoes pouco claras, pouco intuitivo". Escopo aberto para reorganizacao (ver Fase 1).

### Fase 0 -- PRIVACIDADE: nao expor chave Pix no perfil publico (P0, fazer primeiro)

**Bug encontrado pelo Senior auditando a tela**: `Pages/Components/Profile/ProfileContactsDisplay.razor` renderiza `User.PixKey` para **qualquer visitante** do perfil -- o componente nao recebe nem consulta `isOwnProfile`. Ou seja, a chave Pix de qualquer usuario e visivel na rota publica `/profile/{Id}`. Instagram/Discord sao contatos sociais (ok expor); **chave Pix nao e**. Expor a chave do organizador na **tela de pagamento** para quem vai pagar continua correto -- o problema e o perfil.

- Adicionar `[Parameter] public bool IsOwnProfile { get; set; }` ao componente e so renderizar a linha de Pix quando `IsOwnProfile` for true; passar `IsOwnProfile="@isOwnProfile"` em `Pages/Profile.razor`.
- **Teste primeiro**: renderizar/asserir que a chave nao aparece para visitante e aparece para o dono (pode ser teste do componente via parametro, sem bUnit -- se nao der, testar o predicado extraido).

### Fase 1 -- UX do Profile: menos cards, opcoes claras

**Diagnostico do Senior** (auditando `Pages/Profile.razor` + `Pages/Components/Profile/*`), casando com o "poluida e pouco intuitiva" do Robson:

1. **Ate 5 blocos empilhados** no proprio perfil: `ProfileHeaderCard`, `ProfileContactsDisplay`, `ProfileEditForm`, `AvatarUploadSection`, `ProfileSportStats` -- cada um com moldura propria. E scroll longo sem hierarquia.
2. **Conteudo duplicado**: `ProfileContactsDisplay` mostra Instagram/Discord/Pix em modo leitura e logo abaixo o `ProfileEditForm` mostra os **mesmos 3 campos** em modo edicao. O dono ve tudo duas vezes.
3. **Edicao escondida**: o form vive dentro de `<details><summary>Editar perfil</summary>` -- colapsado, sem indicacao de que ha algo dentro. E a principal razao de "opcoes pouco claras".
4. **Avatar separado do avatar**: `AvatarUploadSection` e um card proprio, longe do avatar exibido no header.
5. **Pix sem contexto**: a chave Pix aparece como se fosse um contato social (ao lado de Instagram/Discord), quando na verdade e **configuracao de recebimento** -- e e o destino do link "Configure a sua Chave Pix" vindo do grupo. Quem chega por esse link cai no topo de uma tela longa e nao sabe onde clicar.
6. **Deep link morto**: `Pages/Profile.razor.cs:84` faz `_ = query.TryGetValue("intent", out _);` -- le o parametro `intent` e **descarta**. A intencao de "vim aqui pra configurar o Pix" existe no codigo, mas nao faz nada.

**Alvo**:

- **Fundir** header + avatar num unico card de identidade (avatar clicavel/hover abre o upload para o dono).
- **Um card "Meus dados"** para o dono, com os campos **sempre visiveis** (sem `<details>`), substituindo a dupla leitura+edicao. Visitante continua vendo so a leitura (Instagram/Discord; Pix nunca -- Fase 0).
- **Separar "Recebimento (Pix)"** num bloco proprio, com rotulo explicando para que serve ("usada para receber os pagamentos das partidas do seu grupo").
- **Fazer o `intent` funcionar**: `/profile/{id}?intent=pix` (ou ancora `#pix`) rola ate o bloco de Pix e o destaca; atualizar o link em `PixReceiverSelector.razor` para usar isso.
- Manter `ProfileSportStats` como esta (as abas Futsal/Poker funcionam bem).
- Mobile-first, breakpoint 768px, CSS em `Pages/Profile.razor.css` / arquivo de dominio -- **sem `!important`**.
- **Verificacao visual obrigatoria** (regra 26): 375px e desktop, dono e visitante, registrar no PR.

### Fase 2 -- Ledger da taxa da plataforma (R$ 0,75, futsal, fluxo manual)

**Modelo (snapshot, nao calculo derivado)**: gravar o valor da taxa **no momento em que a confirmacao vira paga**, para que mudanca futura de tarifa nao reescreva o passado.

- `Models/EventConfirmation.cs`: nova coluna `public decimal? PlatformFeeAmount { get; set; }` (null = confirmacao anterior ao ciclo / nao aplicavel).
- `Configuration/FeeOptions.cs`: nova opcao `public decimal ManualPlatformFeeFixed { get; set; } = 0;` e `appsettings.json` -> `"ManualPlatformFeeFixed": 0.75`. **Nao reutilizar** `AppFeeFixed`/`GatewayFeeFixed`: aqueles sao a matematica do V2 com gateway (0,50 + 0,25) e ja aparecem no `EventPaymentChargeCalculator`/`PayoutService`. Misturar os dois quebra o V2.
- Novo `Services/Payment/PlatformFeeLedgerService.cs`:
  - `StampFeeOnPaidAsync(confirmationId)` -- chamado quando o organizador confirma o pagamento (`AdminConfirmationService`) e quando o gateway confirma; **so** carimba se `Event.Group.Sport == Sport.Futsal`, se o grupo esta em modo manual (`!EnablePaymentGateways`) e se `PlatformFeeAmount` ainda e null (**idempotente**).
  - `GetGroupBalanceAsync(groupId)` -> `(decimal Accrued, decimal Settled, decimal Due)`.
- Novo `Models/PlatformFeeSettlement.cs` -- **lote de repasse** com comprovante e status (`EmAnalise`/`Pago`/`Rejeitado`); enviado pelo organizador, confirmado pelo admin do sistema. Estrutura completa e regras na **Fase 3**.
- Migration: **so adicionar coluna/tabela**, sem `UPDATE` em dados existentes (licao do Ciclo 19).
- **Testes obrigatorios**: carimbo idempotente; nao carimba poker; nao carimba grupo com gateway ligado; nao carimba confirmacao pendente; `Due = Accrued - Settled` (contando so lote `Pago`); toggle-back de pago->pendente **nao** apaga o carimbo (a taxa foi devida no momento do pagamento) -- se o Pleno discordar, documentar antes de mudar.

### Fase 3 -- Repasse com comprovante: **mesmo fluxo do pagamento da partida, um nivel acima**

**Decisao do Robson**: *"da mesma forma que o jogador envia um comprovante e o organizador confirma o recebimento, facamos o mesmo: o organizador faz o pix e envia o comprovante, o admin por sua vez confirma o recebimento do organizador daquele grupo"*. O fluxo passa a ser **o mesmo padrao em dois niveis**:

```
Nivel 1 (ja existe):  jogador      --paga--> organizador   --envia comprovante--> organizador confirma
Nivel 2 (Ciclo 26):   organizador  --paga--> plataforma    --envia comprovante--> admin do sistema confirma
```

**Por que isso e bom**: reaproveita o modelo mental do usuario, a UI e o codigo. `PixProofUploadService` (validacao MIME jpeg/png/webp + limite de 5 MB + persistencia em coluna `byte[]`) e `AdminConfirmationService` ja fazem exatamente isso no nivel 1. **Nao inventar um segundo mecanismo de upload** -- generalizar/espelhar o existente, mantendo os mesmos limites e a mesma validacao.

**Criticas do Senior que entram como regra do desenho** (o Robson pediu criticas):

1. **O comprovante e o gatilho, nao a baixa.** Enviar comprovante move o repasse para `EmAnalise`; **so a confirmacao do admin do sistema** move para `Pago`. Assim o organizador ganha uma **acao** (declarar que pagou) sem ganhar o **poder** de quitar a propria divida. No fluxo manual o sistema nao observa a transferencia -- nao ha webhook nem extrato --, entao "pago" so pode significar "o admin do sistema confirmou".
2. **O repasse e por valor, nao por partida.** O organizador manda **um Pix so** cobrindo varias partidas. O comprovante, portanto, se anexa a um **lote de repasse**, nao a uma partida. Exigir um comprovante por partida seria inviavel na pratica.
3. **Baixa FIFO.** Um lote de R$ 15,00 quita as partidas pendentes mais antigas ate esgotar o valor; partida parcialmente coberta continua `Pendente` ate ser totalmente coberta; sobra vira credito abatido no proximo lote. E o que mantem a lista por partida coerente com o saldo.
4. **Rejeicao tem que existir.** Comprovante ilegivel/errado precisa de um caminho de volta (`Rejeitado` + motivo), senao o repasse fica preso em `EmAnalise` para sempre.

**Modelo** -- `Models/PlatformFeeSettlement.cs` passa a ser o **lote de repasse** e nao so o registro de baixa:

```csharp
public int      Id;
public int      GroupId;
public decimal  Amount;                 // valor declarado pelo organizador
public string   SubmittedByUserId;      // organizador que enviou
public DateTime SubmittedAt;
public byte[]?  ProofImageData;         // mesmo padrao de EventConfirmation.PixProofImageData
public string?  ProofContentType;
public PlatformFeeSettlementStatus Status;   // EmAnalise | Pago | Rejeitado
public string?  ReviewedByUserId;       // admin do sistema
public DateTime? ReviewedAt;
public string?  ReviewNote;             // motivo da rejeicao / observacao
```

So lote com `Status == Pago` abate saldo (`Settled`). `EmAnalise` aparece nas duas telas como "aguardando confirmacao", **sem** reduzir o devido.

**Lado do organizador** -- `Pages/Groups/Payments.razor` (tela de admin do grupo, hoje com abas `delinquent`/`history`): **nova aba "Taxa da plataforma"** com:
- **linha por partida**: data, nome da partida, nº de pagantes, taxa da partida (`pagantes x 0,75`) e **status** (`Pago` / `Pendente`);
- **somatorio a repassar** em destaque, mais acumulado, em analise e ja repassado;
- **chave Pix da plataforma** vinda de config (**nao hardcoded**) + QR, espelhando o card que o jogador ve;
- **acao "Enviar comprovante do repasse"**: valor + imagem, criando o lote em `EmAnalise` -- espelho exato do `EventPaymentProof`;
- historico dos lotes enviados com status e, se rejeitado, o motivo;
- **sem** botao de "marcar como pago".
- Exibir a aba **apenas** em grupo `Sport.Futsal` em modo manual (nos demais o saldo e sempre zero e a aba so polui).

**Lado do admin do sistema** -- `Pages/Admin/AdminRevenue.razor` (ja existe): grupos com taxa acumulada separando **pendente**, **em analise** e **quitado**; fila de lotes aguardando confirmacao com **visualizacao do comprovante**; acoes **Confirmar recebimento** e **Rejeitar (com motivo)** -- espelho do que o organizador faz no nivel 1.

**Servir a imagem do comprovante**: `Program.cs` ja expoe `GET /api/pix-proof/{id}` autorizado ao pagador ou admin do grupo. Criar o analogo `GET /api/fee-settlement-proof/{id}`, autorizado **ao organizador do grupo dono do lote ou ao admin do sistema** -- seguindo o mesmo padrao de `Results.Unauthorized/NotFound/Forbid`. **Nao** deixar a imagem publica.

**Testes obrigatorios**: organizador **nao** consegue mover para `Pago`; comprovante em `EmAnalise` nao abate saldo; confirmacao abate FIFO (parcial, exata e maior que o devido); rejeicao devolve o saldo e preserva o motivo; upload rejeita MIME invalido e > 5 MB (mesmos limites do nivel 1); autorizacao do endpoint da imagem (terceiro recebe `Forbid`).

- i18n de todos os textos novos (`GroupTexts` / `AdminTexts`).

### Fase 4 -- Cobranca ao jogador: R$ 0,75 por cima, **detalhada** (CONFIRMADO pelo Robson)

**Decisao do Robson**: *"podemos detalhar a taxa sim, prezemos por transparencia com o user"*. Fase **liberada**.

- A tela de pagamento no modo manual mostra a composicao **discriminada**: `Partida R$ 15,00` + `Taxa da plataforma R$ 0,75` = **`Total R$ 15,75`**. Nada de embutir a taxa silenciosamente no preco.
- O **QR/payload Pix e o valor sugerido** usam o **total** (R$ 15,75) -- senao o organizador recebe a menos e paga a taxa do proprio bolso.
- Reaproveitar o padrao ja existente em `EventPaymentSummary` (que ja discrimina base + taxa no V2); a diferenca e a origem do valor (`ManualPlatformFeeFixed` em vez de `AppFeeFixed + GatewayFeeFixed`).
- **Consequencia a explicitar na UI do organizador**: esses R$ 0,75 caem **na conta dele**; e por isso que ele deve o repasse. O texto da aba "Taxa da plataforma" deve deixar isso claro ("voce recebeu a taxa junto com o valor da partida; repasse o total abaixo").
- So aplicar em `Sport.Futsal` + modo manual + preco > 0 (coerente com a Fase 2). Partida gratuita nao gera taxa.

### Fase 5 -- Pix do admin como pre-requisito (pedido do Pleno, aprovado)

Hoje o erro aparece tarde e do lado errado: o jogador chega em `/pagamento/evento/{id}` e ve "chave nao cadastrada". O Ciclo 25 ja adicionou o aviso em `Groups/Detail.razor`.
- Ao **criar/editar partida com preco > 0** em grupo manual sem nenhum admin com `PixKey`: bloquear o submit com mensagem clara + link direto para a configuracao de Pix.
- **Nao** bloquear a criacao do grupo nem partida gratuita.
- Teste da regra (service, nao UI).

### Fase 6 -- Fechar o i18n (ressalvas da review do C25)

1. **Teste de convencao anti-hardcode** (era a Fase 6 do C25, entregue so parcialmente): falha se aparecer literal acentuado visivel em `.razor` sob `Pages/**`. Comecar com allowlist dos residuais conhecidos e ir esvaziando -- assim o teste entra verde e trava regressao.
2. Migrar os **8 residuais** em `Pages`: `Groups/Components/PayoutAccountEditor.razor:46`, `Admin/AdminUserView.razor:78`, `Poker/Create.razor:84,91`, `Poker/Edit.razor:82`, `Poker/Index.razor:108`, `Users.razor:5`, `Components/EscalacaoVoting.razor:80`.
3. Migrar os **16 literais em `Shared/Components/**`** (`GroupDetailMembers`, `RankingTable`, `GroupMetrics`, `PaginationControls`, `PokerDetailInfo`, `UserSummaryCard`, `FutsalWaitlist`, `MainLayout`) -- `Shared` nunca esteve no escopo do C25.
4. Migrar as **6 strings hardcoded em `Pages/Profile.razor.cs`** (linhas ~90, 91, 99, 108, 117): mensagens de feedback ainda em PT literal, violando a regra 25 (mensagem de retorno tambem passa pelo `UiTextService`).

### O que NAO fazer
- Nao implementar cobranca automatica, split, boleto ou integracao bancaria -- o repasse e **manual** por decisao do Robson.
- Nao permitir que o **organizador** de baixa no proprio saldo devedor.
- Nao mexer no `EventPaymentChargeCalculator`/`PayoutService` (matematica do V2).
- Nao redesenhar o Profile inteiro do zero nem trocar de framework de UI -- reorganizar o que existe (Fase 1).
- Nao completar EN/ES manualmente onde nao houver chave; PT-BR e o baseline.

---

## Review Senior do Ciclo 25 (PR #86) -- APROVADO com ressalvas

- **PR/branch**: #86 (`refactor/ciclo25-i18n-completo`), **ja mergeada na `main`** (merge `3e31964`). Review nesta PR #87.
- **Build**: `dotnet build --no-incremental` na `main` veio com **2 warnings CS8602** (`Pages/Groups/Detail.razor` 158 e 167) -- **regressao** da meta "0 warning". **Corrigido nesta PR de review** (causa: o novo `@if (isAdmin && group is not null && ...)` dentro do bloco `else` onde `group` ja e nao-nulo quebrou a analise de fluxo do compilador; removida a checagem redundante). Apos o fix: **0 warning / 0 error**.
- **Testes**: **2165 passed / 2189** (2133 -> 2165, **+32**). As 24 falhas sao `ProgramConfigurationTests` tentando conectar em `127.0.0.1:5432` sem Postgres local -- **ambiente, nao regressao** (recorrente desde o Ciclo 18).
- **Por fase**:
  - **Fases 1-5 (i18n) -- ATINGIDAS**. Varredura de literais acentuados visiveis em `Pages/**/*.razor` caiu de ~450 para **8 ocorrencias**: `PayoutAccountEditor.razor:46` ("Chave Aleatoria"), `AdminUserView.razor:78` ("Papeis & Permissoes"), `Poker/Create.razor:84,91` e `Poker/Edit.razor:82` (placeholders de endereco/cidade), `Poker/Index.razor:108` ("Late Reg ate"), `Users.razor:5` ("Usuarios"), `Components/EscalacaoVoting.razor:80` ("Voce"). Dominios novos (`FutsalTexts`, `PokerTexts`, `GroupTexts`, `UtilityTexts`) com PT-BR/EN-US/ES-ES preenchidos -- o Pleno foi **alem** do pedido (o plano exigia so PT-BR baseline).
  - **Fase 6 (teste de convencao) -- PARCIAL**: o plano pedia um **teste anti-hardcode** (falhar se aparecer literal acentuado em `.razor`). O Pleno entregou `Ciclo25I18nCompletenessTests.cs` (+32), que valida **paridade de chaves entre idiomas**, valores nao-vazios, existencia das chaves da Fase 5 e EN != PT. E util e complementar, mas **nao guarda contra novo hardcode** -- o objetivo real da fase. Fica para o Ciclo 26 (baixo custo agora que so restam 8 ocorrencias em `Pages`).
- **Extras fora do escopo declarado** (o plano dizia "nao mudar layout/CSS"): padronizacao do botao `btn-view-groups` em 6 telas + `NoGroupsHint`, movimentacao de `detail-admin-btn--whatsapp/--invite/--settings` de `events.css` para `event-detail.css` (ordem de cascata), reposicionamento do card "Pix nao configurado", remocao de botoes "Voltar" redundantes na tela de pagamento. **Aceitos**: sao correcoes de UX levantadas pelo Robson nos testes (regra 22) e vieram documentadas no PR. Sem impacto em regra de negocio.
- **Bug fix legitimo**: `EventPaymentService.BuildPixStaticPayload` ganhou guard para `pixKey` nulo/vazio (retorna `string.Empty`) e o `!` null-forgiving saiu da chamada em `EventPayment.razor`. O componente `EventPaymentPixAdmin` ja tratava a ausencia de chave (`Payment.NoPixKey`), entao o efeito e so eliminar o NullRef.
- **Higiene**: removido `File.WriteAllText(@"C:\temp\grupos_debug.html")` que estava em `GroupsIntegrationTests` -- bom achado; era um caminho Windows que quebraria o CI Linux.
- **Os 7 testes de integracao** que o Pleno reportou como falhando (assertavam strings PT hardcoded) **ja foram corrigidos** no commit `dc0fc82` da propria PR. Suite verde. Nada pendente aqui.
- **Ressalvas (nao bloqueantes)**:
  1. 2 warnings CS8602 introduzidos (corrigidos pelo Senior nesta PR) -- a regra "build 0 warning" precisa ser verificada com `--no-incremental`, senao o build incremental esconde warnings.
  2. Teste de convencao anti-hardcode nao entregue (Fase 6 parcial).
  3. 8 literais residuais em `Pages` + 16 em `Shared/Components/**` (`Shared` nunca esteve no escopo do plano; entra no Ciclo 26).

### Respostas do Senior aos 4 pontos levantados pelo Pleno na PR #86

1. **UX da tela de Profile** -- aceito como fase do **Ciclo 26**, mas precisa de escopo concreto antes de virar plano: o Robson vai listar o que incomoda na tela (o que ele viu testando). Sem lista, o Pleno **nao deve** redesenhar por conta propria (regra 16). Enquanto isso, o Senior levanta o obvio: `/profile/{Id}` acumula perfil + edicao + chat + stats numa unica tela, e e o destino do link "Configurar Pix" -- ou seja, o organizador cai numa tela longa so pra cadastrar a chave. Sugestao: **secao Pix com ancora propria** (`/profile/{id}#pix`) ou um caminho dedicado de configuracao de recebimento.
2. **Repasse manual ao organizador + cobrar taxa e mostrar acumulado** -- **precisa de decisao do Robson antes de virar plano** (muda regra de negocio e modelo financeiro). Contexto factual: hoje a taxa e **fixa em reais**, nao percentual: `Fee:AppFeeFixed = R$ 0,50` + `Fee:GatewayFeeFixed = R$ 0,25` = **R$ 0,75 por confirmacao** (o Pleno escreveu "0,75%", que e outra coisa -- **nao implementar como percentual**). No V1 manual o dinheiro vai **direto** do jogador para o Pix do organizador: a plataforma **nao passa pelo fluxo**, entao a taxa nao pode ser retida -- ela viraria um **saldo devedor do organizador para a plataforma** (cobranca posterior). Isso e um subsistema novo (ledger de taxa a receber, fechamento, cobranca, inadimplencia). **Nao entra no Ciclo 26**; o caminho natural continua sendo o V2 automatico, onde a retencao acontece no split. Se o Robson quiser mesmo cobrar no V1 manual, o passo minimo e apenas **exibir o acumulado devido** (leitura, sem cobranca) numa tela de admin -- e isso precisa de aprovacao explicita.
3. **Pix do admin como pre-requisito** -- **de acordo, e ja comecou**: o proprio Ciclo 25 adicionou o aviso "Pix nao configurado" em `Groups/Detail.razor` para admin quando o grupo esta em modo manual e nenhum admin tem `PixKey`. O que falta (Ciclo 26): (a) mesmo aviso/bloqueio ao **criar partida com preco** em grupo manual sem Pix -- hoje o jogador chega na tela de pagamento e ve "chave nao cadastrada", ou seja, o erro aparece tarde, do lado errado; (b) teste cobrindo a regra. **Nao** bloquear a criacao do grupo (so a cobranca depende do Pix).
4. **7 testes de integracao com string PT hardcoded** -- ja resolvido pelo proprio Pleno em `dc0fc82`; suite verde (2165). Licao para o pipeline: quando uma tela migra para i18n, os testes que assertam texto devem passar a assertar pela **chave via `UiTextService`**, nunca pelo literal.

---

## Ciclo 25 (Pleno) -- i18n completo: zerar debito tecnico de strings hardcoded [EXECUTADO -- ver review acima]

**Objetivo**: migrar TODAS as strings hardcoded restantes em `Pages/**/*.razor` para `@Ui["Dominio.Chave"]` (regra 25), zerando o debito tecnico de i18n para que daqui pra frente a regra 25 seja apenas manutencao natural. Adicionar teste de convencao anti-hardcode (P3) para guardar o progresso.

**Regra de ouro**: PT-BR baseline em todos os dominios. Nao mudar regra de negocio. Build 0 warning + suite verde. 1 commit por fase.

### Fase 1 -- i18n Futsal (maior volume)
Arquivos: `Pages/Futsal/Detail.razor`, `Create.razor`, `Edit.razor`, `Escalacao.razor`, `Schedule/Edit.razor`, `Schedule/Index.razor`, `Components/EditEventForm.razor`, `Components/EscalacaoControls.razor`, `Components/FutsalMatchIdentity.razor`, `Components/FutsalGoalkeeperGroup.razor`, `Components/FutsalOutfieldGroup.razor`, `Components/DetailAdminPanel.razor`, `Components/DetailLocationSection.razor`, `Components/DetailQuorumBar.razor`, `Components/DateTimeSelector.razor`, `Components/EscalacaoShare.razor`, `Components/EscalacaoScoreEditor.razor`, `Components/PriceInput.razor`.
Dominio: `FutsalTexts.cs` (novo) + `CoreTexts.cs` (existentes).

### Fase 2 -- i18n Poker
Arquivos: `Pages/Poker/Detail.razor`, `Create.razor`, `Edit.razor`, `Index.razor`.
Dominio: `PokerTexts.cs` (novo) + `CoreTexts.cs`.

### Fase 3 -- i18n Groups
Arquivos: `Pages/Groups/Detail.razor`, `Create.razor`, `Index.razor`, `Join.razor`, `Features.razor`, `Payments.razor`, `Ranking.razor`, `Components/FeaturesToggles.razor`, `Components/PayoutAccountEditor.razor`, `Components/MembersManager.razor`.
Dominio: `GroupTexts.cs` (novo) + `CoreTexts.cs`.

### Fase 4 -- i18n Admin
Arquivos: `Pages/Admin/Admin.razor`, `AdminRevenue.razor`, `AdminVenues.razor`, `AdminVenueEdit.razor`, `AdminUserView.razor`, `Components/AdminPaymentsSummaryPanel.razor`, `Components/AdminPaymentsAdvancedToolsModal.razor`.
Dominio: `AdminTexts.cs` (existente, estender).

### Fase 5 -- i18n Demais
Arquivos: `Pages/Payment/PaymentCheckoutPanel.razor`, `Pages/MyEvents/Index.razor`, `Pages/VenueManager/Venues.razor`, `Pages/VenueManager/VenueEdit.razor`, `Pages/Profile.razor`, `Pages/Components/Profile/*`, `Pages/Components/EscalacaoVoting.razor`, `Pages/Components/MailboxConversationList.razor`, `Pages/Docs/Integration.razor`, `Pages/Users.razor`, `Pages/Dashboard.razor`.
Dominio: estender dominios existentes + `VenueTexts.cs` (novo) se necessario.

### Fase 6 -- Teste de convencao anti-hardcode i18n
Criar teste que falha se aparecer literal acentuado (PT-BR) em `.razor` dos fluxos principais. Guarda o progresso dos ciclos 24-25.

### O que NAO fazer
- Nao completar EN/ES (so PT-BR baseline).
- Nao mudar regra de negocio nem layout/CSS.
- Nao adicionar bUnit nem E2E.
- Nao remover codigo existente (so substituir strings).

---

## Review Senior do Ciclo 24 (PR #84) -- APROVADO

- **PR/branch**: #84 (`refactor/ciclo24-pagamento-v1-usavel-i18n`), ja na `main` (merge `79033dc`). Review nesta PR #85.
- **Build**: `dotnet build` **0 warning / 0 error**. **Testes**: **2133 passed / 2157** (+6 vs #81); as 24 falhas sao `ProgramConfigurationTests` sem Postgres (ambiente, nao regressao).
- **Por fase**:
  - **Fase 1 (UX pagamento OFF) -- ATINGIDA**: em `EventPayment.razor`, o seletor `<EventPaymentGateways>` agora so renderiza sob `@if (ShouldShowGateways)`; o Pix manual + comprovante sob `@if (ShouldShowManualPix)`. A decisao foi extraida para 2 propriedades `internal` testaveis em `EventPayment.razor.cs`: `ShouldShowGateways => groupGatewaysEnabled` e `ShouldShowManualPix => !groupGatewaysEnabled || FeeOptions.Value.ShowDirectPixToOrganizer`. Em grupo manual (default V1) o jogador ve **so** o Pix direto + upload de comprovante. V2 (gateways ON) inalterado; **nada removido**.
  - **Fase 2 (i18n do pagamento) -- ATINGIDA**: `EventPayment.razor` + os componentes de `Pages/Payment/Components/*` migrados para `@Ui["Payment.*"]`/`Ui.Get(...)`, com `string.Format` nas chaves com placeholder (data/nome). Chaves novas em `Services/Core/UiText/PaymentTexts.cs` (PT-BR baseline; algumas ja com EN/ES). Restou 1 string hardcoded em `Pages/Payment/PaymentCheckoutPanel.razor` -- **fora do escopo** deste ciclo (nao e o fluxo de partida), fica no backlog P1 de i18n.
  - **Fase 3 (cobertura) -- ATINGIDA**: `Confirmai.Tests/Ciclo24PaymentUiLogicTests.cs` (+6 testes) cobre a matriz de `ShouldShowGateways`/`ShouldShowManualPix` (OFF esconde gateway + mostra manual; ON mostra gateway; `ShowDirectPixToOrganizer` combina). O **caminho do dinheiro** (upload -> confirmacao do organizador, pending, toggle-back, replaced proof, coexistencia) **ja estava coberto** por `PixManualPaymentFlowTests.cs` + `PixProofUploadServiceTests.cs` + `AdminConfirmationServiceTests.cs` (pre-existentes) -- por isso o Pleno nao duplicou e focou os testes novos na logica nova.
- **Observacoes (nao-bloqueantes)**: (a) o teste instancia a pagina via `RuntimeHelpers.GetUninitializedObject` + backing-field `<FeeOptions>k__BackingField` -- funciona mas e fragil (quebra se a prop virar campo); aceitavel sem bUnit. (b) o teste redeclara um `OptionsWrapper<T>` interno, que ja existe em `Microsoft.Extensions.Options` -- cosmetico. Nenhum dos dois bloqueia.
- **Pendencias do Robson (inalteradas)**: rotacao do ClientSecret Efi + credenciais (so V2); homologacao Efi + limites (V2); nota fiscal da taxa (V2); OAuth Google prod + SMTP; `SyncPassword=false`/mTLS webhook em prod.
- **Proximo (backlog P1 restante)**: continuar i18n das demais telas (Detail, Payments, MyEvents, PaymentCheckoutPanel), esconder gateway tambem onde aplicavel, e o P2 (catch{} logando, estados vazios padronizados, consistencia de idioma no `UiTextService`).

---

## Ciclo 24 (Pleno) -- Pagamento V1 usavel (gateways OFF) + i18n do pagamento + cobertura do fluxo manual [EXECUTADO -- ver review acima]

**Objetivo**: fechar o P1 do "Aparato Geral do Senior" para o V1 ficar pronto para uso real. Hoje o default e **manual** (`EnablePaymentGateways=false`, decidido no Ciclo 23), mas a tela de pagamento ainda mostra UI de gateway e boa parte das strings do fluxo esta hardcoded. Este ciclo deixa o caminho do dinheiro do V1 limpo, traduzido e coberto por teste.

**Regra de ouro (lembrete)**: TDD (teste antes/junto), SOLID, `IDbContextFactory`, incremental (1 assunto por commit), i18n obrigatoria (regra 25), CSS mobile-first 768px, **sem mudar regra de negocio** sem autorizacao. Build 0 warning + suite verde (24 falhas ambientais de Postgres sao esperadas).

### Fase 1 -- UX do pagamento V1 quando gateways estao OFF

Arquivo-alvo: `Pages/Payment/EventPayment.razor` (+ `EventPayment.razor.cs`, ja tem `groupGatewaysEnabled`).

- Hoje, no ramo do jogador (`else` ~linha 162), sempre renderiza `<EventPaymentGateways>` e, **abaixo**, quando `ShowDirectPixToOrganizer || !groupGatewaysEnabled`, renderiza `<EventPaymentPixAdmin>` + `<EventPaymentProof>`. Resultado: com o **default manual (OFF)** o jogador ve o seletor de gateways (vazio/"nenhum gateway disponivel") **acima** do Pix manual -> confuso.
- **Tarefa**: quando `!groupGatewaysEnabled` (e sem `ShowDirectPixToOrganizer` forcando o contrario), **nao renderizar** `<EventPaymentGateways>`; mostrar direto **so** o Pix do organizador (`EventPaymentPixAdmin`) + envio de comprovante (`EventPaymentProof`), com um cabecalho claro ("Pague via Pix e envie o comprovante"). Quando `groupGatewaysEnabled` (V2), manter o comportamento atual.
- **Nao** remover nenhum componente nem o caminho V2. So condicionar a renderizacao.
- **Aceitacao**: em grupo manual (OFF), a tela do jogador nao mostra seletor de gateway; mostra chave/QR + upload de comprovante. Layout mobile intacto (768px). Teste de logica do ramo (ver Fase 3).

### Fase 2 -- i18n dos fluxos de pagamento (regra 25)

Arquivos-alvo: `Pages/Payment/EventPayment.razor` e componentes em `Pages/Payment/Components/*` (`EventPaymentPixAdmin`, `EventPaymentProof`, `EventPaymentGateways`, `EventPaymentQr`, `EventPaymentStatus`). Dominio: `Services/Core/UiText/PaymentTexts.cs`.

- Migrar as strings hardcoded do fluxo para `@Ui["Payment.*"]` (ex.: "Pagamento confirmado", "Revisao de pagamento", "Comprovante enviado em {0}", "O jogador ainda nao enviou comprovante", "Confirmar pagamento", "Marcar como pago", "Sim, confirmar", "Cancelar", "Voltar para a partida"). Onde houver interpolacao (data/nome), usar chave com placeholder e formatar no code-behind/`_ui.Get(...)`.
- Adicionar as chaves em `PaymentTexts.cs` com **PT-BR** (baseline). EN/ES podem ficar para depois, mas a chave ja deve existir. **Nao** deixar string nova hardcoded.
- **Aceitacao**: os `.razor` do fluxo de pagamento sem literal acentuado visivel ao usuario; contagem de `@Ui[...]` sobe; build 0 warning.

### Fase 3 -- Cobertura do fluxo manual V1 ponta a ponta (TDD)

E o caminho do dinheiro do V1 -- precisa de teste. Alvos: `PixProofUploadService` (upload de comprovante), `EventPaymentService`/comando de confirmacao usado por `AdminMarkPaid`, e o estado do `EventConfirmation` (`PixProofUploadedAt`, `PaymentStatus`).

- Teste 1 (upload): `PixProofUploadService.UploadProofAsync` grava o comprovante e seta `PixProofUploadedAt`; rejeita content-type/arquivo invalido (caracterizar o comportamento atual, sem mudar regra).
- Teste 2 (confirmacao do organizador): a partir de uma confirmacao com comprovante enviado, a acao de marcar como pago move `PaymentStatus` para `Paid` (e o que `AdminMarkPaid` chama). Caracterizar tambem o caso "sem comprovante" (admin ainda pode marcar).
- Teste 3 (ramo de UI da Fase 1): extrair a decisao "mostrar seletor de gateway?" para um metodo/propriedade testavel (ex.: em `EventPayment.razor.cs` ou um pequeno helper) e testar: `groupGatewaysEnabled=false` -> nao mostra gateways; `true` -> mostra. (Sem bUnit no projeto; testar a logica, nao o render.)
- Usar DB in-memory via `TestDbContextFactory` (padrao dos Ciclos 20-22) + Moq so onde necessario.
- **Aceitacao**: arquivo(s) de teste dedicado(s), casos feliz + ramos, 0 diff de regra de negocio, suite verde.

### O que NAO fazer neste ciclo
- Nao mexer no fluxo automatico V2 (gateway/`PayoutService`/`EfiBankPixPayoutService`/webhook/retry) alem de condicionar a renderizacao.
- Nao completar EN/ES (so criar chaves PT-BR).
- Nao migrar i18n de telas fora do fluxo de pagamento (fica para ciclos seguintes do P1).
- Nao adicionar bUnit nem E2E agora.

> **Nota**: a review do Ciclo 23 e o backlog "Aparato Geral" estao nas PRs #81 e #82 (abertas quando este plano foi escrito). Este plano executa o item **P1** do aparato.

---

## Review Senior do Ciclo 23 (PR #80) -- APROVADO (ressalva de testes fechada pelo Senior na #81)

- **PR/branch**: #80 (`refactor/ciclo23-ux-nav-pagamento-i18n`), ja na `main` (merge `cb6c89a`). Review nesta PR #81.
- **Build**: `dotnet build` **0 warning / 0 error**. **Testes**: PR #80 nao adicionou testes (2124/2148); apos os +3 testes que o Senior adicionou nesta PR -> **2127/2151** (24 falhas ambientais de Postgres, nao regressao).
- **Por fase**:
  - **Fase 1 (navegacao) -- ATINGIDA**: botao "Grupos" (`/grupos`) na mesma `detail-header-top` do "Voltar" em `Partidas.razor` e `Features.razor` (classe `detail-back-link--groups`). Card "Como funciona" extraido para componente compartilhado `Shared/Components/Groups/NoGroupsHint.razor` (param `ShowGroupsButton` + CTA "Acessar grupos"), reutilizado em `Groups/Index` (sem botao, ja e a tela de grupos) e em `Pages/Index.razor` (`/eventos`, com botao, em 2 pontos). Sem duplicacao de markup.
  - **Fase 2 (pagamento V1 manual) -- ATINGIDA**: (a) **bug do 404 corrigido** -- `PixReceiverSelector` agora aponta para `/profile/{CurrentUserId}` (parametro novo, cabeado em `Features.razor`) em vez de `/perfil` inexistente; (b) **default invertido** -- `Group.EnablePaymentGateways = false` + migration `20260801030000_DefaultEnablePaymentGatewaysFalse` que so faz `ALTER COLUMN ... SET DEFAULT false` (**sem `UPDATE`** -> grupos existentes inalterados, respeita a licao do Ciclo 19); snapshot regenerado; (c) **codigo do Pix automatico preservado** (gateway/`PayoutService`/`EfiBankPixPayoutService`/webhook/retry intactos) -- V2 atras do toggle.
  - **Fase 3 (i18n) -- ATINGIDA**: strings dos fluxos principais migradas para `UiTextService` (dominios `Groups`/`Onboarding`/`Payment` em `Services/Core/UiText/CoreTexts.cs`, ~40 chaves PT-BR). Regra 25 em vigor; strings novas das Fases 1/2 ja nasceram via `@Ui[...]`.
- **Ressalva (RESOLVIDA pelo Senior nesta PR)**: o plano pedia teste do fix do link e do novo default e o Pleno nao adicionou nenhum. O **Senior fechou a divida aqui** com `Confirmai.Tests/Ciclo23FollowUpTests.cs` (+3 testes): `NewGroup_StartsInManualPaymentMode_ByDefault`, `NewGroup_ManualDefault_DoesNotEnableGatewayOnlyFeatures` (caracteriza o V1 manual no modelo) e `ProfilePage_ExposesProfileIdRoute_SoPixReceiverLinkIsValid` (guarda a rota `/profile/{Id}` alvo do link corrigido, via reflection do `RouteAttribute`). Suite 2124->2127.
- **Higiene (corrigido nesta review)**: a PR #80 commitou por engano um artefato de teste manual `wwwroot/uploads/groups/0f48c3cf-...png` (nao referenciado no codigo) -- **removido** nesta PR de review.
- **Pendencias do Robson (inalteradas)**: rotacionar ClientSecret Efi + reconfigurar credenciais (adiado, viagem); validar Envio de Pix Efi em homologacao; nota fiscal da taxa com contador; OAuth Google prod + SMTP; `SyncPassword=false`/mTLS webhook em prod. (Obs.: com o V1 manual, o Envio de Pix Efi so bloqueia o V2.)
- **Proximo ciclo (sugestao)**: ciclo de testes cobrindo o default flip + `CurrentUserId`, e continuar a i18n nas demais telas (baseline PT-BR ok; EN/ES depois).

---

## Ciclo 23 (Pleno) -- UX de navegacao + Pagamento V1 (fluxo manual como default) + i18n [EXECUTADO -- ver review acima]

**Origem**: requisitos levantados pelo Robson testando o app + retorno do Pleno (regra 22 -- requisitos de teste sao legitimos e entram no plano). Este ciclo destrava o **go-live V1**: pagamento simples (manual) por padrao, navegacao mais fluida e i18n auditada.

**Objetivo**: (1) melhorar a navegacao entre Grupos <-> Partidas e os estados vazios; (2) consolidar o **fluxo de pagamento manual** (comprovante + confirmacao do organizador) como o **default do V1**, deixando o Pix automatico (gateway/payout) como **V2** atras de toggle -- sem remover codigo; (3) corrigir o link Pix quebrado que cai em 404; (4) auditar e padronizar a i18n ate aqui e firmar a nova regra de pipeline.

> **DECISAO DE NEGOCIO (Robson) -- CONFIRMADA**: o fluxo manual e o default do V1. `Group.EnablePaymentGateways = false` por padrao para **grupos novos** (inverte a decisao do Ciclo 19, que tinha default `true`). Grupos existentes NAO devem ser alterados pela migration (mantem a licao do Ciclo 19: sem `UPDATE`). Robson confirmou o flip -- a Fase 2.3 esta liberada.

### Fase 1 -- Navegacao Grupos <-> Partidas (itens 1 e 2 do Robson)
1. **Botao "Grupos" na row do "Voltar" (item 1)**: em `Pages/Groups/Partidas.razor`, dentro de `<div class="detail-header-top">` (hoje so tem o `<a ... detail-back-link>Voltar</a>`), adicionar um link para `/grupos` na **mesma row**. Aplicar o mesmo padrao onde fizer sentido (telas com a mesma `detail-header-top`: Ranking, Payments, Features) para consistencia.
   - *Criterio*: na tela de Partidas, o usuario ve "Voltar" e "Grupos" na mesma linha; ambos navegam corretamente; layout mobile (<=768px) nao quebra.
2. **Estado vazio replicado + botao (item 2)**: hoje o card "Como funciona" (onboarding-hint com os 3 passos) existe SO em `Pages/Groups/Index.razor` (bloco `groups.Count == 0`). Extrair esse card para um **componente compartilhado** (ex.: `Shared/Components/Groups/NoGroupsHint.razor` ou similar) e reutiliza-lo nas demais telas principais quando o usuario **ainda nao tem grupos** (ex.: `/eventos`, `/meus-eventos`, telas de partidas sem contexto de grupo), sempre com um **botao "Acessar grupos"** (`/grupos`) no final do card.
   - *Criterio*: componente unico (sem duplicar markup); aparece nas telas-alvo quando o usuario nao tem grupo; botao leva a `/grupos`. Sem regressao para quem ja tem grupos.

### Fase 2 -- Pagamento V1: fluxo manual como default (item 6)
Contexto: o app tem dois caminhos de pagamento -- **manual** (`EnablePaymentGateways=false`: `PixReceiverSelector`, jogador paga na chave Pix de um admin e envia comprovante, organizador confirma) e **automatico/gateway** (`EnablePaymentGateways=true`: `PayoutAccountEditor` + gateway + payout). O Robson quer o **manual como default no V1**; o automatico vira **V2**.
1. **Fix do link quebrado (bug real -- causa do 404)**: em `Pages/Groups/Components/PixReceiverSelector.razor:17`, o link "Configure a sua Chave Pix" aponta para `/perfil`, que **nao existe** (a rota real e `/profile/{Id}`) -> cai no `App.NotFound` (`App.NotFound.Title`). Corrigir para navegar a pagina de perfil correta (`/profile/{userId}` do admin logado). Escrever teste que garanta que o destino e uma rota valida.
   - *Criterio*: clicar em "Configure a sua Chave Pix" leva ao perfil, nunca ao 404.
2. **Garantir o caminho manual completo e claro**: revisar o fluxo manual ponta a ponta (cadastrar Pix no perfil -> selecionar admin recebedor em Configuracoes -> jogador ve chave/QR e envia comprovante -> organizador confirma em `/grupo/{id}/pagamentos`). Corrigir textos/estados confusos. Sem inventar feature nova.
3. **Default manual para grupos novos** (CONFIRMADO pelo Robson -- liberado): mudar o default de `EnablePaymentGateways` para `false` em `AppDbContext`/modelo + nova migration que **so** altera o default (sem `UPDATE` em grupos existentes). Teste de caracterizacao do comportamento.
   - *Criterio*: grupo novo nasce em modo manual; grupo existente inalterado.
4. **NAO remover** o codigo do Pix automatico (gateway, `PayoutService`, `EfiBankPixPayoutService`, webhook, retry). Ele permanece funcional atras do toggle e documentado como **V2**. Registrar no WORK_PLAN que o Pix automatico e V2.
   - *Criterio*: nenhum arquivo de pagamento automatico deletado; suite de pagamento continua verde.

### Fase 3 -- i18n: auditoria + regra de pipeline (item 3)
1. **Auditar cobertura atual**: a i18n usa o `UiTextService` (facade em `Services/Core/UiTextService.cs`, textos por dominio em `Services/Core/UiText/*`), baseline PT-BR; **nao ha .resx**. Levantar as strings **hardcoded** em `.razor` (ha varias: "Grupo nao encontrado.", "Voltar", "Partidas", titulos, `placeholder`, `title`, `PageTitle` etc.) e migra-las para chaves do `UiTextService` por dominio. Priorizar as telas dos fluxos principais mobile (grupos, partidas, pagamento, comprovante, estados vazios) -- nao precisa varrer 100% do app neste ciclo, mas fechar os fluxos principais.
   - *Criterio*: nas telas dos fluxos principais, 0 string de UI hardcoded; tudo via `@Ui[...]`. PT-BR completo para as chaves novas.
2. **Firmar a regra no pipeline**: a **regra 25** (i18n obrigatorio, junto ao TDD) ja foi adicionada. O Pleno deve segui-la em todo incremento deste ciclo (as strings novas das Fases 1 e 2 ja nascem via `UiTextService`).
   - *Criterio*: nenhuma string nova hardcoded introduzida pelas Fases 1/2.
3. (Opcional, se sobrar espaco) Adicionar um teste de convencao simples que falhe se aparecer texto hardcoded obvio nas telas-alvo (ex.: varredura por literais em `.razor` dos fluxos principais). Nao bloqueante.

### O que NAO fazer
- Nao remover o codigo de pagamento automatico/gateway/payout (e V2, fica atras do toggle).
- Nao alterar grupos existentes via migration (licao do Ciclo 19). Novo default vale so para grupos novos.
- Na migration da Fase 2.3, NAO incluir `UPDATE` que altere grupos existentes (so mudar o default do schema).
- Nao mudar regra de negocio de cobranca/valor/taxa. Nao abrir ciclo dedicado de "Mobile UX" agora (item 4): as telas ja estao sendo revisadas de UX incrementalmente; mobile UX critico permanece no roadmap.
- Nao traduzir EN-US/ES-ES agora (baseline PT-BR basta); so garantir que as chaves existam.

### Meta de saida
- Navegacao: botao Grupos na tela de Partidas + card de estado-vazio reutilizavel com botao em >=2 telas alem de `/grupos`.
- Pagamento: link Pix nunca cai em 404; fluxo manual completo; (se confirmado) default manual para grupos novos, automatico intacto como V2.
- i18n: fluxos principais sem string hardcoded; regra 25 em vigor; +testes das mudancas (TDD).
- `dotnet build` 0 warning; suite verde (as 24 falhas ambientais de Postgres sao esperadas).

### Alvos concretos
- `Pages/Groups/Partidas.razor` (header row), `Pages/Groups/Index.razor` (card onboarding-hint a extrair), telas-alvo do estado vazio (`Pages/Index.razor` `/eventos`, `Pages/MyEvents/Index.razor`).
- `Pages/Groups/Components/PixReceiverSelector.razor:17` (link `/perfil` -> `/profile/{id}`).
- `Data/AppDbContext.cs` + nova migration (default `EnablePaymentGateways`, se confirmado).
- `Services/Core/UiTextService.cs` + `Services/Core/UiText/*` (chaves i18n).

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
24. **Pasta default para prints**: `C:\Users\FreezaTV\Desktop\prints` -- sempre procurar nesta pasta quando o usuario mencionar "ver print na pasta"
25. **i18n obrigatorio (pipeline, junto ao TDD)**: nenhuma string visivel ao usuario pode ser hardcoded em `.razor` (markup, `PageTitle`, atributos `title`/`placeholder`/`alt`) nem em mensagens de retorno de service. Tudo passa pelo `UiTextService` (facade em `Services/Core/UiTextService.cs`, textos por dominio em `Services/Core/UiText/*`) via `@Ui["Dominio.Chave"]` no markup ou `_ui.Get("...")` no service. Ao criar/editar UI: (a) adicionar a chave no dominio correto; (b) preencher PT-BR (baseline); EN-US/ES-ES podem seguir em fase posterior mas a chave ja deve existir. Novas features nao entram sem i18n.

26. **Auditoria/logs como regra no fluxo de desenvolvimento**: alem de TDD, boas praticas de CSS e documentacao, todo incremento deve incluir verificacao de auditoria -- logs relevantes (Serilog) em fluxos criticos, telemetria quando aplicavel, e verificacao de que mudancas de UI/CSS sao confirmadas visualmente (print ou inspecao no browser) antes do commit. O Pleno deve registrar na descricao do PR quais telas foram verificadas visualmente e como. "Nao mudou nada" sem evidencia nao e aceitavel -- se o estilo nao apareceu, investigar causa raiz (cache, ordem de CSS, scoped vs global) antes de commitar.

27. **Entrega do PR ao final do ciclo**: ao concluir um ciclo e fazer push da branch, o Pleno deve responder ao Senior (no chat/IDE) com 3 itens: (a) **title** do PR (titulo conciso, prefixo `feat(cicloN)` ou `refactor(cicloN)`); (b) **body** do PR (resumo do que foi feito por fase, contagem de testes antes/depois, desvios do plano, arquivos modificados); (c) **link de criacao do PR** (URL `https://github.com/.../pull/new/<branch>` gerada pelo `git push`). O Pleno nao deve considerar o ciclo "entregue" ate esses 3 itens estarem apresentados.

28. **Build e suite completa sao responsabilidade do PLENO -- nao do Senior**: rodar `dotnet build --no-incremental` e `dotnet test` (suite inteira, hoje ~2.300 testes) e caro em tempo/tokens e nao pode ser repetido do lado do Senior a cada review. Divisao de trabalho obrigatoria:
    - **Pleno (executa)**: roda build + suite completa **antes de abrir o PR** e cola no corpo do PR, textualmente: a linha `0 Warning(s) / 0 Error(s)`, a linha final do `dotnet test` (`Failed: N, Passed: N, Total: N`) e a classificacao das falhas (as 24 `ProgramConfigurationTests` sem PostgreSQL sao **ambientais**). Sem esses numeros o ciclo **nao esta entregue** (complementa a regra 27). Se a suite estiver vermelha por outro motivo, o PR nao abre.
    - **Senior (revisa)**: le diff, arquitetura, seguranca, caminho do dinheiro, i18n e consistencia com o plano. **Nao** roda a suite completa por padrao -- confia nos numeros do PR e no CI. Pode rodar, no maximo, `dotnet test --filter "FullyQualifiedName~<Area>"` quando precisar **provar** um furo especifico que encontrou, e `dotnet build` quando alterar codigo na propria PR de review.
    - **CI e a rede de seguranca**: o workflow `Build & Test .NET 9` roda a suite completa no GitHub. Divergencia entre o numero declarado no PR e o CI e tratada como problema do Pleno.
    - **E2E de navegador** so acontece quando o Robson pedir explicitamente, nunca como parte automatica da review.
    - Motivo registrado: nas reviews dos Ciclos 27/28 a suite completa foi rodada 2-3x por review no ambiente do Senior, consumindo cota que deveria ir para revisao e direcionamento. A funcao do Senior e organizar, direcionar, orientar e revisar.

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

## CHECKLIST OBRIGATORIO DE RESET PRE-PRODUCAO REAL (bloqueia go-live)

Contexto (decisao do Robson, 14/06/2026): o endereco `https://confirmai.m2gpju.easypanel.host`
roda com `ASPNETCORE_ENVIRONMENT=Production`, mas **nao e producao de verdade** -- e ambiente de
desenvolvimento/teste. Por isso credenciais expostas em chat foram aceitas conscientemente, com a
condicao de que todas sejam resetadas antes do go-live com dominio proprio.

**Nenhum go-live acontece sem executar esta lista inteira.** O risco que ela existe para evitar e
herdar em silencio uma credencial de teste ja vazada, ou um banco com dados de teste.

### Credenciais a rotacionar (todas foram expostas em chat)
- [ ] Google OAuth `client_secret` -- criar novo no Console, **deletar o antigo**.
- [ ] Google OAuth: novo redirect URI `https://<dominio-novo>/signin-google` + JavaScript origin;
      remover as entradas do EasyPanel.
- [ ] `Google__MapsApiKey` -- rotacionar E restringir por HTTP referrer do dominio novo.
      (Restricao por referrer nao espera o go-live: chave de Maps exposta e cobrada na fatura.)
- [ ] `EfiBank__ClientId` / `EfiBank__ClientSecret` -- rotacionar no painel da Efi.
- [ ] `EfiBank__CertificatePath` / `CertificatePassword` -- certificado novo, com senha.
- [ ] `ConnectionStrings__DefaultConnection` -- senha nova do Postgres.
- [ ] `AdminSeed__Password` -- senha aleatoria (nunca placeholder de documentacao).
- [ ] `Email__Password` -- credencial SMTP nova, emitida para o dominio definitivo.
- [ ] `BtcPay__ApiKey` / `BtcPay__WebhookSecret` e `AbacatePay__*` -- quando/se forem configurados.

### Dados
- [ ] Banco novo (ou limpeza total): os usuarios criados durante os testes de Google Auth
      **nao** vao para producao. Tratar o Postgres atual como descartavel.
- [ ] Conferir que nao existe conta admin residual dos testes.
- [ ] Limpar `App_Data/fallback-emails/` (contem tokens de confirmacao/reset validos).

### Configuracao
- [ ] `EfiBank__Sandbox` -- decidir explicitamente (V1 e Pix manual; `true` ate o V2).
- [ ] `Email__Enabled=true` com SMTP real (com `Enabled=false` o codigo desliga
      `RequireConfirmedEmail`, ou seja, ninguem confirma nada).
- [ ] SPF/DKIM/DMARC configurados no dominio definitivo antes do primeiro envio.
- [ ] `AdminSeed__SyncPassword=false` explicito.
- [ ] Porta SMTP **587** (STARTTLS): o `IdentityEmailSender` usa `SmtpClient`, que
      **nao** suporta SSL implicito na 465.
- [ ] Webhooks (`BtcPay__WebhookUrl`, `EfiBank__WebhookUrl`) apontando para o dominio novo.

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
