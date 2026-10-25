# Bateria de testes em prod — Confirmai

Como responder: **"T07 ok"** ou **"T07 falhou"** + print + URL + aparelho/largura + o que você esperava.
Se um teste depender de outro que falhou, responda "T07 bloqueado por T05".

**Desde o C43-B:** pra testar escalacao, placar, votacao e ranking sem juntar gente de verdade, ligue `Testing__MockFillEnabled=true` no EasyPanel. Com a conta Admin, use "Completar com jogadores de teste" na partida. Os jogadores de teste ja entram pagos e com taxa 0, entao eles **nao** servem pros blocos E (pagamento) e H (taxa), que continuam com as contas A, B e C. No fim, "Remover jogadores de teste".

Os resultados (ok, falhou ou bloqueado) ficam no `WORK_PLAN.md`. Os que falharem viram o proximo ciclo do Pleno.

## 0. Preparação (não precisa de ok)
- **Conta A** = organizador (desktop). **Conta B** = jogador (celular, de preferência). **Conta C** = 3º jogador (aba anônima), usada na lista de espera. **Conta Admin** = admin do sistema.
- Use emails reais que você consiga abrir (os testes de email dependem disso).
- Valores pequenos (R$ 1–5). Nada aqui precisa de Pix de verdade: o comprovante pode ser qualquer print.

---

## A. Cadastro e login (`/Identity/Account/...`)
- **T01** Cadastro com email e senha fraca (ex.: `abc123`) → as regras da senha aparecem e o cadastro é recusado com mensagem em PT.
- **T02** Cadastro com senha forte e "Confirmar senha" diferente → avisa que as senhas não conferem.
- **T03** Cadastro válido (conta B) → chega o email de confirmação, em texto legível, sem `&#...;`.
- **T04** Login antes de confirmar o email → é bloqueado e há opção de reenviar a confirmação.
- **T05** Clicar no link do email → email confirmado → login funciona.
- **T06** "Mostrar senha" no cadastro e no login → alterna, alinhado ao campo (desktop e celular).
- **T07** Esqueci a senha → chega o email → redefine → entra com a senha nova.
- **T08** Trocar a senha logado (perfil → trocar senha) → a antiga para de funcionar.
- **T09** Login com Google (conta nova e conta já existente) → entra sem erro, duas vezes seguidas.
- **T10** Errar a senha várias vezes → a conta é bloqueada temporariamente, com mensagem clara.
- **T11** Logout → volta pra landing; clicar em "voltar" no navegador não mostra dados logados.

## B. Grupo e convite (conta A organiza, B entra)
- **T12** A cria um grupo **sem** chave Pix no perfil → o grupo é criado.
- **T13** A tenta criar uma partida **paga** nesse grupo sem Pix → é bloqueado com aviso pra cadastrar o Pix.
- **T14** A cadastra a chave Pix no perfil → a partida paga passa a ser permitida.
- **T15** A copia o link de convite (`/convite/...`) e o botão "compartilhar no WhatsApp".
- **T16** B abre o convite **deslogado** → cadastro/login → volta pro convite e entra no grupo.
- **T17** Abrir o convite com o código alterado (uma letra trocada) → mensagem de convite inválido, sem erro feio.
- **T18** C pede entrada (grupo privado, sem convite) → A vê o pedido → A recusa → C vê a recusa. Depois C pede de novo e A aprova.
- **T19** A promove B a admin em `/grupo/{id}/configuracoes` e depois remove. A não consegue remover o último admin.
- **T20** B tenta sair do grupo **sem dívida** → consegue (depois entre de novo pelo convite).

## C. Futsal — organizador (A)
- **T21** Criar futsal pago (linha e goleiro com preços diferentes), **2 vagas de linha** → aparece na home e em `/grupo/{id}/partidas`.
- **T22** Criar partida com horário que choca com outra no mesmo local → avisa o conflito.
- **T23** Editar o local e o horário → os confirmados recebem o aviso de alteração (email).
- **T24** Editar o **valor** com B confirmado e **devendo** → a dívida de B passa a ser o novo valor.
- **T25** Editar o valor com B **já pago** → o valor de B **não** muda.
- **T26** Reduzir o limite de vagas abaixo do nº de confirmados → anote o que acontece com quem já pagou (ressalva conhecida: pode ir pra espera).
- **T27** Remover um jogador do roster e adicionar vaga extra (ações de admin na partida).
- **T28** Cancelar uma partida (crie uma descartável) → some da home e os confirmados são avisados.

## D. Futsal — jogador (B e C)
- **T29** B confirma presença como linha → aparece na lista e "a pagar" na home.
- **T30** C confirma quando as vagas estão cheias → vai pra **lista de espera**.
- **T31** B (sem pagar) cancela a confirmação → **C é promovido** automaticamente e recebe o aviso.
- **T32** B confirma de novo (vai pra espera) → B sai da espera.
- **T33** C **paga** (ver E) e tenta cancelar a inscrição → é **recusado**.
- **T34** C, com dívida em outra partida, tenta sair do grupo → é **recusado**.
- **T35** Partida passada / cancelada → não aceita confirmar.

