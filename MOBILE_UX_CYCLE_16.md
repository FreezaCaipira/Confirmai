# Ciclo 16 - Refinamento Mobile da Tela de Partidas

## Contexto
Refinamento do layout mobile da tela de partidas (`Pages/Groups/Partidas.razor`) com foco exclusivo na experiência mobile, sem impactar a versão desktop.

## Problema Inicial
As mudanças aplicadas para mobile estavam afetando indevidamente a versão desktop devido à falta de isolamento CSS. A media query da tabela de partidas estava usando um breakpoint muito largo (`@media (max-width: 760px)`), afetando telas desktop.

**Problema Específico**: A media query `@media (max-width: 760px)` que transforma a tabela de partidas em cards estava sendo aplicada em telas desktop, causando layout bugado na web.

## Solução Implementada
Ajuste do breakpoint da media query da tabela de partidas de `760px` para `560px`, restringindo o layout card apenas para telas realmente mobile. As classes base mantêm a centralização como padrão para mobile e web.

## Estrutura CSS Final

### Classes Base (Padrão Mobile e Web)
```css
.detail-header-top {
    display: flex;
    align-items: center;
    justify-content: center;  /* padrão: centralizado */
    gap: 0.5rem;
}

.detail-header-title-row {
    display: flex;
    align-items: center;
    justify-content: center;  /* padrão: centralizado */
    gap: 0.5rem;
    width: 100%;
}

.detail-header-subtitle {
    display: flex;
    align-items: center;
    justify-content: center;  /* padrão: centralizado */
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
    text-align: center;  /* padrão: centralizado */
}
```

### Media Query Mobile (max-width: 560px)
```css
@media (max-width: 560px) {
    .detail-card       { padding: 1.25rem 1rem; }
    .detail-title      { font-size: 1.25rem; }
    .position-btns .confirm-btn { min-width: 0; }
    .create-event-card { padding: 1.5rem 1rem; }
    .detail-header-top { flex-direction: column; align-items: flex-start; gap: 0.5rem; }
    .detail-header-title-row { flex-direction: column; align-items: center; gap: 0.5rem; }
    .detail-chips { flex-wrap: wrap; }
    .detail-admin-bar { flex-direction: column; align-items: stretch; gap: 0.5rem; }
    .detail-admin-btn { width: 100%; justify-content: center; }
    .detail-section-header { flex-direction: column; align-items: flex-start; gap: 0.5rem; }
    .detail-section-filters { width: 100%; flex-wrap: wrap; }
    .detail-section-filters .input { flex: 1; min-width: 0; }
    
    /* Tabela de partidas transformada em cards apenas em telas pequenas */
    .events-table {
        font-size: 0.85rem;
    }
    .events-table thead {
        display: none;
    }
    .events-table tbody tr.events-table-row {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        padding: 0.75rem;
        border: 1px solid var(--ci-accent);
        background: rgba(15, 26, 48, 0.4);
        border-radius: 8px;
        margin-bottom: 0.5rem;
    }
    /* ... demais estilos mobile da tabela ... */
}
```

## Layout Final Mobile

### Header
- **Row 1**: Botão "Voltar" (à esquerda)
- **Row 2**: "⚽ Futsal — Partidas 2026" (centralizado)
- **Row 3**: Nome do grupo (centralizado)

### Menu Hamburguer
- Estilizado com azul (`var(--ci-accent-mid)`) para melhor legibilidade
- Posição original mantida (sem `position: absolute`)
- Estilo aplicado em `site.css` para `.mobile-menu-toggle`

### Notice de Recorrência
- "Partidas geradas automaticamente (semanal)" centralizado
- Classe `.events-recurring-notice` com `justify-content: center`

### Botões de Filtro
- "Próximas" e "Realizadas" com estilo original (sem destaque azul)
- Botão "Nova partida" centralizado acima dos tabs de filtro

### Cards de Partidas
- Borda azul (`var(--ci-accent)`)
- #9 posicionado absolutamente na esquerda (texto branco sobre fundo `var(--ci-accent-mid)`)
- Ícone recorrente + data + hora + badge "próxima" centralizados/direita
- Jogadores e status na linha inferior
- Chevron posicionado absolutamente na direita (texto branco)

## Commits Relacionados
- `48c3c6d`: fix: centralizar texto do events-recurring-notice
- `3397328`: fix: mover botão voltar para esquerda
- `768d96e`: fix: corrigir media query para botão voltar à esquerda no mobile
- `46dd49b`: fix: ajustar breakpoint da tabela de partidas para não afetar web

## Lições Aprendidas

### 1. Breakpoint Adequado
**Problema**: Media query `@media (max-width: 760px)` afetava telas desktop.
**Solução**: Ajustar para `@media (max-width: 560px)` para restringir apenas a telas realmente mobile.

### 2. Centralização como Padrão
**Problema**: Tentativa de isolar centralização removeu padrão correto.
**Solução**: Manter centralização nas classes base (padrão mobile e web), usar media queries apenas para overrides específicos.

### 3. Estilização de Componentes Globais
**Problema**: Menu hamburguer é componente global (`MainLayout.razor`), estilização afeta todas as páginas.
**Solução**: Estilizar apenas visualmente (cores, bordas) sem alterar posicionamento.

## Recomendações Futuras

### Para Novos Ciclos Mobile
1. **Usar breakpoints restritos** (560px ou menos) para mudanças mobile
2. **Manter padrões nas classes base** quando aplicável a mobile e web
3. **Testar em ambas as viewports** após cada mudança
4. **Verificar impacto em componentes relacionados** (tabelas, cards, etc.)
5. **Documentar estrutura CSS** para referência futura

### Estrutura Ideal CSS
```css
/* Classes base - valores padrão (mobile e web) */
.classe {
    /* propriedades padrão */
}

/* Media queries - overrides mobile específicos */
@media (max-width: 560px) {
    .classe {
        /* overrides específicos para mobile */
    }
}
```

## Status
- ✅ Layout mobile finalizado e aprovado
- ✅ Centralização mantida como padrão (mobile e web)
- ✅ Breakpoint ajustado para não afetar web
- ✅ Tabela de partidas web intacta
- ✅ Documentação criada para referência

## Próximos Passos
- Validar layout mobile em dispositivos reais
- Coletar feedback do usuário sobre experiência mobile
- Aplicar mesmo padrão de breakpoints em futuros ciclos mobile
