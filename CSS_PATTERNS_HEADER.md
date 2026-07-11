# Padrões CSS - Header de Detalhes

## Padrão Universal (Aplicável a Todas as Telas)

### Estrutura HTML Padrão
```html
<header class="detail-header">
    <div class="detail-header-top">
        <a href="/voltar" class="detail-back-link">
            <i class="fas fa-arrow-left"></i> Voltar
        </a>
    </div>
    <div class="detail-header-title-row">
        <span class="detail-header-subtitle">
            Ícone + Título Secundário
        </span>
    </div>
    <h1 class="detail-title">Título Principal</h1>
</header>
```

### Classes CSS Base (Web - Padrão)
```css
.detail-header { 
    display: flex; 
    flex-direction: column; 
    gap: 0.4rem; 
}

.detail-header-top {
    display: flex;
    align-items: center;
    justify-content: flex-start;  /* Web: voltar à esquerda */
    gap: 0.5rem;
}

.detail-header-title-row {
    display: flex;
    align-items: center;
    justify-content: center;  /* Web: centralizado */
    gap: 0.5rem;
    width: 100%;
}

.detail-header-subtitle {
    display: flex;
    align-items: center;
    justify-content: center;  /* Web: centralizado */
    gap: 0.3rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--ci-text-blue);
}

.detail-title {
    font-size: 1.5rem;
    font-weight: 800;
    color: var(--ci-text);
    margin: 0;
    line-height: 1.2;
    text-align: center;  /* Web: centralizado */
}
```

### Media Query Mobile (max-width: 560px)
```css
@media (max-width: 560px) {
    .detail-header-top { 
        flex-direction: column; 
        align-items: flex-start;  /* Mobile: voltar à esquerda */
        gap: 0.5rem; 
    }
    
    .detail-header-title-row { 
        flex-direction: column; 
        align-items: center;  /* Mobile: centralizado */
        gap: 0.5rem; 
    }
    
    .detail-header-subtitle { 
        justify-content: center;  /* Mobile: centralizado */
    }
    
    .detail-title { 
        font-size: 1.25rem; 
    }
}
```

## Regras de Breakpoints

### Mobile Estrito (max-width: 480px)
- Transformações drásticas de layout (tabelas em cards, etc.)
- Usar apenas quando necessário para telas muito pequenas

### Mobile Padrão (max-width: 560px)
- Ajustes de layout mobile (flex-direction column, centralização)
- Usar para mudanças de layout mobile gerais

## Padrão de Tabelas

### Web (Padrão)
- Tabelas em formato tradicional (thead, tbody, tr, td)
- Layout em grid
- Elementos mobile ocultos via classes base (`display: none`)

### Mobile (max-width: 480px)
- Tabelas transformadas em cards
- `thead` oculto
- `tr` transformado em `flex` com `flex-direction: column`
- `td` transformado em `block`
- Elementos mobile visíveis via media query (`display: block/flex`)

## Regras de Elementos Mobile

### Classes Mobile - Ocultas na Web
Classes específicas para mobile devem ser ocultas na web via classes base:
```css
.et-cell-header { display: none; }
.et-cell-right { display: none; }
.et-cell-meta { display: none; }
.et-players-mobile { display: none; }
.et-chevron-icon { display: none; }
```

### Media Query Mobile - Mostrar Elementos
Na media query mobile, mostrar esses elementos:
```css
@media (max-width: 480px) {
    .et-cell-header { display: flex; }
    .et-cell-right { display: flex; }
    .et-cell-meta { display: flex; }
    .et-players-mobile { display: flex; }
    .et-chevron-icon { display: block; }
}
```

## Regras Gerais

1. **Centralização**: Textos centralizados por padrão (web e mobile)
2. **Botão Voltar**: À esquerda por padrão (web e mobile)
3. **Breakpoints**: Usar 560px para mobile padrão, 480px para mobile estrito
4. **Isolamento**: Mudanças mobile apenas via media queries, nunca em classes base
5. **Consistência**: Aplicar mesmo padrão em todas as telas com header de detalhes
6. **Elementos Mobile**: Classes mobile devem ser ocultas na web via `display: none` nas classes base, e mostradas apenas via media query mobile
