namespace Confirmai.Services;

public static class AdminAuditSources
{
    public const string SecurityPolicy = "AdminSecurityPolicy";
    public const string Webhook = "Webhook";
    public const string ServerIntegration = "ServerIntegration";
    public const string SignalR = "SignalR";

    // ── Audit-stream sources (April 2026) ──
    public const string Identity            = "Identity";
    public const string Products            = "Products";
    public const string Servers             = "Servers";
    public const string ServerMembers       = "ServerMembers";
    public const string ServerRegistrations = "ServerRegistrations";
    public const string ItemOffers          = "ItemOffers";
    public const string Payments            = "Payments";
    public const string ApiKeys             = "ApiKeys";
    public const string AdminSettings       = "AdminSettings";
}

