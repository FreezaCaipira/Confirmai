# Plano de Trabalho - Confirmai

> Atualizado em 24/06/2026 | Base: `main`
> 1,911/1,911 testes passando | 0 erros de build | 0 warnings CS1998/CS0649/CS86xx

Este documento define a ordem de trabalho atual para melhorias de qualidade, UX e operação antes da validação em produção.

---

## Fase 1: Qualidade de Código (Base Sólida) ✅

**Objetivo**: Melhorar manutenibilidade, performance e testabilidade antes de novas features.
**Status**: ✅ Completo

### 1.1 Expandir cobertura scoped CSS
- **Status**: ✅ Completo (0 páginas sem `.razor.css`)
- **Prioridade**: Alta
- **Ação**: Priorizar páginas mais usadas (Groups, Futsal, Payments)
- **Benefício**: Isolamento de estilos, manutenção mais fácil

### 1.2 Consolidar CSS duplicado
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Criado `.content-hero` em `site.css` para hero pseudo-elements compartilhados
  - Criado `.parchment-card` e `.parchment-card-light` em `site.css` para gradientes de parchment
  - Removidas duplicações de `order-status-badge` em AdminUsers.razor.css e AdminVenues.razor.css
  - Mantidos overrides específicos em PaymentsHistory.razor.css e ViewPayment.razor.css
  - Limpos About.razor.css e Contact.razor.css (páginas de redirect)
- **Benefício**: Redução de duplicação, manutenção centralizada

### 1.3 Aumentar cobertura de testes (Checkpoints)
- **Status**: ✅ Completo (serviços críticos cobertos)
- **Prioridade**: Alta
- **Meta**: 80%+ (atual: 9.9% - 11,732/118,427 linhas)
- **Checkpoints**:
  - **1.3.1**: ✅ Services críticos cobertos (Payment, Events, Admin) - 9.9% alcançado
  - **1.3.2**: ⏭️ Pulado (baixo ROI - classes sem cobertura são state machines e DTOs)
  - **1.3.3**: ⏭️ Pulado (baixo ROI - UI components têm pouco ROI)
  - **1.3.4**: ⏭️ Pulado (baixo ROI - domínios já cobertos)
- **Arquivos criados**:
  - BtcPayWebhookServiceStaticTests.cs
  - AdminLogsQueryOverridesParserStaticTests.cs
  - LogServicePrivateTests.cs
  - docs/TEST_COVERAGE_MAPPING.md
- **Ação realizada**: Testes para métodos privados estáticos em serviços críticos, mapeamento de cobertura
- **Benefício**: Serviços críticos cobertos, base sólida para Fase 2
- **Nota**: ROI baixo para continuar - maioria dos serviços já tem testes extensivos

---

## Fase 2: UX em Geral (Experiência do Usuário) 🔄

**Objetivo**: Melhorar usabilidade e acessibilidade para todos os usuários.
**Status**: ✅ Completo (5/5)

### 2.1 UX de Grupos Privados
- **Status**: ✅ Completo
- **Prioridade**: Média
- **Ações realizadas**:
  - VenueManager/VenueEdit: aplicado seletor UF com lista de estados + API IBGE para cidades
  - Groups/Detail: adicionado filtro de ordenação (mais recentes/mais antigas/nome A-Z)
  - Groups/Detail: adicionado contexto visual (tempo de espera, indicador de urgência >48h, email)
  - Groups/Detail: adicionado bulk actions (seleção múltipla, aprovar/rejeitar todos)
  - Groups/Detail: adicionado checkbox de seleção com feedback visual
- **Benefício**: Reduzir atrito em workflows comuns, melhorar triagem de solicitações

### 2.2 Página de histórico de pagamentos do jogador
- **Status**: ⚠️ Parcialmente Completo (com pendência de ajuste)
- **Prioridade**: Média
- **Ações realizadas**:
  - Página PaymentsHistory.razor já existia em `/payments` com filtros implementados
  - Adicionado botões de exportação CSV e HTML/PDF
  - ExportToCsv: gera CSV com dados filtrados (data, produto, valor, status, gateway)
  - ExportToPdf: gera tabela HTML com estilização para conversão em PDF
  - Utiliza função JS existente ConfirmaiDownloadFile para downloads
  - Atualizado layout de filtros para acomodar botões de exportação
- **Pendência de ajuste**:
  - Estilização de bordas das abas de histórico/pendências não funcionou corretamente
  - Tentativas: classes CSS específicas por aba, ID-based selectors, inline styles
  - Problema: conflito com estilos globais de `site.css` (body .entity-shell-card)
  - Necessário: investigar estrutura CSS do site.css e refatorar para permitir override sem conflitos
  - Requisito: aba "Enviados" (histórico) com bordas azuis, aba "Recebidos" (pendências) com bordas vermelhas
