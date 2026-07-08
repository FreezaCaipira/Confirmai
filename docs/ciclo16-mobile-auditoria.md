# Ciclo 16 - Auditoria de Problemas Mobile

**Data**: 08/07/2026
**Branch**: `fix/ciclo16-mobile-ux`
**Metodologia**: Chrome DevTools (F12) em modo mobile (iPhone SE, iPhone 12 Pro, iPad)

## Problemas Identificados

### Página: `/eventos` (Index.razor)

**Breakpoints existentes**:
- `@media (max-width: 768px)` - Ajusta sports-tabs, my-events-link-btn, sports-grid
- `@media (max-width: 640px)` - Ajusta hero, grid, city-selector

**Problemas potenciais**:
- [ ] Investigar comportamento abaixo de 640px (iPhone SE: 375px)
- [ ] Verificar overflow horizontal em telas muito pequenas
- [ ] Validar wrapping dos cards de esporte
- [ ] Testar city-selector em mobile

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
