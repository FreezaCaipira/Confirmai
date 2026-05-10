using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for the two newer endpoints added in the Tibia integration sprint:
///   POST /api/v1/server/generate-login-link
///   POST /api/v1/server/delivery/log
/// </summary>
public class ServerIntegrationEndpointsNewEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ServerIntegrationEndpointsNewEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/server/generate-login-link
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateLoginLink_ReturnsLoginUrlAndExpiryForValidRequest()
    {
        const int serverId = 8001;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "LoginLink Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "login-link-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/generate-login-link",
            new { playerName = "Knight Alpha" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginLinkResponse>();
        Assert.NotNull(payload);
        Assert.Contains("/auth/game-login?token=", payload!.LoginUrl);
        Assert.Equal("Knight Alpha", payload.PlayerName);
        Assert.Equal(600, payload.ExpiresInSeconds);

        // Token must be persisted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenHex = payload.LoginUrl.Split("token=")[1];
        var record = verifyDb.GameLoginTokens.FirstOrDefault(t => t.Token == tokenHex);
        Assert.NotNull(record);
        Assert.Equal(serverId, record!.ServerId);
        Assert.Equal("Knight Alpha", record.PlayerName);
        Assert.False(record.IsUsed);
        Assert.True(record.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateLoginLink_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/server/generate-login-link",
            new { playerName = "Anyone" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateLoginLink_ReturnsBadRequest_WhenPlayerNameIsEmpty()
    {
        const int serverId = 8002;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "LoginLink Validation Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "login-link-validation-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/generate-login-link",
            new { playerName = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateLoginLink_InvalidatesPreviousUnusedTokenForSamePlayer()
    {
        const int serverId = 8003;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "LoginLink Stale Server", IsActive = true });

            // Pre-seed a stale (unused, not expired) token
            db.GameLoginTokens.Add(new GameLoginToken
            {
                ServerId = serverId,
                PlayerName = "Druid Beta",
                Token = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "login-link-stale-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/generate-login-link",
            new { playerName = "Druid Beta" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Old token must be deleted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stale = verifyDb.GameLoginTokens
            .FirstOrDefault(t => t.Token == "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899");
        Assert.Null(stale);

        // New token exists
        var remaining = verifyDb.GameLoginTokens.Where(t => t.ServerId == serverId && t.PlayerName == "Druid Beta").ToList();
        Assert.Single(remaining);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/server/delivery/log
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeliveryLog_PersistsAuditEntryAndReturnsLoggedTrue()
    {
        const int serverId = 9001;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "DeliveryLog Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "delivery-log-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = "DeliverItem",
            actorPlayerName = "Knight Alpha",
            targetPlayerName = "Druid Beta",
            itemKey = "magic plate armor",
            itemName = "Magic Plate Armor",
            quantity = 1,
            success = true,
            detail = "Item entregue via depot"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DeliveryLogResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.Logged);
        Assert.True(payload.Id > 0);

        // Verify persisted entry
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = verifyDb.DeliveryAuditLogs.FirstOrDefault(d => d.Id == payload.Id);
        Assert.NotNull(entry);
        Assert.Equal(serverId, entry!.ServerId);
        Assert.Equal("DeliverItem", entry.EventType);
        Assert.Equal("Knight Alpha", entry.ActorPlayerName);
        Assert.Equal("Druid Beta", entry.TargetPlayerName);
        Assert.Equal("MAGIC PLATE ARMOR", entry.ItemKey); // uppercased
        Assert.Equal(1, entry.Quantity);
        Assert.True(entry.Success);
        Assert.Equal("Item entregue via depot", entry.Detail);
    }

    [Fact]
    public async Task DeliveryLog_PersistsFailureEntry_WhenSuccessIsFalse()
    {
        const int serverId = 9002;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "DeliveryLog Fail Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "delivery-log-fail-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = "SellerOffline",
            actorPlayerName = "Knight Alpha",
            success = false,
            detail = "Vendedor nao estava online"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DeliveryLogResponse>();
        Assert.NotNull(payload);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = verifyDb.DeliveryAuditLogs.FirstOrDefault(d => d.Id == payload!.Id);
        Assert.NotNull(entry);
        Assert.False(entry!.Success);
        Assert.Equal("SellerOffline", entry.EventType);
    }

    [Fact]
    public async Task DeliveryLog_LinksOrderIdWhenProvided()
    {
        const int serverId = 9003;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "DeliveryLog Order Server", IsActive = true });
            db.Users.Add(new ApplicationUser { Id = "seller-log", UserName = "SellerLog" });
            db.Products.Add(new Product { Id = 7001, Name = "Boots of Haste", Description = "Fast", Price = 0.01m, UserId = "seller-log" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 6001,
                ServerId = serverId,
                ProductId = 7001,
                UserId = "buyer-log",
                SellerId = "seller-log",
                Amount = 0.01m,
                PaymentId = "inv-log",
                IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 5001,
                ServerId = serverId,
                ProductId = 7001,
                BuyerId = "buyer-log",
                SellerId = "seller-log",
                Amount = 0.01m,
                IsPaid = true,
                PaymentId = 6001,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "delivery-log-order-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = "ConfirmTrade",
            orderId = 5001,
            actorPlayerName = "SellerLog",
            success = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DeliveryLogResponse>();
        Assert.NotNull(payload);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = verifyDb.DeliveryAuditLogs.FirstOrDefault(d => d.Id == payload!.Id);
        Assert.NotNull(entry);
        Assert.Equal(5001, entry!.OrderId);
    }

    [Fact]
    public async Task DeliveryLog_ReturnsBadRequest_WhenEventTypeIsEmpty()
    {
        const int serverId = 9004;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "DeliveryLog Validation Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "delivery-log-validation-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = "",
            actorPlayerName = "Knight Alpha"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeliveryLog_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = "DeliverItem",
            actorPlayerName = "Anyone"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeliveryLog_TruncatesEventTypeTo60Chars()
    {
        const int serverId = 9005;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "DeliveryLog Truncate Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "delivery-log-truncate-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var longEventType = new string('X', 80); // 80 chars — should be truncated to 60
        var response = await client.PostAsJsonAsync("/api/v1/server/delivery/log", new
        {
            eventType = longEventType,
            success = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DeliveryLogResponse>();
        Assert.NotNull(payload);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = verifyDb.DeliveryAuditLogs.FirstOrDefault(d => d.Id == payload!.Id);
        Assert.NotNull(entry);
        Assert.Equal(60, entry!.EventType.Length);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private DTOs
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class LoginLinkResponse
    {
        public string LoginUrl { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }

    private sealed class DeliveryLogResponse
    {
        public bool Logged { get; set; }
        public int Id { get; set; }
    }
}
