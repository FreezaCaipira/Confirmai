using System.Net;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for the /futsal/{id}/escalacao page access control and rendering.
/// </summary>
public class FutsalEscalacaoIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public FutsalEscalacaoIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─── Route not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task EscalacaoPage_Returns200_AndShowsNotFound_WhenEventDoesNotExist()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/futsal/99999/escalacao");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("encontrada", html);
    }

    // ─── Anonymous user, event with no confirmed lineup ───────────────────────

    [Fact]
    public async Task EscalacaoPage_ShowsUnavailable_WhenLineupNotConfirmed_AndUserIsAnonymous()
    {
        var eventId = await _factory.SeedFutsalEventAsync(lineupConfirmed: false);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/futsal/{eventId}/escalacao");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("montada", html);
    }

    // ─── Anonymous user, event with confirmed lineup ──────────────────────────

    [Fact]
    public async Task EscalacaoPage_ShowsConfirmedLineup_WhenLineupIsConfirmed_AndUserIsAnonymous()
    {
        var eventId = await _factory.SeedFutsalEventAsync(lineupConfirmed: true);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/futsal/{eventId}/escalacao");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Confirmed state renders the confirmation badge
        Assert.Contains("confirmada", html);
        // No draft/admin controls visible to anonymous users
        Assert.DoesNotContain("esc-btn--danger", html, StringComparison.Ordinal);
    }

    // ─── Page title ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EscalacaoPage_Returns200_ForValidEvent()
    {
        var eventId = await _factory.SeedFutsalEventAsync(lineupConfirmed: false);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/futsal/{eventId}/escalacao");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Page renders some content (not empty / redirect)
        Assert.Contains("esc-", html, StringComparison.Ordinal);
    }
}
