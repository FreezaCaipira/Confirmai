namespace Confirmai.Configuration;

public class AbacatePayOptions
{
    public const string Section = "AbacatePay";

    // AbacatePay's shared public HMAC key used to sign webhook payloads.
    // Source: https://docs.abacatepay.com/pages/webhooks
    public const string PublicHmacKey =
        "t9dXRhHHo3yDEj5pVDYz0frf7q6bMKyMRmxxCPIPp3RCplBfXRxqlC6ZpiWmOqj4" +
        "L63qEaeUOtrCI8P0VMUgo6iIga2ri9ogaHFs0WIIywSMg0q7RmBfybe1E5XJcfC4" +
        "IW3alNqym0tXoAKkzvfEjZxV6bE0oG2zJrNNYmUCKZyV0KZ3JS8Votf9EAWWYdi" +
        "DkMkpbMdPggfh1EqHlVkMiTady6jOR3hyzGEHrIz2Ret0xHKMbiqkr9HS1JhNHDX9";

    /// <summary>Bearer API key for AbacatePay REST API.</summary>
    public string? ApiKey { get; set; }

    /// <summary>REST API base URL. Defaults to production.</summary>
    public string BaseUrl { get; set; } = "https://api.abacatepay.com/v2";

    /// <summary>
    /// Your user-defined secret sent as <c>?webhookSecret=</c> in the webhook URL.
    /// AbacatePay passes it back on every call; we verify it matches.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>PIX charge expiry in seconds. Defaults to 1 hour.</summary>
    public int PixExpiresInSeconds { get; set; } = 3600;

    /// <summary>Whether AbacatePay is active (ApiKey configured).</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);
}
