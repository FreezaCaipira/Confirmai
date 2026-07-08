# Ciclo 16 - Auditoria de Problemas Mobile (PRIORIDADE CRÍTICA)

**Data**: 08/07/2026
**Branch**: `fix/ciclo16-mobile-ux`
**Prioridade**: CRÍTICA - Público predominante mobile
**Metodologia**: Chrome DevTools (F12) em modo mobile (iPhone SE, iPhone 12 Pro, iPad)

## Contexto

Este projeto terá a maioria dos usuários acessando via dispositivos móveis. A experiência mobile não é apenas uma melhoria UX, mas um requisito crítico de negócio. Todos os problemas identificados nesta auditoria devem ser tratados com máxima prioridade.

## Problemas Identificados

### Página: `/eventos` (Index.razor)

**Screenshots analisados**: 1.PNG a 6.PNG (crescimento progressivo de largura)

**1.PNG - Largura muito pequena (~320px)**:
- [ ] Sidebar ocupa largura total horizontalmente, navegadores ficam muito pequenos
- [ ] Cards de esporte não visíveis ou cortados
- [ ] Hero title muito grande para o espaço disponível
- [ ] Overflow horizontal evidente

**2.PNG - Largura pequena (~375px - iPhone SE)**:
- [ ] Sidebar ainda ocupa muito espaço horizontal
- [ ] Navegação horizontal com scroll no sidebar
- [ ] Cards de esporte começam a aparecer mas layout comprimido
- [ ] City-selector quebrado ou cortado

**3.PNG - Largura média-pequena (~480px)**:
- [ ] Sidebar ainda em modo horizontal ocupando espaço significativo
- [ ] Grid de cards ainda não se ajustou para coluna única
- [ ] Elementos sobrepostos ou com spacing inadequado
- [ ] Botão "Meus Eventos" posicionado incorretamente

**4.PNG - Largura média (~640px)**:
- [ ] Sidebar começa a se ajustar mas ainda ocupa espaço
- [ ] Grid de cards em transição entre multi-coluna e single-coluna
- [ ] Filtros e city-selector com layout inconsistente
- [ ] Padding/margin inadequado para o tamanho

**5.PNG - Largura média-grande (~768px - tablet portrait)**:
- [ ] Sidebar ainda visível quando deveria ser colapsada
- [ ] Grid de cards em layout intermediário não otimizado
- [ ] Elementos de UI não aproveitando o espaço disponível
- [ ] Responsividade inconsistente entre componentes

**6.PNG - Largura grande (~980px+)**:
- [ ] Layout começa a se estabilizar mas ainda com problemas
- [ ] Sidebar em tamanho intermediário não ideal
- [ ] Cards e elementos com spacing desproporcional
- [ ] Transição para desktop não suave

**Problemas gerais identificados**:
- [x] Sidebar não colapsa adequadamente em mobile
- [x] Grid de cards não responde de forma consistente aos breakpoints
- [x] Overflow horizontal em múltiplas larguras
- [x] Elementos fixos (como "Meus Eventos") não se reposicionam corretamente
- [x] City-selector e filtros não se adaptam ao espaço disponível
- [x] Transições entre breakpoints não suaves
- [x] Falta de breakpoint padrão para telas muito pequenas (<375px)

### Correções Aplicadas (/eventos)

**site.css**:
- Ajustado sidebar para mobile (<700px): reduzido padding de 0.4rem para 0.3rem
- Adicionado `-webkit-overflow-scrolling: touch` para scroll suave no sidebar
- Reduzido fontes de navegação de 0.74rem para 0.7rem
- Reduzido padding de links de 0.45rem 0.65rem para 0.35rem 0.5rem
- Adicionado breakpoint <375px para telas muito pequenas:
  - Padding sidebar: 0.25rem 0
  - Fontes navegação: 0.65rem
  - Padding links: 0.3rem 0.4rem
  - Padding main-content: 0.5rem 0.4rem

**Index.razor.css**:
- Ajustado sports-tabs para mobile: flex-direction: column com gap 0.5rem
- Ajustado my-events-link-btn: width 100%, justify-content: center
- Ajustado sports-grid: grid-template-columns: 1fr com gap 1rem
- Adicionado breakpoint <375px:
  - Hero title: 1.25rem (de 1.45rem)
  - Hero sub: 0.85rem
  - Sport card padding: 1.25rem 1rem 1rem
  - Sport card icon: 2.2rem (de 2.8rem)
  - Sport card name: 1.2rem (de 1.45rem)
  - Sport card desc: 0.85rem
  - Sport card CTA: 0.8rem
  - City selector inputs: font-size 0.85rem, padding reduzido
  - My conf card: padding 0.5rem 0.6rem
  - My conf name: 0.85rem
  - My conf btn: font-size 0.7rem, padding 0.25rem 0.5rem

**Status**: ✅ Corrigido e commitado (46b7be7)

### Página: `/grupos` (Groups/Index.razor)

**Status**: ✅ Corrigido e commitado (077aea9)

