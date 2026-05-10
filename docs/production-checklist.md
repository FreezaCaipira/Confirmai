# Checklist de Produção — Confirmai

Use este documento antes de qualquer deploy em produção para garantir que todos os requisitos operacionais estão satisfeitos.

---

## 1. Variáveis de ambiente / User Secrets

Todas as entradas marcadas como `__SET_VIA_USER_SECRETS_OR_ENV__` em `appsettings.Production.json` precisam ser definidas via variáveis de ambiente (`ASPNETCORE_*` ou via Docker env) ou via .NET User Secrets no servidor.

### Banco de dados

| Chave | Descrição |
|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL. Ex: `Host=db;Port=5432;Database=Confirmai;Username=app;Password=<secret>;SslMode=Require` |

### Conta admin inicial (seed)

| Chave | Descrição |
|---|---|
| `AdminSeed__Email` | E-mail do usuário admin criado no primeiro boot |
| `AdminSeed__Password` | Senha forte (mín. 12 chars, maiúsc, símbolo) |
| `AdminSeed__FullName` | Nome de exibição do admin |

> Após o primeiro boot, troque a senha via interface e remova/invalide a variável de seed.

### BTCPay Server

| Chave | Descrição |
|---|---|
| `BtcPay__Url` | URL base do seu BTCPay Server. Ex: `https://btcpay.suaempresa.com` |
| `BtcPay__StoreId` | Store ID do BTCPay |
| `BtcPay__ApiKey` | API Key com permissão de criar invoices |
| `BtcPay__WebhookSecret` | Segredo configurado no webhook do BTCPay (mín. 32 chars aleatórios) |
| `BtcPay__WebhookUrl` | URL pública do webhook: `https://Confirmai.suaempresa.com/api/btcpay/webhook` |

### SMTP / E-mail

| Chave | Descrição |
|---|---|
| `Email__Host` | Servidor SMTP. Ex: `smtp.sendgrid.net` |
| `Email__Port` | Porta SMTP. Ex: `587` (STARTTLS) |
| `Email__UseSsl` | `true` para STARTTLS/SSL |
| `Email__Username` | Usuário SMTP |
| `Email__Password` | Senha ou token SMTP |
| `Email__FromEmail` | Endereço remetente. Ex: `no-reply@Confirmai.com` |
| `Email__FromName` | Nome exibido no e-mail. Ex: `Confirmai` |
| `Email__Enabled` | `true` para habilitar envio de e-mails |

### OpenTelemetry (observabilidade) — opcional mas recomendado

| Chave | Descrição |
|---|---|
| `OpenTelemetry__Endpoint` | URL OTLP gRPC/HTTP. Ex: `https://otel.suaempresa.com:4317` |
| `OpenTelemetry__Headers` | Headers de autenticação OTLP. Ex: `Authorization=Bearer <token>` |
| `OpenTelemetry__ServiceName` | `Confirmai` (ou nome personalizado) |
| `OpenTelemetry__Environment` | `production` |

---

## 2. ASPNETCORE_ENVIRONMENT

```bash
ASPNETCORE_ENVIRONMENT=Production
```

- **Nunca** use `Development` em produção — expõe erros detalhados, endpoints dev-only (`/api/test/*`) e reduz restrições de segurança.
- O Dockerfile já define `ASPNETCORE_ENVIRONMENT=Production` por padrão.

---

## 3. HTTPS e certificados

- [ ] Certificado TLS válido instalado (Let's Encrypt ou CA comercial)
- [ ] Redirecionamento HTTP → HTTPS configurado no reverse proxy (nginx/Caddy/Traefik)
- [ ] HSTS ativo: o app já configura `max-age=365d; includeSubDomains` em produção
- [ ] Não exponha a porta 5000 diretamente — use o reverse proxy como terminador TLS

---

## 4. Banco de dados PostgreSQL

- [ ] PostgreSQL 15+ instalado e acessível
- [ ] Usuário da aplicação com permissões mínimas (sem `SUPERUSER`)
- [ ] SSL exigido na connection string (`SslMode=Require` ou `SslMode=VerifyFull`)
- [ ] Migrations aplicadas automaticamente no boot (`db.Database.MigrateAsync()`) ou manualmente com `dotnet ef database update`
- [ ] Backup automático configurado (diário, mínimo 7 dias de retenção)
- [ ] Testar restore de backup antes de ir para produção

---

## 5. Segurança operacional

- [ ] Senhas e chaves NÃO estão em arquivos versionados (`appsettings.Production.json` só tem placeholders `__SET_VIA__`)
- [ ] Logs não expõem senhas, tokens ou dados pessoais (verificar `LogRetentionService` — IPs anonimizados após 30 dias, logs não-financeiros purgados após 90 dias)
- [ ] Rate limiting ativo: o app configura `webhook` rate limiter em produção
- [ ] Headers de segurança ativos: CSP com nonce, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` (testados via `SecurityHeadersIntegrationTests`)
- [ ] Endpoint `/api/test/seed-order` **não está acessível** (automaticamente omitido quando `ASPNETCORE_ENVIRONMENT != Development`)
- [ ] `SeedTestUsers:Enabled` não está definido como `true` em produção

---

## 6. Monitoramento e alertas

- [ ] OTLP endpoint configurado e recebendo traces + métricas
- [ ] Alertas configurados para: `5xx` em taxa > 1%, latência P99 > 2s, disco > 80%
- [ ] Logs de auditoria acessíveis em `/admin/logs` (apenas admins)
- [ ] Timeline de entidades acessível em `/admin/audit/{entityType}/{entityId}`

---

## 7. Smoke test pós-deploy

Execute os seguintes passos manualmente após cada deploy:

1. [ ] Acesse `https://Confirmai.suaempresa.com/health` → deve retornar `200 OK`
2. [ ] Faça login com a conta admin
3. [ ] Acesse `/admin` → dashboard carrega sem erro
4. [ ] Acesse `/marketplace` → produtos são listados
5. [ ] Acesse `/admin/logs` → logs recentes aparecem
6. [ ] (Staging apenas) Execute `npx playwright test` completo com `E2E_ADMIN_EMAIL` e `E2E_ADMIN_PASSWORD` definidos

---

## 8. Checklist de rollback

Se algo der errado após o deploy:

1. Reverter imagem Docker para a versão anterior: `docker pull Confirmai:<versao-anterior>`
2. Verificar se migrations foram destrutivas — se sim, restaurar backup antes do rollback
3. Verificar logs de erro: `docker logs <container>` ou painel OTLP
4. Contato de emergência: admin do servidor

---

## Referências

- [README.md](README.md) — setup local e visão geral
- [roadmap.md](roadmap.md) — histórico de fases e features
- [appsettings.Production.json](appsettings.Production.json) — template de configuração
- [Dockerfile](Dockerfile) — imagem de produção
- `.github/workflows/ci.yml` — pipeline CI/CD