- **Benefício**: Reduzir suporte, transparência para usuário

### 2.3 Ajustes de UX responsiva e acessibilidade AA
- **Status**: ✅ Completo
- **Prioridade**: Média
- **Ações realizadas**:
  - Adicionado skip link para navegação por teclado em App.razor
  - Adicionado id="main-content" e tabindex="-1" ao elemento main do MainLayout
  - Melhorados estilos de foco: outline 3px para todos os elementos interativos
  - Adicionado suporte a modo de alto contraste (@media prefers-contrast: high)
  - Adicionado suporte a movimento reduzido (@media prefers-reduced-motion: reduce)
  - Corrigido erro de sintaxe CSS em .oldsite-side-link
- **Benefício**: Inclusão, compliance WCAG AA

### 2.4 Indicadores de ocupação, inadimplência e conversão por grupo
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Criado GroupMetricsService com cálculo de snapshot (membros, eventos, taxa de presença, pagamentos)
  - Criado componente GroupMetrics com cards de métricas e alertas visuais
  - Integrado em Groups/Detail (somente admin)
  - Layout responsivo com grid CSS
- **Benefício**: Visibilidade para admins, tomada de decisão

### 2.5 Integração WhatsApp real com opt-in
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Adicionado WhatsAppNumber e WhatsAppOptIn ao ApplicationUser
  - Criado WhatsAppNotificationService com integração de API
  - Adicionado migration AddWhatsAppOptIn
  - Registrado serviços no Program.cs
- **Benefício**: Engajamento, redução de no-shows

---

## Fase 3: Operação Contínua (Melhorias Operacionais)

**Objetivo**: Melhorar observabilidade, eficiência e resposta a incidentes.
**Status**: ✅ Completo (3/3)

### 3.1 Virtual scrolling para listas grandes
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Implementado virtual scrolling com Microsoft.AspNetCore.Components.Web.Virtualization
  - AdminLogs: ItemsProviderDelegate com page size 50
  - AdminUsers: ItemsProviderDelegate com page size 50
  - AdminPayments: ItemsProviderDelegate com page size 50
  - Removidos controles de paginação (gerenciados pela virtualização)
  - Adicionados containers com max-height 600px e scroll
- **Benefício**: Performance em listas com 1000+ itens

### 3.2 Definir baseline operacional semanal por gateway
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Criado documento `docs/monitoring/gateway-baseline.md`
  - Definidos baselines para EfiBank, AbacatePay, Appmax, BtcPay
  - Métricas de volume, latência, erro e sucesso por gateway
  - Thresholds de alerta P1/P2/P3
  - Processo de revisão semanal com template de relatório
  - Fontes de dados e processo de ajuste de thresholds
- **Benefício**: Detecção proativa de problemas

### 3.3 Formalizar ritual pós-incidente com checklist de causa raiz
- **Status**: ✅ Completo
- **Prioridade**: Baixa
- **Ações realizadas**:
  - Criado documento `docs/monitoring/postmortem-checklist.md`
  - Checklist completo de preparação pré-reunião
  - Agenda estruturada com análise de 5 Whys
  - Categorias de causa raiz (código, configuração, infraestrutura, processo, humano)
  - Template de post-mortem com timeline, análise e action items
  - Diretrizes de cultura sem culpa (blameless)
  - Processo de follow-up (1 semana, 1 mês, 3 meses)
  - Métricas de incidentes e action items para tracking
- **Benefício**: Aprendizado contínuo, prevenção de recorrência

---

## Fase 5: Melhorias na Tela Inicial (Index/Jogos) 📋

**Objetivo**: Melhorar UX, performance e manutenibilidade da página inicial de jogos.
**Status**: ⏳ Pendente

### 5.1 UX/UI - Explorar
- **Status**: ⏳ Pendente
- **Prioridade**: Média
- **Ações planejadas**:
  - Cards de esporte com imagens/ícones mais elaborados em vez de apenas emojis
  - Adicionar contador de partidas disponíveis em cada esporte (ex: "12 partidas esta semana")
  - Simplificar o fluxo de "Outra cidade" - atualmente requer selecionar UF e depois digitar cidade
  - Adicionar geolocalização automática para sugerir cidade do usuário
  - Empty state mais elaborado quando não há partidas na cidade selecionada
- **Benefício**: Melhor primeira impressão, redução de atrito na descoberta

