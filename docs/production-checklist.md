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

## 3. HTTPS, nginx e mTLS EfiBank

- [ ] Certificado TLS válido instalado (Let's Encrypt via certbot)
- [ ] nginx configurado com `ops/nginx/confirmai.conf` (ou equivalente)
- [ ] Redirecionamento HTTP → HTTPS ativo no nginx
- [ ] HSTS ativo: o app já configura `max-age=365d; includeSubDomains` em produção
- [ ] `ssl_verify_client optional_no_ca` configurado no nginx (necessário para receber webhooks EfiBank com mTLS)
- [ ] Header `X-SSL-Client-Cert $ssl_client_escaped_cert` configurado no nginx → backend
- [ ] (Opcional, mais seguro) CA da Efí baixado em `/etc/nginx/certs/efipay-ca.crt` e `ssl_verify_client optional` ativo
- [ ] Não exponha a porta 8080 diretamente — o nginx é o único ponto de entrada externo

### EfiBank Pix (produção)

| Chave | Descrição |
|---|---|
| `EfiBank__ClientId` | Client ID de produção (painel Efí → API → Credenciais) |
| `EfiBank__ClientSecret` | Client Secret de produção |
| `EfiBank__CertificatePath` | Caminho do `.p12` de produção dentro do container: `/run/secrets/efibank.p12` |
| `EfiBank__CertificatePassword` | Senha do `.p12` (vazio se não houver) |
| `EfiBank__PixKey` | Chave Pix cadastrada na conta Efí (CPF, CNPJ, e-mail ou EVP) |
| `EfiBank__Sandbox` | `false` em produção |
| `EfiBank__WebhookSecret` | Segredo de query-string do webhook (mín. 32 chars aleatórios) |
| `EfiBank__WebhookUrl` | `https://SEU_DOMINIO/api/webhooks/efibank/pix?webhookSecret=<WebhookSecret>` |
| `EfiBank__WebhookClientCertSubject` | `conta.efipay.com.br` (padrão — não alterar) |
| `EFIBANK_CERT_FILE` | (`.env`) caminho local do `.p12` no host para o Docker secret |

- [ ] `EfiBank__Sandbox` = `false`
- [ ] Certificado `.p12` de **produção** montado via Docker secret em `/run/secrets/efibank.p12`
- [ ] Webhook registrado na Efí (automático no boot quando `WebhookUrl` está configurado — verificar log `Webhook registrado com sucesso`)
- [ ] Guard de sandbox ativo: se `Sandbox=true` em produção, o app lança exceção e não sobe

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
- [ ] `otel-collector` ativo no ambiente e expondo métricas OTLP convertidas em Prometheus (`ops/monitoring/otel-collector.yml`)
- [ ] Prometheus carregando `ops/monitoring/prometheus-payments-alerts.yml` sem erros
- [ ] Alertmanager carregando `ops/monitoring/alertmanager.yml` e entregando notificações ao receiver esperado
- [ ] Alertas configurados para: `5xx` em taxa > 1%, latência P99 > 2s, disco > 80%
- [ ] Logs de auditoria acessíveis em `/admin/logs` (apenas admins)
- [ ] Timeline de entidades acessível em `/admin/audit/{entityType}/{entityId}`
- [ ] Runbook de incidentes de pagamentos/reconciliação revisado e disponível para on-call (`docs/observability-payments-runbook.md`)
- [ ] Regras de alerta de pagamentos/reconciliação implantadas e validadas (`docs/payments-alert-rules.md`, `ops/monitoring/prometheus-payments-alerts.yml`)
- [ ] Templates operacionais de Prometheus/Alertmanager adaptados para o ambiente (`docs/monitoring/`, `ops/monitoring/`)
- [ ] Fallback de alertas por logs (LogQL) ativo quando contadores de domínio ainda não estiverem disponíveis (`docs/monitoring/logql-payments-alerts.example.md`)
- [ ] Dashboard Grafana de pagamentos importado e validado (`docs/monitoring/grafana-payments-dashboard.example.json`)

---

## 7. Smoke test pós-deploy

Execute o smoke automatizado apos cada deploy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-postdeploy.ps1 -BaseUrl https://Confirmai.suaempresa.com
```

Quando o profile `monitoring` estiver habilitado no ambiente, execute tambem:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-monitoring-alerts.ps1 -RequirePaymentRules -RequireCollectorMetrics
```

Opcional (somente quando ja houver sessao admin autenticada no host de validacao):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-postdeploy.ps1 -BaseUrl https://Confirmai.suaempresa.com -StrictAdmin
```

Checklist manual complementar:

1. [ ] Acesse `https://Confirmai.suaempresa.com/health` → deve retornar `200 OK`
2. [ ] Faça login com a conta admin
3. [ ] Acesse `/admin` → dashboard carrega sem erro
4. [ ] Acesse `/marketplace` → produtos são listados
5. [ ] Acesse `/admin/logs` → logs recentes aparecem
6. [ ] (Staging apenas) Execute `npx playwright test` completo com `E2E_ADMIN_EMAIL` e `E2E_ADMIN_PASSWORD` definidos

---

## 8. Checklist de rollback

Se algo der errado após o deploy:

1. Congelar deploy atual e coletar evidencias (status do smoke, logs e ultima imagem ativa).
2. Reverter imagem Docker para a versao anterior conhecida como estavel.
3. Reiniciar apenas o servico `app` com a imagem anterior e validar `/health`.
4. Reexecutar smoke pos-rollback (`scripts/smoke-postdeploy.ps1`).
5. Se migration destrutiva tiver sido aplicada, restaurar backup e repetir smoke.
6. Registrar incidente com horario, causa provavel e acao corretiva.

---

## Referências

- [README.md](../README.md) — setup local e visão geral
- [ROADMAP.md](../ROADMAP.md) — progresso e próximos passos
- [CONTRIBUTING.md](../CONTRIBUTING.md) — convenções e workflow
- [appsettings.Production.json](../appsettings.Production.json) — template de configuração
- [Dockerfile](../Dockerfile) — imagem de produção
- `.github/workflows/ci.yml` — pipeline CI/CD
- [Runbook de observabilidade](observability-payments-runbook.md) — triagem e resposta para pagamentos/reconciliação
- [Templates de monitoramento](monitoring/README.md) — arquivos exemplo para Prometheus/Alertmanager
