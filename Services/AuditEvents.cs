namespace Confirmai.Services;

/// <summary>
/// Stable, machine-readable audit event names. Stored in
/// <c>AppLog.EventType</c>. Use dot-separated <c>entity.action</c> pattern.
/// Renaming a constant is a BREAKING change for log analytics — only add new
/// values, never repurpose existing ones.
/// </summary>
public static class AuditEvents
{
    // ── User / Identity ──
    public const string UserRegistered      = "user.registered";
    public const string UserLoginSuccess    = "user.login.success";
    public const string UserLoginFailed     = "user.login.failed";
    public const string UserLockedOut       = "user.locked";
    public const string UserPasswordChanged = "user.password.changed";
    public const string UserPasswordReset   = "user.password.reset";
    public const string UserRoleAssigned    = "user.role.assigned";
    public const string UserRoleRemoved     = "user.role.removed";
    public const string UserDeleted         = "user.deleted";

    // ── Product (catalog item) ──
    public const string ProductCreated  = "product.created";
    public const string ProductUpdated  = "product.updated";
    public const string ProductDeleted  = "product.deleted";
    public const string ProductArchived = "product.archived";

    // ── Item Offer (in-game item listing) ──
    public const string ItemOfferCreated   = "itemoffer.created";
    public const string ItemOfferCancelled = "itemoffer.cancelled";
    public const string ItemOfferSold      = "itemoffer.sold";

    // ── Server ──
    public const string ServerRequested      = "server.requested";
    public const string ServerRequestApproved = "server.request.approved";
    public const string ServerRequestRejected = "server.request.rejected";
    public const string ServerCreated        = "server.created";
    public const string ServerUpdated        = "server.updated";
    public const string ServerDeleted        = "server.deleted";
    public const string ServerMemberAdded    = "server.member.added";
    public const string ServerMemberRemoved  = "server.member.removed";
    public const string ServerMemberUpdated  = "server.member.updated";
    public const string ServerApiKeyIssued   = "server.apikey.issued";
    public const string ServerApiKeyRevoked  = "server.apikey.revoked";

    // ── Payment ──
    public const string PaymentInvoiceCreated = "payment.invoice.created";
    public const string PaymentReceived       = "payment.received";       // webhook arrived
    public const string PaymentConfirmed      = "payment.confirmed";      // settled / IsPaid=true
    public const string PaymentFailed         = "payment.failed";
    public const string PaymentRefunded       = "payment.refunded";
    public const string PaymentStatusChanged  = "payment.status.changed";
    public const string PaymentReconciliationSweep = "payment.reconciliation.sweep";
    public const string PaymentReconciliationPanelStale = "payment.reconciliation.panel.stale";
    public const string PaymentReplayed       = "payment.replayed";       // duplicate delivery rejected
    public const string PaymentInvalid        = "payment.invalid";        // bad payload / signature
    public const string PaymentRefused        = "payment.refused";        // missing/expired/etc.

    // ── Admin / Security ──
    public const string AdminSecurityPolicyChanged = "admin.security_policy.changed";
    public const string AdminSettingChanged        = "admin.setting.changed";
    public const string AdminAuditViewed           = "admin.audit.viewed";

    // ── Webhook / integration ──
    public const string WebhookReceived     = "webhook.received";
    public const string WebhookUnauthorized = "webhook.unauthorized";
    public const string WebhookOversize     = "webhook.oversize";
}

/// <summary>
/// Logical entity types referenced from audit events. Stored in
/// <c>AppLog.EntityType</c>.
/// </summary>
public static class AuditEntities
{
    public const string User      = "User";
    public const string Product   = "Product";
    public const string Payment   = "Payment";
    public const string Server    = "Server";
    public const string ItemOffer = "ItemOffer";
    public const string Webhook   = "Webhook";
    public const string ApiKey    = "ApiKey";
    public const string Setting   = "Setting";
}
