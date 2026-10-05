# Design system — padrão mínimo

Fonte única de valores: `wwwroot/css/tokens.css`. Nenhum hex/rgba fora dele
(exceção: branding oficial de terceiros, ex. botão Google). Este documento
define **como** usar os tokens; vale para o Confirmai e para os próximos
projetos.

## 1. Elevação: "a luz vem de cima"

Cada nível de aninhamento no markup sobe **um** degrau de tom. Um filho nunca
tem o mesmo tom do pai — se tiver, a estrutura desaparece (tela plana).

| Nível | Token | Uso | Borda |
|---|---|---|---|
| L0 | `--bg` | página e *chrome*: header, footer, ticker de novidades, breadcrumb. Chrome não é caixa: só linha divisória (`border-bottom`/`border-top`), sem fundo próprio | `--border` |
| L1 | `--surface` | painéis/setores (hero, "Meus grupos", "Próximos", widget lateral, formulários) | `--border` |
| L2 | `--surface-2` | cards **dentro** de um painel (partida, chip de grupo, estado vazio, botão secundário, toggle) | `--border-2` |
| L3 | `--surface-3` | hover/ativo de um L2, popover, drawer, cookie banner, tooltip | `--border-2` |

Regras:

1. Máximo de 3 níveis aninhados (L1 → L2 → L3). Se precisar de um quarto,
   o layout está errado — quebre em outra tela/seção.
2. **Cabeçalho de painel não é container.** Mesmo tom do painel, só
   `border-bottom: 1px solid var(--border)` + título. Destaque vem do filete
   azul à esquerda do título, não de um fundo diferente.
3. Borda clareia junto com o nível (`--border` em L0/L1, `--border-2` em
   L2/L3). Sombra só em L3 flutuante (`--shadow-card`).
4. Hover sobe um nível (`L2 → --surface-3`); nunca desce nem muda de matiz.
5. Sem gradiente de fundo. Sem cor por esporte/categoria em fundo ou borda. **Excecoes**: (a) o hero de pagina (`.home-hero`), que pode usar o degrade de acento para se distinguir dos paineis de conteudo — e L1 sem filhos-card (so texto + toggle) e e o unico elemento da tela cujo papel e orientar. Decisao do Senior no C36-D Fase 3. (b) o header do shell, via token `--header-gradient` — degrade sutil L1→L0 (tom-para-tom, sem matiz), so chrome; o texto mantem o contraste AA medido sobre --surface/--bg. C37 Fase 7.

## 2. Azul: direcionamento, não decoração

| Papel | Token |
|---|---|
| **Um** próximo passo por tela (CTA primário) | `--accent` (fundo) + `--accent-fg` |
| Hover do CTA | `--accent-hover` |
| Estado ativo (nav, tab, toggle), badge de destaque | `--accent-soft` fundo + `--accent-text` texto + `inset 0 0 0 1px --accent-border` |
| Links, contadores, ícones de apoio | `--accent-text` |
| Contorno de destaque (hero, filete de header/título, borda ao hover) | `--accent-border` / `--accent` (2–3px) |
| Foco de teclado | `outline: 2px solid --accent-ring; outline-offset: 2px` |

Ações secundárias ficam neutras (L2) e só ganham azul no hover/foco.
Sucesso, atenção e erro usam `--success/--warning/--danger` (+ `-soft`),
nunca azul. Verde não é botão.

## 3. Texto

`--text` títulos e valores; `--text-2` corpo secundário/meta; `--text-3`
placeholders e rodapé. Contraste AA é garantido por
`DesignTokensContrastTests` — ao mexer em tokens, o teste é o gate.

## 4. Forma e espaço

- Raio: `--radius-sm` (controles), `--radius` (cards), `--radius-lg` (painéis),
  `--radius-full` (chips/badges).
- Espaço: escala `--space-1..7`; padding de painel `--space-5`, de card
  `--space-4`, gap entre seções `--space-5`.
- Largura de conteúdo: `min(1280px, calc(100% - 2 * var(--space-5)))`,
  centralizada; header e footer usam a **mesma** largura interna.

## 5. Cor por feature: identidade, não decoração

Cada feature do grupo tem **um** matiz, para as telas e os cards se
distinguirem de relance sem virar arco-íris. O matiz só aparece em ícone,
filete superior do painel, avatar/iniciais e borda de hover — nunca em
botão, fundo de painel ou texto corrido. Azul continua sendo ação/foco/ativo.

| Feature | Classe | Token |
|---|---|---|
| Partidas, eventos | `.feat--matches` | `--feat-matches` (azul claro) |
| Pagamentos, Pix | `.feat--payments` | `--feat-payments` (teal) |
| Grupos, membros | `.feat--members` | `--feat-members` (violeta) |
| Ranking | `.feat--ranking` | `--feat-ranking` (laranja — não confunde com o amarelo de atenção) |
| Configurações | `.feat--settings` | `--feat-settings` (ardósia) |
| Presença | `.feat--attendance` | `--feat-attendance` (verde) |
| Convite WhatsApp | `.feat--invite` | `--feat-invite` (verde da marca) |

Uso: a classe vai no container (`<section class="panel feat--payments">`,
`<a class="hub-action feat--matches">`) e define `--feat`, `--feat-soft` e
`--feat-border`; os filhos leem `var(--feat, var(--accent-text))`, então
sem classe tudo cai no azul padrão. `.feat-icon` é o quadrado de ícone pronto.
Status (sucesso/atenção/erro) continua semântico e vence a cor da feature.

### 5.1 Painel "Hoje" (home)

`Shared/Components/HomeTodayPanel.razor` é um painel L1 com tiles L2, um por
feature (`.feat--matches`, `.feat--payments`, `.feat--settings` para a caixa
postal). O tile de pagamentos ganha borda `--warning` quando há pendência
(status vence feature). Os dados vêm de `HomeToday.Build` (puro, testado em
`HomeTodayTests`), calculado sobre o que a home já carrega + 1 `COUNT` de
mensagens não lidas. A dica do dia é um carrossel sem JS (`TipKeys`).

## 6. Checklist por PR visual

- [ ] Zero hex/rgba fora de `tokens.css`; zero `linear-gradient`; zero vars
      legadas (`--ci-*`, `--futsal-*`, `--poker-*`, `--green-*`, `--red-*`,
      `--parchment-*`) em código novo.
- [ ] Cada container tem tom diferente do pai (tabela §1).
- [ ] Um único `--accent` cheio por tela.
- [ ] Cor de feature só via `.feat--*` (§5), nunca em botão ou fundo.
- [ ] `DesignTokensContrastTests`, `AntiHardcode*` e `Css*` verdes.
- [ ] Prints 1366 e 390 no PR.
