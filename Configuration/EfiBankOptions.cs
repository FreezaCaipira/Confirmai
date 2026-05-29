namespace Confirmai.Configuration;

public class EfiBankOptions
{
    public const string Section = "EfiBank";

    /// <summary>OAuth2 Client ID from Efí Bank API dashboard.</summary>
    public string? ClientId { get; set; }

    /// <summary>OAuth2 Client Secret from Efí Bank API dashboard.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Absolute path to the .p12 certificate file downloaded from Efí Bank dashboard.
    /// Example: /run/secrets/efibank.p12
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>Password for the .p12 certificate (leave empty if none).</summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Your Pix key registered at Efí Bank (CPF, CNPJ, e-mail, telefone or EVP/random key).
    /// This is the recipient key for all event charges.
    /// </summary>
    public string? PixKey { get; set; }

    /// <summary>Use sandbox (homologação) environment. Default: false.</summary>
    public bool Sandbox { get; set; } = false;

    /// <summary>Pix charge expiry in seconds. Default: 3600 (1 hour).</summary>
    public int PixExpiresInSeconds { get; set; } = 3600;

    /// <summary>
    /// Optional shared secret appended as ?webhookSecret= to the webhook URL so we can
    /// do a quick first-layer check before processing the payload.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Expected substring of the TLS client-certificate Subject that EfiBank presents when
    /// posting webhook notifications (mTLS server-side validation).
    /// Example: "conta.efipay.com.br"
    /// Leave empty to skip certificate validation (rely on WebhookSecret only).
    /// </summary>
    public string? WebhookClientCertSubject { get; set; }

    /// <summary>
    /// Public HTTPS URL that EfiBank will POST Pix notifications to.
    /// Example: https://app.confirmai.com.br/api/efibank/webhook
    /// Leave empty to skip automatic webhook registration at startup.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>API base URL, selected by <see cref="Sandbox"/> flag.</summary>
    public string BaseUrl => Sandbox
        ? "https://pix-h.api.efipay.com.br"
        : "https://pix.api.efipay.com.br";

    /// <summary>True when all required fields are configured.</summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(CertificatePath) &&
        !string.IsNullOrWhiteSpace(PixKey);
}
