# Checklist de Produção — Confirmai

Use este documento antes de qualquer deploy em produção para garantir que todos os requisitos operacionais estão satisfeitos.

---

## 0. EasyPanel (deploy atual)

Um item verificável por linha. As seções 1+ abaixo seguem válidas como referência
de variáveis; esta seção cobre o deploy real de hoje. Antes do go-live real,
juntar com o "CHECKLIST OBRIGATORIO DE RESET PRE-PRODUCAO REAL" do `WORK_PLAN.md`
(rotação de todas as credenciais) — este documento não o repete.

### Variáveis de ambiente obrigatórias (efeito de cada uma faltando)

- [ ] `ConnectionStrings__DefaultConnection` — sem ela o app **não sobe** (`MigrateAsync` falha no boot).
- [ ] `ASPNETCORE_ENVIRONMENT=Production` — o `Dockerfile` já define, e sem a variável o ASP.NET Core assume `Production`. O risco real é alguém setar `Development` (ou `Testing`) no painel: cookies sem TLS, `AdminSeed:SyncPassword` default `true`, `ws:` na CSP, sem HTTPS redirect e endpoints `/api/test/*` expostos. Conferir que o valor é exatamente `Production`.
- [ ] `AdminSeed__SyncPassword=false` — **explícito**. Em Production o default já é `false`, mas fixá-lo impede que uma cópia de config de dev reescreva a senha do admin a cada boot.
- [ ] `AdminSeed__Email` / `AdminSeed__Password` / `AdminSeed__FullName` — sem elas nenhum admin é criado (log: "Pulando criação de usuário admin seed"). **Remover após o primeiro boot e troca de senha.**
- [ ] `Authentication__Google__ClientId` + `Authentication__Google__ClientSecret` — sem as duas o botão do Google **some** (o `AddGoogle` não é registrado) e o login por email/senha continua funcionando. Rollback do Google = remover as variáveis.
- [ ] Bloco Brevo completo: `Email__Enabled=true`, `Email__Host=smtp-relay.brevo.com`, `Email__Port=587`, `Email__UseSsl=true`, `Email__Username` (login SMTP `...@smtp-brevo.com`), `Email__Password` (**SMTP key** `xsmtpsib-...`, não API key `xkeysib-...`), `Email__FromEmail` (**sender validado na Brevo** — se divergir, a Brevo recusa o envio), `Email__FromName`. Sem `Email__Enabled=true` o `RequireConfirmedEmail` fica desligado; com sender inválido o cadastro por email **trava sem erro claro** (email nunca chega).
- [ ] `OpenTelemetry__Endpoint` + `OpenTelemetry__Headers` (opcional) — sem elas as métricas `confirmai_*` existem mas não saem do processo.

### Proxy reverso e boot

- [ ] `ForwardedHeaders` ativo — o EasyPanel termina TLS no proxy; sem `X-Forwarded-Proto` o `redirect_uri` do OAuth sai em `http://` e o Google recusa (PR #98).
- [ ] Log de boot mostra migrations aplicadas — procurar por `Migrate`/seed no log do primeiro deploy; `db.Database.MigrateAsync()` roda no startup.
- [ ] `ASPNETCORE_URLS`/porta conforme o proxy do EasyPanel espera (app escuta HTTP interno; TLS só na borda).

### Gateways de pagamento (V2 — desligados)

- [ ] BTCPay, AbacatePay e Efi estão **V2 / desligados**: sem credenciais de gateway nas env vars e `EnablePaymentGateways=false` em todos os grupos (o toggle é por grupo, no banco). O caminho do dinheiro do V1 é 100% manual (Pix do admin + comprovante).
- [ ] Webhook Efi mTLS — só quando o V2 for ativado; hoje não há endpoint esperando webhook externo de gateway.

### WhatsApp / Evolution API (C31 — desligado até liberação)

O C31 já está no código, mas **tudo é no-op** sem as variáveis abaixo. Ordem de ativação: subir a Evolution no EasyPanel → parear o chip dedicado (já aquecido, fora de automação prévia) → `Enabled=true` + `DryRun=true` por alguns dias → `AllowedGroupJids` com o(s) JID(s) reais → só então `DryRun=false`.

- [ ] `WhatsApp__Enabled=false` (default) — sem as demais variáveis o app funciona normalmente; nenhuma mensagem é tentada.
- [ ] `WhatsApp__DryRun=true` (default) — com `Enabled=true` registra cada mensagem como `dry_run` na tabela `WhatsAppDispatches` e na métrica `confirmai_whatsapp_send_total{result="dry_run"}` **sem chamar a Evolution**. Fase de aquecimento/observação.
- [ ] `WhatsApp__BaseUrl` — URL **interna** da Evolution no EasyPanel (ex.: `http://evolution:8080`), nunca domínio público. Ausente com `DryRun=false` = **boot falha** (guard rail).
- [ ] `WhatsApp__Instance` — nome da instância pareada (o chip dedicado). Mesma trava de boot.
- [ ] `WhatsApp__ApiKey` — `AUTHENTICATION_API_KEY` da Evolution, via env var, **nunca** em appsettings versionado. Mesma trava de boot.
- [ ] `WhatsApp__AllowedGroupJids` — CSV de JIDs `...@g.us` permitidos. Com `Enabled=true` + `DryRun=false` fora de dev, lista **vazia falha o boot**; JID fora da lista é bloqueado (`result=blocked_allowlist`).
- [ ] `WhatsApp__PublicBaseUrl` — URL pública do app (ex.: `https://confirmai.com`) para os links das mensagens. Vazio = mensagem vai **sem link** (não falha).
- [ ] `App__PublicBaseUrl` (C40) — URL pública do app para os **links dos emails** (partida, grupo, pagamentos). Vazio = email sai sem CTA (não falha). `WhatsApp__PublicBaseUrl` continua valendo como base; `App__PublicBaseUrl` é o fallback.
- [ ] `TZ=America/Sao_Paulo` — os lembretes ("hoje tem", "falta 1h") e os horários das mensagens usam a TZ do container; sem ela sobem em UTC.
- [ ] JID do grupo configurado em `/grupo/{id}/configuracoes` **pelo admin do grupo** (o campo valida `...@g.us`; a guarda é no service, não só na tela).
- [ ] Rollback do WhatsApp = `WhatsApp__Enabled=false` + redeploy. Nada crítico depende do canal (é o desenho: canal secundário).

### Rollback no EasyPanel

- [ ] Identificar a imagem/commit do deploy anterior (tag no EasyPanel) **antes** de deployar.
- [ ] Rollback = redeploy da imagem/commit anterior + smoke: home abre, login por email/senha funciona, `/grupos` carrega.
- [ ] Se a falha for no Google: remover `Authentication__Google__ClientSecret` + redeploy (o botão some, app segue).
- [ ] Se a falha for no email: `Email__Enabled=false` + redeploy (desliga `RequireConfirmedEmail`; cadastro volta a não exigir confirmação).

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
- [ ] Endpoint `/api/test/seed-event-confirmations` **não está acessível** (só é mapeado em `Development` ou `Testing`)
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
