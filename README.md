# Confirmai

[![CI](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml/badge.svg)](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml)
[![Coverage](https://raw.githubusercontent.com/FreezaCaipira/Confirmai/badges/badges/coverage.svg)](https://github.com/FreezaCaipira/Confirmai/actions/workflows/ci.yml)

Plataforma SaaS para organização de esportes amadores (Futsal) e eventos sociais (Poker). Gerencia agendamento de partidas, confirmação de jogadores, pagamentos automatizados via Pix/Bitcoin e operação administrativa de grupos.

## Status Atual

| Métrica | Valor |
|---------|-------|
| Testes | 641/641 passando |
| Build | 0 erros, 8 warnings preexistentes |
| Idiomas | PT-BR, EN-US, ES-ES (691 chaves traduzidas) |
| CSS Scoped | 110+ arquivos `.razor.css` |
| Services | 78 serviços organizados por domínio |

## Funcionalidades

- **Grupos e eventos** — Criação de comunidades, agendamento recorrente de partidas, confirmação por posição (linha/goleiro), fila de espera e promoção automática
- **Pagamentos** — Multi-gateway (BTCPayServer, AbacatePay, EfiBank, Appmax), reconciliação automática/manual, webhooks com idempotência
- **Pós-partida** — Escalação com randomização, placar editável, votação de destaque (MVP) com quórum de 50%, ranking por grupo
- **Administração** — Painel de pagamentos com telemetria por gateway, logs/auditoria estruturados, gestão de usuários e quadras
- **Autenticação** — ASP.NET Identity com roles, lockout, políticas de senha por ambiente, confirmação de e-mail
- **Observabilidade** — OpenTelemetry, alertas de webhooks pendentes e certificados, runbook operacional

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Framework | .NET 9, Blazor Server |
| Banco | PostgreSQL + EF Core |
| Real-time | SignalR (PaymentHub) |
| Pagamentos | BTCPayServer, AbacatePay, EfiBank, Appmax |
| Monitoramento | Serilog, OpenTelemetry, Prometheus/Grafana |
| Testes | xUnit + Moq (641/641 passando), Playwright E2E |
| CI | GitHub Actions (build + test + coverage) |

## Como rodar

### Pré-requisitos

- .NET 9.0 SDK
- PostgreSQL 15+

### Setup

```bash
git clone https://github.com/FreezaCaipira/Confirmai.git
cd Confirmai

# Criar banco
sudo -u postgres psql -c "CREATE USER confirmai WITH PASSWORD 'sua-senha';"
sudo -u postgres psql -c "CREATE DATABASE Confirmai OWNER confirmai;"

# Configurar appsettings.json com a connection string
# Rodar migrações
dotnet ef database update

# Iniciar
dotnet watch run
```

Acesse em `http://localhost:5000`.

**E-mail em dev**: Com `Email:Enabled=false`, e-mails são salvos em `wwwroot/uploads/dev-emails/` como `.html` e `.txt`.

### Testes

```bash
# Unitários + integração
dotnet test Confirmai.Tests/Confirmai.Tests.csproj

# E2E (requer app rodando + Node.js 20+)
cd e2e && npm install && npm run install:browsers && npm test
```

## Estrutura do Projeto

```
Confirmai/
├── Pages/                    # ~97 páginas Blazor organizadas por domínio
│   ├── Admin/                # Dashboard admin + componentes
│   ├── Futsal/               # Partidas, escalação, schedule
│   ├── Groups/               # Grupos, ranking, configurações
│   ├── Payment/              # Pagamentos, checkout, histórico
│   ├── Poker/                # Torneios de poker
│   └── ...                   # Profile, Mailbox, VenueManager, etc.
├── Shared/Components/        # 28 componentes reutilizáveis
├── Services/                 # 78 serviços organizados por domínio
│   ├── Admin/                # Logs, filtros, segurança, auditoria
│   ├── Core/                 # UiText, log, auth, certificados
│   ├── Payment/              # Gateways, webhooks, reconciliação
│   ├── Events/               # Métricas, colisão, notificações
│   ├── EventPayments/        # Gateways de pagamento por evento
│   ├── Crypto/               # Cotações BTC/USD/BRL
│   ├── User/                 # Preferências, claims, perfil
│   ├── Factories/            # BitcoinPaymentFactory, EventPaymentGatewayFactory
│   ├── Interfaces/           # Contratos de serviço
│   └── Utility/              # Email, PII, produtos, testnet
├── Models/                   # Entidades do domínio
├── Data/                     # AppDbContext + Factory
├── Configuration/            # Options (BtcPay, Email, Security)
├── Hubs/                     # PaymentHub (SignalR)
├── wwwroot/css/              # 4 CSS globais + 110+ scoped (.razor.css)
├── Confirmai.Tests/          # xUnit (641 testes)
└── e2e/                      # Playwright E2E (TypeScript)
```

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [WORK_PLAN.md](WORK_PLAN.md) | Ordem de trabalho atual (Qualidade → UX → Operação → Produção) |
| [ROADMAP.md](ROADMAP.md) | Progresso, prioridades e próximos passos |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Convenções, patterns e guia para contribuidores |
| [docs/deploy.md](docs/deploy.md) | Guia completo de deploy com Docker |
| [docs/production-checklist.md](docs/production-checklist.md) | Checklist de variáveis e verificações para produção |
| [docs/observability-payments-runbook.md](docs/observability-payments-runbook.md) | Runbook de incidentes de pagamento |
| [docs/troubleshooting-pix.md](docs/troubleshooting-pix.md) | Troubleshooting de Pix/webhooks |
| [docs/monitoring/](docs/monitoring/) | Templates Prometheus, Alertmanager e Grafana |

## Licença

MIT
