using Confirmai.Config;
using Confirmai.Configuration;

namespace Confirmai.Tests;

public class ConfigurationDefaultsTests
{
    [Fact]
    public void SecurityPolicyDefaults_Create_ReturnsDevelopmentDefaults_WhenIsDevelopmentIsTrue()
    {
        var snapshot = SecurityPolicyDefaults.Create(isDevelopment: true);

        Assert.False(snapshot.RequireConfirmedEmail);
        Assert.Equal(6, snapshot.PasswordRequiredLength);
        Assert.True(snapshot.PasswordRequireDigit);
        Assert.True(snapshot.PasswordRequireLowercase);
        Assert.False(snapshot.PasswordRequireUppercase);
        Assert.False(snapshot.PasswordRequireNonAlphanumeric);
        Assert.Equal(1, snapshot.PasswordRequiredUniqueChars);
        Assert.Equal(5, snapshot.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, snapshot.LockoutMinutes);
        Assert.Equal(720, snapshot.SessionTimeoutMinutes);
    }

    [Fact]
    public void SecurityPolicyDefaults_Create_ReturnsProductionDefaults_WhenIsDevelopmentIsFalse()
    {
        var snapshot = SecurityPolicyDefaults.Create(isDevelopment: false);

        Assert.True(snapshot.RequireConfirmedEmail);
        Assert.Equal(10, snapshot.PasswordRequiredLength);
        Assert.True(snapshot.PasswordRequireDigit);
        Assert.True(snapshot.PasswordRequireLowercase);
        Assert.True(snapshot.PasswordRequireUppercase);
        Assert.True(snapshot.PasswordRequireNonAlphanumeric);
        Assert.Equal(3, snapshot.PasswordRequiredUniqueChars);
        Assert.Equal(5, snapshot.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, snapshot.LockoutMinutes);
        Assert.Equal(480, snapshot.SessionTimeoutMinutes);
    }

    [Fact]
    public void BtcPayOptions_HasExpectedDefaultValues_AndSetters()
    {
        var options = new BtcPayOptions();

        Assert.Equal(string.Empty, options.WebhookUrlLocal);
        Assert.Equal(string.Empty, options.WebhookUrlProd);

        options.WebhookUrlLocal = "http://localhost/webhook";
        options.WebhookUrlProd = "https://example.com/webhook";

        Assert.Equal("http://localhost/webhook", options.WebhookUrlLocal);
        Assert.Equal("https://example.com/webhook", options.WebhookUrlProd);
    }

    [Fact]
    public void AppmaxOptions_IsEnabled_RequiresCheckoutFlagAndAuth()
    {
        var options = new AppmaxOptions
        {
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        Assert.False(options.IsEnabled);

        options.EnableEventCheckout = true;
        Assert.False(options.IsEnabled);

        options.ApiKey = "static-token";
        Assert.True(options.IsEnabled);

        options.ApiKey = null;
        options.ClientId = "merchant-client-id";
        options.ClientSecret = "merchant-client-secret";
        Assert.True(options.IsEnabled);
    }
}