**Problemas identificados**:
- Grid de grupos não se ajustava para coluna única em mobile
- Cards de grupo com altura fixa muito alta para mobile
- Fontes e padding inadequados para telas pequenas
- Botão de aprovação pendente posicionado incorretamente em mobile pequeno

**Correções aplicadas (events.css)**:
- Expandir breakpoint <760px:
  - groups-grid: grid-template-columns: 1fr
  - group-card: min-height 220px (de 260px)
  - Reduzir padding e fontes
- Adicionar breakpoint <375px:
  - groups-block: padding 0.6rem 0.5rem 0.7rem
  - groups-title: font-size 1.1rem
  - join-code-input: font-size 0.9rem, padding reduzido
  - group-card: min-height 200px
  - group-card-icon: font-size 3.5rem (de 4.5rem)
  - group-card-sport: font-size 0.65rem
  - player-tag: font-size 0.65rem, padding 0.2rem 0.4rem
  - group-card-name: font-size 1rem
  - group-card-meta: flex-direction: column, gap 0.3rem
  - group-card-pending-actions: bottom 0.4rem, right 0.4rem
  - group-card-approve-btn: font-size 0.75rem

### Página: `/grupo/{Id}` (Groups/Detail.razor)

**Status**: ✅ Corrigido e commitado (aa41b06)

**Problemas identificados**:
- Header e chips não se ajustavam para mobile
- Admin bar com layout horizontal inadequado
- Section header e filters com layout inadequado
- Events tabs não se ajustavam para mobile
- Pending requests com layout horizontal inadequado
- Fontes e padding inadequados para telas pequenas

**Correções aplicadas (events.css)**:
- Expandir breakpoint <560px:
  - detail-header-top: flex-direction: column, align-items: flex-start
  - detail-chips: flex-wrap: wrap
  - detail-admin-bar: flex-direction: column, width: 100%
  - detail-admin-btn: width: 100%, justify-content: center
  - detail-section-header: flex-direction: column
  - detail-section-filters: width: 100%, flex-wrap: wrap
- Expandir breakpoint <480px:
  - detail-card: padding 1rem 0.8rem
  - detail-title: font-size 1.1rem
  - detail-sport-badge: font-size 0.7rem
  - detail-chip: font-size 0.75rem
  - detail-events-header: flex-direction: column
  - detail-events-tabs: flex-direction: column, width: 100%
  - detail-events-tab: flex: 1, justify-content: center
  - pending-request-item: flex-direction: column
  - pending-request-actions: width: 100%
  - bulk-actions: width: 100%
- Adicionar breakpoint <375px:
  - detail-card: padding 0.85rem 0.6rem
  - detail-title: font-size 1rem
  - detail-sport-badge: font-size 0.65rem
  - detail-chip: font-size 0.7rem
  - detail-admin-btn: font-size 0.8rem
  - detail-section-title: font-size 0.9rem
  - detail-events-title: font-size 0.95rem
  - detail-events-tab: font-size 0.75rem
  - pending-request-item: padding 0.6rem 0.75rem
  - pending-request-name: font-size 0.85rem
  - pending-request-meta: flex-direction: column
  - btn-approve, btn-reject: font-size 0.75rem
  - detail-pix-notice: padding 0.75rem

### Página: `/futsal` (Futsal/Index.razor)

**Status**: ✅ Corrigido e commitado (7cd83c4)

**Problemas identificados**:
- Event cards com padding e fontes inadequados para mobile
- Event time com tamanho muito grande para telas pequenas
- Meta chips com fontes e padding inadequados
- Botão de criar evento não centralizado em mobile
- Background icon muito grande para telas pequenas

**Correções aplicadas (Futsal/Index.razor.css)**:
- Expandir breakpoint <640px:
  - event-card: padding 0.85rem 1rem, gap 0.75rem
  - event-time: font-size 1.1rem, min-width: auto
  - event-name: font-size 0.95rem
  - event-venue-name: font-size 0.78rem
  - event-location: font-size 0.75rem
  - meta-chip: font-size 0.7rem, padding 0.15rem 0.5rem
  - listing-page-actions: justify-content: center
- Adicionar breakpoint <375px:
  - event-card: padding 0.75rem 0.85rem, gap 0.6rem
  - event-time: font-size 1rem, min-width: 2.5rem
  - event-name: font-size 0.9rem
  - event-venue-name: font-size 0.75rem
  - event-location: font-size 0.72rem
  - event-meta: gap 0.25rem
  - meta-chip: font-size 0.65rem, padding 0.12rem 0.4rem
  - event-action: font-size 0.75rem, padding 0.35rem 0.7rem
  - event-card-bg-icon: font-size 2.5rem, right: 1rem
  - btn-create-event: font-size 0.75rem, padding 0.35rem 0.8rem

### Página: `/poker` (Poker/Index.razor)

**Status**: ✅ Corrigido e commitado (a1022db)

