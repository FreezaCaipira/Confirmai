using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// C36-D Fase 8: shared GroupHeader + GroupSubNav across the 5 group screens.
/// The active tab follows the current route; "Configuracoes" only renders for
/// admins; child screens no longer render their own "Voltar" link.
/// </summary>
public class GroupSubNavTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public GroupSubNavTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> SeedGroupAsync(string adminUserId, string memberUserId)
    {
        var groupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Racha Subnav");
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

        await db.SaveChangesAsync();
        return groupId;
    }

    private static HttpClient ClientFor(IntegrationTestWebAppFactory factory, string userId, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", name);
        return client;
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("partidas", "partidas")]
    [InlineData("pagamentos", "pagamentos")]
    [InlineData("ranking", "ranking")]
    [InlineData("configuracoes", "configuracoes")]
    public async Task GroupRoutes_RenderSharedHeader_WithActiveTab(string route, string activeSegment)
    {
        const string adminUserId = "subnav-admin-1";
        const string memberUserId = "subnav-member-1";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var client = ClientFor(_factory, adminUserId, "admin");
        var suffix = string.IsNullOrEmpty(route) ? string.Empty : $"/{route}";

        var response = await client.GetAsync($"/grupo/{groupId}{suffix}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Shared header with the group name on every group screen.
        Assert.Contains("group-header", html);
        Assert.Contains("Racha Subnav", html);
        Assert.Contains("group-subnav", html);

        // Exactly one active tab, pointing at the current route.
        var activeHref = string.IsNullOrEmpty(activeSegment)
            ? $"href=\"/grupo/{groupId}\""
            : $"href=\"/grupo/{groupId}/{activeSegment}\"";
        Assert.Contains($"seg-item seg-item--active\" {activeHref}", html);
    }

    [Fact]
    public async Task GroupSubNav_ConfigTab_OnlyRendersForAdmin()
    {
        const string adminUserId = "subnav-admin-2";
        const string memberUserId = "subnav-member-2";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var adminClient = ClientFor(_factory, adminUserId, "admin");
        var adminHtml = await (await adminClient.GetAsync($"/grupo/{groupId}/partidas"))
            .Content.ReadAsStringAsync();
        Assert.Contains($"href=\"/grupo/{groupId}/configuracoes\"", adminHtml);

        var memberClient = ClientFor(_factory, memberUserId, "member");
        var memberHtml = await (await memberClient.GetAsync($"/grupo/{groupId}/partidas"))
            .Content.ReadAsStringAsync();
        Assert.DoesNotContain($"href=\"/grupo/{groupId}/configuracoes\"", memberHtml);
    }

    [Fact]
    public async Task Partidas_NoLongerRendersBackLink()
    {
        const string adminUserId = "subnav-admin-3";
        const string memberUserId = "subnav-member-3";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var client = ClientFor(_factory, memberUserId, "member");
        var response = await client.GetAsync($"/grupo/{groupId}/partidas");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Voltar", html);
    }

    [Fact]
    public async Task GroupRoutes_RenderBreadcrumb_WithGroupName()
    {
        const string adminUserId = "subnav-admin-4";
        const string memberUserId = "subnav-member-4";
        var groupId = await SeedGroupAsync(adminUserId, memberUserId);

        var client = ClientFor(_factory, memberUserId, "member");
        var html = await (await client.GetAsync($"/grupo/{groupId}/partidas"))
            .Content.ReadAsStringAsync();

        // Grupos > Racha Subnav > Partidas
        Assert.Contains("breadcrumb-nav", html);
        Assert.Contains("Racha Subnav", html);
    }
}
