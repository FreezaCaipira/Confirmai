using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C31 Fase 4 — boot guard: real WhatsApp sends (Enabled + !DryRun) must fail
/// the boot on incomplete Evolution config, and outside dev/test on an empty
/// allowlist.
/// </summary>
public class C31WhatsAppBootGuardTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C31WhatsAppBootGuardTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    [Fact]
    public void RealSend_WithoutEvolutionConfig_FailsBoot()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["WhatsApp:Enabled"] = "true",
                    ["WhatsApp:DryRun"] = "false",
                    ["WhatsApp:AllowedGroupJids"] = "111@g.us"
                    // BaseUrl / Instance / ApiKey deliberately missing
                })));

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("WhatsApp__BaseUrl", ex.ToString());
    }

    [Fact]
    public void RealSend_EmptyAllowlist_FailsBoot_OutsideDevelopment()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["WhatsApp:Enabled"] = "true",
                    ["WhatsApp:DryRun"] = "false",
                    ["WhatsApp:BaseUrl"] = "http://evolution:8080",
                    ["WhatsApp:Instance"] = "Confirmai-Bot",
                    ["WhatsApp:ApiKey"] = "some-key"
                    // AllowedGroupJids deliberately absent
                }));
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("AllowedGroupJids", ex.ToString());
    }

    [Fact]
    public void RealSend_EmptyAllowlist_InTesting_StillBoots()
    {
        // Same setup but Testing env: the allowlist guard must not fire in
        // dev/test — only the config-completeness one (which is satisfied).
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["WhatsApp:Enabled"] = "true",
                    ["WhatsApp:DryRun"] = "false",
                    ["WhatsApp:BaseUrl"] = "http://evolution:8080",
                    ["WhatsApp:Instance"] = "Confirmai-Bot",
                    ["WhatsApp:ApiKey"] = "some-key"
                })));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        Assert.NotNull(client);
    }

    [Fact]
    public async Task DefaultConfig_BootsNormally()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/about");
        Assert.True(response.IsSuccessStatusCode);
    }
}
