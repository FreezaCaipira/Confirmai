using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// C36-D Fase 7: hub /grupo/{id} as a column of L1 panels. Admin sees the
/// internal navigation (Partidas, Pagamentos, Ranking, Configuracoes) plus
/// the invite action; a plain member sees navigation without Configuracoes
/// and no invite link; a non-member sees only the empty-state panel.
/// </summary>
public class GroupDetailIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public GroupDetailIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> SeedGroupAsync(string adminUserId, string memberUserId)
    {
        var groupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Hub Teste");
        await _factory.EnsureUserAsync(memberUserId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await db.Groups.FindAsync(groupId);
        group!.EnablePostMatchRanking = true;

        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = memberUserId,
            Role = GroupMemberRole.Member,
        });

        db.Events.Add(new Event
        {
            GroupId = groupId,
            Sport = Sport.Futsal,
            Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(2),
            MaxPlayers = 12,
            IsActive = true,
            CreatedByUserId = adminUserId,
        });

        await db.SaveChangesAsync();
        return groupId;
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
    public async Task GroupHub_Admin_SeesAllNavigationActions_AndInviteButton()
    {
        const string adminUserId = "hub-admin-1";
        const string memberUserId = "hub-member-1";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync($"/grupo/{groupId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Navigation actions: Partidas, Pagamentos, Ranking, Configuracoes
        Assert.Contains($"href=\"/grupo/{groupId}/partidas\"", html);
        Assert.Contains($"href=\"/grupo/{groupId}/pagamentos\"", html);
        Assert.Contains($"href=\"/grupo/{groupId}/ranking\"", html);
        Assert.Contains($"href=\"/grupo/{groupId}/configuracoes\"", html);

        // Invite stays available as an action (WhatsApp share button),
        // but the permanent invite block moved to Configuracoes.
        Assert.Contains("hub-action", html);
        Assert.Contains("fa-whatsapp", html);
        Assert.DoesNotContain("invite-code-display", html);

        // Upcoming count shown in the Partidas action sub-line
        Assert.Matches(@"1\s+(upcoming|pr(&#xF3;|.)ximos|pr(&#xF3;|.)ximas)", html);
    }

    [Fact]
    public async Task GroupHub_Member_SeesNavigation_WithoutConfigOrInvite()
    {
        const string adminUserId = "hub-admin-2";
        const string memberUserId = "hub-member-2";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", memberUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "member");

        var response = await client.GetAsync($"/grupo/{groupId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains($"href=\"/grupo/{groupId}/partidas\"", html);
        Assert.Contains($"href=\"/grupo/{groupId}/pagamentos\"", html);
        Assert.Contains($"href=\"/grupo/{groupId}/ranking\"", html);

        // No admin affordances at all
        Assert.DoesNotContain($"href=\"/grupo/{groupId}/configuracoes\"", html);
        Assert.DoesNotContain("fa-whatsapp", html);
        Assert.DoesNotContain("invite-code-display", html);
    }

    [Fact]
    public async Task GroupHub_NonMember_SeesOnlyEmptyStatePanel()
    {
        const string adminUserId = "hub-admin-3";
        const string memberUserId = "hub-member-3";
        const string outsiderId = "hub-outsider-1";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);
        await _factory.EnsureUserAsync(outsiderId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", outsiderId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "outsider");

        var response = await client.GetAsync($"/grupo/{groupId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("empty-state", html);

        // No member data, metrics, actions or invite leak to non-members
        Assert.DoesNotContain("hub-action", html);
        Assert.DoesNotContain("member-row", html);
        Assert.DoesNotContain("metric-card", html);
        Assert.DoesNotContain("invite-code-display", html);
    }
}
