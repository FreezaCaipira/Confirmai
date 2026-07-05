# Ciclo 12 — Revisao Senior: Pendencias e Ajustes Fora do Escopo

> **Branch**: `fix/ciclo12-ux-cleanup`
> **Data**: 05 Jul 2026
> **Status**: Pendencia nao resolvida — requer analise do Senior

---

## 1. PENDENCIA NAO RESOLVIDA: Background da tela de eventos e subtelas

### Problema
A tela de eventos (`/eventos`) e suas subtelas (`/futsal`, `/poker`) nao seguem o padrao de cores de background da tela de grupos (`/grupos`).

### Padrao esperado (norte: tela de listagem de grupos `/grupos`)
- **Nivel 1 (container da pagina)**: sem background proprio — herda o body (`var(--ci-bg): #090f18`)
- **Nivel 2 (block/conteudo)**: degrade cinza → azul escuro
  ```css
  background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%);
  /* --ci-bg-alt: #0a1928; --ci-bg: #090f18; */
  border: 1px solid var(--ci-border);
  border-radius: 14px;
  box-shadow: inset 0 1px 0 rgba(79, 156, 248, 0.14);
  ```
- **Cards (nivel 3)**: gradient por esporte (futsal verde, poker roxo) com overlay vertical, icone de fundo com baixa opacidade, hover com elevacao

### Estado atual das telas
| Tela | Rota | Container | Background atual | Problema |
|------|------|-----------|------------------|----------|
| Grupos | `/grupos` | `.groups-page` + `.groups-block` | Degrade correto | OK (padrao de referencia) |
| Eventos | `/eventos` | `.sports-page` + `.sports-shell` | Degrade aplicado | Parcialmente alinhado |
| Futsal | `/futsal` | `.listing-page` + `.listing-block` | Degrade aplicado no `.listing-block` | Container `.listing-page` sem background definido |
| Poker | `/poker` | `.listing-page` + `.listing-block` | Degrade aplicado no `.listing-block` | Container `.listing-page` sem background definido |
| Meus Eventos | `/meus-eventos` | `.events-page` + `.events-block` | Degrade aplicado | Alinhado, mas tela nao tem link de navegacao |

### O que foi tentado (3 tentativas, todas sem efeito visual confirmado pelo usuario)

#### Tentativa 1 — Modificar `Pages/MyEvents/Index.razor` (rota `/meus-eventos`)
- **O que foi feito**: Trocado `entity-shell` por `events-page`/`events-block` com degrade
- **Resultado**: Sem efeito visual — o usuario estava olhando `/eventos` e `/futsal`, nao `/meus-eventos`
- **Erro de diagnostico**: Confusao entre tela de eventos (`Pages/Index.razor`, rota `/eventos`) e tela de meus eventos (`Pages/MyEvents/Index.razor`, rota `/meus-eventos`)
- **Commits**: `0763d5a`, `19bf314`

#### Tentativa 2 — Aplicar gradient nos event-card de `/futsal` e `/poker`
- **O que foi feito**:
  - Adicionado `<div class="event-card-overlay">` e `<div class="event-card-bg-icon">` no Razor de `Futsal/Index.razor` e `Poker/Index.razor`
  - CSS: gradient `linear-gradient(135deg, var(--futsal-bg-dark), var(--ci-bg-deepest))` nos cards de futsal, `linear-gradient(135deg, var(--poker-bg-deepest), var(--ci-bg-deepest))` nos cards de poker
  - Adicionado `position: relative`, `overflow: hidden`, z-index nos elementos de conteudo
  - Removido ~130 linhas de CSS duplicado em `Poker/Index.razor.css`
  - Adicionado degrade no `.sports-shell` de `Pages/Index.razor.css`
  - Adicionado botao "Meus Eventos" na tela `/eventos`
- **Resultado**: Usuario reportou que "a tela de eventos e suas subtelas continuam com background diferente"
- **Possivel causa**: O degrade foi aplicado nos cards (nivel 3) e no `.sports-shell` (nivel 2 da tela /eventos), mas o container das subtelas `/futsal` e `/poker` (`.listing-page`) continuava sem background
- **Commit**: `1c311f0`

#### Tentativa 3 — Envolver `EventListingShell` em container com degrade
- **O que foi feito**:
  - Adicionado `<div class="listing-block">` no `EventListingShell.razor` envolvendo todo o conteudo
  - CSS `.listing-block` com `linear-gradient(180deg, var(--ci-bg-alt), var(--ci-bg))` — igual ao `.groups-block`
  - `.listing-page` reestruturado para `max-width: 980px; margin: 0 auto` — igual ao `.groups-page`
- **Resultado**: Usuario reportou que "nao vai, desisto" — efeito visual nao confirmado
- **Possivel causa**: Pode ser cache do navegador, hot reload nao aplicando scoped CSS de componentes compartilhados, ou especificidade CSS sobrepondo o degrade
- **Commit**: `d59be9e`

