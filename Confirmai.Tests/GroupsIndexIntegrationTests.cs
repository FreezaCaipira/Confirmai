using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// C36-D Fase 4: /grupos is participation management, not an event listing.
/// Groups render as management rows with an "Abrir" action; pending join
/// requests the user sent appear with "Aguardando aprovacao"; the green
/// "Ver Partidas" affordance is gone.
/// </summary>
public class GroupsIndexIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public GroupsIndexIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private static HttpClient ClientFor(IntegrationTestWebAppFactory factory, string userId, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", name);
        return client;
    }

    private async Task AddMemberAsync(int groupId, string userId, GroupMemberRole role)
    {
        await _factory.EnsureUserAsync(userId);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = userId, Role = role });
        await db.SaveChangesAsync();
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    [Fact]
    public async Task GroupsIndex_UserWithTwoGroups_SeesTwoRows_AndCreateIsPrimary()
    {
        const string userId = "gidx-member-1";
        var g1 = await _factory.SeedGroupWithAdminAsync("gidx-admin-1", "Racha Um");
        var g2 = await _factory.SeedGroupWithAdminAsync("gidx-admin-2", "Racha Dois");
        await AddMemberAsync(g1, userId, GroupMemberRole.Member);
        await AddMemberAsync(g2, userId, GroupMemberRole.Member);

        var client = ClientFor(_factory, userId, "member");
        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Two management rows, each with an "Abrir" action
        Assert.Equal(2, CountOccurrences(html, "class=\"group-row\""));
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "btn btn-secondary btn-sm\"[^>]*>Abrir").Count);
        Assert.Contains("Racha Um", html);
        Assert.Contains("Racha Dois", html);

        // "Criar novo grupo" is the primary action of the entry panel
        Assert.Matches("class=\"btn btn-primary\"[^>]*>\\s*Criar novo grupo", html);

        // Event-listing leftovers are gone
        Assert.DoesNotContain("Ver Partidas", html);
        Assert.DoesNotContain("group-card", html);
    }

    [Fact]
    public async Task GroupsIndex_PendingJoinRequest_RendersAwaitingApprovalRow()
    {
        const string userId = "gidx-requester-1";
        var groupId = await _factory.SeedGroupWithAdminAsync("gidx-admin-3", "Racha Pedido");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await _factory.EnsureUserAsync(userId);
            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = userId,
                Status = JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = ClientFor(_factory, userId, "requester");
        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Racha Pedido", html);
        Assert.Contains("Aguardando aprova", html);
        Assert.Contains("Cancelar pedido", html);
    }

    [Fact]
    public async Task GroupsIndex_AdminSeesAdminBadge_AndPendingJoinCount()
    {
        const string adminId = "gidx-admin-4";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId, "Racha Admin");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await _factory.EnsureUserAsync("gidx-joiner-1");
            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = "gidx-joiner-1",
                Status = JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = ClientFor(_factory, adminId, "admin");
        var html = await (await client.GetAsync("/grupos")).Content.ReadAsStringAsync();

        Assert.Matches("status-badge--accent\"[^>]*>admin", html);
        Assert.Contains("pendente", html);
        Assert.DoesNotContain("Ver Partidas", html);
    }
}
