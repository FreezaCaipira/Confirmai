# Arquitetura — visão de componentes

Visão de alto nível para quem chega ao código. Fluxos de negócio detalhados estão em
[`casos-de-uso.md`](casos-de-uso.md), [`estados.md`](estados.md) e
[`sequencia-pagamento-manual.md`](sequencia-pagamento-manual.md).

## Camadas

```mermaid
flowchart TB
    subgraph Browser
        UI[Blazor Server<br/>circuito SignalR]
    end
    subgraph App[ASP.NET Core .NET 9]
        Pages[Pages/ + Shared/Components<br/>só UI, sem regra de negócio]
        Services[Services/<br/>regra de negócio + autorização]
        Hub[Hubs/PaymentHub<br/>status de pagamento em tempo real]
        Workers[Hosted services<br/>agendadores e reconciliação]
        Db[(AppDbContext<br/>EF Core)]
    end
    PG[(PostgreSQL)]
    Mail[SMTP Brevo]
    WA[Evolution API<br/>WhatsApp em grupo]
    PSP[Gateways opcionais<br/>EfiBank / AbacatePay / BTCPay]
    Obs[OpenTelemetry + Prometheus]

    UI <--> Pages
    Pages --> Services
    Services --> Db --> PG
    Services --> Hub --> UI
    Workers --> Services
    Services --> Mail
    Services --> WA
    Services --> PSP
    App --> Obs
```

Regras de organização:

- **Autorização mora no service**, não só na tela (`GroupAccess`, checagem de admin/membro).
- **Dinheiro:** taxa e ledger só consideram `PaymentStatus == Paid`.
- **Pix do grupo:** regra única em `EventPaymentService.GetGroupAdminPixKey`.
- **Textos:** todo texto de UI vem de `UiTextService` (PT/EN/ES em `Services/Core/UiText/`).
- **CSS:** tokens em `wwwroot/css/tokens.css`; padrões em [`../design-system.md`](../design-system.md).

## Workers em segundo plano

| Worker | Função |
|---|---|
| `RachaSchedulerService` | Gera as partidas das agendas semanais. |
| `EventNotificationSchedulerService` | Avisos internos de partidas. |
| `WhatsAppReminderSchedulerService` | Lembretes no grupo do WhatsApp (dry-run por padrão). |
| `EventPaymentReconciliationWorker` | Reconcilia cobranças dos gateways automáticos. |
| `PendingWebhooksAlertService` | Alerta webhooks parados. |
| `PayoutRetryService` | Retenta repasses de gateways. |
| `StaleSettlementMetricsService` | Métrica de repasse parado em análise. |
| `LogRetentionService` | Limpa logs antigos. |
| `CertificateHealthCheckService` | Valida o certificado do EfiBank. |

## Envio de aviso no WhatsApp

```mermaid
sequenceDiagram
    participant Org as Organizador
    participant S as Service da partida
    participant D as WhatsAppDispatchService
    participant DB as PostgreSQL
    participant E as Evolution API
    Org->>S: cancela / muda horário
    S->>DB: grava a alteração
    S->>D: notificar grupo
    D->>DB: registra o envio (chave única por aviso)
    alt grupo na allowlist e envio real ligado
        D->>E: POST /message/sendText/{instance}
        E-->>D: ok / erro (timeout 10s)
    else dry-run
        D-->>D: só loga
    end
    D-->>S: resultado (não bloqueia a operação)
```
