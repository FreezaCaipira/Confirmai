# Review: Onboarding hint para users novos

## Contexto
O fluxo do Confirmai não é óbvio para users novos: **Grupo → Convidar membros → Criar partida → Confirmações automáticas**. Hoje a tela de grupos vazia diz "Use um código de convite ou crie um novo grupo para começar", mas não explica o porquê nem conecta grupos com partidas.

## Proposta
Adicionar uma faixa explicativa na tela de grupos (`/grupos`) quando o user não tem grupos, explicando o fluxo:

> **Como funciona:**
> 1. Crie um grupo e convide seus amigos
> 2. Crie partidas recortes ou avulsas para o grupo
> 3. Os membros confirmam presença automaticamente

### Detalhes de implementação
- Aparecer apenas quando `groups.Count == 0` (user sem grupos)
- Bloco destacado com fundo sutil + ícone de info
- Opcional: botão "entendi" para dispensar (localStorage)
- Não polui a tela de users que já conhecem o fluxo

### Perguntas para o senior
1. Concorda com a abordagem de faixa contextual vs texto fixo?
2. O fluxo de 3 passos está correto/faltando algo?
3. Vale a pena fazer o mesmo na tela de partidas vazias (`/meus-eventos`)?
4. O botão "entendi" (dispensável) é necessário ou pode ser sempre visível para users sem grupos?

## Alternativas consideradas
- **Tour guiado**: mais completo, mas mais complexo de implementar
- **Texto fixo no header**: simples, mas polui para users experientes
- **Tooltip/modal no primeiro acesso**: intrusivo