### Analise tecnica para o Senior
1. **Scoped CSS em componente compartilhado**: `EventListingShell.razor.css` e scoped, mas o componente e usado por ambas as paginas `/futsal` e `/poker`. O Blazor scoped CSS aplica atributos `b-xxx` nos elementos do componente. Verificar se o scoped CSS esta sendo aplicado corretamente em ambos os contextos
2. **Especificidade CSS**: O `body` tem `background: var(--ci-bg)` (linha 489 de `site.css`) e `background-attachment: fixed` (linha 3012). O `.listing-block` deveria sobrepor isso dentro do seu escopo, mas verificar se ha conflito
3. **Cache/hot reload**: O usuario testava sem rebuild completo em varios momentos devido ao file lock do `Confirmai.exe`. Algumas mudancas podem nao ter sido aplicadas visualmente
4. **Diferenca estrutural**: A tela de grupos usa `.groups-page` (sem background) + `.groups-block` (com degrade). As subtelas usam `.listing-page` (sem background) + `.listing-block` (com degrade). A estrutura e analoga, mas pode haver diferenca na forma como o Blazor renderiza o componente `EventListingShell` vs a markup direta de `Groups/Index.razor`

### Arquivos modificados nas tentativas
- `Pages/MyEvents/Index.razor` — troca de `entity-shell` para `events-page`/`events-block`
- `Pages/MyEvents/Index.razor.css` — CSS de `.events-page`, `.events-block`, gradient nos cards
- `Pages/Futsal/Index.razor` — adicionado overlay e bg-icon no card
- `Pages/Futsal/Index.razor.css` — gradient no `.event-card`, overlay, bg-icon, z-index, `.listing-page` reestruturado
- `Pages/Poker/Index.razor` — adicionado overlay e bg-icon no card
- `Pages/Poker/Index.razor.css` — gradient no `.event-card`, overlay, bg-icon, z-index, CSS duplicado removido, `.listing-page` reestruturado
- `Pages/Index.razor` — adicionado botao "Meus Eventos"
- `Pages/Index.razor.css` — degrade no `.sports-shell`, CSS do botao `.my-events-link-btn`, `.sports-tabs` com `space-between`
- `Shared/Components/EventListingShell.razor` — adicionado wrapper `.listing-block`
- `Shared/Components/EventListingShell.razor.css` — CSS de `.listing-block` com degrade

---

## 2. AJUSTES FEITOS ALEM DOS ITENS DO SENIOR (Ciclo 12)

Os itens abaixo foram realizados durante o Ciclo 12 mas nao estavam na especificacao original do WORK_PLAN.md (Fases 1-11).

### 2.1 Botao "Meus Eventos" na tela `/eventos`
- **Arquivo**: `Pages/Index.razor` + `Pages/Index.razor.css`
- **Motivo**: O usuario reportou que "o sistema nao tem caminho nenhum que leve para tela meus-eventos"
- **O que foi feito**: Adicionado `<a href="/meus-eventos" class="my-events-link-btn">` na barra `.sports-tabs`, visivel apenas para usuarios autenticados (`<AuthorizeView>`)
- **CSS**: Botao com gradient azul (`var(--ci-accent)`), hover com brilho, `text-shadow` para legibilidade
- **Impacto**: Adicao de feature de navegacao — pode precisar de validacao do Senior sobre posicionamento e fluxo

### 2.2 Remocao de CSS duplicado em `Poker/Index.razor.css`
- **Arquivo**: `Pages/Poker/Index.razor.css`
- **Motivo**: O arquivo tinha ~130 linhas de CSS duplicado (bloco `.event-card` e filhos apareciam 2x)
- **O que foi feito**: Removido o segundo bloco duplicado (linhas 247-375 originais)
- **Impacto**: Reducao de ~130 linhas de CSS morto — melhoria de manutenibilidade

### 2.3 Degrade no `.sports-shell` da tela `/eventos`
- **Arquivo**: `Pages/Index.razor.css`
- **Motivo**: Tentar alinhar o background da tela de eventos ao padrao de grupos
- **O que foi feito**: Trocado `background: var(--ci-bg-card)` por `background: linear-gradient(180deg, var(--ci-bg-alt) 0%, var(--ci-bg) 100%)`
- **Impacto**: Mudanca visual nao confirmada pelo usuario — pode precisar revertida se o Senior preferir outra abordagem

### 2.4 Container `.listing-block` no `EventListingShell`
- **Arquivo**: `Shared/Components/EventListingShell.razor` + `.razor.css`
- **Motivo**: Tentar alinhar o background das subtelas `/futsal` e `/poker` ao padrao de grupos
- **O que foi feito**: Adicionado wrapper `<div class="listing-block">` com degrade igual ao `.groups-block`
- **Impacto**: Mudanca em componente compartilhado — afeta todas as paginas que usam `EventListingShell`

---

## 3. RESUMO PARA DECISAO DO SENIOR

| Item | Status | Acao recomendada |
|------|--------|------------------|
| Background de `/eventos` | Parcialmente alinhado | Confirmar visualmente aps rebuild limpo |
| Background de `/futsal` e `/poker` | Degrade aplicado mas nao confirmado | Verificar scoped CSS do `EventListingShell` |
| Background de `/meus-eventos` | Alinhado | OK |
| Botao "Meus Eventos" em `/eventos` | Implementado | Validar fluxo de navegacao |
| CSS duplicado em Poker/Index.razor.css | Removido | OK |
| Gradient nos event-card | Implementado | Confirmar visualmente |

**Recomendacao**: Fazer rebuild limpo (deletar `bin/` e `obj/`), testar com Ctrl+F5 (sem cache), e se o problema persistir, investigar se o scoped CSS do `EventListingShell.razor.css` esta sendo aplicado via Blazor CSS isolation corretamente.
