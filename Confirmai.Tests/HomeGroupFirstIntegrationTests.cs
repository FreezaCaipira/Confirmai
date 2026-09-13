using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Confirmai.Tests;

/// <summary>
/// C36-A: the home is "Minhas partidas" (scoped to the user's groups). There is no
/// public showcase of matches by city/sport anymore; legacy routes redirect to "/".
/// </summary>
public class HomeGroupFirstIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public HomeGroupFirstIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonymousClient() => _factory.CreateClient();

    private HttpClient NoRedirectClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    private HttpClient AuthenticatedClient(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "testuser");
        return client;
    }

    [Theory]
    [InlineData("/futsal")]
    [InlineData("/poker")]
    [InlineData("/dashboard")]
    [InlineData("/meus-eventos")]
    [InlineData("/minhas-confirmacoes")]
    public async Task LegacyRoute_RedirectsToHome(string route)
    {
        var response = await NoRedirectClient().GetAsync(route);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/eventos")]
    [InlineData("/jogos")]
    public async Task Home_Anonymous_ShowsLandingWithoutMatchListing(string route)
    {
        await _factory.SeedFutsalEventAsync(creatorId: "home-anon-seed");

        var response = await AnonymousClient().GetAsync(route);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", content);
        Assert.DoesNotContain("Quadra de Teste", content);
        Assert.DoesNotContain("Pouso Alegre", content);
    }

    [Fact]
    public async Task Home_UserWithoutGroup_OffersJoinOrCreateOnly()
    {
        await _factory.SeedFutsalEventAsync(creatorId: "home-nogroup-seed");

        var response = await AuthenticatedClient("home-nogroup-user").GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/grupos/criar", content);
        Assert.Contains("group-entry-input", content);
        Assert.DoesNotContain("Quadra de Teste", content);
    }

    [Fact]
    public async Task Home_GroupAdmin_SeesOwnGroupAndOrganizeToggle()
    {
        var groupId = await _factory.SeedGroupWithAdminAsync("home-admin-user", "Racha do Admin");

        var response = await AuthenticatedClient("home-admin-user").GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Racha do Admin", content);
        Assert.Contains($"/grupo/{groupId}", content);
        Assert.Contains("home-view-toggle", content);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(content, "role=\"tab\"").Count);
        Assert.DoesNotContain("group-entry-input", content);
    }
}