**Problemas identificados**:
- Event cards com padding e fontes inadequados para mobile
- Event time com tamanho muito grande para telas pequenas
- Poker type badges e meta chips com fontes inadequadas
- Dropdown de criar evento alinhado à direita em mobile
- Menu dropdown com largura fixa inadequada para mobile
- Background icon muito grande para telas pequenas

**Correções aplicadas (Poker/Index.razor.css)**:
- Expandir breakpoint <640px:
  - event-card: padding 0.85rem 1rem, gap 0.75rem
  - event-time: font-size 1.1rem, min-width: auto
  - event-name: font-size 0.95rem
  - event-location: font-size 0.75rem
  - poker-type-badge: font-size 0.68rem, padding 1px 6px
  - meta-chip: font-size 0.7rem, padding 0.15rem 0.5rem
  - create-poker-dropdown: text-align: center
  - create-poker-menu: left: 0, min-width: 100%
- Adicionar breakpoint <375px:
  - event-card: padding 0.75rem 0.85rem, gap 0.6rem
  - event-time: font-size 1rem, min-width: 2.5rem
  - event-name: font-size 0.9rem
  - event-location: font-size 0.72rem
  - event-meta: gap 0.25rem
  - poker-type-badge: font-size 0.65rem, padding 1px 5px
  - meta-chip: font-size 0.65rem, padding 0.12rem 0.4rem
  - event-action: font-size 0.75rem, padding 0.35rem 0.7rem
  - event-card-bg-icon: font-size 2.5rem, right: 1rem
  - btn-create-event: font-size 0.8rem, padding 0.4rem 1rem
  - create-poker-menu-item: padding 0.6rem 0.85rem, gap 0.6rem
  - create-poker-menu-item i: font-size 1rem, width 1.3rem
  - create-poker-menu-item strong: font-size 0.85rem
  - create-poker-menu-item small: font-size 0.7rem

### Página: `/meus-eventos` (MyEvents/Index.razor)

**Status**: ✅ Corrigido e commitado (94a11c7)

**Problemas identificados**:
- Event cards com padding inadequado para mobile
- Header com layout horizontal inadequado
- View tabs e create buttons não se ajustavam para mobile
- Sport badges com tamanho muito grande para telas pequenas
- Fontes e badges inadequados para telas pequenas
- Background icon muito grande para telas pequenas

**Correções aplicadas (MyEvents/Index.razor.css)**:
- Expandir breakpoint <640px:
  - events-block: padding 0.8rem 0.8rem 0.9rem
  - my-event-card: padding 0.7rem 0.8rem, gap 0.6rem
  - my-events-header: flex-direction: column, align-items: flex-start
  - my-events-view-tabs: width 100%, justify-content: flex-start
  - my-events-create-btns: width 100%, justify-content: flex-start
  - my-event-sport-badge: width 2rem, height 2rem, font-size 1rem
  - my-event-name: font-size 0.9rem
  - my-event-meta: font-size 0.75rem
  - my-event-location: font-size 0.73rem
- Adicionar breakpoint <375px:
  - events-block: padding 0.6rem 0.6rem 0.7rem
  - events-block .page-section-title: font-size 1.2rem
  - my-event-card: padding 0.6rem 0.7rem, gap 0.5rem
  - my-event-sport-badge: width 1.8rem, height 1.8rem, font-size 0.9rem
  - my-event-name: font-size 0.85rem
  - my-event-status: font-size 0.65rem, padding 0.12rem 0.4rem
  - my-event-type-badge: font-size 0.65rem, padding 0.12rem 0.4rem
  - my-event-meta: font-size 0.7rem
  - my-event-location: font-size 0.68rem
  - my-event-action-btn: font-size 0.72rem, padding 0.28rem 0.6rem
  - my-events-tab: font-size 0.75rem, padding 0.25rem 0.6rem
  - my-events-create-btn: font-size 0.75rem, padding 0.35rem 0.7rem
  - my-event-card-bg-icon: font-size 2.5rem, right: 0.8rem
  - my-schedule-card: padding 0.6rem 0.7rem, gap 0.5rem

### Página: `/profile`

**Status**: Pendente auditoria

### Página: `/admin`

**Status**: Pendente auditoria

## Breakpoints Globais (site.css)

**Breakpoints identificados**:
- `@media (max-width: 980px)` - Sidebar reduzida
- `@media (max-width: 700px)` - Sidebar horizontal, main sem margin-left
- `@media (max-width: 760px)` - Vários componentes
- `@media (max-width: 480px)` - Alguns componentes específicos
- `@media (max-width: 520px)` - Componentes específicos
- `@media (max-width: 1024px)` - Componentes específicos
- `@media (max-width: 1120px)` - Componentes específicos
- `@media (max-width: 1180px)` - Componentes específicos
- `@media (max-width: 1280px)` - Componentes específicos

**Observação**: Muitos breakpoints diferentes sem padronização clara. Possível causa de inconsistências.

## Próximos Passos

1. Testar cada página em Chrome DevTools com diferentes tamanhos de tela
2. Documentar problemas específicos com screenshots
3. Priorizar correções por criticidade
