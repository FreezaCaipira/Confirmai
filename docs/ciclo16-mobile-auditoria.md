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

**Status**: Pendente auditoria

### Página: `/grupo/{Id}` (Groups/Detail.razor)

**Status**: Pendente auditoria

### Página: `/futsal` (Futsal/Index.razor)

**Status**: Pendente auditoria

### Página: `/poker` (Poker/Index.razor)

**Status**: Pendente auditoria

### Página: `/meus-eventos`

**Status**: Pendente auditoria

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
