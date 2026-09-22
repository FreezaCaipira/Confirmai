using System.Net;

namespace Confirmai.Tests;

/// <summary>
/// C36-D Fase 6: /profile migrada para paineis L1 — sem mk-btn/mk-input,
/// com seg-toggle nos esportes e empty-state quando a chave Pix nao
/// esta configurada.
/// </summary>
public class ProfilePageIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ProfilePageIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_OwnPage_RendersPanels_WithoutMkButtons()
    {
        const string userId = "profile-page-user-1";
        await _factory.EnsureUserAsync(userId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "profileuser");

        var response = await client.GetAsync($"/profile/{userId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Legacy marketplace controls are gone from the migrated page
        Assert.DoesNotContain("mk-btn", html);
        Assert.DoesNotContain("mk-input", html);
        // New system: panels, seg-toggle for sports, shared buttons
        Assert.Contains("panel", html);
        Assert.Contains("seg-toggle", html);
        Assert.Contains("btn-primary", html);
    }

    [Fact]
    public async Task Profile_OwnPage_WithoutPixKey_ShowsEmptyStateCta()
    {
        const string userId = "profile-page-user-2";
        await _factory.EnsureUserAsync(userId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "profileuser2");

        var response = await client.GetAsync($"/profile/{userId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Unconfigured Pix is an empty-state with a primary CTA, not gray text
        Assert.Contains("profile-pix-empty", html);
        Assert.Contains("Cadastrar chave Pix", html);
    }
}
