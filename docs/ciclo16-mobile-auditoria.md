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

### Página: `/profile` (Profile.razor + componentes)

**Status**: ✅ Corrigido e commitado (08e7e57)

**Problemas identificados**:
- Profile page com padding e margin inadequados para mobile
- Message card com fontes e padding inadequados
- Chat thread com altura muito alta para mobile
- Avatar com tamanho muito grande para telas pequenas
- Header card com layout inadequado para mobile
- Edit form grid não se ajustava para coluna única em mobile
- Botões de ação não se ajustavam para mobile

**Correções aplicadas**:
- **Profile.razor.css**:
  - Expandir breakpoint <640px: reduzir padding e margin, reduzir fontes, ajustar max-height do chat thread
  - Adicionar breakpoint <375px: reduzir ainda mais padding, fontes e max-height
- **ProfileHeaderCard.razor.css**:
  - Expandir breakpoint <640px: avatar 60px, font-size 1.5rem, reduzir fontes de name e since
  - Adicionar breakpoint <375px: avatar 52px, font-size 1.3rem, border-width 2px
- **ProfileEditForm.razor.css**:
  - Expandir breakpoint <640px: grid-template-columns 1fr, flex-direction: column nos actions, botões width 100%
  - Adicionar breakpoint <375px: reduzir ainda mais fontes e padding de inputs e botões

### Página: `/admin` (Admin.razor)

**Status**: ✅ Corrigido e commitado (9d81c28)

**Problemas identificados**:
- Admin settings card com padding inadequado para mobile
- Security panels com layout inadequado para mobile
- Admin nav com fontes e padding inadequados
- KPI grid não se ajustava para mobile
- Reconciliation health head e thresholds com layout inadequado
- Fontes e badges inadequados para telas pequenas

**Correções aplicadas (Admin.razor.css)**:
- Expandir breakpoint <640px:
  - admin-settings-card: padding 0.8rem, gap 0.6rem
  - admin-welcome: font-size 1.1rem
  - security-panels: grid-template-columns 1fr, gap 0.7rem
  - security-panel: padding 0.75rem 0.85rem, gap 0.6rem
  - admin-nav: margin reduzido, gap 0.5rem
  - admin-nav a: font-size 0.7rem, padding 0.5rem 0.8rem
  - admin-kpi-grid: grid-template-columns repeat(2, 1fr)
  - admin-kpi-value: font-size 1.3rem
  - admin-reconciliation-health-head: flex-direction: column
  - admin-reconciliation-thresholds: flex-direction: column
- Adicionar breakpoint <375px:
  - admin-welcome: font-size 1rem
  - admin-settings-card: padding 0.7rem 0.6rem, gap 0.5rem
  - security-panel: padding 0.65rem 0.75rem, gap 0.5rem
  - security-panel h4: font-size 0.85rem
  - security-label, security-value: font-size 0.85rem
  - security-boolean-badge: font-size 0.72rem, min-width 42px
  - admin-nav: margin 1.2rem 0 0.8rem, gap 0.4rem
  - admin-nav a: font-size 0.68rem, padding 0.45rem 0.7rem
  - admin-kpi-grid: grid-template-columns 1fr
  - admin-kpi-value: font-size 1.2rem
  - fee-input, fiat-select: font-size 0.85rem, padding 0.32rem 0.5rem
  - security-toggle input: width 14px, height 14px

## Fase 3 — Testes Mobile Cross-Browser

**Status**: ⏳ Em andamento (requer testes manuais)

### Instruções para Testes Manuais

**Aplicação rodando em**: http://localhost:5000

#### Browsers para testar:
1. **Chrome** (Desktop + Mobile DevTools)
2. **Firefox** (Desktop + Mobile DevTools)
3. **Edge** (Desktop + Mobile DevTools)
4. **Safari** (se disponível no macOS/iOS)

#### Tamanhos de tela para testar:
- **320px** - Muito pequeno (iPhone SE antigo)
- **375px** - Pequeno (iPhone SE, iPhone 12/13 mini)
- **414px** - Médio (iPhone 12/13 Pro)
- **768px** - Tablet (iPad portrait)
- **1024px** - Tablet landscape

