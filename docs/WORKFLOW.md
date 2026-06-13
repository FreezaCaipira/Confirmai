# Workflow do Projeto Confirmai

Este documento define o fluxo de trabalho padrão para alterações assistidas por IA/Aider no projeto Confirmai.

O objetivo é reduzir retrabalho, evitar alterações fora do escopo e deixar claro quando uma tarefa está pronta para continuar.

---

## Princípios

1. Toda tarefa deve ter escopo pequeno e verificável.
2. Nenhuma alteração deve começar sem verificar o estado do Git.
3. O Aider não deve fazer commit automático.
4. Arquivos permitidos e proibidos devem ser definidos antes da execução.
5. Qualquer arquivo inesperado deve ser recusado no Aider.
6. O commit deve ser manual, após revisão.
7. O projeto só avança quando o critério de pronto for validado.

---

## Fluxo padrão

### 1. Verificar estado inicial

Antes de qualquer alteração, rode:

```powershell
git status --short
```

Se houver alterações não comitadas, resolva-as antes de prosseguir.

### 2. Definir escopo da tarefa

Especifique claramente:
- O que será alterado
- Arquivos envolvidos
- Resultado esperado

### 3. Configurar Aider

Antes de iniciar, defina:
- Arquivos permitidos para alteração
- Arquivos proibidos (nunca devem ser modificados)
- Pasta de trabalho atual

Exemplo de configuração:

```
Arquivos permitidos:
- README.md
- PROGRESS.md
- docs/WORKFLOW.md

Arquivos proibidos:
- appsettings.json
- qualquer arquivo .cs
- qualquer arquivo dentro de wwwroot/
```

### 4. Executar alterações com Aider

Instrua o Aider com:
1. O escopo definido
2. Os arquivos permitidos
3. O resultado esperado

Exemplo de prompt:

```
Você deve atualizar o README.md para incluir informações sobre a nova funcionalidade de pagamento por evento.
Limite-se aos arquivos: README.md
Não altere: qualquer arquivo .cs, appsettings.json, arquivos em wwwroot/
```

### 5. Revisar alterações

Após as alterações:
- Verifique se apenas arquivos permitidos foram modificados
- Confirme que o conteúdo está correto
- Valide formatação e estrutura

### 6. Validar critério de pronto

Antes do commit, verifique:
- [ ] A alteração atende ao escopo definido
- [ ] Nenhum arquivo proibido foi alterado
- [ ] O conteúdo está tecnicamente correto
- [ ] A formatação está adequada
- [ ] Não há informações sensíveis expostas

### 7. Commit manual

Faça o commit manualmente com mensagem clara:

```bash
git add ARQUIVO_ALTERADO
git commit -m "docs: atualiza README com informações de pagamento por evento"
```

---

## Arquivos permitidos vs proibidos

### Permitidos (podem ser alterados pelo Aider)
- Documentação (README.md, PROGRESS.md, docs/*.md)
- Arquivos de configuração de documentação
- Arquivos de workflow e processo

### Proibidos (nunca devem ser alterados pelo Aider)
- Código fonte (.cs, .razor)
- Arquivos de configuração sensíveis (appsettings.json, secrets)
- Arquivos binários
- Arquivos em wwwroot/
- Arquivos de build e deploy

---

## Boas práticas

1. **Sempre comece com git status**
2. **Defina escopo antes de usar Aider**
3. **Revise todas as alterações antes do commit**
4. **Mantenha commits pequenos e focados**
5. **Use mensagens de commit descritivas**
6. **Nunca commite informações sensíveis**