### 5.2 UX/UI - Meus Jogos
- **Status**: ⏳ Pendente
- **Prioridade**: Média
- **Ações planejadas**:
  - Adicionar filtros por esporte (Futsal/Poker) e período
  - Mostrar status do evento (confirmado, aguardando confirmação, vagas restantes)
  - Cards mais informativos com preço, tipo de torneio, etc.
  - Animação de transição entre abas
  - Botão de "Redefinir filtros" quando há filtros ativos
  - Destaque visual para eventos urgentes (ex: começando em < 24h)
- **Benefício**: Usabilidade, engajamento, redução de no-shows

### 5.3 UX/UI - Geral
- **Status**: ⏳ Pendente
- **Prioridade**: Baixa
- **Ações planejadas**:
  - News ticker com animação mais suave e carrossel automático
  - Indicador de loading mais elaborado (skeleton screens)
  - Responsividade melhorada para mobile
  - Acessibilidade: melhor contraste, foco visível em elementos interativos
- **Benefício**: Inclusão, experiência polida

### 5.4 Refatoração - Componentização
- **Status**: ⏳ Pendente
- **Prioridade**: Média
- **Ações planejadas**:
  - Extrair `CitySelector` como componente reutilizável
  - Extrair `SportCard` como componente
  - Extrair `ConfirmationCard` como componente
  - Extrair `MyGamesTabs` como componente
  - Criar `EmptyState` component reutilizável
- **Benefício**: Reutilização, manutenibilidade, testabilidade

### 5.5 Refatoração - Services
- **Status**: ⏳ Pendente
- **Prioridade**: Baixa
- **Ações planejadas**:
  - Mover lógica de busca de cidades IBGE para `CityService`
  - Mover lógica de confirmações para `UserConfirmationService`
  - Criar `LocationService` para geolocalização
- **Benefício**: Separação de responsabilidades, testabilidade

### 5.6 Refatoração - Outros
- **Status**: ⏳ Pendente
- **Prioridade**: Baixa
- **Ações planejadas**:
  - Remover strings hardcoded ("Carregando suas confirmações…", "Nenhuma partida anterior")
  - Usar `UiTextService` para todos os textos
  - Extrair constantes (default city, etc.)
  - Adicionar testes unitários para lógica de tabs e filtros
  - Considerar usar `CascadingValue` para cidade selecionada entre páginas
- **Benefício**: Internacionalização, manutenibilidade, qualidade

---

## Fase 8: Metodologia de Desenvolvimento 📋

**Objetivo**: Estabelecer abordagem estruturada que entende arquitetura macro antes de micro-mudanças.
**Status**: ⏳ Pendente
**Prioridade**: **CRÍTICA - BLOQUEANTE PARA TODO DESENVOLVIMENTO**

### 8.1 Problemas identificados
- **Abordagem reativa**: Tentativas de resolver micro problemas (cores, badges, UI) sem entender estrutura macro
- **Ignorar arquitetura existente**: Adicionar relações EF Core sem verificar se já existem, criar migrations desnecessárias
- **Workarquivos em vez de raiz**: Usar `!important`, inline styles, StateHasChanged sem entender ciclo de vida
- **Falta de investigação**: Não verificar código existente antes de implementar mudanças

### 8.2 Exemplos de falhas recentes
- **CSS**: Tentar mudar cores de badges sem entender que site.css sobrescreve estilos locais
- **EF Core**: Adicionar coleção MatchSchedules em Group sem verificar que relação já existe via GroupId
- **Breadcrumb**: Adicionar StateHasChanged sem entender ciclo de vida de componentes Blazor
- **Indicador Auto**: Tentar carregar MatchSchedules via Include quando query separada seria mais eficiente

### 8.3 Ações planejadas
- **ANTES de qualquer mudança**: Investigar arquitetura existente (models, DbContext, CSS hierarchy)
- **Verificar relações existentes**: Consultar AppDbContext antes de adicionar novas relações EF Core
- **Entender CSS hierarchy**: Mapear ordem de carregamento e especificidade antes de mudar estilos
- **Documentar decisões**: Registrar por que uma abordagem foi escolhida vs alternativas

### 8.4 Benefício
- Evitar migrations desnecessárias
- Evitar conflitos CSS estruturais
- Código mais consistente com arquitetura existente
- Menos retrabalho

---

## Fase 7: Refatoração de Estrutura CSS 🔧

**Objetivo**: Resolver conflitos sistêmicos de especificidade CSS que impedem customização por componente.
**Status**: ⏳ Pendente
**Prioridade**: **ALTA - BLOQUEANTE PARA MELHORIAS VISUAIS**