## E. Pagamento manual (Pix + comprovante)
- **T36** B abre "pagar" (`/pagamento/evento/...`) → aparece o QR e o copia-e-cola com o **valor + taxa** certos e a chave de A.
- **T37** B envia comprovante com imagem válida → status "em análise".
- **T38** B tenta enviar um arquivo que não é imagem (PDF/txt renomeado) → recusado.
- **T39** B troca o comprovante → fica o novo.
- **T40** A, em `/grupo/{id}/pagamentos`, abre o comprovante (modal) → **recusa** com motivo → B vê "recusado" e pode reenviar.
- **T41** B reenvia → A **aprova** → B fica "pago" (home, partida e página de pagamentos).
- **T42** A marca outro jogador como pago sem comprovante (dinheiro em mão) → e depois desmarca.
- **T43** A clica em "notificar inadimplentes" → o devedor recebe o aviso.
- **T44** B em `/grupo/{id}/pagamentos` → vê **só os próprios** pagamentos; A vê todos.
- **T45** B tenta abrir a URL do comprovante de outro jogador (`/api/pix-proof/{id}` trocando o id) → negado.

## F. Poker (A organiza, B joga)
- **T46** Criar torneio pago com taxa % → só aceita % dentro da faixa min/max; fora dela recusa.
- **T47** B se inscreve no torneio → o valor cobrado = entrada + taxa.
- **T48** Criar cash com **2 mesas** de preços diferentes → B escolhe uma mesa ao se inscrever.
- **T49** B (não pago) troca de mesa → o valor a pagar acompanha a mesa nova.
- **T50** B **pago** tenta trocar de mesa → anote o comportamento.
- **T51** Editar o preço de uma mesa com devedores e pagos → só os devedores mudam.
- **T52** Reduzir vagas abaixo do nº de pagos (cash) → é recusado.
- **T53** Mesa cheia → entra na fila de espera; promovido quando abre vaga.
- **T54** Cancelar o poker (descartável) → avisos e some da home.
- **T55** Grupo **parceiro** (parceria configurada pelo admin, ver I) → B paga só a parcela dele da taxa.

## G. Pós-partida: escalação, placar, MVP, ranking
- **T56** A monta e confirma a escalação → B vê e compartilha o texto.
- **T57** Após a partida, lançar o placar → aparece na partida.
- **T58** Votação de MVP: B vota (não consegue votar em si mesmo) → MVP revelado com quórum.
- **T59** `/grupo/{id}/ranking` → pontos 3 por vitória e 1 por empate batem com os placares.
- **T60** "Copiar pro WhatsApp" do ranking → texto copiado; colar num chat sai legível. Repetir no celular.
- **T61** Desligar ranking/MVP em configurações → somem das telas; religar → voltam.

## H. Taxa e repasse (A)
- **T62** Aba da taxa em `/grupo/{id}/pagamentos` → saldo conta **só pagamentos pagos**.
- **T63** A envia repasse com comprovante → status "em análise".
- **T64** Admin recusa com motivo → A vê o motivo e reenvia → Admin aprova → saldo zera.

## I. Admin do sistema (`/admin/...`)
- **T65** Conta A/B tenta abrir `/admin`, `/admin/revenue`, `/admin/users` → negado.
- **T66** `/admin/revenue` → receita só com pagos; fila de repasses (T64) aparece.
- **T67** Configurar parceria de um grupo → o formulário carrega a parcela atual; salvar e reabrir mantém.
- **T68** Isenção de taxa por grupo, com prazo e motivo → nova cobrança no grupo sai com taxa 0; data passada é recusada.
- **T69** `/admin/users` → bloquear B → B não loga → desbloquear → B loga.
- **T70** `/admin/logs` → aparecem os eventos dos testes acima (comprovante, repasse, bloqueio), com o admin como autor; exportar CSV.
- **T71** Timeline de auditoria de um grupo/partida (link a partir dos logs).
- **T72** `/admin/security` → mudar uma regra (ex.: tamanho da senha) → vale no cadastro → voltar ao padrão.

## J. Emails
- **T73** Todos os emails recebidos nos testes: botão leva à partida certa; texto puro sem `&#...;`.
- **T74** Clicar no botão do email **deslogado** → login → volta pra partida.
- **T75** Emails em EN/ES quando o idioma da conta é EN/ES.

## K. WhatsApp em dry-run (sem chip)
- **T76** Bridge no EasyPanel de pé (log `listening on :8090`, bolinha verde).
- **T77** Com `WhatsApp__DryRun=true` e JID fictício no grupo, criar partida → aparece registro em `WhatsAppDispatches`.

## L. Transversal
- **T78** Landing deslogada: 4 passos com imagens, poker "em breve", um rodapé só (desktop e celular).
- **T79** Trocar idioma (PT/EN/ES) → home, partida, pagamento e Identity traduzidos, sem chave crua tipo `Group.Xxx`.
- **T80** Celular 390 px e 320 px (ou DevTools): home, grupo, partida, pagar, pagamentos e configurações sem rolagem lateral.
- **T81** Nome de grupo/local bem longo → quebra a linha, não estoura o layout.
- **T82** Página inexistente (`/futsal/999999`) → mensagem de não encontrado, sem erro técnico.
- **T83** Recarregar a página (F5) no meio do fluxo de pagamento → continua no mesmo estado.
