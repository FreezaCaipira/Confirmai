using Confirmai.Configuration;

namespace Confirmai.Tests;

public class EfiBankOptionsTests
{
    [Fact]
    public void Section_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("EfiBank", EfiBankOptions.Section);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new EfiBankOptions();

        // Assert
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
        Assert.Null(options.CertificatePath);
        Assert.Null(options.CertificatePassword);
        Assert.Null(options.PixKey);
        Assert.False(options.Sandbox);
        Assert.Equal(3600, options.PixExpiresInSeconds);
        Assert.Null(options.WebhookSecret);
        Assert.Null(options.WebhookClientCertSubject);
        Assert.Null(options.WebhookUrl);
    }

    [Fact]
    public void BaseUrl_WhenSandboxTrue_ReturnsSandboxUrl()
    {
        // Arrange
        var options = new EfiBankOptions { Sandbox = true };

        // Act
        var baseUrl = options.BaseUrl;

        // Assert
        Assert.Equal("https://pix-h.api.efipay.com.br", baseUrl);
    }

    [Fact]
    public void BaseUrl_WhenSandboxFalse_ReturnsProductionUrl()
    {
        // Arrange
        var options = new EfiBankOptions { Sandbox = false };

        // Act
        var baseUrl = options.BaseUrl;

        // Assert
        Assert.Equal("https://pix.api.efipay.com.br", baseUrl);
    }

    [Fact]
    public void IsEnabled_WhenAllRequiredFieldsPresent_ReturnsTrue()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            CertificatePath = "/path/to/cert.p12",
            PixKey = "pix-key"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenClientIdMissing_ReturnsFalse()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = null,
            ClientSecret = "client-secret",
            CertificatePath = "/path/to/cert.p12",
            PixKey = "pix-key"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenClientSecretMissing_ReturnsFalse()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = "client-id",
            ClientSecret = null,
            CertificatePath = "/path/to/cert.p12",
            PixKey = "pix-key"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenCertificatePathMissing_ReturnsFalse()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            CertificatePath = null,
            PixKey = "pix-key"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenPixKeyMissing_ReturnsFalse()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            CertificatePath = "/path/to/cert.p12",
            PixKey = null
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenValuesArePlaceholders_ReturnsFalse()
    {
        // Arrange — repository placeholders must not count as configured
        var options = new EfiBankOptions
        {
            ClientId = "__SET_VIA_USER_SECRETS__",
            ClientSecret = "__SET_VIA_USER_SECRETS__",
            CertificatePath = "__SET_VIA_USER_SECRETS__",
            PixKey = "__SET_VIA_USER_SECRETS__"
        };

        // Act & Assert
        Assert.False(options.IsEnabled);
    }

    [Fact]
    public void IsEnabled_WhenCertificateBase64Provided_ReturnsTrue()
    {
        // Arrange — base64 cert is a valid alternative to a file path
        var options = new EfiBankOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            CertificateBase64 = "MIIExamplebase64",
            PixKey = "pix-key"
        };

        // Act & Assert
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var options = new EfiBankOptions
        {
            ClientId = "my-client-id",
            ClientSecret = "my-client-secret",
            CertificatePath = "/run/secrets/efibank.p12",
            CertificatePassword = "cert-password",
            PixKey = "my-pix-key",
            Sandbox = true,
            PixExpiresInSeconds = 7200,
            WebhookSecret = "webhook-secret",
            WebhookClientCertSubject = "conta.efipay.com.br",
            WebhookUrl = "https://app.confirmai.com.br/api/efibank/webhook"
        };

        // Act & Assert
        Assert.Equal("my-client-id", options.ClientId);
        Assert.Equal("my-client-secret", options.ClientSecret);
        Assert.Equal("/run/secrets/efibank.p12", options.CertificatePath);
        Assert.Equal("cert-password", options.CertificatePassword);
        Assert.Equal("my-pix-key", options.PixKey);
        Assert.True(options.Sandbox);
        Assert.Equal(7200, options.PixExpiresInSeconds);
        Assert.Equal("webhook-secret", options.WebhookSecret);
        Assert.Equal("conta.efipay.com.br", options.WebhookClientCertSubject);
        Assert.Equal("https://app.confirmai.com.br/api/efibank/webhook", options.WebhookUrl);
    }

    [Fact]
    public void PixExpiresInSeconds_CanBeSetToDifferentValues()
    {
        // Arrange
        var options = new EfiBankOptions();

        // Act
        options.PixExpiresInSeconds = 1800;

        // Assert
        Assert.Equal(1800, options.PixExpiresInSeconds);
    }

    [Fact]
    public void Sandbox_CanBeToggled()
    {
        // Arrange
        var options = new EfiBankOptions();

        // Act
        options.Sandbox = true;

        // Assert
        Assert.True(options.Sandbox);
        Assert.Equal("https://pix-h.api.efipay.com.br", options.BaseUrl);

        // Act
        options.Sandbox = false;

        // Assert
        Assert.False(options.Sandbox);
        Assert.Equal("https://pix.api.efipay.com.br", options.BaseUrl);
    }
}
