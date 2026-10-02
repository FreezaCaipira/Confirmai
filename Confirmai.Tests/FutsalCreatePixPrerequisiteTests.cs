using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

public class FutsalCreatePixPrerequisiteTests
{
    private static TestAuthStateProvider CreateAuthProvider(string userId)
        => new(userId);

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly string _userId;
        public TestAuthStateProvider(string userId) => _userId = userId;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId),
            }, "Test");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) SetupDb()
        => TestDataFactory.CreateDbContextWithFactory();

    private static FutsalCreateService CreateService(
        IDbContextFactory<AppDbContext> factory,
        string userId)
    {
        var authProvider = CreateAuthProvider(userId);
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var collisionService = new EventCollisionService(factory);
        return new FutsalCreateService(factory, authProvider, collisionService, logService, new UiTextService(new LanguagePreferenceService()));
    }

    [Fact]
    public async Task SaveAsync_PricedMatch_NoPixKey_ReturnsErrorWithProfileLink()
    {
        var ctx = SetupDb();
        var userId = "admin-no-pix";

        var user = new ApplicationUser { Id = userId, UserName = "Admin", FullName = "Admin" };
        ctx.db.Users.Add(user);

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = userId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 20.0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, userId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.False(result.Success);
        Assert.Contains("Pix", result.Error);
        Assert.Contains("intent=pix", result.CollisionHref);
    }

    [Fact]
    public async Task SaveAsync_PricedMatch_WithPixKey_Succeeds()
    {
        var ctx = SetupDb();
        var userId = "admin-with-pix";

        var user = TestDataFactory.CreateUserWithPixKey(userId, "Admin", "admin@pix");
        ctx.db.Users.Add(user);

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = userId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 20.0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, userId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task SaveAsync_ZeroPrice_NoPixKey_Succeeds()
    {
        var ctx = SetupDb();
        var userId = "admin-no-pix-free";

        var user = new ApplicationUser { Id = userId, UserName = "Admin", FullName = "Admin" };
        ctx.db.Users.Add(user);

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = userId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, userId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SaveAsync_PricedMatch_GatewayEnabled_NoPixKey_Succeeds()
    {
        var ctx = SetupDb();
        var userId = "admin-no-pix-gateway";

        var user = new ApplicationUser { Id = userId, UserName = "Admin", FullName = "Admin" };
        ctx.db.Users.Add(user);

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: true);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = userId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 20.0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, userId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
    }


    [Fact]
    public async Task SaveAsync_PricedMatch_CoAdminWithoutPix_OtherAdminHasPix_Succeeds()
    {
        var ctx = SetupDb();
        var creatorId = "admin-no-pix-2";
        var receiverId = "admin-with-pix-2";

        ctx.db.Users.Add(new ApplicationUser { Id = creatorId, UserName = "Creator", FullName = "Creator" });
        ctx.db.Users.Add(TestDataFactory.CreateUserWithPixKey(receiverId, "Receiver", "receiver@pix"));

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = creatorId, Role = GroupMemberRole.Admin });
        group.Members.Add(new GroupMember { UserId = receiverId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 20.0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, creatorId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task SaveAsync_PricedMatch_ChosenReceiverWithPix_CreatorWithout_Succeeds()
    {
        var ctx = SetupDb();
        var creatorId = "admin-no-pix-3";
        var receiverId = "receiver-pix-3";

        ctx.db.Users.Add(new ApplicationUser { Id = creatorId, UserName = "Creator", FullName = "Creator" });
        ctx.db.Users.Add(TestDataFactory.CreateUserWithPixKey(receiverId, "Receiver", "receiver@pix"));

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.PixReceiverUserId = receiverId;
        group.Members.Add(new GroupMember { UserId = creatorId, Role = GroupMemberRole.Admin });
        group.Members.Add(new GroupMember { UserId = receiverId, Role = GroupMemberRole.Member });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var venue = new Venue { Name = "Quadra", City = "SP", StateCode = "SP", IsActive = true };
        ctx.db.Venues.Add(venue);
        await ctx.db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "Test",
            VenueId = venue.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time = new TimeOnly(20, 0),
            Price = 20.0m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
        };

        var service = CreateService(ctx.factory, creatorId);
        var result = await service.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }


    [Fact]
    public async Task InitializeAsync_GroupWithoutAnyAdminPix_ReturnsGroupHasPixKeyFalse()
    {
        var ctx = SetupDb();
        var adminId = "init-admin-no-pix";

        ctx.db.Users.Add(new ApplicationUser { Id = adminId, UserName = "Admin", FullName = "Admin" });

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = adminId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory, adminId);
        var data = await service.InitializeAsync(group.Id);

        Assert.NotNull(data.PreselectedGroup);
        Assert.False(data.GroupHasPixKey);
        Assert.Equal(adminId, data.AdminUserId);
    }

    [Fact]
    public async Task InitializeAsync_OtherAdminWithPix_ReturnsGroupHasPixKeyTrue()
    {
        var ctx = SetupDb();
        var creatorId = "init-creator-no-pix";
        var otherAdminId = "init-other-admin-pix";

        ctx.db.Users.Add(new ApplicationUser { Id = creatorId, UserName = "Creator", FullName = "Creator" });
        ctx.db.Users.Add(TestDataFactory.CreateUserWithPixKey(otherAdminId, "Other", "other@pix"));

        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        group.Members.Add(new GroupMember { UserId = creatorId, Role = GroupMemberRole.Admin });
        group.Members.Add(new GroupMember { UserId = otherAdminId, Role = GroupMemberRole.Admin });
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory, creatorId);
        var data = await service.InitializeAsync(group.Id);

        Assert.NotNull(data.PreselectedGroup);
        Assert.True(data.GroupHasPixKey);
    }
}
