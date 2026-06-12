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