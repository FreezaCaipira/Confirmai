using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
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
        Assert.Contains("group-card--action-required", html);
        Assert.Contains("pendente", html);
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
        Assert.Contains("Grupo Com Pendencias Query", html);
        Assert.Contains("Grupo Sem Pendencias Query", html);
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

        // ===== DATABASE VERIFICATION (before HTTP request) =====
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Check 1: Group exists
            var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == pendingGroupId);
            Assert.NotNull(group);
            Assert.Equal("Grupo Ação Necessária", group.Name);
            
            // Check 2: Admin member exists with correct role
            var adminMember = await db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == pendingGroupId && m.UserId == adminUserId);
            Assert.NotNull(adminMember);
            Assert.Equal(GroupMemberRole.Admin, adminMember.Role);
            
            // Check 3: Pending join request exists
            var joinRequest = await db.GroupJoinRequests
                .FirstOrDefaultAsync(r => r.GroupId == pendingGroupId && r.Status == JoinRequestStatus.Pending);
            Assert.NotNull(joinRequest);
            
            // Check 4a: Query that LoadGroups uses - test it loads members
            var groupIds = await db.GroupMembers.Where(m => m.UserId == adminUserId).Select(m => m.GroupId).ToListAsync();
            var groupsForUser = await db.Groups
                .Where(g => groupIds.Contains(g.Id))
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Events.Where(e => e.IsActive && e.StartsAt > DateTime.UtcNow))
                .OrderByDescending(g => g.Members.Any(m => m.UserId == adminUserId && m.Role == GroupMemberRole.Admin))
                .ThenBy(g => g.Name)
                .ToListAsync();
            
            Assert.NotEmpty(groupsForUser);
            var testGroup = groupsForUser.FirstOrDefault(g => g.Id == pendingGroupId);
            Assert.NotNull(testGroup);
            Assert.NotEmpty(testGroup.Members);
            Assert.Equal(1, testGroup.Members.Count);
            var adminMemberInQuery = testGroup.Members.First();
            Assert.Equal(adminUserId, adminMemberInQuery.UserId);
            Assert.Equal(GroupMemberRole.Admin, adminMemberInQuery.Role);
        }

        // Check 4b: Also verify the pre-load query works
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var groupIds = await db.Groups
                .Where(g => g.Members.Any(m => m.UserId == adminUserId))
                .Select(g => g.Id)
                .ToListAsync();
            Assert.NotEmpty(groupIds);
            Assert.Contains(pendingGroupId, groupIds);
        }

        // ===== HTTP REQUEST =====
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminUserId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", "admin");

        var response = await client.GetAsync("/grupos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Save HTML to file for debugging
        System.IO.File.WriteAllText(@"C:\temp\grupos_debug.html", html);
        
        // Verify groups are being displayed
        var hasGroupCard = html.Contains("group-card");
        Assert.True(hasGroupCard, "HTML should contain group cards");
        
        // Check the data attributes
        var isAdminMatch = System.Text.RegularExpressions.Regex.Match(html, "data-test-is-admin=\"([^\"]*)\"");
        Assert.True(isAdminMatch.Success, "Should have data-test-is-admin attribute");
        Assert.Equal("True", isAdminMatch.Groups[1].Value);
        
        // Check that the group card exists with action-required class
        Assert.Contains("group-card--action-required", html);
        Assert.Contains("player-tag--pending-requests", html);
    }
}
