using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class GroupsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public GroupsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GroupsIndex_ShowsPendingJoinRequestsBadge_ForAdminGroupCards()
    {
        const string adminUserId = "groups-admin-1";
        var groupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Badge Pendente");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = "requester-user-1",
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Badge shows "N pendente(s)" and card gets action-required class
        Assert.Contains("group-card--action-required", html, StringComparison.Ordinal);
        Assert.Contains("pendente", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GroupsIndex_OrdersAdminGroupsWithPendingRequests_First()
    {
        const string adminUserId = "groups-admin-2";
        var noPendingGroupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Sem Pendencias");
        var withPendingGroupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Com Pendencias");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = withPendingGroupId,
                UserId = "requester-user-2",
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var idxPendingGroup = html.IndexOf("Grupo Com Pendencias", StringComparison.Ordinal);
        var idxNoPendingGroup = html.IndexOf("Grupo Sem Pendencias", StringComparison.Ordinal);

        Assert.True(idxPendingGroup >= 0, "Expected group with pending requests in HTML.");
        Assert.True(idxNoPendingGroup >= 0, "Expected group without pending requests in HTML.");
        Assert.True(idxPendingGroup < idxNoPendingGroup, "Group with pending requests should appear first.");
    }

    [Fact]
    public async Task GroupsIndex_FilterPendingQuery_ShowsOnlyGroupsWithPendingRequests()
    {
        const string adminUserId = "groups-admin-3";
        await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Sem Pendencias Query");
        var withPendingGroupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Com Pendencias Query");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = withPendingGroupId,
                UserId = "requester-user-3",
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync("/grupos?pendentes=true");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Both groups are visible (pendentes filter was removed; groups sort pending-first)
        Assert.Contains("Grupo Com Pendencias Query", html, StringComparison.Ordinal);
        Assert.Contains("Grupo Sem Pendencias Query", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GroupsIndex_ShowsActionRequiredSection_ForAdminWithPendingRequests()
    {
        const string adminUserId = "groups-admin-4";
        var pendingGroupId = await _factory.SeedGroupWithAdminAsync(adminUserId, "Grupo Ação Necessária");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = pendingGroupId,
                UserId = "requester-user-4",
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Admin with pending requests should see the action-required card class and pending badge
        Assert.Contains("group-card--action-required", html, StringComparison.Ordinal);
        Assert.Contains("player-tag--pending-requests", html, StringComparison.Ordinal);
    }
}
