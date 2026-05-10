using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

// ────────────────────────────────────────────────────────────────────────────────
// TibiaServerService — member management
// ────────────────────────────────────────────────────────────────────────────────

public class ServerManageMembersTests
{
    private static TibiaServerService CreateService(AppDbContext db)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        return new TibiaServerService(db, env.Object);
    }

    // ── GetMembersAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMembersAsync_ReturnsMembers_OrderedByRoleDescThenName()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "Alice", Email = "alice@test.com" },
            new ApplicationUser { Id = "u2", UserName = "Bob", Email = "bob@test.com" },
            new ApplicationUser { Id = "u3", UserName = "Charlie", Email = "charlie@test.com" }
        );
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.AddRange(
            new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.User },
            new ServerMember { ServerId = server.Id, UserId = "u2", Role = ServerMemberRole.ServerAdmin },
            new ServerMember { ServerId = server.Id, UserId = "u3", Role = ServerMemberRole.User }
        );
        await db.SaveChangesAsync();

        var members = await service.GetMembersAsync(server.Id);

        Assert.Equal(3, members.Count);
        // ServerAdmin (higher int value) should come first
        Assert.Equal("Bob", members[0].UserName);
        // Then User role, ordered by name
        Assert.Equal("Alice", members[1].UserName);
        Assert.Equal("Charlie", members[2].UserName);
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsEmpty_WhenNoMembers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var members = await service.GetMembersAsync(server.Id);

        Assert.Empty(members);
    }

    [Fact]
    public async Task GetMembersAsync_MarksSystemAdmins_Correctly()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "Admin", Email = "admin@test.com" },
            new ApplicationUser { Id = "u2", UserName = "Regular", Email = "regular@test.com" }
        );
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);

        // Create "admin" role and assign u1 to it
        var adminRole = new Microsoft.AspNetCore.Identity.IdentityRole { Id = "role1", Name = "admin", NormalizedName = "ADMIN" };
        db.Roles.Add(adminRole);
        db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { UserId = "u1", RoleId = "role1" });

        await db.SaveChangesAsync();

        db.ServerMembers.AddRange(
            new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.ServerAdmin },
            new ServerMember { ServerId = server.Id, UserId = "u2", Role = ServerMemberRole.User }
        );
        await db.SaveChangesAsync();

        var members = await service.GetMembersAsync(server.Id);

        var adminMember = members.FirstOrDefault(m => m.UserId == "u1");
        var regularMember = members.FirstOrDefault(m => m.UserId == "u2");

        Assert.NotNull(adminMember);
        Assert.True(adminMember!.IsSystemAdmin);
        Assert.NotNull(regularMember);
        Assert.False(regularMember!.IsSystemAdmin);
    }

    [Fact]
    public async Task GetMembersAsync_DoesNotReturn_MembersFromOtherServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1", Email = "u1@test.com" });
        var server1 = new TibiaServer { Name = "S1", IsActive = true };
        var server2 = new TibiaServer { Name = "S2", IsActive = true };
        db.Servers.AddRange(server1, server2);
        await db.SaveChangesAsync();

        db.ServerMembers.AddRange(
            new ServerMember { ServerId = server1.Id, UserId = "u1", Role = ServerMemberRole.User },
            new ServerMember { ServerId = server2.Id, UserId = "u1", Role = ServerMemberRole.ServerAdmin }
        );
        await db.SaveChangesAsync();

        var members = await service.GetMembersAsync(server1.Id);

        Assert.Single(members);
        Assert.Equal(ServerMemberRole.User, members[0].Role);
    }

    // ── GetLinkedPlayersAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetLinkedPlayersAsync_IncludesServerMembers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "Member", Email = "m@test.com" });
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        var players = await service.GetLinkedPlayersAsync(server.Id);

        Assert.Single(players);
        Assert.True(players[0].IsServerMember);
        Assert.Equal("Member", players[0].UserName);
    }

    [Fact]
    public async Task GetLinkedPlayersAsync_IncludesMarketplaceSellers_EvenIfNotMembers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.AddRange(
            new ApplicationUser { Id = "seller", UserName = "Seller", Email = "seller@test.com" }
        );
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        // Add active offer but no ServerMember row
        db.ItemOffers.Add(new ItemOffer
        {
            ServerId = server.Id,
            SellerUserId = "seller",
            ItemKey = "Sword",
            ItemName = "Sword",
            Quantity = 1,
            UnitPrice = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var players = await service.GetLinkedPlayersAsync(server.Id);

        Assert.Single(players);
        Assert.Equal("Seller", players[0].UserName);
        Assert.True(players[0].HasMarketplaceOffer);
        Assert.False(players[0].IsServerMember);
    }

    [Fact]
    public async Task GetLinkedPlayersAsync_SetsIsServerMember_AndHasMarketplaceOffer_Correctly()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.AddRange(
            new ApplicationUser { Id = "both", UserName = "Both", Email = "both@test.com" },
            new ApplicationUser { Id = "memonly", UserName = "MemOnly", Email = "memonly@test.com" }
        );
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.AddRange(
            new ServerMember { ServerId = server.Id, UserId = "both", Role = ServerMemberRole.User },
            new ServerMember { ServerId = server.Id, UserId = "memonly", Role = ServerMemberRole.User }
        );
        db.ItemOffers.Add(new ItemOffer
        {
            ServerId = server.Id,
            SellerUserId = "both",
            ItemKey = "Key",
            ItemName = "Key",
            Quantity = 1,
            UnitPrice = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var players = await service.GetLinkedPlayersAsync(server.Id);

        Assert.Equal(2, players.Count);

        var both = players.First(p => p.UserId == "both");
        Assert.True(both.IsServerMember);
        Assert.True(both.HasMarketplaceOffer);

        var memOnly = players.First(p => p.UserId == "memonly");
        Assert.True(memOnly.IsServerMember);
        Assert.False(memOnly.HasMarketplaceOffer);
    }

    [Fact]
    public async Task GetLinkedPlayersAsync_ReturnsEmpty_WhenNeitherMembersNorSellers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var players = await service.GetLinkedPlayersAsync(server.Id);

        Assert.Empty(players);
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// ServerApiKeyService — unit tests
// ────────────────────────────────────────────────────────────────────────────────

public class ServerApiKeyServiceTests
{
    private static ServerApiKeyService CreateService(AppDbContext db) => new(db);

    // ── CreateKeyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateKeyAsync_ReturnsSuccess_AndPersistsKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "TestServer", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.CreateKeyAsync(server.Id, "Integration Label");

        Assert.True(result.Success);
        Assert.NotNull(result.RawKey);
        Assert.StartsWith("osm_", result.RawKey);
        Assert.NotNull(result.Entity);
        Assert.True(result.Entity!.Id > 0);
        Assert.Equal(server.Id, result.Entity.ServerId);
        Assert.Equal("Integration Label", result.Entity.Label);
        Assert.True(result.Entity.IsActive);
    }

    [Fact]
    public async Task CreateKeyAsync_ReturnsFailure_WhenServerNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateKeyAsync(999, null);

        Assert.False(result.Success);
        Assert.Null(result.RawKey);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task CreateKeyAsync_StoresHashedKey_NotRawKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.CreateKeyAsync(server.Id, null);

        var entity = await db.ServerApiKeys.FindAsync(result.Entity!.Id);
        Assert.NotNull(entity);
        // The stored hash must NOT equal the raw key
        Assert.NotEqual(result.RawKey, entity!.KeyHash);
        // But the prefix is a short excerpt of the raw key (for display)
        Assert.StartsWith(entity.KeyPrefix, result.RawKey!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateKeyAsync_TreatsBlankLabel_AsNull()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.CreateKeyAsync(server.Id, "   ");

        Assert.Null(result.Entity!.Label);
    }

    // ── GetKeysForServerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetKeysForServerAsync_ReturnsOnlyKeysForServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var s1 = new TibiaServer { Name = "S1", IsActive = true };
        var s2 = new TibiaServer { Name = "S2", IsActive = true };
        db.Servers.AddRange(s1, s2);
        await db.SaveChangesAsync();

        await service.CreateKeyAsync(s1.Id, "key-a");
        await service.CreateKeyAsync(s1.Id, "key-b");
        await service.CreateKeyAsync(s2.Id, "key-c");

        var keys = await service.GetKeysForServerAsync(s1.Id);

        Assert.Equal(2, keys.Count);
        Assert.All(keys, k => Assert.Equal(s1.Id, k.ServerId));
    }

    [Fact]
    public async Task GetKeysForServerAsync_ReturnsEmpty_WhenNoKeys()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var keys = await service.GetKeysForServerAsync(server.Id);

        Assert.Empty(keys);
    }

    [Fact]
    public async Task GetKeysForServerAsync_OrderedByCreatedAtDescending()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        await service.CreateKeyAsync(server.Id, "first");
        await service.CreateKeyAsync(server.Id, "second");

        var keys = await service.GetKeysForServerAsync(server.Id);

        // Most recent key should be first
        Assert.Equal("second", keys[0].Label);
        Assert.Equal("first", keys[1].Label);
    }

    // ── RevokeKeyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeKeyAsync_RevokesKey_ReturnsTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var created = await service.CreateKeyAsync(server.Id, null);
        var keyId = created.Entity!.Id;

        var revoked = await service.RevokeKeyAsync(keyId);

        Assert.True(revoked);

        var entity = await db.ServerApiKeys.FindAsync(keyId);
        Assert.False(entity!.IsActive);
        Assert.NotNull(entity.RevokedAt);
    }

    [Fact]
    public async Task RevokeKeyAsync_ReturnsFalse_WhenKeyNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.RevokeKeyAsync(99999);

        Assert.False(result);
    }

    // ── ValidateKeyAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateKeyAsync_ReturnsKeyAndServer_ForValidRawKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var created = await service.CreateKeyAsync(server.Id, null);
        var rawKey = created.RawKey!;

        var (key, returnedServer) = await service.ValidateKeyAsync(rawKey);

        Assert.NotNull(key);
        Assert.NotNull(returnedServer);
        Assert.Equal(server.Id, returnedServer!.Id);
        Assert.Equal(server.Name, returnedServer.Name);
    }

    [Fact]
    public async Task ValidateKeyAsync_UpdatesLastUsedAt_OnSuccess()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var created = await service.CreateKeyAsync(server.Id, null);
        var entity = await db.ServerApiKeys.FindAsync(created.Entity!.Id);
        Assert.Null(entity!.LastUsedAt);

        await service.ValidateKeyAsync(created.RawKey!);

        await db.Entry(entity).ReloadAsync();
        Assert.NotNull(entity.LastUsedAt);
    }

    [Fact]
    public async Task ValidateKeyAsync_ReturnsNull_ForInvalidKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var (key, server) = await service.ValidateKeyAsync("osm_invalidkeyvalue");

        Assert.Null(key);
        Assert.Null(server);
    }

    [Fact]
    public async Task ValidateKeyAsync_ReturnsNull_ForRevokedKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var serverEntity = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(serverEntity);
        await db.SaveChangesAsync();

        var created = await service.CreateKeyAsync(serverEntity.Id, null);
        await service.RevokeKeyAsync(created.Entity!.Id);

        var (key, server) = await service.ValidateKeyAsync(created.RawKey!);

        Assert.Null(key);
        Assert.Null(server);
    }

    [Fact]
    public async Task ValidateKeyAsync_ReturnsNull_ForBlankInput()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var (k1, s1) = await service.ValidateKeyAsync("");
        var (k2, s2) = await service.ValidateKeyAsync("   ");

        Assert.Null(k1);
        Assert.Null(s1);
        Assert.Null(k2);
        Assert.Null(s2);
    }
}
