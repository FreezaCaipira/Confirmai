using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class TibiaServerServiceTests
{
    [Fact]
    public async Task GetItemsForServerAsync_ReturnsOnlyProductsLinkedToServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var user = new ApplicationUser { Id = "u1", UserName = "testuser" };
        db.Users.Add(user);

        var server1 = new TibiaServer { Name = "Server 1", IsActive = true };
        var server2 = new TibiaServer { Name = "Server 2", IsActive = true };
        db.Servers.AddRange(server1, server2);
        await db.SaveChangesAsync();

        var product1 = new Product { Name = "Magic Plate Armor", Description = "d", UserId = "u1" };
        var product2 = new Product { Name = "Golden Armor", Description = "d", UserId = "u1" };
        var product3 = new Product { Name = "Demon Helmet", Description = "d", UserId = "u1" };
        db.Products.AddRange(product1, product2, product3);
        await db.SaveChangesAsync();

        db.ProductServers.AddRange(
            new ProductServer { ProductId = product1.Id, ServerId = server1.Id },
            new ProductServer { ProductId = product2.Id, ServerId = server2.Id },
            new ProductServer { ProductId = product3.Id, ServerId = server1.Id }
        );
        await db.SaveChangesAsync();

        var result = await service.GetItemsForServerAsync(server1.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Magic Plate Armor");
        Assert.Contains(result, p => p.Name == "Demon Helmet");
        Assert.DoesNotContain(result, p => p.Name == "Golden Armor");
    }

    [Fact]
    public async Task GetItemsForServerAsync_ExcludesArchivedProducts()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var user = new ApplicationUser { Id = "u1", UserName = "testuser" };
        db.Users.Add(user);

        var server = new TibiaServer { Name = "Server A", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var active = new Product { Name = "Boots of Haste", Description = "d", UserId = "u1" };
        var archived = new Product { Name = "Old Item", Description = "d", UserId = "u1", Category = "legacy-archived" };
        var deleted = new Product { Name = "Deleted Item", Description = "d", UserId = "u1", Category = "deleted-archived" };
        db.Products.AddRange(active, archived, deleted);
        await db.SaveChangesAsync();

        db.ProductServers.AddRange(
            new ProductServer { ProductId = active.Id, ServerId = server.Id },
            new ProductServer { ProductId = archived.Id, ServerId = server.Id },
            new ProductServer { ProductId = deleted.Id, ServerId = server.Id }
        );
        await db.SaveChangesAsync();

        var result = await service.GetItemsForServerAsync(server.Id);

        Assert.Single(result);
        Assert.Equal("Boots of Haste", result[0].Name);
    }

    [Fact]
    public async Task GetItemsForServerAsync_ReturnsEmpty_WhenServerIsInactive()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "Inactive Server", IsActive = false };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var product = new Product { Name = "Some Item", Description = "d", UserId = "u1" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.ProductServers.Add(new ProductServer { ProductId = product.Id, ServerId = server.Id });
        await db.SaveChangesAsync();

        var result = await service.GetItemsForServerAsync(server.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemsForServerAsync_ReturnsEmpty_WhenNoProductsLinked()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "Empty Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var product = new Product { Name = "Unlinked", Description = "d", UserId = "u1" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await service.GetItemsForServerAsync(server.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedProductIdsAsync_ReturnsOnlyIdsForServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S1", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var p1 = new Product { Name = "P1", Description = "d", UserId = "u1" };
        var p2 = new Product { Name = "P2", Description = "d", UserId = "u1" };
        var p3 = new Product { Name = "P3", Description = "d", UserId = "u1" };
        db.Products.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        db.ProductServers.AddRange(
            new ProductServer { ProductId = p1.Id, ServerId = server.Id },
            new ProductServer { ProductId = p3.Id, ServerId = server.Id }
        );
        await db.SaveChangesAsync();

        var result = await service.GetLinkedProductIdsAsync(server.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(p1.Id, result);
        Assert.Contains(p3.Id, result);
        Assert.DoesNotContain(p2.Id, result);
    }

    private static TibiaServerService CreateService(AppDbContext db, PaymentEventBus? eventBus = null)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        return new TibiaServerService(db, env.Object, eventBus: eventBus);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsServersOrderedByActiveDescThenName()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Servers.AddRange(
            new TibiaServer { Name = "Zebra", IsActive = false },
            new TibiaServer { Name = "Alpha", IsActive = true },
            new TibiaServer { Name = "Beta", IsActive = true }
        );
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsServer_WhenExists()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "Test", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.GetByIdAsync(server.Id);

        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.Null(await service.GetByIdAsync(999));
    }

    // ── CountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Servers.AddRange(
            new TibiaServer { Name = "A" },
            new TibiaServer { Name = "B" }
        );
        await db.SaveChangesAsync();

        Assert.Equal(2, await service.CountAsync());
    }

    // ── AddAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsServer_AndCreatesMember()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "creator", UserName = "creator" });
        await db.SaveChangesAsync();

        var server = new TibiaServer { Name = "New", IsActive = true };
        await service.AddAsync(server, "creator");

        Assert.True(server.Id > 0);
        Assert.Equal("creator", server.CreatedByUserId);

        var member = await db.ServerMembers.FirstOrDefaultAsync(m => m.ServerId == server.Id && m.UserId == "creator");
        Assert.NotNull(member);
        Assert.Equal(ServerMemberRole.ServerAdmin, member!.Role);
    }

    [Fact]
    public async Task AddAsync_CreatesMemberForPrimaryGM_WhenProvided()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.AddRange(
            new ApplicationUser { Id = "owner", UserName = "owner" },
            new ApplicationUser { Id = "gm", UserName = "gm" }
        );
        await db.SaveChangesAsync();

        var server = new TibiaServer { Name = "WithGM", IsActive = true };
        await service.AddAsync(server, "owner", "gm");

        Assert.Equal("gm", server.PrimaryGameMasterUserId);

        var gmMember = await db.ServerMembers.FirstOrDefaultAsync(m => m.ServerId == server.Id && m.UserId == "gm");
        Assert.NotNull(gmMember);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFields_ReturnsTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1" });
        var server = new TibiaServer { Name = "Old", IsActive = true, Region = "BR" };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var updated = new TibiaServer { Name = "New", IsActive = false, Region = "US", TibiaVersion = "8.60", WebsiteUrl = "https://x.com" };
        var result = await service.UpdateAsync(server.Id, updated, null, null);

        Assert.True(result);
        var reloaded = await db.Servers.FindAsync(server.Id);
        Assert.Equal("New", reloaded!.Name);
        Assert.False(reloaded.IsActive);
        Assert.Equal("US", reloaded.Region);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateAsync(999, new TibiaServer { Name = "X" }, null, null);
        Assert.False(result);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesServer_ReturnsTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "ToDelete", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        Assert.True(await service.DeleteAsync(server.Id));
        Assert.Null(await db.Servers.FindAsync(server.Id));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.False(await service.DeleteAsync(999));
    }

    // ── TryGetUserIdByEmailAsync ─────────────────────────────────────

    [Fact]
    public async Task TryGetUserIdByEmailAsync_ReturnsUserId_CaseInsensitive()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user", Email = "User@Test.com" });
        await db.SaveChangesAsync();

        Assert.Equal("u1", await service.TryGetUserIdByEmailAsync("user@test.com"));
    }

    [Fact]
    public async Task TryGetUserIdByEmailAsync_ReturnsNull_WhenBlank()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.Null(await service.TryGetUserIdByEmailAsync("  "));
        Assert.Null(await service.TryGetUserIdByEmailAsync(null));
    }

    // ── GetUserContactByIdAsync ──────────────────────────────────────

    [Fact]
    public async Task GetUserContactByIdAsync_ReturnsContact_WhenFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "joe", Email = "joe@test.com" });
        await db.SaveChangesAsync();

        var (userName, email) = await service.GetUserContactByIdAsync("u1");
        Assert.Equal("joe", userName);
        Assert.Equal("joe@test.com", email);
    }

    [Fact]
    public async Task GetUserContactByIdAsync_ReturnsNulls_WhenBlankOrNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var (a, b) = await service.GetUserContactByIdAsync(null);
        Assert.Null(a);
        Assert.Null(b);

        var (c, d) = await service.GetUserContactByIdAsync("nonexistent");
        Assert.Null(c);
        Assert.Null(d);
    }

    // ── AddOrUpdateMemberByEmailAsync ────────────────────────────────

    [Fact]
    public async Task AddOrUpdateMemberByEmailAsync_AddsNewMember()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user", Email = "user@test.com" });
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.AddOrUpdateMemberByEmailAsync(server.Id, "user@test.com", ServerMemberRole.User);

        Assert.Equal(TibiaServerService.AddServerMemberResult.Added, result);
        Assert.True(await db.ServerMembers.AnyAsync(m => m.ServerId == server.Id && m.UserId == "u1"));
    }

    [Fact]
    public async Task AddOrUpdateMemberByEmailAsync_UpdatesRole_WhenExisting()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user", Email = "user@test.com" });
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        var result = await service.AddOrUpdateMemberByEmailAsync(server.Id, "user@test.com", ServerMemberRole.ServerAdmin);

        Assert.Equal(TibiaServerService.AddServerMemberResult.Updated, result);
        var member = await db.ServerMembers.FirstAsync(m => m.ServerId == server.Id && m.UserId == "u1");
        Assert.Equal(ServerMemberRole.ServerAdmin, member.Role);
    }

    [Fact]
    public async Task AddOrUpdateMemberByEmailAsync_ReturnsUserNotFound_WhenEmailBlank()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.Equal(TibiaServerService.AddServerMemberResult.UserNotFound,
            await service.AddOrUpdateMemberByEmailAsync(1, "", ServerMemberRole.User));
    }

    [Fact]
    public async Task AddOrUpdateMemberByEmailAsync_ReturnsServerNotFound_WhenInvalidServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "user", Email = "user@test.com" });
        await db.SaveChangesAsync();

        Assert.Equal(TibiaServerService.AddServerMemberResult.ServerNotFound,
            await service.AddOrUpdateMemberByEmailAsync(999, "user@test.com", ServerMemberRole.User));
    }

    // ── RemoveMemberAsync ────────────────────────────────────────────

    [Fact]
    public async Task RemoveMemberAsync_RemovesMember_ReturnsTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        Assert.True(await service.RemoveMemberAsync(server.Id, "u1"));
        Assert.False(await db.ServerMembers.AnyAsync(m => m.ServerId == server.Id && m.UserId == "u1"));
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.False(await service.RemoveMemberAsync(1, "nonexistent"));
    }

    // ── CanManageServerAsync ─────────────────────────────────────────

    [Fact]
    public async Task CanManageServerAsync_ReturnsTrue_ForSiteAdmin()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.True(await service.CanManageServerAsync(1, "anyone", isSiteAdmin: true));
    }

    [Fact]
    public async Task CanManageServerAsync_ReturnsTrue_ForServerAdmin()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.ServerAdmin });
        await db.SaveChangesAsync();

        Assert.True(await service.CanManageServerAsync(server.Id, "u1", isSiteAdmin: false));
    }

    [Fact]
    public async Task CanManageServerAsync_ReturnsFalse_ForRegularMember()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "u1", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        Assert.False(await service.CanManageServerAsync(server.Id, "u1", isSiteAdmin: false));
    }

    [Fact]
    public async Task CanManageServerAsync_ReturnsFalse_ForNullUserId()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.False(await service.CanManageServerAsync(1, null, isSiteAdmin: false));
    }

    // ── CreateItemOfferAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateItemOfferAsync_CreatesOffer_WhenValid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller", UserName = "seller" });
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = server.Id,
            ItemKey = "magic plate armor",
            SellerUserId = "seller",
            Quantity = 1,
            UnitPrice = 0.001m,
            PricingGateway = "btcpay"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Offer);
        Assert.Equal("MAGIC PLATE ARMOR", result.Offer!.ItemKey);
        Assert.True(result.Offer.IsActive);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenServerInvalid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 0,
            ItemKey = "item",
            SellerUserId = "u1",
            Quantity = 1,
            UnitPrice = 1m,
            PricingGateway = "btcpay"
        });

        Assert.False(result.Success);
        Assert.Contains("invalido", result.Message);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenItemKeyBlank()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 1,
            ItemKey = "  ",
            SellerUserId = "u1",
            Quantity = 1,
            UnitPrice = 1m,
            PricingGateway = "btcpay"
        });

        Assert.False(result.Success);
        Assert.Contains("invalido", result.Message);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenQuantityZero()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 1,
            ItemKey = "item",
            SellerUserId = "u1",
            Quantity = 0,
            UnitPrice = 1m,
            PricingGateway = "btcpay"
        });

        Assert.False(result.Success);
        Assert.Contains("Quantidade", result.Message);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenPriceZero()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 1,
            ItemKey = "item",
            SellerUserId = "u1",
            Quantity = 1,
            UnitPrice = 0m,
            PricingGateway = "btcpay"
        });

        Assert.False(result.Success);
        Assert.Contains("Preco", result.Message);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenNoPricingGateway()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 1,
            ItemKey = "item",
            SellerUserId = "u1",
            Quantity = 1,
            UnitPrice = 1m,
            PricingGateway = null
        });

        Assert.False(result.Success);
        Assert.Contains("gateway", result.Message);
    }

    [Fact]
    public async Task CreateItemOfferAsync_Fails_WhenServerNotActive()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Servers.Add(new TibiaServer { Name = "Inactive", IsActive = false });
        await db.SaveChangesAsync();

        var result = await service.CreateItemOfferAsync(new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = 1,
            ItemKey = "item",
            SellerUserId = "u1",
            Quantity = 1,
            UnitPrice = 1m,
            PricingGateway = "btcpay"
        });

        Assert.False(result.Success);
        Assert.Contains("nao encontrado", result.Message);
    }

    // ── DeactivateOwnItemOffersAsync ─────────────────────────────────

    [Fact]
    public async Task DeactivateOwnItemOffersAsync_DeactivatesMatching_ReturnsCount()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ItemOffers.AddRange(
            new ItemOffer { ServerId = server.Id, ItemKey = "SWORD", ItemName = "Sword", SellerUserId = "u1", Quantity = 1, UnitPrice = 1, IsActive = true },
            new ItemOffer { ServerId = server.Id, ItemKey = "SWORD", ItemName = "Sword", SellerUserId = "u1", Quantity = 2, UnitPrice = 1, IsActive = true },
            new ItemOffer { ServerId = server.Id, ItemKey = "SWORD", ItemName = "Sword", SellerUserId = "u2", Quantity = 1, UnitPrice = 1, IsActive = true }
        );
        await db.SaveChangesAsync();

        var count = await service.DeactivateOwnItemOffersAsync(server.Id, "sword", "u1");

        Assert.Equal(2, count);
        Assert.Equal(1, await db.ItemOffers.CountAsync(o => o.IsActive));
    }

    [Fact]
    public async Task DeactivateOwnItemOffersAsync_ReturnsZero_WhenInvalidInput()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        Assert.Equal(0, await service.DeactivateOwnItemOffersAsync(0, "item", "u1"));
        Assert.Equal(0, await service.DeactivateOwnItemOffersAsync(1, "", "u1"));
        Assert.Equal(0, await service.DeactivateOwnItemOffersAsync(1, "item", ""));
    }

    // ── GetActiveOfferCountsByItemAsync ──────────────────────────────

    [Fact]
    public async Task GetActiveOfferCountsByItemAsync_GroupsByItemKey()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ItemOffers.AddRange(
            new ItemOffer { ServerId = server.Id, ItemKey = "SWORD", ItemName = "Sword", SellerUserId = "u1", Quantity = 1, UnitPrice = 1, IsActive = true },
            new ItemOffer { ServerId = server.Id, ItemKey = "SWORD", ItemName = "Sword", SellerUserId = "u2", Quantity = 1, UnitPrice = 1, IsActive = true },
            new ItemOffer { ServerId = server.Id, ItemKey = "SHIELD", ItemName = "Shield", SellerUserId = "u1", Quantity = 1, UnitPrice = 1, IsActive = true },
            new ItemOffer { ServerId = server.Id, ItemKey = "DEAD", ItemName = "Dead", SellerUserId = "u1", Quantity = 1, UnitPrice = 1, IsActive = false }
        );
        await db.SaveChangesAsync();

        var counts = await service.GetActiveOfferCountsByItemAsync(server.Id);

        Assert.Equal(2, counts["SWORD"]);
        Assert.Equal(1, counts["SHIELD"]);
        Assert.False(counts.ContainsKey("DEAD"));
    }

    // ── LinkProductsAsync ────────────────────────────────────────────

    [Fact]
    public async Task LinkProductsAsync_AddsNew_RemovesOld()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        var p1 = new Product { Name = "P1", Description = "d", UserId = "u" };
        var p2 = new Product { Name = "P2", Description = "d", UserId = "u" };
        var p3 = new Product { Name = "P3", Description = "d", UserId = "u" };
        db.Products.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        db.ProductServers.Add(new ProductServer { ServerId = server.Id, ProductId = p1.Id });
        await db.SaveChangesAsync();

        // Link p2 and p3, remove p1
        await service.LinkProductsAsync(server.Id, new[] { p2.Id, p3.Id });

        var linked = await db.ProductServers.Where(ps => ps.ServerId == server.Id).Select(ps => ps.ProductId).ToListAsync();
        Assert.DoesNotContain(p1.Id, linked);
        Assert.Contains(p2.Id, linked);
        Assert.Contains(p3.Id, linked);
    }

    // ── ToggleSellerRecommendationAsync ──────────────────────────────

    [Fact]
    public async Task ToggleSellerRecommendationAsync_AddsRecommendation()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        await service.ToggleSellerRecommendationAsync(server.Id, "sword", "seller1", "recommender1");

        Assert.Single(await db.SellerRecommendations.ToListAsync());
    }

    [Fact]
    public async Task ToggleSellerRecommendationAsync_RemovesExisting()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.SellerRecommendations.Add(new SellerRecommendation
        {
            ServerId = server.Id,
            ItemKey = "SWORD",
            SellerUserId = "seller1",
            RecommenderUserId = "recommender1"
        });
        await db.SaveChangesAsync();

        await service.ToggleSellerRecommendationAsync(server.Id, "sword", "seller1", "recommender1");

        Assert.Empty(await db.SellerRecommendations.ToListAsync());
    }

    [Fact]
    public async Task ToggleSellerRecommendationAsync_DoesNothing_WhenSelfRecommend()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        await service.ToggleSellerRecommendationAsync(server.Id, "sword", "same-user", "same-user");

        Assert.Empty(await db.SellerRecommendations.ToListAsync());
    }

    // ── GetGlobalSummaryAsync ────────────────────────────────────────

    [Fact]
    public async Task GetGlobalSummaryAsync_AggregatesCorrectly()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1" });
        var s1 = new TibiaServer { Name = "Active1", IsActive = true };
        var s2 = new TibiaServer { Name = "Inactive", IsActive = false };
        db.Servers.AddRange(s1, s2);
        var p = new Product { Name = "Item", Description = "d", UserId = "u1" };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        db.Orders.AddRange(
            new OrderModel { ProductId = p.Id, BuyerId = "u1", Amount = 0.5m, IsPaid = true, ServerId = s1.Id },
            new OrderModel { ProductId = p.Id, BuyerId = "u1", Amount = 0.3m, IsPaid = true, ServerId = s1.Id },
            new OrderModel { ProductId = p.Id, BuyerId = "u1", Amount = 1m, IsPaid = false, ServerId = s1.Id }
        );
        await db.SaveChangesAsync();

        var summary = await service.GetGlobalSummaryAsync();

        Assert.Equal(2, summary.TotalServers);
        Assert.Equal(1, summary.ActiveServers);
        Assert.Equal(2, summary.PaidOrders);
        Assert.Equal(0.8m, summary.PaidVolumeBtc);
    }

    // ── GetAllServerCardStatsAsync ───────────────────────────────────

    [Fact]
    public async Task GetAllServerCardStatsAsync_AggregatesSalesAndOffers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1" });
        var server = new TibiaServer { Name = "S", IsActive = true };
        db.Servers.Add(server);
        var p = new Product { Name = "Item", Description = "d", UserId = "u1" };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        db.Orders.Add(new OrderModel { ProductId = p.Id, BuyerId = "u1", Amount = 1m, IsPaid = true, ServerId = server.Id });
        db.ItemOffers.Add(new ItemOffer { ServerId = server.Id, ItemKey = "K", ItemName = "K", SellerUserId = "u1", Quantity = 1, UnitPrice = 1, IsActive = true });
        await db.SaveChangesAsync();

        var stats = await service.GetAllServerCardStatsAsync();

        Assert.True(stats.ContainsKey(server.Id));
        Assert.Equal(1, stats[server.Id].CompletedSales);
        Assert.Equal(1, stats[server.Id].ActiveOffers);
    }

    // ── GetPendingTradesAsync ────────────────────────────────────────

    [Fact]
    public async Task GetPendingTradesAsync_ReturnsOnlyAguardandoEntregaInGame_ForServerId()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9001, Name = "Trade Server", IsActive = true };
        db.Servers.Add(server);

        var product = new Product { Id = 9001, Name = "Crystal Coin", Description = "d", Price = 0.001m, UserId = "s1" };
        db.Products.Add(product);

        var payment = new PaymentRecord
        {
            Id = 9001, ServerId = server.Id, ProductId = product.Id,
            UserId = "b1", SellerId = "s1", Amount = 0.001m,
            PaymentId = "inv-pending", IsPaid = true, InGamePlayerName = "Knight Test"
        };
        db.Payments.Add(payment);

        db.Orders.AddRange(
            new OrderModel
            {
                Id = 9001, ServerId = server.Id, ProductId = product.Id,
                BuyerId = "b1", SellerId = "s1", Amount = 0.001m,
                IsPaid = true, PaymentId = payment.Id,
                Status = PaymentStatus.AguardandoEntregaInGame
            },
            new OrderModel
            {
                Id = 9002, ServerId = server.Id, ProductId = product.Id,
                BuyerId = "b1", SellerId = "s1", Amount = 0.001m,
                IsPaid = true, PaymentId = payment.Id,
                Status = PaymentStatus.Finalizado, FundsReleased = true
            });
        await db.SaveChangesAsync();

        var result = await service.GetPendingTradesAsync(server.Id);

        Assert.Single(result);
        Assert.Equal(9001, result[0].Id);
        Assert.Equal("AguardandoEntregaInGame", result[0].Status);
    }

    [Fact]
    public async Task GetPendingTradesAsync_ExcludesOtherServersOrders()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var serverA = new TibiaServer { Id = 9101, Name = "Server A", IsActive = true };
        var serverB = new TibiaServer { Id = 9102, Name = "Server B", IsActive = true };
        db.Servers.AddRange(serverA, serverB);

        var product = new Product { Id = 9101, Name = "Item", Description = "d", Price = 0.001m, UserId = "s1" };
        db.Products.Add(product);

        db.Orders.AddRange(
            new OrderModel
            {
                Id = 9101, ServerId = serverA.Id, ProductId = product.Id,
                BuyerId = "b1", SellerId = "s1", Amount = 0.001m, IsPaid = true,
                Status = PaymentStatus.AguardandoEntregaInGame
            },
            new OrderModel
            {
                Id = 9102, ServerId = serverB.Id, ProductId = product.Id,
                BuyerId = "b2", SellerId = "s1", Amount = 0.001m, IsPaid = true,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
        await db.SaveChangesAsync();

        var result = await service.GetPendingTradesAsync(serverA.Id);

        Assert.Single(result);
        Assert.Equal(9101, result[0].Id);
    }

    [Fact]
    public async Task GetPendingTradesAsync_ReturnsEmpty_WhenNoMatchingOrders()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9201, Name = "Empty Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.GetPendingTradesAsync(server.Id);

        Assert.Empty(result);
    }

    // ── ConfirmPendingTradeAsync ─────────────────────────────────────

    [Fact]
    public async Task ConfirmPendingTradeAsync_TransitionsToAguardandoRevisaoAdm()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9301, Name = "Confirm Server", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9301, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9301, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.AguardandoEntregaInGame
        });
        await db.SaveChangesAsync();

        var result = await service.ConfirmPendingTradeAsync(server.Id, 9301);

        Assert.True(result.Success);
        Assert.Equal("Confirmed", result.Reason);
        Assert.NotNull(result.Order);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, result.Order!.Status);
        Assert.True(result.Order.IsDelivered);
        Assert.True(result.Order.DeliveryPendingApproval);
        Assert.NotNull(result.Order.DeliveredAt);

        var persisted = await db.Orders.FindAsync(9301);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, persisted!.Status);
    }

    [Fact]
    public async Task ConfirmPendingTradeAsync_ReturnsTradeNotFound_WhenOrderMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.ConfirmPendingTradeAsync(1, 99999);

        Assert.False(result.Success);
        Assert.Equal("TradeNotFound", result.Reason);
    }

    [Fact]
    public async Task ConfirmPendingTradeAsync_ReturnsInvalidStatus_WhenOrderAlreadyFinalizado()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9401, Name = "S", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9401, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9401, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.Finalizado, FundsReleased = true
        });
        await db.SaveChangesAsync();

        var result = await service.ConfirmPendingTradeAsync(server.Id, 9401);

        Assert.False(result.Success);
        Assert.Equal("InvalidStatus", result.Reason);
        Assert.NotNull(result.Order);
    }

    [Fact]
    public async Task ConfirmPendingTradeAsync_FiresOnOrderEnteredReviewEvent()
    {
        using var db = TestDataFactory.CreateDbContext();
        var eventBus = new PaymentEventBus();
        int? firedOrderId = null;
        eventBus.OnOrderEnteredReview += id => firedOrderId = id;

        var service = CreateService(db, eventBus);

        var server = new TibiaServer { Id = 9450, Name = "Event Server", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9450, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9450, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.AguardandoEntregaInGame
        });
        await db.SaveChangesAsync();

        var result = await service.ConfirmPendingTradeAsync(server.Id, 9450);

        Assert.True(result.Success);
        Assert.Equal(9450, firedOrderId);
    }

    // ── RejectPendingTradeAsync ──────────────────────────────────────

    [Fact]
    public async Task RejectPendingTradeAsync_TransitionsToDisputa()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9501, Name = "Reject Server", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9501, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9501, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.AguardandoEntregaInGame
        });
        await db.SaveChangesAsync();

        var result = await service.RejectPendingTradeAsync(server.Id, 9501, "player offline");

        Assert.True(result.Success);
        Assert.Equal("Rejected", result.Reason);
        Assert.NotNull(result.Order);
        Assert.Equal(PaymentStatus.Disputa, result.Order!.Status);

        var persisted = await db.Orders.FindAsync(9501);
        Assert.Equal(PaymentStatus.Disputa, persisted!.Status);
    }

    [Fact]
    public async Task RejectPendingTradeAsync_ReturnsTradeNotFound_WhenOrderMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var result = await service.RejectPendingTradeAsync(1, 99999, null);

        Assert.False(result.Success);
        Assert.Equal("TradeNotFound", result.Reason);
    }

    [Fact]
    public async Task RejectPendingTradeAsync_ReturnsInvalidStatus_WhenOrderAlreadyFinalizado()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9601, Name = "S", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9601, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9601, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.Finalizado, FundsReleased = true
        });
        await db.SaveChangesAsync();

        var result = await service.RejectPendingTradeAsync(server.Id, 9601, "motivo");

        Assert.False(result.Success);
        Assert.Equal("InvalidStatus", result.Reason);
        Assert.NotNull(result.Order);
    }

    [Fact]
    public async Task RejectPendingTradeAsync_AcceptsNullReason()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 9701, Name = "S", IsActive = true };
        db.Servers.Add(server);
        var product = new Product { Id = 9701, Name = "P", Description = "d", Price = 0.01m, UserId = "s1" };
        db.Products.Add(product);
        db.Orders.Add(new OrderModel
        {
            Id = 9701, ServerId = server.Id, ProductId = product.Id,
            BuyerId = "b1", SellerId = "s1", Amount = 0.01m, IsPaid = true,
            Status = PaymentStatus.AguardandoEntregaInGame
        });
        await db.SaveChangesAsync();

        var result = await service.RejectPendingTradeAsync(server.Id, 9701, null);

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Disputa, result.Order!.Status);
    }

    // ── GetActiveItemOffersAsync ─────────────────────────────────────

    [Fact]
    public async Task GetActiveItemOffers_ReturnsOnlyActiveConfirmedOffersForServer()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller-o1", UserName = "SellerO1" });
        var server = new TibiaServer { Id = 10001, Name = "OfferServer", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ItemOffers.AddRange(
            // Valid
            new ItemOffer { Id = 10001, ServerId = 10001, ItemKey = "CRYSTAL COIN", ItemName = "Crystal Coin", Quantity = 10, UnitPrice = 0.001m, PricingGateway = "btc", SellerUserId = "seller-o1", IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            // Inactive — should be excluded
            new ItemOffer { Id = 10002, ServerId = 10001, ItemKey = "CRYSTAL COIN", ItemName = "Crystal Coin", Quantity = 5, UnitPrice = 0.0005m, PricingGateway = "btc", SellerUserId = "seller-o1", IsActive = false, CreatedAt = DateTime.UtcNow },
            // Active — now shows (no inventory pre-validation)
            new ItemOffer { Id = 10003, ServerId = 10001, ItemKey = "CRYSTAL COIN", ItemName = "Crystal Coin", Quantity = 3, UnitPrice = 0.0008m, PricingGateway = "btc", SellerUserId = "seller-o1", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetActiveItemOffersAsync(10001, "crystal coin");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.OfferId == 10001);
        Assert.Contains(result, r => r.OfferId == 10003);
    }

    [Fact]
    public async Task GetActiveItemOffers_ExcludesOffersFromOtherServers()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller-o2", UserName = "SellerO2" });
        db.Servers.AddRange(
            new TibiaServer { Id = 10101, Name = "ServerA", IsActive = true },
            new TibiaServer { Id = 10102, Name = "ServerB", IsActive = true });
        await db.SaveChangesAsync();

        db.ItemOffers.AddRange(
            new ItemOffer { Id = 10101, ServerId = 10101, ItemKey = "PLATINUM COIN", ItemName = "Platinum Coin", Quantity = 10, UnitPrice = 0.001m, PricingGateway = "btc", SellerUserId = "seller-o2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ItemOffer { Id = 10102, ServerId = 10102, ItemKey = "PLATINUM COIN", ItemName = "Platinum Coin", Quantity = 20, UnitPrice = 0.002m, PricingGateway = "btc", SellerUserId = "seller-o2", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetActiveItemOffersAsync(10101, "platinum coin");

        Assert.Single(result);
        Assert.Equal(10101, result[0].OfferId);
    }

    [Fact]
    public async Task GetActiveItemOffers_NormalizesItemKeyForQuery()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller-o3", UserName = "SellerO3" });
        var server = new TibiaServer { Id = 10201, Name = "NormServer", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ItemOffers.Add(new ItemOffer
        {
            Id = 10201, ServerId = 10201, ItemKey = "BOOTS OF HASTE",
            ItemName = "Boots of Haste", Quantity = 2, UnitPrice = 0.05m,
            PricingGateway = "btc", SellerUserId = "seller-o3",
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Call with mixed-case and extra spaces
        var result1 = await service.GetActiveItemOffersAsync(10201, "  Boots of Haste  ");
        var result2 = await service.GetActiveItemOffersAsync(10201, "boots of haste");

        Assert.Single(result1);
        Assert.Single(result2);
    }

    [Fact]
    public async Task GetActiveItemOffers_SortsByCreatedAtDesc_ByDefault()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller-o4", UserName = "SellerO4" });
        var server = new TibiaServer { Id = 10301, Name = "SortServer1", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var base_ = DateTime.UtcNow;
        db.ItemOffers.AddRange(
            new ItemOffer { Id = 10301, ServerId = 10301, ItemKey = "TIBIA COIN", ItemName = "Tibia Coin", Quantity = 5, UnitPrice = 0.003m, PricingGateway = "btc", SellerUserId = "seller-o4", IsActive = true, CreatedAt = base_.AddMinutes(-10) },
            new ItemOffer { Id = 10302, ServerId = 10301, ItemKey = "TIBIA COIN", ItemName = "Tibia Coin", Quantity = 3, UnitPrice = 0.001m, PricingGateway = "btc", SellerUserId = "seller-o4", IsActive = true, CreatedAt = base_.AddMinutes(-1) },
            new ItemOffer { Id = 10303, ServerId = 10301, ItemKey = "TIBIA COIN", ItemName = "Tibia Coin", Quantity = 7, UnitPrice = 0.002m, PricingGateway = "btc", SellerUserId = "seller-o4", IsActive = true, CreatedAt = base_.AddMinutes(-5) }
        );
        await db.SaveChangesAsync();

        var result = await service.GetActiveItemOffersAsync(10301, "tibia coin");

        Assert.Equal(3, result.Count);
        // Newest first: 10302, 10303, 10301
        Assert.Equal(10302, result[0].OfferId);
        Assert.Equal(10303, result[1].OfferId);
        Assert.Equal(10301, result[2].OfferId);
    }

    [Fact]
    public async Task GetActiveItemOffers_SortsByUnitPriceAsc_WhenSortByPriceTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        db.Users.Add(new ApplicationUser { Id = "seller-o5", UserName = "SellerO5" });
        var server = new TibiaServer { Id = 10401, Name = "SortServer2", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var base_ = DateTime.UtcNow;
        db.ItemOffers.AddRange(
            new ItemOffer { Id = 10401, ServerId = 10401, ItemKey = "MAGIC SWORD", ItemName = "Magic Sword", Quantity = 1, UnitPrice = 0.005m, PricingGateway = "btc", SellerUserId = "seller-o5", IsActive = true, CreatedAt = base_.AddMinutes(-10) },
            new ItemOffer { Id = 10402, ServerId = 10401, ItemKey = "MAGIC SWORD", ItemName = "Magic Sword", Quantity = 1, UnitPrice = 0.001m, PricingGateway = "btc", SellerUserId = "seller-o5", IsActive = true, CreatedAt = base_.AddMinutes(-5) },
            new ItemOffer { Id = 10403, ServerId = 10401, ItemKey = "MAGIC SWORD", ItemName = "Magic Sword", Quantity = 1, UnitPrice = 0.003m, PricingGateway = "btc", SellerUserId = "seller-o5", IsActive = true, CreatedAt = base_.AddMinutes(-1) }
        );
        await db.SaveChangesAsync();

        var result = await service.GetActiveItemOffersAsync(10401, "magic sword", sortByPrice: true);

        Assert.Equal(3, result.Count);
        // Cheapest first: 10402 (0.001), 10403 (0.003), 10401 (0.005)
        Assert.Equal(10402, result[0].OfferId);
        Assert.Equal(10403, result[1].OfferId);
        Assert.Equal(10401, result[2].OfferId);
    }

    [Fact]
    public async Task GetActiveItemOffers_ReturnsEmpty_WhenNoOffersExist()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db);

        var server = new TibiaServer { Id = 10501, Name = "EmptyOfferServer", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.GetActiveItemOffersAsync(10501, "nonexistent item");

        Assert.Empty(result);
    }
}
