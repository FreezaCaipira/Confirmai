# Padrões CSS Mobile - Confirmai

**Data**: 08/07/2026
**Branch**: `fix/ciclo16-mobile-ux`
**Contexto**: Fase 4 - Menu Mobile Hamburger

## Estrutura CSS Atual

### Arquivos Modificados

1. **MainLayout.razor.css** - Menu mobile e internacionalização
2. **Index.razor.css** - Botão meus eventos e city selector
3. **CitySelector.razor.css** - City selector responsivo
4. **site.css** - Header z-index e sidebar

### Breakpoints Atuais

- **MainLayout.razor.css**: `@media (max-width: 700px)`
- **Index.razor.css**: `@media (max-width: 768px)`
- **CitySelector.razor.css**: `@media (max-width: 700px)`

### Elementos Fora do Media Query

**MainLayout.razor.css**:
```css
.mobile-menu-toggle {
    display: none; /* Escondido por padrão (desktop) */
}

.mobile-nav-text {
    display: none; /* Escondido por padrão (desktop) */
}
```

**Razão**: Esses elementos precisam estar fora do media query para serem escondidos no desktop e mostrados apenas no mobile.

## Padrões Estabelecidos

### 1. Separação Desktop/Mobile

**✅ Bom**:
- Todos os estilos mobile dentro de `@media (max-width: XXXpx)`
- Elementos que devem ser escondidos no desktop ficam fora com `display: none`

**⚠️ Atenção**:
- Elementos fora do media query afetam desktop
- Documentar claramente o motivo de estar fora

### 2. Especificidade CSS

**✅ Bom**:
- Usar especificidade mínima necessária
- Aumentar apenas quando necessário (`body .oldsite-top-nav`)
- Não usar `!important`

**Exemplo**:
```css
/* Especificidade baixa (preferido) */
.oldsite-top-nav { }

/* Especificidade aumentada (quando necessário) */
body .oldsite-top-nav { }
```

### 3. Box-Sizing

**✅ Bom**:
- Usar `box-sizing: border-box` em elementos mobile
- Isso evita overflow horizontal

**Exemplo**:
```css
@media (max-width: 700px) {
    .elemento {
        box-sizing: border-box;
        width: 100%;
    }
}
```

### 4. Herança de Estilos

**✅ Bom**:
- Herdar estilos do site.css sempre que possível
- Não sobrescrever cor/fonte desnecessariamente
- Isso evita interferência no layout da web

**Exemplo**:
```css
/* ❌ Ruim - afeta layout da web */
body .oldsite-top-nav .elemento {
    color: var(--ci-text);
    font-size: 0.78rem;
    font-weight: 700;
}

/* ✅ Bom - apenas layout mobile */
body .oldsite-top-nav .elemento {
    width: 100%;
    padding: 0.6rem 0.8rem;
    box-sizing: border-box;
}
```

### 5. Overflow

**✅ Bom**:
- Usar `overflow: hidden` em containers
- Usar `white-space: nowrap` + `text-overflow: ellipsis` em texto longo
- Remover `max-height` e `overflow-y` desnecessários (causam barra de scroll)

## Dúvidas para Senior

### 1. Padronização de Breakpoints

**Problema**: Breakpoints inconsistentes entre arquivos (700px vs 768px).

**Pergunta**: Devemos padronizar todos os breakpoints para `<700px` ou `<768px`? Ou manter diferentes conforme necessário?

**Opções**:
- A) Padronizar para `<700px` (mais conservador)
- B) Padronizar para `<768px` (mais comum na indústria)
- C) Manter diferentes conforme necessidade de cada componente

### 2. Elementos Fora do Media Query

**Problema**: `.mobile-nav-text` e `.mobile-menu-toggle` estão fora do media query.

**Pergunta**: Esta é a melhor abordagem? Ou deveríamos usar uma classe específica no body/desktop?

**Opções**:
- A) Manter atual (elementos fora com `display: none`)
- B) Adicionar classe `.is-mobile` ao body em mobile
- C) Usar `@media (min-width: 701px)` para esconder no desktop

### 3. Testes Específicos para Mobile

**Pergunta**: Devemos criar testes específicos para mobile? (Playwright, Cypress, etc.)

**Opções**:
- A) Sim, criar testes E2E para mobile
- B) Não, testes manuais são suficientes
- C) Criar testes visuais (screenshots) para mobile

### 4. Isolamento de Estilos Mobile

**Pergunta**: Devemos criar um arquivo CSS separado para mobile (ex: `mobile.css`)?

**Opções**:
- A) Sim, criar `mobile.css` para todos os estilos mobile
- B) Não, manter estilos mobile nos arquivos `.razor.css` correspondentes
- C) Criar para componentes compartilhados (MainLayout), manter `.razor.css` para páginas

## Checklist para Novas Telas

Ao adicionar estilos mobile a uma nova tela:

- [ ] Criar/adicionar media query no arquivo `.razor.css` correspondente
- [ ] Usar breakpoint padronizado (definir qual)
- [ ] Colocar todos os estilos mobile dentro do media query
- [ ] Usar `box-sizing: border-box` em elementos com width 100%
- [ ] Herdar estilos do site.css sempre que possível
- [ ] Não sobrescrever cor/fonte desnecessariamente
- [ ] Testar em múltiplos tamanhos (375px, 414px, 768px)
- [ ] Verificar que não afeta layout da web
- [ ] Documentar no `docs/ciclo16-mobile-auditoria.md`

## Próximos Passos

1. Aguardar definição do senior sobre as dúvidas acima
2. Aplicar padrões definidos às demais telas
3. Criar testes se necessário
4. Atualizar documentação conforme decisões
