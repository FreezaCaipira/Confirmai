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

3. **Configure o `appsettings.json`**
   
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

### Nota importante sobre paginação e componentização

Decisão atual do projeto:
- As paginações foram mantidas em código local das páginas (sem componente compartilhado de paginação) por estabilidade operacional.

Evidências observadas no projeto:
- Na tela `"/admin/logs"`, a versão componentizada da paginação apresentou cliques sem avanço de página em ambiente real.
- Ao substituir por botões locais na própria página, o comportamento voltou ao normal imediatamente.
- O comportamento foi percebido em histórico anterior do projeto e reproduzido novamente no ciclo atual.

Registro cronológico recente (refatoração/auditoria):
- Cenário inicial estável: .NET 9 + paginação local na tela de logs.
- Tentativa de padronização com componente de paginação: regressão de clique sem avanço em logs.
- Mitigação imediata: descomponentização da paginação em logs e posteriormente em todas as rotas paginadas.
- Tentativa de atualização para .NET 10 para eliminar hipótese de bug de versão: compilou, mas o comportamento reportado em ambiente real não estabilizou.
- Decisão operacional: rollback completo para .NET 9 (`global.json`, `TargetFramework` da aplicação e testes), preservando paginação local.
- Estado validado após rollback: comportamento voltou a funcionar no fluxo reportado.

Risco conhecido e lição aprendida:
- Risco: regressão silenciosa de interatividade em paginações durante recomposição/upgrade.
- Lição: preferir estabilidade observável em runtime real antes de consolidar abstrações compartilhadas.
- Regra prática: abstração só permanece quando o comportamento final for equivalente em todas as rotas críticas.

Escopo atual da decisão:
- Páginas administrativas e de usuário com paginação usam implementação local.
- O componente `Shared/Components/PaginationControls.razor` não é obrigatório para os fluxos atuais.
- Ações críticas de filtros (`Filtrar`/`Limpar`) em páginas admin também usam botões locais (sem componente intermediário de ação) para reduzir risco de regressão de callback.

Diretriz para futura recomposição:
- Só reintroduzir paginação componentizada com teste manual obrigatório nas rotas: `/admin/logs`, `/admin/users`, `/admin/products`, `/admin/payments`, `/admin/orders`, `/admin/orders-review`, `/orders`, `/payments`, `/products`, `/marketplace`.
- Registrar evidência do teste (data, versão .NET, navegador e resultado por rota) antes de consolidar a recomposição.
- Em caso de regressão em qualquer rota, voltar para paginação local nessa rota.
- Recomendação para PR de recomposição: incluir checklist de validação de clique, persistência de página atual e comportamento após filtro/ordenação.

Referências para debate na comunidade:
- Issue tracker ASP.NET Core: https://github.com/dotnet/aspnetcore/issues
- Discussões ASP.NET Core: https://github.com/dotnet/aspnetcore/discussions
- Docs de EventCallback: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling
- Docs de render modes/interatividade: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes

---

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

## Roadmap e Progresso

Veja o arquivo [roadmap.md](roadmap.md) para detalhes das fases e próximos passos.

