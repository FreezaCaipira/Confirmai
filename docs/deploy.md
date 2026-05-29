# Guia de Deploy — Confirmai

Guia completo para colocar o Confirmai em produção com Docker, PostgreSQL e nginx.

---

## Pré-requisitos

| Requisito | Versão mínima |
|---|---|
| Docker Engine | 24+ |
| Docker Compose | v2.24+ |
| PostgreSQL | 15+ (ou via container) |
| Domínio com DNS apontado | — |
| Certificado TLS (Let's Encrypt) | — |

> Se preferir PostgreSQL gerenciado (Supabase, Neon, RDS, etc.), pule o container `db` do Compose e use a connection string do provedor.

---

## 1. Clonar o repositório

```bash
git clone https://github.com/FreezaCaipira/Confirmai.git
cd Confirmai
```

---

## 2. Usar o `docker-compose.yml` incluído no repositório

O arquivo `docker-compose.yml` já está versionado na raiz do projeto com todos os serviços configurados via variáveis de ambiente. Basta criar o `.env` (próxima seção) e executar os comandos de deploy.

Para referência, o conteúdo do arquivo:

```yaml
services:
  db:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: Confirmai
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d Confirmai"]
      interval: 10s
      timeout: 5s
      retries: 5

  app:
    build: .
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=Confirmai;Username=app;Password=${DB_PASSWORD};SslMode=Disable"
      AdminSeed__Email: ${ADMIN_EMAIL}
      AdminSeed__Password: ${ADMIN_PASSWORD}
      AdminSeed__FullName: ${ADMIN_FULLNAME}
      BtcPay__Url: ${BTCPAY_URL}
      BtcPay__StoreId: ${BTCPAY_STORE_ID}
      BtcPay__ApiKey: ${BTCPAY_API_KEY}
      BtcPay__WebhookSecret: ${BTCPAY_WEBHOOK_SECRET}
      BtcPay__WebhookUrl: "https://${DOMAIN}/api/btcpay/webhook"
      Email__Host: ${SMTP_HOST}
      Email__Port: ${SMTP_PORT}
      Email__UseSsl: "true"
      Email__Username: ${SMTP_USERNAME}
      Email__Password: ${SMTP_PASSWORD}
      Email__FromEmail: ${SMTP_FROM_EMAIL}
      Email__FromName: "Confirmai"
      Email__Enabled: "true"
      OpenTelemetry__Endpoint: ${OTEL_ENDPOINT:-http://otel-collector:4318}
      OpenTelemetry__Headers: ${OTEL_HEADERS:-}
      OpenTelemetry__ServiceName: ${OTEL_SERVICE_NAME:-Confirmai}
      OpenTelemetry__Environment: ${OTEL_ENVIRONMENT:-production}

volumes:
  pgdata:
```

---

## 3. Criar o arquivo `.env`

Crie `.env` na raiz do projeto (este arquivo **não** é versionado — veja `.gitignore`):

```dotenv
# Banco de dados
DB_PASSWORD=senha_forte_aqui_min32chars

# Admin inicial (removido após primeiro boot)
ADMIN_EMAIL=admin@suaempresa.com
ADMIN_PASSWORD=SenhaForte!123
ADMIN_FULLNAME=Administrador

# Domínio público
DOMAIN=Confirmai.suaempresa.com

# BTCPay Server
BTCPAY_URL=https://btcpay.suaempresa.com
BTCPAY_STORE_ID=AbCdEfGhIjKl
BTCPAY_API_KEY=token_btcpay_aqui
BTCPAY_WEBHOOK_SECRET=segredo_min32chars_aleatorio

# SMTP
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USERNAME=apikey
SMTP_PASSWORD=SG.token_sendgrid_aqui
SMTP_FROM_EMAIL=no-reply@suaempresa.com

# EfiBank Pix
EFIBANK_CLIENT_ID=seu_client_id_producao
EFIBANK_CLIENT_SECRET=seu_client_secret_producao
EFIBANK_CERT_FILE=/home/deploy/efibank-producao.p12
EFIBANK_CERT_PASSWORD=
EFIBANK_PIX_KEY=sua_chave_pix_cadastrada_na_efi
EFIBANK_WEBHOOK_SECRET=segredo_min32chars_aleatorio
# EFIBANK_WEBHOOK_CERT_SUBJECT=conta.efipay.com.br  # padrão, não precisa alterar

# Opcional: sobrescrever destino OTLP se nao usar o collector interno do compose
# OTEL_ENDPOINT=http://otel-collector:4318
# OTEL_HEADERS=Authorization=Bearer seu_token
```

> **Nunca versione `.env`** — adicione ao `.gitignore` e faça backup seguro (ex: gestor de segredos).

---

## 4. Configurar o nginx como reverse proxy

O arquivo de configuração nginx já está incluído no repositório em `ops/nginx/confirmai.conf`.
Ele configura:
- Redirecionamento HTTP → HTTPS
- TLS (Let's Encrypt)
- **mTLS opcional** para que o EfiBank possa enviar webhooks com certificado cliente
- WebSocket (necessário para Blazor Server / SignalR)
- Cabeçalhos de proxy corretos (`X-Forwarded-For`, `X-Forwarded-Proto`)

```bash
# Copie, edite o domínio e ative
sudo cp ops/nginx/confirmai.conf /etc/nginx/sites-available/confirmai
sudo sed -i 's/confirmai.suaempresa.com/SEU_DOMINIO_REAL/g' /etc/nginx/sites-available/confirmai
sudo ln -s /etc/nginx/sites-available/confirmai /etc/nginx/sites-enabled/confirmai
sudo nginx -t && sudo systemctl reload nginx
```

### Certificado TLS com Let's Encrypt

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d confirmai.suaempresa.com
```

### mTLS EfiBank — validação de CA (recomendado em produção)

Por padrão o `confirmai.conf` usa `ssl_verify_client optional_no_ca`, que aceita qualquer
certificado cliente sem validar a cadeia de CA. O app valida apenas o Subject (`conta.efipay.com.br`).

Para validação completa com o CA da Efí:

```bash
sudo mkdir -p /etc/nginx/certs
# Produção
sudo curl -o /etc/nginx/certs/efipay-ca.crt \
     https://certificados.efipay.com.br/efipay.crt
```

Depois edite `/etc/nginx/sites-available/confirmai` e substitua:

```nginx
# DE:
ssl_verify_client optional_no_ca;

# PARA:
ssl_verify_client    optional;
ssl_client_certificate /etc/nginx/certs/efipay-ca.crt;
```

Recarregue: `sudo nginx -t && sudo systemctl reload nginx`

### Renovação automática do certificado (cron)

O certbot instala automaticamente um timer systemd ou cron job. Verifique se está ativo:

```bash
# via systemd (recomendado no Ubuntu 20.04+)
sudo systemctl status certbot.timer

# ou via cron legado
sudo cat /etc/cron.d/certbot
```

Se precisar configurar manualmente, adicione em `/etc/cron.d/certbot`:

```cron
# Tenta renovar diariamente às 03:12 e 15:12; recarrega nginx se bem-sucedido
12 3,15 * * * root certbot renew --quiet --deploy-hook "systemctl reload nginx"
```

Teste a renovação sem efetivá-la:

```bash
sudo certbot renew --dry-run
```

---

## 5. Build e primeiro boot

```bash
# Construir a imagem
docker compose build

# Subir os serviços (migrations + seed rodam automaticamente no boot)
docker compose up -d

# Acompanhar logs do primeiro boot
docker compose logs -f app
```

Aguarde a linha `Application started. Press Ctrl+C to shut down.` — as migrations já foram aplicadas e o admin seed criado.

---

## 6. Verificar saúde da aplicação

```bash
curl -f https://Confirmai.suaempresa.com/health
# Esperado: 200 OK
```

---

## 7. Configurar webhook no BTCPay Server

1. Acesse seu BTCPay Server → Store → **Settings → Webhooks → Add Webhook**
2. URL: `https://confirmai.suaempresa.com/api/btcpay/webhook`
3. Secret: mesmo valor de `BTCPAY_WEBHOOK_SECRET` no `.env`
4. Eventos: `InvoiceSettled`, `InvoiceExpired`, `InvoiceInvalid`

---

## 8. Registrar webhook no EfiBank (Pix)

O app registra o webhook automaticamente na inicialização quando `EfiBank:WebhookUrl` está configurado.
Verifique nos logs do primeiro boot:

```
[INF] EfiBankPixService: Webhook registrado com sucesso para chave <PIX_KEY>
```

Se quiser registrar manualmente via API:

```bash
# Substitua <PIX_KEY> pela sua chave Pix e <ACCESS_TOKEN> pelo token OAuth2
curl -X PUT https://pix.api.efipay.com.br/v2/webhook/<PIX_KEY> \
  --cert efibank-producao.p12 \
  --key efibank-producao.p12 \
  -H 'Authorization: Bearer <ACCESS_TOKEN>' \
  -H 'Content-Type: application/json' \
  -d '{"webhookUrl": "https://confirmai.suaempresa.com/api/webhooks/efibank/pix?webhookSecret=SEU_WEBHOOK_SECRET"}'
```

---

## 9. Primeiro login e configurações pós-boot

1. Acesse `https://confirmai.suaempresa.com` e faça login com as credenciais de `ADMIN_EMAIL` / `ADMIN_PASSWORD`
2. Troque a senha imediatamente via `/manage/change-password`
3. Acesse `/admin` → configure a taxa de operação e a chave PIX intermediária
4. Remova as variáveis `AdminSeed__*` do `.env` (ou defina-as como vazio) e reinicie:

```bash
docker compose up -d app
```

---

## 10. Atualizando para uma nova versão

```bash
git pull origin main
docker compose build app
docker compose up -d app
docker compose logs -f app
```

As migrations novas são aplicadas automaticamente no boot.

### Smoke pos-deploy (obrigatorio)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-postdeploy.ps1 -BaseUrl https://Confirmai.suaempresa.com
```

Se algum check falhar, nao prossiga com operacao normal: execute rollback imediato.

### Rollback rapido

```bash
# Exemplo: voltar para imagem anterior ja publicada
docker compose pull app
docker compose up -d app

# Validar saude apos rollback
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-postdeploy.ps1 -BaseUrl https://Confirmai.suaempresa.com
```

---

## 11. Backup e restore do banco

### Backup manual

```bash
docker compose exec db pg_dump -U app Confirmai | gzip > backup_$(date +%Y%m%d_%H%M%S).sql.gz
```

### Restore

```bash
gunzip -c backup_YYYYMMDD_HHMMSS.sql.gz | docker compose exec -T db psql -U app Confirmai
```

### Backup automático com cron

```bash
# /etc/cron.d/Confirmai-backup
0 3 * * * root docker compose -f /caminho/do/projeto/docker-compose.yml exec -T db pg_dump -U app Confirmai | gzip > /backups/Confirmai_$(date +\%Y\%m\%d).sql.gz && find /backups -name "Confirmai_*.sql.gz" -mtime +7 -delete
```

---

## 12. Escalar horizontalmente (opcional)

O Confirmai usa `PaymentEventBus` como event bus **in-process** (singleton Blazor Server). Para múltiplas réplicas, substitua o event bus por um broker externo (Redis Pub/Sub ou Azure Service Bus) e configure o `IDataProtection` com storage compartilhado.

Para a maioria dos casos de uso de OTServs, uma única réplica é suficiente.

---

## 13. Alertas reais de pagamentos/reconciliação

Os arquivos operacionais agora ficam em `ops/monitoring/` e podem ser usados diretamente com Docker Compose.

### Subir a stack

```bash
docker compose up -d
docker compose --profile monitoring up -d prometheus alertmanager grafana alert-webhook
```

### Validar a cadeia

```bash
docker compose --profile monitoring ps
docker compose logs prometheus --tail=100
docker compose logs alertmanager --tail=100
docker compose logs alert-webhook --tail=100
```

Smoke consolidado:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-monitoring-alerts.ps1 -RequirePaymentRules -RequireCollectorMetrics
```

Verificações esperadas:

1. `otel-collector` escutando OTLP em `4317/4318` e expondo métricas em `9464`.
2. Prometheus com target `otel-collector:9464` saudável.
3. Alertmanager carregando `ops/monitoring/alertmanager.yml` sem erro.
4. Grafana saudável em `http://localhost:3000` com datasource `Prometheus` e dashboard provisionado.
5. Webhook de validação recebendo notificações em `http://localhost:18080`.

### Antes de produção externa

1. Troque os receivers de validação em `ops/monitoring/alertmanager.yml` por Slack/PagerDuty/Teams.
2. Revise os thresholds em `ops/monitoring/prometheus-payments-alerts.yml`.
3. Mantenha o fallback LogQL ativo durante a janela inicial de rollout.

---

## Referências

- [Checklist de produção](production-checklist.md) — verificar antes de cada deploy
- [Smoke pós-deploy](../scripts/smoke-postdeploy.ps1) — automação mínima de validação operacional
- [Runbook de observabilidade](observability-payments-runbook.md) — triagem e resposta a incidentes de pagamento
- [Regras de alertas](payments-alert-rules.md) — thresholds e escalonamento para monitoramento contínuo
- [Templates de monitoramento](monitoring/README.md) — arquivos exemplo para Prometheus/Alertmanager
- [Fallback LogQL](monitoring/logql-payments-alerts.example.md) — alerta por logs enquanto contadores de domínio não estiverem instrumentados
- [Dashboard Grafana (exemplo)](monitoring/grafana-payments-dashboard.example.json) — visão operacional dos sinais de pagamento/reconciliação
- [Dockerfile](../Dockerfile) — multi-stage build SDK → runtime
- [README.md](../README.md) — visão geral do projeto
