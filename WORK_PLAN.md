# Plano de Trabalho - Confirmai

> Atualizado em 22/06/2026 | Base: `main`
> 641/641 testes passando | 0 erros de build | 0 warnings CS1998/CS0649/CS86xx

Este documento define a ordem de trabalho atual para melhorias de qualidade, UX e operação antes da validação em produção.

---

## Fase 1: Qualidade de Código (Base Sólida)

**Objetivo**: Melhorar manutenibilidade, performance e testabilidade antes de novas features.

### 1.1 Expandir cobertura scoped CSS
- **Status**: 55 páginas ainda sem `.razor.css`
- **Prioridade**: Alta
- **Ação**: Priorizar páginas mais usadas (Groups, Futsal, Payments)
- **Benefício**: Isolamento de estilos, manutenção mais fácil

### 1.2 Consolidar CSS duplicado
- **Status**: Pendente
- **Prioridade**: Média
- **Ação**: Identificar estilos repetidos, criar utility classes em `wwwroot/css/`, adicionar design tokens
- **Benefício**: Reduzir tamanho do bundle, consistência visual

### 1.3 Aumentar cobertura de testes
- **Status**: Pendente
- **Prioridade**: Média
- **Meta**: 80%+ (atual: desconhecido)
- **Ação**: Priorizar services críticos (Payment, Events, Admin), adicionar testes de integração
- **Benefício**: Regressões detectadas mais cedo

---

## Fase 2: UX em Geral (Experiência do Usuário)

**Objetivo**: Melhorar usabilidade e acessibilidade para todos os usuários.

### 2.1 UX de Grupos Privados
- **Status**: Em Andamento (ROADMAP.md)
- **Prioridade**: Média
- **Ação**: Atalhos de aprovação/rejeição direta, filtros e contexto visual, VenueEdit com UF/IBGE
- **Benefício**: Reduzir atrito em workflows comuns

### 2.2 Página de histórico de pagamentos do jogador
- **Status**: Pendente
- **Prioridade**: Média
- **Ação**: Auto-serviço para consulta, filtros por data/status/valor, exportação PDF/CSV
- **Benefício**: Reduzir suporte, transparência para usuário

### 2.3 Ajustes de UX responsiva e acessibilidade AA
- **Status**: Pendente
- **Prioridade**: Média
- **Ação**: Testar em diferentes tamanhos de tela, verificar contraste WCAG AA, navegação por teclado
- **Benefício**: Inclusão, compliance

### 2.4 Indicadores de ocupação, inadimplência e conversão por grupo
- **Status**: Pendente
- **Prioridade**: Baixa
- **Ação**: Dashboard com métricas por grupo, gráficos de tendência, alertas configuráveis
- **Benefício**: Visibilidade para admins, tomada de decisão

### 2.5 Integração WhatsApp real com opt-in
- **Status**: Pendente
- **Prioridade**: Baixa
- **Ação**: Webhook de mensagens, opt-in explícito, notificações de eventos/pagamentos
- **Benefício**: Engajamento, redução de no-shows

---

## Fase 3: Operação Contínua (Melhorias Operacionais)

**Objetivo**: Melhorar observabilidade, eficiência e resposta a incidentes.

### 3.1 Virtual scrolling para listas grandes
- **Status**: Pendente
- **Prioridade**: Baixa
- **Ação**: Implementar em AdminLogs, AdminUsers, AdminPayments
- **Benefício**: Performance em listas com 1000+ itens

### 3.2 Definir baseline operacional semanal por gateway
- **Status**: Pendente
- **Prioridade**: Baixa
- **Ação**: Métricas de volume/latência/taxa de erro, alertas automatizados
- **Benefício**: Detecção proativa de problemas

### 3.3 Formalizar ritual pós-incidente com checklist de causa raiz
- **Status**: Pendente
- **Prioridade**: Baixa
- **Ação**: Template de post-mortem, checklist de análise, ações de follow-up
- **Benefício**: Aprendizado contínuo, prevenção de recorrência

---

## Fase 4: Validação em Produção

**Objetivo**: Garantir que fluxos críticos funcionam em ambiente real.

### 4.1 Testar fluxo Pix completo em produção (EfiBank)
- **Status**: Pendente
- **Prioridade**: Alta

### 4.2 Ativar webhook AbacatePay em produção
- **Status**: Pendente
- **Prioridade**: Alta

### 4.3 Implantar alertas reais e validar escalonamento fim a fim
- **Status**: Pendente
- **Prioridade**: Alta

---

## Justificativa da Ordem

1. **Qualidade primeiro**: Base sólida reduz regressões em features futuras
2. **UX depois**: Melhorias visíveis para usuário aumentam valor percebido
3. **Operação depois**: Melhorias internas que não impactam usuário diretamente
4. **Produção por último**: Validar tudo junto em ambiente real, não em pedaços

---

## Estimativa de Esforço

- Fase 1 (Qualidade): 2-3 semanas
- Fase 2 (UX): 3-4 semanas
- Fase 3 (Operação): 1-2 semanas
- Fase 4 (Produção): 1 semana

**Total**: 7-10 semanas

---

## Referências

- [ROADMAP.md](ROADMAP.md) - Roadmap detalhado de features
- [CONTRIBUTING.md](CONTRIBUTING.md) - Guia de contribuição
- [docs/](docs/) - Documentação operacional
