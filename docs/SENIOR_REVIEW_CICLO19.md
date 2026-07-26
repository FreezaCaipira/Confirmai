# Review Senior — Ciclo 19 (Polimento UX + Simulação de Dados)

**Data:** 25/07/2026  
**Branch:** `review/senior-overview`  
**Autor:** FreezaTV (pair programming com Cascade)  
**Baseline:** Ciclo 18 (commit `e6428b1`)

---

## 1. Resumo Executivo

Este ciclo focou em **polimento de UX mobile**, **correções de legibilidade** e **preparação de dados de teste** para validação de fluxos. O projeto está em estado avançado (1.876 testes, 139 páginas, 87 serviços) e precisa de revisão sênior para lapidação antes de produção.

---

## 2. Progresso deste Ciclo

### 2.1 UX Mobile — Player List
- **"Você" badge no mobile**: Em telas ≤768px, o nome completo do usuário atual é substituído por um badge "Você" compacto, economizando espaço horizontal crítico
  - Implementado em 9 componentes: `FutsalOutfieldGroup`, `FutsalGoalkeeperGroup`, `FutsalDetail`, `EscalacaoVoting`, `MembersManager`, `FutsalWaitlist`, `GroupDetailMembers`, `RankingTable`, `Poker/Detail`
  - CSS: `.player-item--me .player-name { display: none }` + `.player-tag--me` estilizado como nome no mobile

- **Ocultar tags durante confirmação (mobile only)**: Quando um botão de confirmação (cancelar/pagar/remover) está ativo, os outros tags do mesmo `player-item` são ocultados via `:has()` selector — apenas no mobile (`@media max-width: 768px`)
  - Evita quebra de linha e melhora usabilidade em telas pequenas
  - Regra movida para dentro da media query após feedback de que afetava desktop indevidamente

### 2.2 UX — Venue Edit (Google Maps)
- **Hint condicional**: A dica sobre autocomplete do Google Maps só aparece quando há API key válida configurada
- **Placeholder dinâmico**: O placeholder do campo endereço muda conforme presença da API key ("Preenchido automaticamente..." vs "Ex: Rua das Palmeiras, 123")
- Implementado via flag `hasMapsApiKey` em `VenueEdit.razor.cs`

### 2.3 UX — Botão "Montar Escalação"
- Botão ativo (com quorum) agora tem gradiente verde sutil, texto verde brilhante (`--green-bright`), `font-weight: 700` e hover com box-shadow
- Antes: texto `--green-strong` sobre background escuro → aparência desbotada/desabilitada

### 2.4 UX — Badge "Escalação Confirmada"
- Cor do texto: `--green` → `--green-bright` (mais legível)
- Background: `rgba(34,197,94,.1)` → `--green-opacity-sm` (mais visível)
- Borda: `rgba(34,197,94,.25)` → `--green-strong` (mais definida)
- Adicionado `font-weight: 600`

### 2.5 Simulação de Dados
- Eventos 1 e 8 preenchidos com 10 confirmações cada (2 goleiros + 8 linha)
- Mix de pagos/pendentes (50/50) para testar fluxo de pagamento
- Usuários de teste: `goleiro01-02@teste.com`, `jogador01-08@teste.com`
- Inserido diretamente via SQL no PostgreSQL (não via app)

---

## 3. Arquivos Modificados

| Arquivo | Tipo | Descrição |
|---------|------|-----------|
| `wwwroot/css/events.css` | CSS | Badge "Você" mobile, `:has()` confirmation hide, lineup button, confirmed badge |
| `Pages/Futsal/Components/FutsalOutfieldGroup.razor` | Razor | "Você" capitalizado |
| `Pages/Futsal/Components/FutsalGoalkeeperGroup.razor` | Razor | "Você" capitalizado |
| `Pages/Futsal/Detail.razor` | Razor | "Você" capitalizado |
| `Pages/Poker/Detail.razor` | Razor | "Você" capitalizado |
| `Pages/Components/EscalacaoVoting.razor` | Razor | "Você" capitalizado |
| `Pages/Groups/Components/MembersManager.razor` | Razor | "Você" capitalizado |
| `Shared/Components/Futsal/FutsalWaitlist.razor` | Razor | "Você" capitalizado |
| `Shared/Components/Groups/GroupDetailMembers.razor` | Razor | "Você" capitalizado |
| `Shared/Components/Groups/RankingTable.razor` | Razor | "Você" capitalizado |
| `Pages/VenueManager/VenueEdit.razor` | Razor | Hint e placeholder condicionais |
| `Pages/VenueManager/VenueEdit.razor.cs` | C# | Flag `hasMapsApiKey` |

---

## 4. Solicitação de Overview Senior

Solicitamos uma revisão geral do projeto cobrindo:

### 4.1 Progresso e Estado Atual
- Validação das métricas atuais (1.876 testes, 139 páginas, 87 serviços)
- Avaliação do roadmap P0 (validação em produção)
- Revisão dos ciclos 1-18 (WORK_PLAN.md)