#### Checklist por página:

**Página `/eventos`**:
- [ ] Sidebar não overflow horizontal
- [ ] Sports tabs wrapping corretamente
- [ ] Event cards em coluna única em mobile
- [ ] City selector funcional
- [ ] "My Conf" cards ajustados
- [ ] Fontes legíveis em <375px

**Página `/grupos`**:
- [ ] Grid de grupos em coluna única em mobile
- [ ] Cards com altura adequada
- [ ] Botões de ação centralizados
- [ ] Badges e fontes legíveis em <375px

**Página `/grupo/{Id}`**:
- [ ] Header e chips wrapping corretamente
- [ ] Admin bar em coluna em mobile
- [ ] Events tabs ajustados
- [ ] Pending requests em coluna
- [ ] Fontes e padding adequados em <375px

**Página `/futsal`**:
- [ ] Event cards wrapping corretamente
- [ ] Time badge tamanho adequado
- [ ] Meta chips legíveis
- [ ] Botão de criar evento centralizado
- [ ] Background icon não intrusivo em <375px

**Página `/poker`**:
- [ ] Event cards wrapping corretamente
- [ ] Dropdown menu alinhado à esquerda em mobile
- [ ] Type badges legíveis
- [ ] Menu dropdown width 100% em mobile
- [ ] Fontes e padding adequados em <375px

**Página `/meus-eventos`**:
- [ ] Header em coluna em mobile
- [ ] View tabs e create buttons ajustados
- [ ] Event cards wrapping corretamente
- [ ] Sport badges tamanho adequado
- [ ] Fontes e badges legíveis em <375px

**Página `/profile`**:
- [ ] Profile page padding adequado
- [ ] Avatar tamanho adequado (60px -> 52px em <375px)
- [ ] Edit form grid em coluna única
- [ ] Botões width 100% em mobile
- [ ] Chat thread altura adequada

**Página `/admin`**:
- [ ] Settings card padding adequado
- [ ] Security panels em coluna única
- [ ] Admin nav ajustado
- [ ] KPI grid ajustado (2 colunas em mobile, 1 em <375px)
- [ ] Fontes e badges legíveis em <375px

#### Como testar:
1. Abrir DevTools (F12)
2. Ativar modo mobile (Ctrl+Shift+M)
3. Selecionar dispositivo ou inserir largura manual
4. Navegar por cada página
5. Verificar itens do checklist
6. Documentar problemas encontrados

#### Problemas a documentar:
- Página/componente
- Largura de tela
- Browser
- Descrição do problema
- Screenshot (opcional)

## Breakpoints Globais (site.css)

**Breakpoints identificados**:
- `@media (max-width: 980px)` - Sidebar reduzida
- `@media (max-width: 700px)` - Sidebar horizontal, main sem margin-left
- `@media (max-width: 480px)` - **NOVO**: Celulares médios (iPhone 12/13 Pro, Android grandes)
- `@media (max-width: 375px)` - Mobile muito pequeno
- `@media (max-width: 760px)` - Vários componentes
- `@media (max-width: 520px)` - Componentes específicos
- `@media (max-width: 1024px)` - Componentes específicos
- `@media (max-width: 1120px)` - Componentes específicos
- `@media (max-width: 1180px)` - Componentes específicos

### Correção Adicional: Menu Superior em Celulares

**Problema identificado**: Menu superior quebrava em celulares (mas funcionava em iPads)

**Correção aplicada (site.css)**:
- Adicionado breakpoint `<480px` para celulares médios (iPhone 12/13 Pro, Android grandes)
- Reduzido padding e gap da sidebar
- Font-size dos links do menu: 0.68rem em <480px, 0.62rem em <375px
- Isso garante que o menu não quebre em dispositivos entre 375px e 480px

**Commit**: 3969373

## Fase 4 — Menu Mobile Hamburger (Correções Críticas)

