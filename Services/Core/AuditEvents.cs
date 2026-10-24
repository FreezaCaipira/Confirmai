using Confirmai.Services.Admin;
using Confirmai.Services.Core;
namespace Confirmai.Services.Core;

/// <summary>
/// Stable, machine-readable audit event names. Stored in
/// <c>AppLog.EventType</c>. Use dot-separated <c>entity.action</c> pattern.
/// Renaming a constant is a BREAKING change for log analytics — only add new
/// values, never repurpose existing ones.
///
/// C42-B: guardian test fails if a constant here is not referenced by
/// production code — dead constants are removed, not kept around.
/// </summary>
public static class AuditEvents
{
    // -- User / Identity --
    public const string UserRegistered           = "user.registered";
    public const string UserLoginSuccess         = "user.login.success";
    public const string UserLoginFailed          = "user.login.failed";
    public const string UserLinkedExternalLogin  = "user.external_login.linked";
    public const string UserLockedOut            = "user.locked";
    public const string UserUnlocked             = "user.unlocked";
    public const string UserPasswordChanged = "user.password.changed";
    public const string UserPasswordReset   = "user.password.reset";
    public const string UserRoleAssigned    = "user.role.assigned";
    public const string UserRoleRemoved     = "user.role.removed";
    public const string UserDeleted         = "user.deleted";

    // -- Product (catalog item) --
    public const string ProductCreated  = "product.created";
    public const string ProductUpdated  = "product.updated";
    public const string ProductDeleted  = "product.deleted";
    public const string ProductArchived = "product.archived";

    // -- Payment --
    public const string PaymentConfirmed      = "payment.confirmed";      // settled / IsPaid=true
    public const string PaymentFailed         = "payment.failed";
    public const string PaymentRefunded       = "payment.refunded";
    public const string PaymentStatusChanged  = "payment.status.changed";
    public const string PaymentReconciliationSweep = "payment.reconciliation.sweep";
    public const string PaymentReconciliationPanelStale = "payment.reconciliation.panel.stale";

    // -- Admin / Security --
    public const string AdminSecurityPolicyChanged = "admin.security_policy.changed";
    public const string AdminSettingChanged        = "admin.setting.changed";
    public const string AdminAuditViewed           = "admin.audit.viewed";

    // -- Platform fee settlement (repasse do organizador -> plataforma) --
    public const string SettlementSubmitted = "settlement.submitted";
    public const string SettlementConfirmed = "settlement.confirmed";
    public const string SettlementRejected  = "settlement.rejected";

    // -- Comprovante Pix do jogador --
    public const string ProofUploaded = "proof.uploaded";
    public const string ProofReplaced = "proof.replaced";

    // -- User --
    public const string UserProfileUpdated = "user.profile.updated"; // dados de recebimento (PixKey)

    // -- Event (partida/sessão) --
    public const string EventCreated = "event.created";
    public const string EventUpdated = "event.updated";
    public const string EventCancelled = "event.cancelled";
    public const string EventCapacityChanged = "event.capacity.changed";   // vagas / goleiros (C42-B)
    public const string EventScoreRegistered = "event.score.registered";   // placar (C42-B)
    public const string EventPostMatchVoteCast = "event.vote.cast";        // voto destaque/MVP (C42-B)

    // -- EventConfirmation (presença / pagamento) --
    public const string EventConfirmationAdded         = "event.confirmation.added";
    public const string EventConfirmationRemoved       = "event.confirmation.removed";
    public const string EventConfirmationPaidManual    = "event.confirmation.paid.manual";    // admin marcou pago manualmente
    public const string EventConfirmationUnpaidManual  = "event.confirmation.unpaid.manual";  // admin desconfirmou pagamento manual
    public const string EventConfirmationProofRejected = "event.confirmation.proof.rejected"; // admin rejeitou comprovante pix
    public const string EventConfirmationTableChanged  = "event.confirmation.table.changed";  // troca de mesa no cash (C42-B)
    public const string EventConfirmationRestamped     = "event.confirmation.restamped";      // recarimbo de devedores (C42-B)
    public const string EventWaitlistJoined            = "event.waitlist.joined";             // entrou na espera (C42-B)
    public const string EventWaitlistPromoted          = "event.waitlist.promoted";           // promovido da espera (C42-B)
    public const string EventWaitlistRemoved           = "event.waitlist.removed";
    public const string EventMockFilled              = "event.mock.filled";      // C43-B
    public const string EventMockCleared             = "event.mock.cleared";     // C43-B

    // -- Group --
    public const string GroupCreated       = "group.created";
    public const string GroupUpdated       = "group.updated";
    public const string GroupMemberAdded   = "group.member.added";
    public const string GroupMemberRemoved = "group.member.removed";
    public const string GroupMemberRoleChanged = "group.member.role.changed";
    public const string GroupMemberRoleChangeDenied = "group.member.role.change.denied";
    public const string GroupFeatureToggled = "group.feature.toggled";
    public const string GroupPixReceiverChanged = "group.pix.receiver.changed";
    public const string GroupWhatsAppJidChanged = "group.whatsapp.jid.changed";
    public const string GroupPayoutAccountCreated = "group.payout.account.created";
    public const string GroupPayoutAccountUpdated = "group.payout.account.updated";
    public const string GroupFeeWaiverChanged    = "group.fee.waiver.changed";
    public const string GroupPartnerFeeShareChanged = "group.partner.fee.share.changed";
    public const string GroupJoinRequested       = "group.join.requested";
    public const string GroupJoinApproved        = "group.join.approved";
    public const string GroupJoinRejected        = "group.join.rejected";

    // -- Product metrics (C42-B: alimentam /admin/metrics; nunca PII desnecessaria) --
    public const string GroupInviteOpened  = "group.invite.opened";  // pagina /convite/{code} aberta
    public const string GroupMemberJoined  = "group.member.joined";  // membro entrou — metadata via: code|approval|admin
    public const string PageView           = "page.view";            // Message = template de rota; metadata: device

    // -- Delinquência --
    public const string DelinquencyNotified = "delinquency.notified";
}

/// <summary>
/// Logical entity types referenced from audit events. Stored in
/// <c>AppLog.EntityType</c>.
/// </summary>
public static class AuditEntities
{
    public const string User              = "User";
    public const string Product           = "Product";
    public const string Payment           = "Payment";
    public const string Setting           = "Setting";
    public const string Event             = "Event";
    public const string Group             = "Group";
    public const string EventConfirmation = "EventConfirmation";
    public const string PlatformFeeSettlement = "PlatformFeeSettlement";
    public const string GroupJoinRequest  = "GroupJoinRequest";
    public const string GroupMember       = "GroupMember";
    public const string PageView          = "PageView";
}
