# Sequencia — pagamento manual da partida (Ciclo 33)

O caminho do dinheiro no V1 (gateways desligados para o grupo):
jogador confirma → ve Pix do organizador → paga no banco → envia comprovante →
organizador confirma → taxa e carimbada no ledger → organizador repassa → admin confirma.

```mermaid
sequenceDiagram
    autonumber
    actor J as Jogador
    participant EP as /pagamento/evento/{id}<br>EventPayment.razor
    participant EPS as EventPaymentService
    participant PUS as PixProofUploadService
    actor O as Organizador
    participant GP as /grupo/{id}/pagamentos<br>Payments.razor
    participant GPS as GroupPaymentsService
    participant LED as PlatformFeeLedgerService
    participant PFS as PlatformFeeSettlementService
    actor A as Admin plataforma
    participant DB as PostgreSQL

    J->>EP: confirmou presenca (EventConfirmation Pending)
    EP->>EPS: GetGroupAdminPixKey / BuildPixStaticPayload
    EPS-->>EP: chave Pix do admin + QR estatico (brcode)
    EP-->>J: exibe QR + copia-e-cola
    Note over J: paga no app do banco (fora do sistema)

    J->>EP: upload do comprovante (<=5MB, jpg/png/webp)
    EP->>PUS: UploadProofAsync(confirmationId, bytes, mime)
    PUS->>DB: PixProofImageData/ContentType/UploadedAt
    Note over PUS: valida mime real (magic bytes), tamanho,<br>recusa se ja pago. SEM AuditAsync (gap C34)

    O->>GP: abre painel de pagamentos
    GP->>GPS: LoadPaymentsDataAsync (lista PendingProofEntry)
    GPS-->>GP: comprovante pendente do jogador
    O->>GP: ve imagem via GET /api/pix-proof/{id}
    Note over GP: autoriza: pagador OU admin do grupo

    alt Aceita o comprovante
        O->>GPS: MarkPaidAsync(confirmationId)
        GPS->>DB: PaymentStatus=Paid, HasPaid,<br>MarkedPaidByUserId/At
        GPS->>LED: StampFeeOnPaidAsync(confirmationId)
        LED->>DB: PlatformFeeAmount = snapshot da taxa
        GPS->>DB: AuditAsync(EventConfirmationPaidManual)
    else Rejeita
        O->>GPS: RejectProofAsync(confirmationId)
        GPS->>DB: limpa PixProof* (segue Pending)
        GPS->>DB: AuditAsync(EventConfirmationProofRejected)
        Note over J: pode re-enviar novo comprovante
    end

    Note over O,A: Nivel 2 — repasse da taxa acumulada
    O->>GP: aba taxa → GetGroupFeeOverviewAsync
    GP-->>O: partidas com taxa devida + Pix da plataforma
    Note over O: paga ao Pix da plataforma no banco
    O->>PFS: SubmitSettlementAsync(partidas, valor, comprovante)
    PFS->>DB: PlatformFeeSettlement EmAnalise + Items
    Note over PFS: re-verifica admin no service; valor deve<br>ser a taxa residual. SEM AuditAsync (gap C34)

    A->>A: /admin/revenue → fila EmAnalise
    A->>A: ve comprovante via GET /api/fee-settlement-proof/{id}
    alt Confirma
        A->>PFS: ReviewSettlementAsync(approve)
        PFS->>DB: Status=Pago, ReviewedByUserId/At
        Note over PFS: Items quitam as partidas. SEM AuditAsync (gap C34)
    else Rejeita
        A->>PFS: ReviewSettlementAsync(reject, motivo)
        PFS->>DB: Status=Rejeitado + ReviewNote
        Note over O: saldo volta a dever; re-envia depois
    end
```

## Variantes do nivel 1 (jogador → organizador)

- **Gateway ligado** (`Group.EnablePaymentGateways`, V2/futuro): `EPS.GeneratePixChargeAsync`
  cria cobranca real (EfiBank/AbacatePay/Appmax); webhook POST confirma via
  `WebhookPaymentMarker` → `Paid` + audit `PaymentConfirmed`. O jogador nao envia
  comprovante — a confirmacao e automatica.
- **Reconciliacao** (background + manual em `/admin/payments`): poll de pendentes
  com `PixTxId` → confirma ou marca `Expired` apos a janela.
- **Toggle admin** (`AdminConfirmationService.TogglePaidAsync`): baixa manual
  alternativa em `/futsal/{id}` e `/pagamento/evento/{id}` (admin view); bloqueia
  desmarcar o que foi pago via gateway.

## Onde a autorizacao e verificada (defesa em profundidade)

| Ponto | Onde |
|---|---|
| Upload de proof e do proprio jogador | `PixProofUploadService` (confirmationId → UserId) |
| Ver proof do jogador | endpoint `/api/pix-proof/{id}` inline em `Program.cs` (pagador OU admin do grupo) |
| Painel de pagamentos | `GroupPaymentsService.LoadGroupAndCheckAdminAsync` (no service) |
| Marcar pago / rejeitar proof | page-gated (`isAdmin`); service nao revalida papel |
| Submit de repasse | `PlatformFeeSettlementService` re-verifica `Role == Admin` |
| Ver proof de repasse | `PlatformFeeSettlementProofAuthorizer` (submitter OU admin grupo OU sysadmin) |
| Review de repasse | `ReviewSettlementAsync` (sysadmin; organizador nao revisa o proprio) |
