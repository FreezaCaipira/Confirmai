# Confirmai - Marketplace Bitcoin

[![CI](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml/badge.svg)](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml)
[![Coverage](https://raw.githubusercontent.com/FreezaCaipira/Confirmai/badges/badges/coverage.svg)](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml)

## Visão Geral

Confirmai é um marketplace descentralizado focado em transações com Bitcoin, com integração a gateways, painel administrativo, sistema de pedidos, chat e notificações.

---

## Funcionalidades

- Cadastro e autenticação de usuários (roles: admin, user, GM de servidor)
- Fluxos de conta Identity: login, registro, recuperar senha, redefinir senha, reenviar confirmação, confirmar e-mail e alterar senha
- Cadastro, edição e listagem de produtos
- Integração com gateways Bitcoin (BTCPayServer, Testnet, etc)
- Geração de QR Code para pagamentos
- Confirmação automática de pagamentos
- Histórico de pedidos e detalhes de transações
- Painel administrativo completo (produtos, usuários, pedidos, logs, API keys)
- Notificações visuais (toast)
- Suporte a múltiplos servidores OpenTibia (Canary, TFS, etc) com marketplace por servidor
- **Integração de servidores via API Key** — snippet Lua pronto (polling a cada 5 min, sem expor portas)
- Portal do GM: tela exclusiva para admins de servidor com guia de integração, API key e downloads
- Layout responsivo e tema escuro
- **Caixa de mensagens entre usuários** (`/mailbox`) com suporte a anexos e arquivamento
- **Catálogo de itens por servidor** (`/servers/{id}/catalog`) com ofertas por item e histórico de vendas
- **Recomendações de vendedores** — compradores podem recomendar vendedores após transações
- **Handles de contato no perfil** (Pix, Discord, etc.) para facilitar entregas in-game
- **Agentes de entrega** — cadastro e gestão de entregadores externos no painel admin
- **Preços por gateway** configuráveis por produto e por oferta de item
- **Suporte multi-moeda** nos pagamentos (BTC/USD/BRL) com concorrência protegida
- **Filtros e ordenação persistentes** em todas as telas administrativas
- **Exportação de logs** administrativos e gestão de idiomas no painel admin
- **Auditoria estruturada de eventos** (registro/login, CRUD de produtos/servidores, pagamentos confirmados, pedidos criados/liberados) — campos `EventType`/`EntityType`/`EntityId`/`MetadataJson` (jsonb) em `Logs`

---

## Como rodar localmente

### Pré-requisitos
- .NET 9.0 SDK
- PostgreSQL instalado e rodando

### Configuração do Ambiente

1. **Clone o repositório**
   ```bash
   git clone <url-do-repositorio>
   cd Confirmai
   ```

2. **Configure o PostgreSQL**
   
   Verifique se o PostgreSQL está rodando:
   ```bash
   sudo systemctl status postgresql
   ```
   
   Crie o usuário e banco de dados:
   ```bash
   # Criar usuário (substitua 'suasenha' pela senha desejada)
   sudo -u postgres psql -c "CREATE USER seuusuario WITH PASSWORD 'SUASENHA';"
   
   # Criar banco de dados
   sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER seuusuario;"
   
   # Conceder privilégios
   sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE Confirmai TO seuusuario;"
   ```

3. **Configure o `appsettings.json`
   
   Ajuste a string de conexão se necessário e configure as chaves dos gateways Bitcoin desejados.

   Para habilitar envio real de e-mails do Identity (recuperação de senha e confirmação), configure a seção `Email`:

   ```json
   "Email": {
     "Enabled": true,
     "Host": "smtp.seuprovedor.com",
     "Port": 587,
     "UseSsl": true,
     "Username": "__SET_VIA_USER_SECRETS__",
     "Password": "__SET_VIA_USER_SECRETS__",
     "FromEmail": "no-reply@seusite.com",
     "FromName": "Confirmai"
   }
   ```

   Em ambiente local, se `Enabled=false` ou sem credenciais, o sistema usa fallback em log sem quebrar os fluxos.
   Nesse modo de fallback, os e-mails tambem sao salvos em arquivos `.html` e `.txt` em `wwwroot/uploads/dev-emails` para facilitar testes locais dos links.

   Padrões de segurança de autenticação atualmente configurados:
   - Lockout após 5 tentativas inválidas.
   - Duração do lockout: 15 minutos.
    - Cookie de autenticação com renovação por atividade (sliding expiration).
    - Expiração de sessão: 60 minutos em Development e 30 minutos em Production/Staging.
    - Cookie `Secure` exige HTTPS em Production/Staging.
   - Em Production/Staging, login exige e-mail confirmado; em Development o fluxo permanece flexível para testes locais.
    - Política de senha:
       - Development: mínimo 6 caracteres, com dígito e minúscula.
       - Production/Staging: mínimo 10 caracteres, exigindo maiúscula, minúscula, dígito, símbolo e 3 caracteres únicos.

4. **Execute as migrações do banco de dados**
   ```bash
   dotnet ef database update
   ```

5. **Rode o projeto**
   ```bash
   dotnet watch run
   ```

6. **Acesse a aplicação**
   
   Abra o navegador em `http://localhost:5000`

### Troubleshooting

**Erro de autenticação PostgreSQL:**
```
password authentication failed for user "freeza"
```
- Verifique se o usuário foi criado corretamente
- Confirme se a senha no `appsettings.json` está correta
- Certifique-se que o PostgreSQL está rodando

**Erro de conexão com banco:**
```
database "Confirmai" does not exist
```
- Execute os comandos de criação do banco listados acima
- Verifique se o nome do banco no `appsettings.json` está correto

**Para resetar o banco (se necessário):**
```bash
# Remover banco existente
sudo -u postgres psql -c "DROP DATABASE IF EXISTS Confirmai;"

# Recriar banco
sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER freeza;"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE Confirmai TO freeza;"

# Executar migrações novamente
dotnet ef database update
```

## Testes E2E (Playwright)

Além dos testes automatizados .NET (`Confirmai.Tests`), o projeto possui uma suíte de testes E2E de navegador na pasta [e2e](e2e) para validar interações reais de UI/JavaScript.

### Cenários cobertos atualmente

- Banner de consentimento de cookies (aceitar/personalizar/persistência)
- Troca de idioma via flags e persistência em rotas protegidas

### Pré-requisitos

- Node.js 20+
- npm
- Aplicação rodando localmente (ex.: `http://127.0.0.1:5000`)

### Como executar

```bash
cd e2e
npm install
npm run install:browsers
npm test
```

Para apontar para outra URL:

```bash
set E2E_BASE_URL=http://127.0.0.1:5001
npm test
```

---

## Estrutura de Pastas

- `Pages/` - Páginas principais (Marketplace, Admin, Pedidos, Pagamentos)
- `Shared/Components/` - Componentes reutilizáveis (Header, Footer, Toast, etc)
- `Models/` - Modelos de dados
- `Services/` - Serviços de integração e lógica de negócio
- `Data/` - Contexto do banco de dados

---

## Documentação

Para informações detalhadas sobre arquitetura, desenvolvimento, deployment e operação, consulte a documentação em [docs/](docs/):

- [Arquitetura](docs/architecture/system-overview.md)
- [Desenvolvimento](docs/development/getting-started.md)
- [Deployment](docs/deployment/production-checklist.md)
- [Operações](docs/operations/troubleshooting.md)

---

## Roadmap e Progresso

Veja os arquivos [roadmap.md](roadmap.md) e [PROGRESS.md](PROGRESS.md) para o plano detalhado.

**Status rápido (Junho/2026 — Phase 18 COMPLETA):**
- ✅ Build e suíte principal estáveis: **552/575 testes** passando em `Confirmai.Tests` (99.5%)
- ✅ CSS Scoped Isolation (Phase 18): Ranking e Config pages agora renderizam com styling correto
  - 5 arquivos `.razor.css` criados (RankingViewSelector, RankingTable, FeaturesToggles, MembersManager, PixReceiverSelector)
  - Padrão consolidado: uma `.razor.css` por componente com CSS isolation completa
  - Lição: Sub-componentes com CSS distribuído = múltiplos IDs de isolamento = CSS não funciona
- ✅ Fluxo de futebol robusto: criação/edição de partidas, confirmação por posição (linha/goleiro), fila de espera, conflitos de horário e gestão admin.
- ✅ Escalação concluída: `/futsal/{id}/escalacao` com randomização, confirmação, reset e leitura pública após confirmação.
- ✅ Fluxo de grupos privados evoluído: não-membros agora podem solicitar entrada diretamente nos detalhes de futsal/poker, com estado visível de solicitação pendente/rejeitada.
- ✅ Operação de grupos melhorada para admins: `/grupos` prioriza comunidades com solicitações pendentes, exibe badge de contagem, chip de urgência e seção destacada de ação necessária.
- ✅ Camada de segurança e observabilidade consolidada (CSP com nonce, headers, logs/audit estruturados).
- ✅ Pagamentos por evento com multi-gateway e reconciliação operacional entregues:
   - tela de pagamento por evento (`/pagamento/evento/{confirmationId}`) com seleção de gateway
   - webhooks (AbacatePay/Efi) marcando confirmação paga com idempotência
   - reconciliação automática (worker) + reconciliação manual por `chargeId/txId`
   - painel operacional em `/admin/payments` com contadores, última varredura automática/manual, tendência 24h, visão por gateway, varredura imediata e atualização contínua (30s)
   - alerta visual por limiar de tendência (+5 pendências em 24h), badge de severidade (`ok/atenção/crítico`) e histórico das últimas 5 varreduras automáticas
   - mini gráfico de tendência no card de varreduras e limiares de severidade configuráveis no `/admin`
   - resumo de saúde de reconciliação também no `/admin`, com timestamp de última atualização
   - throttle de auto-refresh quando a aba está em background
   - contador em tempo real de "sem atualizar há" no painel `/admin/payments`, com alerta visual de defasagem
   - auditoria automática (`Warning`) quando o painel de reconciliação fica defasado continuamente por mais de 5 minutos
   - filtro rápido em `/admin/logs` para incidentes `payment.reconciliation.panel.stale`
   - link direto no card de reconciliação para abrir `/admin/logs` já filtrado por `payment.reconciliation.panel.stale` e período padrão de 7 dias
   - parsing de querystring de logs consolidado em helper testado (`eventType/source/level/entityType/startDate/endDate`)
   - merge de estado salvo + querystring com precedência explícita para querystring no carregamento inicial de `/admin/logs`
   - builder dedicado para deep-link de staleness no `/admin/payments`, com testes determinísticos do range padrão
   - cobertura adicional de testes para casos de precedência parcial (datas/eventType) e persistência do quick-filter `PaymentPanelStale`
   - teste de integração HTTP garantindo que `/admin/payments` renderiza o deep-link de staleness com período padrão (7 dias)
   - `/admin/logs` agora aplica overrides de querystring já no carregamento inicial (SSR), evitando primeira renderização com dados fora do filtro
   - transições auditáveis de status por confirmação (`Pending/Paid/Failed/Refunded`) com operação administrativa controlada
   - script de smoke pós-deploy (`scripts/smoke-postdeploy.ps1`) para validação operacional e suporte a rollback rápido
- ✅ Hardening de release e observabilidade base documentados: checklist de produção, guia de deploy, runbook de incidentes, regras de alerta e templates de monitoramento.
- ✅ UX/roteamento (30/05/2026): `/grupos` agora é a tela inicial; Explorar moveu para `/jogos`; navegação reordenada (Grupos → Jogos → Pagamentos); UF select + datalist de cidades IBGE no formulário admin de quadras e na tela Explorar; Google Maps integrado via JS/CSP (ativo quando `Google__MapsApiKey` configurado).
- 🟡 Foco atual: validação de pagamentos Pix em produção, alertas operacionais reais e UX de grupos privados.

**Próximos tópicos priorizados:**
1. Pagamentos: testar fluxo Pix completo em produção (EfiBank) e ativar webhook AbacatePay.
2. Operação contínua: implantar alertas reais no ambiente e validar escalonamento fim a fim.
3. Produto/admin: consolidar fluxo de aprovação de grupos privados com contexto visual e atalhos de triagem.
4. Produto: indicadores de ocupação, inadimplência e conversão em pagamento.

**Operação de produção:**
1. Checklist de produção: [docs/deployment/production-checklist.md](docs/deployment/production-checklist.md)
2. Guia de deploy: [docs/deployment/README.md](docs/deployment/README.md)
3. Runbook de observabilidade (pagamentos/reconciliação): [docs/operations/observability-payments-runbook.md](docs/operations/observability-payments-runbook.md)
4. Regras de alertas operacionais: [docs/operations/payments-alert-rules.md](docs/operations/payments-alert-rules.md)
5. Templates Prometheus/Alertmanager: [docs/monitoring/README.md](docs/monitoring/README.md)
6. Dashboard Grafana (exemplo): [docs/monitoring/grafana-payments-dashboard.example.json](docs/monitoring/grafana-payments-dashboard.example.json)

---

## 📋 Revisão Estrutural (Junho 2026)

### Achados Principais

Uma auditoria completa foi realizada em **estrutura CSS, organização Blazor, separação de responsabilidades e segurança**.

#### 🔴 **Problemas Críticos Identificados**

| Problema | Impacto | Status |
|----------|---------|--------|
| **StateHasChanged() desnecessário** | 101 chamadas (60% evitáveis) causam re-renders excessivos | 🔄 **EM PROGRESSO** |
| **Memory leaks potenciais** | Apenas 3 páginas têm `IAsyncDisposable` | 🔄 **PRÓXIMO** |
| **Componentes muito grandes** | AdminPayments (1.220 linhas), Groups/Detail (1.041 linhas) | 📋 Backlog |
| **Serviços desorganizados** | 71+ serviços no nível raiz sem subpastas | 📋 Backlog |
| **CSS duplicado** | Estilos de cards, borders, gradientes repetidos | 📋 Backlog |

#### ✅ **Pontos Fortes Confirmados**

| Aspecto | Qualidade |
|---------|-----------|
| **Segurança** | ⭐⭐⭐⭐⭐ CSRF tokens, XSS protection, rate limiting, mTLS |
| **DI/Arquitetura** | ⭐⭐⭐⭐ Factories, Scoped/Singleton bem aplicados |
| **Padrões CSS** | ⭐⭐⭐⭐ Design tokens, BEM, CSS scoped |
| **Separação de responsabilidades** | ⭐⭐⭐ Serviços bem definidos |

### Plano de Ação Priorizado

**🔴 CRÍTICA (1-2 semanas):**
1. ✅ Auditoria e análise completa — [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md)
2. 🔄 **Implementar `IAsyncDisposable` globalmente** (todas as páginas)
3. 🔄 Auditoria e remoção de `StateHasChanged()` desnecessários

**🟡 MÉDIA (1-2 meses):**
4. Refatorar componentes gigantes (AdminPayments, Groups/Detail)
5. Reorganizar 71+ serviços em subpastas temáticas (`Services/Admin/`, `Services/Payment/`, etc.)
6. Consolidar CSS duplicado em utilities reutilizáveis

**🟢 BAIXA (3-6 meses):**
7. Virtual Scrolling para listas grandes
8. Cobertura de testes → meta 80%+
9. Documentação expandida (ADRs, guias Blazor)

Para detalhes completos, veja:
- [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md) — análise estrutural completa (CSS, Blazor, Serviços, Segurança, Boas Práticas)
- [DEVELOPMENT.md](DEVELOPMENT.md) — notas de desenvolvimento e decisões técnicas

---

## Checklist de Deploy (Produção)

### Variáveis obrigatórias (via User Secrets ou variáveis de ambiente)

| Chave | Descrição |
|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL |
| `AdminSeed__Email` | E-mail do admin inicial |
| `AdminSeed__Password` | Senha do admin inicial |
| `AdminSeed__FullName` | Nome completo do admin |
| `Email__Username` | Usuário SMTP |
| `Email__Password` | Senha SMTP |
| `BtcPay__WebhookSecret` | Segredo do webhook BTCPay (se ativo) |
| `BtcPay__ApiKey` | API key BTCPay (se ativo) |
| `BtcPay__StoreId` | Store ID BTCPay (se ativo) |
| `CoinGecko__ApiKey` | API key CoinGecko (opcional) |

### Passos de deploy

```bash
# 1. Aplicar migrações pendentes
dotnet ef database update --project Confirmai.csproj

# 2. Build de produção
dotnet publish -c Release -o ./publish

# 3. Rodar
cd publish
ASPNETCORE_ENVIRONMENT=Production dotnet Confirmai.dll
```

> O seed de admin e gateways padrão (Pix + Testnet ativos, BTCPay desabilitado) é executado automaticamente na inicialização.

### Recuperação de banco

```bash
# Backup
pg_dump -U <user> Confirmai > backup_$(date +%Y%m%d).sql

# Restore
psql -U <user> Confirmai < backup_YYYYMMDD.sql

# Reset completo (cuidado — destrói dados)
sudo -u postgres psql -c "DROP DATABASE IF EXISTS Confirmai;"
sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER <user>;"
dotnet ef database update
```

## Problemas Técnicos Resolvidos

### `@onclick` em componentes compartilhados não funciona (cliques ignorados)

**Sintoma:** Botões com `@onclick` dentro de componentes em `Shared/Components/` não respondem a cliques. O circuito Blazor trava na conexão inicial com o erro:
```
InvalidCharacterError: Failed to execute 'setAttribute' on 'Element': '@onclick' is not a valid attribute name.
```
Componentes dentro de `Pages/` com os mesmos handlers funcionam normalmente.

**Causa raiz:** O projeto não tinha um `_Imports.razor` na raiz. Em Blazor, cada `_Imports.razor` aplica seus `@using` apenas para arquivos `.razor` na mesma pasta e subpastas. Sem o arquivo raiz, componentes em `Shared/Components/` compilavam sem `@using Microsoft.AspNetCore.Components.Web`. O compilador Razor, não conseguindo resolver os tipos de evento (`MouseEventArgs`, etc.), emitia `@onclick` como atributo HTML literal em vez de `EventCallback`. O circuito falhava ao tentar fazer `element.setAttribute('@onclick', ...)` — inválido no DOM — e morria silenciosamente na hidratação.

**Fix:** Criar `_Imports.razor` na raiz do projeto com os imports padrão Blazor:
```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Authorization
```

**Por que mover botões para a própria página "resolvia":** Páginas em `Pages/` recebiam o `Pages/_Imports.razor` → `@onclick` compilava corretamente. Parecia um bug de componentização, mas era ausência do arquivo raiz. A paginação componentizada (`PaginationControls.razor`) e qualquer `EventCallback` em componentes de `Shared/` vão funcionar após o fix.

**Como diagnosticar em outros projetos:**
1. Ver o HTML da página (`Ctrl+U` ou DevTools → Network): se os botões tiverem `@onclick="NomeDaFuncao"` como atributo literal, a causa é essa.
2. DevTools → Console: erro `setAttribute` com `@onclick` confirma.
3. Verificar se existe `_Imports.razor` na raiz do projeto (ao lado do `.csproj`).

---

## Contribuição

Pull requests são bem-vindos! Veja o roadmap e abra issues para sugestões ou bugs.

---

## Licença

MIT