**Status**: ✅ Corrigido e commitado (Branch: fix/ciclo16-mobile-ux)

### Problemas Identificados

1. **Menu superior quebrava em celulares** (mas funcionava em iPads)
2. **Menu hamburger abria por trás do layout** (z-index não funcionava)
3. **City selector extrapolava suas divs** em mobile
4. **Menu mobile desconfigurado visualmente** (internacionalização não aparecia primeiro)
5. **Botão "Meus Eventos" extrapolava horizontalmente**
6. **Linhas separatorias extrapolavam a direita do menu**
7. **Faltava linha divisória acima do sair**
8. **Background da internacionalização extrapolava/desalinhado**
9. **Ícones de mensagens e perfil sem textos em mobile**

### Correções Aplicadas

#### 1. Implementação do Menu Hamburger (MainLayout.razor + MainLayout.razor.css)

**Problema**: Menu superior quebrava em celulares, precisava de menu hamburger.

**Solução**:
- Adicionado botão `.mobile-menu-toggle` no MainLayout.razor
- Adicionado estado `isMobileMenuOpen` e método `ToggleMobileMenu()`
- CSS: menu hamburger visível apenas em `<700px`
- CSS: menu dropdown com `position: absolute`, `z-index: 9999`

**Commits**:
- 8812f16: Implementação inicial do menu hamburger
- 2c2193d: Reorganização do menu (internacionalização primeiro)

#### 2. Correção do Z-Index (site.css + MainLayout.razor.css)

**Problema**: Menu hamburger abria por trás do layout existente.

**Causa**: `isolation: isolate` no header criava novo stacking context, impedindo z-index de funcionar.

**Solução**:
- Removido `isolation: isolate` de `.oldsite-header` no site.css
- Adicionado `z-index: 100` ao header
- Aumentado especificidade no MainLayout.razor.css usando `body .oldsite-top-nav`

**Commits**:
- 8812f16: Correção inicial do z-index
- dbd2338: Aumento de especificidade CSS

#### 3. Correção do City Selector (CitySelector.razor.css)

**Problema**: City selector extrapolava suas divs em mobile.

**Solução**:
- Adicionado media query `<700px` no CitySelector.razor.css
- Flex-direction: column, width 100%, gap 0.4rem
- City-select: min-width: 0, width 100%
- Adicionado media query `<375px` para telas muito pequenas
- Padding e font-size reduzidos

**Commit**: 8812f16

#### 4. Reorganização do Menu Mobile (MainLayout.razor + MainLayout.razor.css)

**Problema**: Internacionalização não aparecia primeiro no menu mobile.

**Solução**:
- Reordenado elementos no MainLayout.razor (nav-language-flags antes dos links)
- CSS: `order: -1` para nav-language-flags
- CSS: `position: static`, `transform: none` (era absolute com transform)
- CSS: `z-index: auto` (era 1, links tinham 2)

**Commits**:
- 2c2193d: Reorganização inicial
- dbd2338: Correção de especificidade e position/transform

#### 5. Ajuste do Botão "Meus Eventos" (Index.razor.css)

**Problema**: Botão extrapolava horizontalmente (6º report).

**Solução**:
- Múltiplas tentativas de ajuste (font-size, padding, white-space)
- Solução final: `width: auto`, `max-width: 100%`
- `white-space: nowrap`, `overflow: hidden`, `text-overflow: ellipsis`
- Removido ícone (`display: none`) para reduzir largura

**Commits**:
- ddf007b: Ajuste inicial
- 3787ce4: Diminuição vertical (revertido)
- caab903: Remoção de ícone
- 30c9cee: Ajuste final (width: auto)

#### 6. Linhas Separatorias e Layout (MainLayout.razor.css)

**Problema**: Linhas separatorias extrapolavam a direita, faltava linha acima do sair.

**Solução**:
- Adicionado `overflow: hidden` ao menu container
- Adicionado `box-sizing: border-box` a todos os elementos
- Adicionado `border-top` ao `.oldsite-top-nav-logout`
- Removido `max-height` e `overflow-y` (causava barra de scroll)

