# CSS Mobile/Web Issues - Documentação para Senior

## Contexto
Durante o ciclo de desenvolvimento mobile UX (Ciclo 16), enfrentamos diversas dificuldades em manter layouts separados para mobile e web na tabela de partidas (`GroupDetailEvents.razor`).

## Problemas Identificados

### 1. Falta de Estrutura Clara de Separação

**Problema:** O CSS atual não tem uma separação clara entre estilos mobile e web. Estilos mobile às vezes afetam o web e vice-versa.

**Exemplo:** 
- `et-cell-header`, `et-cell-right`, `et-recurring-icon` foram alternados entre `display: none` e `display: flex/block` dependendo do estado atual
- Isso causou conflitos quando ajustes para web quebravam o mobile e vice-versa

### 2. Media Queries Não Suficientes

**Problema:** Media queries sozinhas não garantem isolamento quando o HTML é compartilhado.

**Caso Recente:**
- HTML tem 5 colunas: ID, Data, Jogadores, Status, Acessar
- CSS mobile ocultava colunas 2-5 (`td:nth-child(2-5) { display: none }`)
- Quando ajustamos o HTML para web (separando ID e Data em colunas diferentes), o mobile parou de funcionar porque esperava tudo na coluna 1

### 3. Dependência de Estrutura HTML

**Problema:** O CSS mobile atual depende fortemente de uma estrutura HTML específica (tudo na coluna 1 com wrappers `.et-cell`).

**Consequência:** Qualquer mudança no HTML para web quebra o mobile, exigindo ajustes manuais no CSS mobile.

### 4. Dificuldade de Reversão

**Problema:** Quando ajustes web não funcionam, é difícil reverter para um estado "ok" anterior sem quebrar o mobile.

**Exemplo:** Tivemos que usar `git reset --hard` para voltar a um commit anterior porque ajustes incrementais criaram um estado inconsistente.

## Proposta de Estrutura CSS

### 1. Prefixo para Classes Mobile

**Sugestão:** Usar prefixo `.mobile-` para classes que só devem existir no mobile.

```css
/* Base - Web por padrão */
.events-table { width: 100%; }

/* Mobile - explicitamente prefixado */
@media (max-width: 480px) {
    .mobile-only { display: block; }
    .mobile-hide { display: none; }
}
```

### 2. Isolamento Total de Componentes

**Sugestão:** Criar componentes CSS separados para mobile e web quando o layout for significativamente diferente.

```css
/* Web */
.events-table-web { /* estilos web */ }

/* Mobile */
@media (max-width: 480px) {
    .events-table-mobile { /* estilos mobile completamente separados */ }
}
```

### 3. Classes de Visibilidade Explícitas

**Sugestão:** Usar classes semânticas para controle de visibilidade em vez de `nth-child`.

```html
<td class="col-id web-only">#1</td>
<td class="col-data mobile-only">10/07/2026</td>
```

```css
@media (max-width: 480px) {
    .web-only { display: none; }
    .mobile-only { display: block; }
}
```

### 4. Documentação de Breakpoints

**Sugestão:** Documentar claramente os breakpoints usados e o que cada um afeta.

```css
/* 
 * BREAKPOINTS
 * - Mobile: max-width: 480px
 * - Tablet: 481px - 768px (futuro)
 * - Desktop: min-width: 769px
 */
```

## Histórico de Problemas Específicos

### Commit f45b806 → 6e56cd0 (Badge Próxima)
- **Objetivo:** Mover badge "próxima" para coluna Status
- **Problema:** CSS mobile esperava badge na coluna Data (dentro de `.et-cell-right`)
- **Solução:** Ajustado HTML, mas mobile quebrou porque media query ocultava coluna 2
- **Fix:** Removido `td:nth-child(2)` da lista de ocultação no mobile

### Commits dae7d24 → f0b64f5 (Ordem de Colunas)
- **Objetivo:** Separar ID e Data em colunas diferentes
- **Problema:** Mobile esperava tudo junto na coluna 1
- **Consequência:** Mobile só mostrava ID, sem Data
- **Reversão:** Usado `git reset --hard` para voltar ao estado anterior

## Recomendações

1. **Não compartilhar HTML complexo entre mobile e web** quando os layouts forem muito diferentes
2. **Usar classes semânticas** (`.mobile-only`, `.web-only`) em vez de seletores estruturais (`nth-child`)
3. **Documentar cada breakpoint** e o que ele afeta
4. **Testar mobile e web** após cada mudança CSS
5. **Considerar componentes separados** para layouts drasticamente diferentes

## Arquivos Afetados

- `wwwroot/css/events.css` - CSS principal da tabela de eventos
- `Shared/Components/Groups/GroupDetailEvents.razor` - HTML da tabela

## Próximos Passos

1. Revisar estrutura CSS atual com base nestas recomendações
2. Implementar prefixo `.mobile-` para classes mobile
3. Criar classes de visibilidade explícitas
4. Documentar todos os breakpoints no projeto