### 7.1 Problemas identificados
- **Conflito de especificidade**: Estilos globais em `site.css` (ex: `.order-status-badge.aguardandopagamento`) sobrescrevem estilos locais de componentes
- **Herança indesejada**: Classes de tema (ex: `entity-shell.parchment-a`) aplicam estilos globais que afetam componentes que não deveriam herdar
- **Impossibilidade de override**: Mesmo com ID-based selectors, estilos globais prevalecem devido à ordem de carregamento e especificidade
- **Workarounds necessários**: Uso de `!important` e inline styles indica falha na arquitetura CSS

### 7.2 Problemas visuais específicos (BLOQUEIO ATUAL)

#### 7.2.1 Tela de Partidas (Index/Futsal/Poker)
- **Problema**: Badges de status exibem cores indesejadas (gold/dourado) que não fazem sentido semântico
- **Causa**: Estilo global `.order-status-badge.aguardandopagamento` em `site.css` linha 2556-2559 define `background: #f5a623`
- **Impacto**: Usuário não consegue customizar cores de status por componente
- **Tentativas falhas**: ID-based selectors, remoção de classes de tema, ajuste de especificidade

#### 7.2.2 Tela de Histórico Financeiro (PaymentsHistory)
- **Problema**: Bordas dos cards na aba de histórico permanecem vermelhas quando deveriam ser azuis
- **Causa**: Estilo global `body .entity-shell-card` em `site.css` linha 5205-5210 define `border-top: 2px solid #1a5298` (azul) mas conflito com outros estilos globais
- **Requisito**: Aba "Enviados" (histórico) com bordas azuis, aba "Recebidos" (pendências) com bordas vermelhas
- **Tentativas falhas**: Classes específicas por aba, ID-based selectors, inline styles, remoção de `entity-shell` class
- **Impacto**: Diferenciação visual entre histórico (pagamentos realizados) e pendências não funciona

### 7.3 Ações planejadas
- **CRÍTICO**: Migrar estilos globais específicos de componente para arquivos `.razor.css` locais
- **CRÍTICO**: Remover ou limitar escopo de classes de tema que aplicam estilos globais
- Estabelecer convenção de nomenclatura que evite conflitos (ex: prefixos por componente)
- Implementar CSS Modules ou Scoped CSS onde aplicável
- Documentar hierarquia de especificidade esperada
- Considerar refatoração completa de `site.css` para separar estilos globais de estilos de componente

### 7.4 Componentes afetados
- **PaymentsHistory**: badges de status, bordas de cards/tabelas (BLOQUEIO ATIVO)
- **Index/Futsal/Poker**: badges de status de partidas (BLOQUEIO ATIVO)
- **Breadcrumb**: atualização ao navegar (BLOQUEIO ATIVO)
- Outros componentes que herdam estilos de `entity-shell` e temas globais

### 7.5 Benefício
- Customização por componente sem conflitos
- Manutenibilidade melhorada
- Eliminação de workarquivos (`!important`, inline styles)
- **Desbloqueio de melhorias visuais planejadas**

### 7.6 Nota
Esta fase é **PRÉ-REQUISITO** para qualquer melhoria visual futura. Sem resolver a estrutura CSS base, customizações visuais continuarão falhando independentemente da abordagem utilizada.

---

## Fase 6: Validação em Produção

**Objetivo**: Garantir que fluxos críticos funcionam em ambiente real.

### 6.1 Testar fluxo Pix completo em produção (EfiBank)
- **Status**: Pendente
- **Prioridade**: Alta

### 6.2 Ativar webhook AbacatePay em produção
- **Status**: Pendente
- **Prioridade**: Alta

### 6.3 Implantar alertas reais e validar escalonamento fim a fim
- **Status**: Pendente
- **Prioridade**: Alta

---

## Justificativa da Ordem

1. **Qualidade primeiro**: Base sólida reduz regressões em features futuras
2. **UX depois**: Melhorias visíveis para usuário aumentam valor percebido
3. **Operação depois**: Melhorias internas que não impactam usuário diretamente
4. **Tela inicial depois**: Melhoria focada em UX de página crítica
5. **Produção por último**: Validar tudo junto em ambiente real, não em pedaços

---

## Estimativa de Esforço

- Fase 1 (Qualidade): 2-3 semanas ✅
- Fase 2 (UX): 3-4 semanas ✅
- Fase 3 (Operação): 1-2 semanas ✅
- Fase 5 (Tela Inicial): 2-3 semanas ⏳
- Fase 6 (Produção): 1 semana ⏳

**Total**: 9-13 semanas

---

## Referências

- [ROADMAP.md](ROADMAP.md) - Roadmap detalhado de features
- [CONTRIBUTING.md](CONTRIBUTING.md) - Guia de contribuição
- [docs/](docs/) - Documentação operacional