**Commits**:
- 03a3f93: Correção das linhas separatorias
- 3f6c1bf: Remoção da barra de scroll (revertido)
- 3787ce4: Remoção de max-height/overflow-y

#### 7. Background da Internacionalização (MainLayout.razor.css)

**Problema**: Background extrapolava à direita e tinha lacuna à esquerda.

**Solução**:
- Removido `max-width` e `overflow` do nav-language-flags
- Adicionado `box-sizing: border-box`
- Padding: `0.5rem` (completo)

**Commits**:
- 03a3f93: Correção do background
- 3787ce4: Ajuste de padding (revertido)
- 03a3f93: Volta ao padding completo

#### 8. Textos em Mensagens e Perfil (MainLayout.razor + MainLayout.razor.css)

**Problema**: Ícones de mensagens e perfil sem textos em mobile.

**Solução**:
- Adicionado `<span class="mobile-nav-text">` com textos no MainLayout.razor
- CSS: `display: none` por padrão (desktop)
- CSS: `display: inline` em `<700px` (mobile)
- CSS: `margin-left: 0.5rem` para espaçamento

**Commit**: 14f3eb3

#### 9. Correção de Cor/Fonte (MainLayout.razor.css)

**Problema**: Ícones de mensagens e perfil com cor/fonte diferentes.

**Solução**:
- Tentativa inicial: adicionar color, font-size, font-weight, etc.
- **Revertido**: isso afetava o layout da web
- Solução final: manter apenas estilos de layout (width, padding, border, box-sizing)
- Deixar cor/fonte herdar do site.css

**Commits**:
- d3bef78: Adição de cor/fonte
- 5bc08e8: Adição de display/gap/font
- c521904: Revert (remoção de estilos que afetavam web)

### Estrutura CSS Final

**Princípios**:
1. Todos os estilos mobile dentro de `@media (max-width: 700px)`
2. Não influenciar layout da web (estilos específicos apenas para mobile)
3. Aumentar especificidade apenas quando necessário (`body .oldsite-top-nav`)
4. Usar `box-sizing: border-box` para evitar overflow
5. Herdar estilos do site.css sempre que possível

**Breakpoints**:
- `<700px`: Menu mobile habilitado
- `<375px`: Ajustes para telas muito pequenas

### Commits da Fase 4

1. **8812f16**: Implementação do menu hamburger e correção do city selector
2. **2c2193d**: Reorganização do menu (internacionalização primeiro)
3. **ddf007b**: Correção do overflow do menu e botão meus eventos
4. **acb8ba0**: Correção da sobreposição do menu e separadores
5. **dbd2338**: Correção da especificidade CSS e botão meus eventos
6. **3f6c1bf**: Remoção da barra de scroll visível
7. **3787ce4**: Correção da barra scroll, lacuna internacionalização e botão
8. **caab903**: Correção do background internacionalização e botão meus eventos
9. **03a3f93**: Correção das linhas separatorias, background e linha acima do sair
10. **14f3eb3**: Adição de textos às opções mensagens e perfil no menu mobile
11. **d3bef78**: Correção da coloração dos ícones de mensagens e perfil
12. **5bc08e8**: Correção da fonte e ícones de mensagens e perfil
13. **c521904**: Revert - remoção de estilos que afetavam layout da web
14. **30c9cee**: Ajuste final do botão meus eventos (width: auto)

### Status Final

✅ Menu mobile completamente funcional
✅ Internacionalização aparece primeiro
✅ City selector não extrapola
✅ Botão meus eventos ajustado
✅ Linhas separatorias corretas
✅ Textos em mensagens e perfil (mobile only)
✅ Layout da web não afetado
✅ Estrutura CSS organizada e escalável

### Próximos Passos

1. Testar em múltiplos dispositivos e browsers
2. Aplicar mesma estrutura CSS a outras telas que precisarem de ajustes mobile
3. Documentar padrões CSS estabelecidos para uso futuro