**Status rápido (Jul/2025):**
- ✅ Suite de testes: **560 testes** unitários e de integração passando, 0 falhas (`Confirmai.Tests`) + suíte E2E Playwright completa incluindo fluxo ponta-a-ponta.
- ✅ **E2E ponta-a-ponta** (`purchase-flow-full.spec.ts`): pagamento confirmado → pedido em /orders → admin libera → status Finalizado (8 testes determinísticos via endpoint dev-only `POST /api/test/seed-order`).
- ✅ **Checklist de produção** documentado em [docs/production-checklist.md](docs/production-checklist.md): variáveis de ambiente, HTTPS, PostgreSQL, SMTP, OTLP, segurança e smoke test pós-deploy.
- ✅ **Guia de deploy** em [docs/deploy.md](docs/deploy.md): Docker Compose, nginx, Let's Encrypt, backup/restore e atualização.
- ✅ Fluxo de pagamento robustecido: Pix (gateway principal), Testnet e BTCPayServer (desabilitado). Confirmação Pix manual pelo vendedor.
- ✅ Sistema multi-servidor OpenTibia completo: catálogo de itens, membros, GMs, ownership e API Keys por servidor.
- ✅ Mailbox entre usuários (`/mailbox`) com anexos, arquivamento e conversas.
- ✅ Catálogo de itens com ofertas por servidor (`/servers/{id}/catalog`, `/servers/{id}/items/{key}`).
- ✅ Perfil expandido: handles de contato (Pix, Discord), recomendações de vendedores.
- ✅ **Performance da home** (`/servers`): skeleton loading, `PersistentComponentState`, `IMemoryCache` (TTL 60s), consultas `AsNoTracking` sem `Include(Members)`, redirect `/` consolidado em `/servers` sem JS.
- ✅ **Compressão HTTP** (Brotli + Gzip) habilitada para HTTPS, MIME types incluindo `text/css`, `application/javascript`, `application/json` e `image/svg+xml`.
- ✅ **CSS preload + deferral de fontes** em `_Host.cshtml` (Google Fonts + Font Awesome carregados com `media="print"` e promovidos via JS após `load`).
- ✅ **CSP endurecida**: nonce por requisição em `script-src`, `base-uri 'self'`, `form-action 'self'`, `frame-ancestors 'none'`. Removido `X-XSS-Protection` legado e `'unsafe-inline'` de scripts.
- ✅ **Sistema de botões unificado** em `site.css`: `.btn` + `.btn--primary/--success/--danger/--neutral/--info/--gold` + `.btn--sm/--icon`. Classes legadas mantidas como aliases.
- ✅ **Auditoria estruturada de eventos**: `AppLog` extendido com `EventType`, `EntityType`, `EntityId`, `IpAddress`, `CorrelationId`, `MetadataJson` (jsonb) + índices. `LogService.AuditAsync(...)` cobre `user.registered/login.*`, `product.*`, `server.*`, `payment.confirmed`, `order.created/released`. Constantes em `Services/AuditEvents.cs`.
- ✅ Agentes de entrega cadastrados e gerenciáveis pelo painel admin.
- ✅ Filtros e ordenação persistentes em todas as telas administrativas, com testes dedicados.
- ✅ Exportação de logs administrativos e gestão de idiomas no painel admin.
- ✅ HSTS configurado para 365 dias com `includeSubDomains` em produção.
- ✅ UX responsiva consolidada: card-stacking mobile em todas as telas admin e de pedidos.
- ✅ **Exclusão segura de usuários (Fase 6)**: FKs com `SetNull`/`Cascade`; pedidos bloqueados quando participante deletado; roteamento automático para revisão admin; UI exibe "(deletado)"; moeda exibida corretamente em BRL/USD.
- ✅ **Testes de audit logging** (`AuditLoggingHooksTests`): hooks de `ProductService`, `ServerRegistrationRequestService` e `LogService` cobertos (7 testes).
- ✅ **Testes de cache da home** (`TibiaServerServiceCacheTests`): `IMemoryCache` em `GetAllAsync` e `GetAllServerCardStatsAsync` cobertos (3 testes).
- ✅ **Testes de security headers** (`SecurityHeadersIntegrationTests`): CSP nonce único por request, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` (3 testes).
- ✅ **9 testes `ServerIntegrationEndpoints` corrigidos**: falha era `503 ServiceUnavailable` por ausência do feature flag `LuaDeliveryEnabled`; `EnsureLuaDeliveryEnabledAsync()` adicionado em `IntegrationTestWebAppFactory`.
- 🟡 Próxima frente: ampliar auditoria para `ItemOffer`/`ServerMember`/`AppSettings`/senhas; página admin de timeline por entidade; CI/CD GitHub Actions.

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
- Testes E2E (Playwright) para fluxos de compra, pedido e acesso admin

---

## Contribuição

Pull requests são bem-vindos! Veja o roadmap e abra issues para sugestões ou bugs.

---

## Licença

MIT


