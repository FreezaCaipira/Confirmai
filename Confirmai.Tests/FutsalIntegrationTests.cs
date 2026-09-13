using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for the Futsal pages: listing, detail, create, edit.
/// Uses <see cref="IntegrationTestWebAppFactory"/> which runs the full app
/// with an in-memory database and a stub authentication handler.
/// </summary>
public class FutsalIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public FutsalIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────
    // Client helpers
    // ──────────────────────────────────────────────

    private HttpClient AnonymousClient() => _factory.CreateClient();

    private HttpClient AuthenticatedClient(string userId, string userName = "testuser")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", userName);
        return client;
    }

    private static async Task<string> ReadContentAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // ──────────────────────────────────────────────
    // Detail page – /futsal/{id}
    // ──────────────────────────────────────────────

    [Fact]
    public async Task FutsalDetail_Returns200_ForExistingEvent()
    {
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "detail-creator");
        var client = AnonymousClient();

        var response = await client.GetAsync($"/futsal/{eventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FutsalDetail_ShowsGroupName_ForExistingEvent()
    {
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "detail-creator-name");
        var client = AnonymousClient();

        var response = await client.GetAsync($"/futsal/{eventId}");
        var content = await ReadContentAsync(response);

        Assert.Contains("Racha de Teste", content);
    }

    [Fact]
    public async Task FutsalDetail_ShowsNotFound_ForMissingEvent()
    {
        var client = AnonymousClient();

        var response = await client.GetAsync("/futsal/999999999");
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("encontrada", content);
    }

    [Fact]
    public async Task FutsalDetail_ShowsFutsalSportBadge_ForExistingEvent()
    {
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "detail-creator-badge");
        var client = AnonymousClient();

        var response = await client.GetAsync($"/futsal/{eventId}");
        var content = await ReadContentAsync(response);

        Assert.Contains("Futsal", content);
    }

    [Fact]
    public async Task FutsalDetail_PrivateGroup_ShowsJoinRequestButton_ForAuthenticatedNonMember()
    {
        var eventId = await SeedPrivateFutsalEventAsync(creatorId: "private-futsal-creator");
        var client = AuthenticatedClient("private-futsal-viewer");

        var response = await client.GetAsync($"/futsal/{eventId}");
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("privado", content);
        Assert.Contains("Solicitar entrada", content);
    }

    // ──────────────────────────────────────────────
    // Create page – /futsal/create
    // ──────────────────────────────────────────────

    [Fact]
    public async Task FutsalCreate_RedirectsToLogin_ForAnonymousUser()
    {
        // Create a client that does NOT follow redirects so we can observe the initial challenge.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/futsal/create");

        // The auth challenge issues a redirect to the login page.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("Login", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FutsalCreate_Returns200_AndShowsForm_ForAuthenticatedUser()
    {
        const string userId = "create-test-user";
        var groupId = await _factory.SeedGroupWithAdminAsync(userId);

        var client = AuthenticatedClient(userId);

        var response = await client.GetAsync($"/futsal/create?groupId={groupId}");
        var content  = await ReadContentAsync(response);

        Assert.Contains("Nova Partida", content);
        Assert.Contains("Identidade da Partida", content);
    }

    // ──────────────────────────────────────────────
    // Edit page – /futsal/{id}/edit
    // ──────────────────────────────────────────────

    [Fact]
    public async Task FutsalEdit_RedirectsToLogin_ForAnonymousUser()
    {
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "edit-anon-creator");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/futsal/{eventId}/edit");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("Login", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FutsalEdit_ShowsAccessDenied_WhenAuthenticatedUserIsNotCreator()
    {
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "edit-real-creator");

        // A different user tries to edit
        var client = AuthenticatedClient("edit-other-user");

        var response = await client.GetAsync($"/futsal/{eventId}/edit");
        var content = await ReadContentAsync(response);

        Assert.Contains("negado", content);
    }

    [Fact]
    public async Task FutsalEdit_Returns200AndShowsForm_WhenCreatorAccesses()
    {
        const string creatorId = "edit-form-creator";
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: creatorId);

        var client = AuthenticatedClient(creatorId);

        var response = await client.GetAsync($"/futsal/{eventId}/edit");
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Identidade da Partida", content);
    }

    [Fact]
    public async Task FutsalEdit_ShowsNotFound_ForNonExistentEvent_WhenAuthenticated()
    {
        var client = AuthenticatedClient("edit-notfound-user");

        var response = await client.GetAsync("/futsal/999999999/edit");
        var content = await ReadContentAsync(response);

        Assert.Contains("encontrada", content);
    }

    private async Task<int> SeedPrivateFutsalEventAsync(string creatorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = new Group
        {
            Name = "Racha Privado de Teste",
            Sport = Sport.Futsal,
            City = "Pouso Alegre",
            StateCode = "MG",
            CreatedByUserId = creatorId,
            IsPrivate = true,
            InviteCode = "PRIVATE01"
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = creatorId,
            Role = GroupMemberRole.Admin,
            CreatedAt = DateTime.UtcNow,
        });

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra Privada",
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 12,
            MaxGoalkeepers = 2,
            IsActive = true,
            CreatedByUserId = creatorId,
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return ev.Id;
    }
}
