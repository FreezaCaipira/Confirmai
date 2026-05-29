namespace Confirmai.Configuration;

public sealed class AppmaxOptions
{
    public const string Section = "Appmax";

    /// <summary>
    /// Optional static bearer token used in API requests.
    /// If omitted, the app uses OAuth client_credentials with ClientId/ClientSecret.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Merchant client_id used to generate OAuth access tokens.</summary>
    public string? ClientId { get; set; }

    /// <summary>Merchant client_secret used to generate OAuth access tokens.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Appmax API base URL.</summary>
    public string BaseUrl { get; set; } = "https://api.appmax.com.br";

    /// <summary>Appmax auth server base URL for OAuth token generation.</summary>
    public string AuthBaseUrl { get; set; } = "https://auth.appmax.com.br";

    /// <summary>
    /// Placeholder flag for event-payment checkout support.
    /// Keep disabled until checkout redirect and webhook contract is finalized.
    /// </summary>
    public bool EnableEventCheckout { get; set; } = false;

    public bool HasStaticToken => !string.IsNullOrWhiteSpace(ApiKey);

    public bool HasClientCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsEnabled =>
        EnableEventCheckout &&
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        !string.IsNullOrWhiteSpace(AuthBaseUrl) &&
        (HasStaticToken || HasClientCredentials);
}
