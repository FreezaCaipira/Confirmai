using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

public class GroupFeaturesServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, GroupFeaturesService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"gfeat-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var gatewayFactory = new EventPaymentGatewayFactory(
            new List<Services.Interfaces.IEventPaymentGateway>(),
            new GatewayService(factory));
        var svc = new GroupFeaturesService(factory, logService, gatewayFactory);
        return (factory, svc);
    }

    private static async Task<(IDbContextFactory<AppDbContext> factory, GroupFeaturesService svc, int groupId)> SetupWithGroupAsync(string creatorId = "creator-1")
    {
        var (factory, svc) = Setup();
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "Test Group", CreatedByUserId = creatorId };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return (factory, svc, group.Id);
    }

    [Fact]
    public async Task LoadAsync_ReturnsGroup_WhenFound()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "user-1", UserName = "Member", FullName = "Member" });
        db.GroupMembers.Add(new GroupMember { UserId = "user-1", GroupId = groupId, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();

        var result = await svc.LoadAsync(groupId, "user-1");

        Assert.NotNull(result.Group);
        Assert.True(result.IsAdmin);
        Assert.False(result.IsCreator);
    }

    [Fact]
    public async Task LoadAsync_ReturnsIsCreator_WhenUserIsCreator()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");

        var result = await svc.LoadAsync(groupId, "creator-1");

        Assert.True(result.IsCreator);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullGroup_WhenNotFound()
    {
        var (factory, svc) = Setup();

        var result = await svc.LoadAsync(999, "user-1");

        Assert.Null(result.Group);
        Assert.False(result.IsAdmin);
    }

    [Fact]
    public async Task TogglePostMatchRankingAsync_TogglesValue()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FindAsync(groupId);
        group!.EnablePostMatchRanking = false;
        await db.SaveChangesAsync();

        var newValue = await svc.TogglePostMatchRankingAsync(groupId, false, "user-1");

        Assert.True(newValue);
        await using var db2 = factory.CreateDbContext();
        var dbGroup = await db2.Groups.FindAsync(groupId);
        Assert.True(dbGroup!.EnablePostMatchRanking);
    }

    [Fact]
    public async Task TogglePostMatchRankingAsync_ReturnsFalse_WhenGroupNotFound()
    {
        var (factory, svc) = Setup();

        var result = await svc.TogglePostMatchRankingAsync(999, false, "user-1");

        Assert.False(result);
    }

    [Fact]
    public async Task ToggleBestPlayerVotingAsync_TogglesValue()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FindAsync(groupId);
        group!.EnableBestPlayerVoting = false;
        await db.SaveChangesAsync();

        var newValue = await svc.ToggleBestPlayerVotingAsync(groupId, false, "user-1");

        Assert.True(newValue);
        await using var db2 = factory.CreateDbContext();
        var dbGroup = await db2.Groups.FindAsync(groupId);
        Assert.True(dbGroup!.EnableBestPlayerVoting);
    }

    [Fact]
    public async Task TogglePaymentGatewaysAsync_EnablesGateways()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FindAsync(groupId);
        group!.EnablePaymentGateways = false;
        await db.SaveChangesAsync();

        var (success, newValue, message) = await svc.TogglePaymentGatewaysAsync(groupId, false, "user-1");

        Assert.True(success);
        Assert.True(newValue);
        Assert.Contains("ativados", message);
    }

    [Fact]
    public async Task TogglePaymentGatewaysAsync_DisablesGatewaysAndCascades()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FindAsync(groupId);
        group!.EnablePaymentGateways = true;
        group.EnablePostMatchRanking = true;
        group.EnableBestPlayerVoting = true;
        await db.SaveChangesAsync();

        var (success, newValue, message) = await svc.TogglePaymentGatewaysAsync(groupId, true, "user-1");

        Assert.True(success);
        Assert.False(newValue);
        await using var db2 = factory.CreateDbContext();
        var dbGroup = await db2.Groups.FindAsync(groupId);
        Assert.False(dbGroup!.EnablePaymentGateways);
        Assert.False(dbGroup.EnablePostMatchRanking);
        Assert.False(dbGroup.EnableBestPlayerVoting);
    }

    [Fact]
    public async Task SetMemberRoleAsync_PromotesToAdmin_WhenCreatorRequests()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "member-1", UserName = "Member" });
        var member = new GroupMember { UserId = "member-1", GroupId = groupId, Role = GroupMemberRole.Member };
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync();

        var (success, msg) = await svc.SetMemberRoleAsync(groupId, member.Id, GroupMemberRole.Admin, "creator-1");

        Assert.True(success);
        Assert.Contains("promovido", msg);
        await using var db2 = factory.CreateDbContext();
        var dbMember = await db2.GroupMembers.FindAsync(member.Id);
        Assert.Equal(GroupMemberRole.Admin, dbMember!.Role);
    }

    [Fact]
    public async Task SetMemberRoleAsync_Denies_WhenNotCreator()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "member-1", UserName = "Member" });
        var member = new GroupMember { UserId = "member-1", GroupId = groupId, Role = GroupMemberRole.Member };
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync();

        var (success, msg) = await svc.SetMemberRoleAsync(groupId, member.Id, GroupMemberRole.Admin, "other-user");

        Assert.False(success);
        Assert.Contains("criador", msg);
    }

    [Fact]
    public async Task SetMemberRoleAsync_PreventsRemovingLastAdmin()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin" });
        var member = new GroupMember { UserId = "admin-1", GroupId = groupId, Role = GroupMemberRole.Admin };
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync();

        var (success, msg) = await svc.SetMemberRoleAsync(groupId, member.Id, GroupMemberRole.Member, "creator-1");

        Assert.False(success);
        Assert.Contains("pelo menos um admin", msg);
    }

    [Fact]
    public async Task SavePixReceiverAsync_SavesReceiver_WhenEligible()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin", PixKey = "admin@pix.com" });
        db.GroupMembers.Add(new GroupMember { UserId = "admin-1", GroupId = groupId, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();

        var (success, msg) = await svc.SavePixReceiverAsync(groupId, "admin-1", "creator-1");

        Assert.True(success);
        await using var db2 = factory.CreateDbContext();
        var dbGroup = await db2.Groups.FindAsync(groupId);
        Assert.Equal("admin-1", dbGroup!.PixReceiverUserId);
    }

    [Fact]
    public async Task SavePixReceiverAsync_Rejects_WhenNotAdmin()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "member-1", UserName = "Member", PixKey = "member@pix.com" });
        db.GroupMembers.Add(new GroupMember { UserId = "member-1", GroupId = groupId, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var (success, msg) = await svc.SavePixReceiverAsync(groupId, "member-1", "creator-1");

        Assert.False(success);
        Assert.Contains("admin", msg);
    }

    [Fact]
    public async Task SavePixReceiverAsync_ClearsReceiver_WhenEmptyString()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FindAsync(groupId);
        group!.PixReceiverUserId = "old-admin";
        await db.SaveChangesAsync();

        var (success, _) = await svc.SavePixReceiverAsync(groupId, "", "creator-1");

        Assert.True(success);
        await using var db2 = factory.CreateDbContext();
        var dbGroup = await db2.Groups.FindAsync(groupId);
        Assert.Null(dbGroup!.PixReceiverUserId);
    }

    [Fact]
    public async Task SavePayoutAccountAsync_CreatesNewAccount()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");

        var formData = new GroupPayoutAccount
        {
            PixKeyType = PixKeyType.Email,
            PixKeyValue = "test@pix.com",
            BeneficiaryName = "Test"
        };

        var (success, msg) = await svc.SavePayoutAccountAsync(groupId, formData, null, "creator-1");

        Assert.True(success);
        await using var db = factory.CreateDbContext();
        var account = await db.GroupPayoutAccounts.FirstOrDefaultAsync(a => a.GroupId == groupId);
        Assert.NotNull(account);
        Assert.Equal("test@pix.com", account!.PixKeyValue);
        Assert.True(account.IsActive);
    }

    [Fact]
    public async Task SavePayoutAccountAsync_UpdatesExistingAccount()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync("creator-1");
        await using var db = factory.CreateDbContext();
        var existing = new GroupPayoutAccount
        {
            GroupId = groupId,
            PixKeyType = PixKeyType.Email,
            PixKeyValue = "old@pix.com",
            IsActive = true
        };
        db.GroupPayoutAccounts.Add(existing);
        await db.SaveChangesAsync();

        var formData = new GroupPayoutAccount
        {
            PixKeyType = PixKeyType.Phone,
            PixKeyValue = "+5511999999999",
            BeneficiaryName = "Updated"
        };

        var (success, _) = await svc.SavePayoutAccountAsync(groupId, formData, existing.Id, "creator-1");

        Assert.True(success);
        await using var db2 = factory.CreateDbContext();
        var account = await db2.GroupPayoutAccounts.FindAsync(existing.Id);
        Assert.Equal("+5511999999999", account!.PixKeyValue);
        Assert.Equal(PixKeyType.Phone, account.PixKeyType);
    }
}
