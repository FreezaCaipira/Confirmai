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
      # Opcional — OpenTelemetry
      # OpenTelemetry__Endpoint: ${OTEL_ENDPOINT}
      # OpenTelemetry__Headers: ${OTEL_HEADERS}

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
```

> **Nunca versione `.env`** — adicione ao `.gitignore` e faça backup seguro (ex: gestor de segredos).

---

## 4. Configurar o nginx como reverse proxy

Instale o nginx no servidor host e configure o virtual host em `/etc/nginx/sites-available/Confirmai`:

```nginx
server {
    listen 80;
    server_name Confirmai.suaempresa.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name Confirmai.suaempresa.com;

    ssl_certificate     /etc/letsencrypt/live/Confirmai.suaempresa.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/Confirmai.suaempresa.com/privkey.pem;

    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers off;

    # Necessário para SignalR (WebSocket)
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection $connection_upgrade;

    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # Upload de evidências (máx 10 MB)
    client_max_body_size 10M;

    location / {
        proxy_pass http://127.0.0.1:8080;
    }
}

map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}
```

Ative e recarregue:

```bash
sudo ln -s /etc/nginx/sites-available/Confirmai /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Certificado TLS com Let's Encrypt

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d Confirmai.suaempresa.com
```

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
2. URL: `https://Confirmai.suaempresa.com/api/btcpay/webhook`
3. Secret: mesmo valor de `BTCPAY_WEBHOOK_SECRET` no `.env`
4. Eventos: `InvoiceSettled`, `InvoiceExpired`, `InvoiceInvalid`

---

## 8. Primeiro login e configurações pós-boot

1. Acesse `https://Confirmai.suaempresa.com` e faça login com as credenciais de `ADMIN_EMAIL` / `ADMIN_PASSWORD`
2. Troque a senha imediatamente via `/manage/change-password`
3. Acesse `/admin` → configure a taxa de operação e a chave PIX intermediária
4. Remova as variáveis `AdminSeed__*` do `.env` (ou defina-as como vazio) e reinicie:

```bash
docker compose up -d app
```

---

## 9. Atualizando para uma nova versão

```bash
git pull origin main
docker compose build app
docker compose up -d app
docker compose logs -f app
```

As migrations novas são aplicadas automaticamente no boot.

---

## 10. Backup e restore do banco

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

## 11. Escalar horizontalmente (opcional)

O Confirmai usa `PaymentEventBus` como event bus **in-process** (singleton Blazor Server). Para múltiplas réplicas, substitua o event bus por um broker externo (Redis Pub/Sub ou Azure Service Bus) e configure o `IDataProtection` com storage compartilhado.

Para a maioria dos casos de uso de OTServs, uma única réplica é suficiente.

---

## Referências

- [Checklist de produção](production-checklist.md) — verificar antes de cada deploy
- [Dockerfile](../Dockerfile) — multi-stage build SDK → runtime
- [README.md](../README.md) — visão geral do projeto