### 4.2 Próximos Passos
- Priorização do backlog P1-P4
- Estratégia para deploy em produção
- Validação do fluxo Pix completo (EfiBank)

### 4.3 Possíveis Melhorias
- UX mobile em 375px/414px (testes reais em devices)
- Fluxo de onboarding de novos usuários
- Dashboard de métricas por grupo (ocupação, inadimplência)
- Integração WhatsApp real (atualmente preparado mas não ativo)

### 4.4 Dívidas Técnicas
- **Cobertura de testes**: 9.9% → meta 15%+ (5 services sem cobertura)
- **`!important` em site.css**: 11 ocorrências (maioria legítima)
- **rgba() hardcoded scoped**: 185 restantes (padrões únicos)
- **StateHasChanged()**: 15 chamadas restantes (objetivo: reduzir mais)
- **PingController**: 0% cobertura (novo no Ciclo 14)
- **CSS events.css**: ~5.000 linhas em arquivo único — considerar modularização

### 4.5 Segurança
- Revisão de CSP (nonce por request, sem `unsafe-inline`)
- Validação de webhooks de pagamento (idempotência, assinatura HMAC)
- Políticas de senha por ambiente (dev vs prod)
- Lockout e cookie sliding expiration
- **Pendente**: Auditoria de permissões por role em todas as páginas admin
- **Pendente**: Pen-test do fluxo de pagamento (EfiBank, AbacatePay, BTCPay)

### 4.6 Escalabilidade
- IDbContextFactory: 100% migrado (0 AppDbContext direto) ✓
- Paginação manual em todas as tabelas admin (20 itens/página) ✓
- SignalR PaymentHub: avaliar limite de conexões concorrentes
- PostgreSQL: índices necessários? (EventConfirmations.EventId, UserId)
- Background workers (reconciliação): avaliar throughput em produção
- Cache: nenhum cache distribuído implementado (Redis?)

### 4.7 Arquitetura
- 87 serviços em 12 domínios — avaliar se há responsabilidade duplicada
- 139 páginas Blazor — avaliar se há páginas subutilizadas/removíveis
- Multi-gateway de pagamentos (4 gateways) — avaliar consolidação
- i18n: 833 chaves em 3 idiomas — avaliar processo de tradução colaborativa

### 4.8 DevOps/Infra
- CI: GitHub Actions (build + test + coverage) ✓
- CD: Deploy via Docker + EasyPanel ✓
- Monitoramento: Prometheus/Grafana/Alertmanager templates ✓
- **Pendente**: Validar alertas em produção
- **Pendente**: Definir baseline operacional semanal por gateway
- **Pendente**: Ritual pós-incidente com checklist de causa raiz

---

## 5. Como Revisar

```bash
# Checkout da branch
git checkout review/senior-overview

# Build
dotnet build

# Testes
dotnet test Confirmai.Tests/Confirmai.Tests.csproj

# Rodar localmente
dotnet watch run
# Acessar http://localhost:5000/futsal/1 (evento com 10 confirmações)
```

### Pontos de inspeção visual
1. **Mobile (≤768px)**: Player list mostra "Você" ao invés do email completo
2. **Mobile**: Ao clicar em cancelar/pagar, outros tags somem (apenas mobile)
3. **Desktop**: Tags de confirmação não somem (comportamento normal)
4. **Venue Edit**: Sem API key → sem hint, placeholder normal
5. **Botão "Montar Escalação"**: Verde brilhante, claramente ativo
6. **Badge "Escalação Confirmada"**: Texto legível com bom contraste

---

## 6. Decisões Pendentes para o Senior

1. **CSS events.css modularização**: 5.000+ linhas em arquivo único — dividir por domínio (futsal.css, poker.css, admin.css)?
2. **Cache distribuído**: Implementar Redis para sessões/cache?
3. **Cobertura de testes**: Estratégia para sair de 9.9% → 15%+ (foco em services sem cobertura?)
4. **WhatsApp**: Priorizar integração real ou manter como backlog?
5. **Mobile testing**: Investir em testes E2E mobile (Playwright mobile viewport)?
6. **Multi-gateway**: Consolidar gateways ou manter os 4?
7. **Índices PostgreSQL**: Criar índices em EventConfirmations(EventId, UserId)?

---

## 7. Métricas Pós-Ciclo

| Métrica | Antes | Depois |
|---------|-------|--------|
| Componentes com "Você" | 9 (minúsculo) | 9 (capitalizado) |
| Regras CSS mobile-only | 0 | 3 (`:has()` confirmation) |
| Botões com baixa visibilidade | 1 (lineup) | 0 |
| Badges com baixo contraste | 1 (confirmed) | 0 |
| Eventos com dados de teste | 0 | 2 (eventos 1 e 8) |
| Venue hint condicional | Não | Sim |
| Venue placeholder dinâmico | Não | Sim |
