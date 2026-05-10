using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

/// <summary>
/// Tests for API key authentication and cross-server authorization in
/// /api/v1/server/* endpoints.
///
/// Covers:
///   - All protected endpoints return 401 when key is missing
///   - All protected endpoints return 401 when key is invalid (random string)
///   - All protected endpoints return 401 when key belongs to an inactive server
///   - GET /offers returns only offers scoped to the authenticated server
///   - GET /offers sorts by unit price ascending (cheapest first)
/// </summary>
public class ServerIntegrationEndpointsAuthorizationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ServerIntegrationEndpointsAuthorizationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    public static IEnumerable<object[]> ProtectedGetEndpoints() =>
    [
        ["/api/v1/server/ping"],
        ["/api/v1/server/trades/pending"],
        ["/api/v1/server/offers?itemKey=sword"],
    ];

    public static IEnumerable<object[]> ProtectedPostEndpoints() =>
    [
        ["/api/v1/server/trades/confirm"],
        ["/api/v1/server/trades/reject"],

        ["/api/v1/server/generate-login-link"],
        ["/api/v1/server/delivery/log"],
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Missing API key → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ProtectedGetEndpoints))]
    public async Task GetEndpoints_Return401_WhenApiKeyIsMissing(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedPostEndpoints))]
    public async Task PostEndpoints_Return401_WhenApiKeyIsMissing(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(url, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Invalid (random) API key → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ProtectedGetEndpoints))]
    public async Task GetEndpoints_Return401_WhenApiKeyIsInvalid(string url)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "svmk_invalid_key_that_does_not_exist_in_db");

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedPostEndpoints))]
    public async Task PostEndpoints_Return401_WhenApiKeyIsInvalid(string url)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "svmk_invalid_key_that_does_not_exist_in_db");

        var response = await client.PostAsJsonAsync(url, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Valid key but inactive server → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingTrades_Returns401_WhenServerIsInactive()
    {
        const int serverId = 20001;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            // Create server as active first so key can be generated
            db.Servers.Add(new TibiaServer { Id = serverId, Name = "About To Go Inactive", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "inactive-server-test");
            apiKey = created.RawKey!;

            // Now deactivate the server
            var server = await db.Servers.FindAsync(serverId);
            server!.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/trades/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_Returns401_WhenServerIsInactive()
    {
        const int serverId = 20002;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Offers Inactive Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "offers-inactive-test");
            apiKey = created.RawKey!;

            var server = await db.Servers.FindAsync(serverId);
            server!.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/offers?itemKey=sword");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmTrade_Returns401_WhenServerIsInactive()
    {
        const int serverId = 20003;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Confirm Inactive", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "confirm-inactive-test");
            apiKey = created.RawKey!;

            var server = await db.Servers.FindAsync(serverId);
            server!.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/confirm", new { tradeId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Revoked (inactive) API key → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingTrades_Returns401_WhenApiKeyIsRevoked()
    {
        const int serverId = 20101;
        string apiKey;
        int keyId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "RevokedKey Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "revoked-key-test");
            apiKey = created.RawKey!;
            keyId = created.Entity!.Id;

            await apiKeyService.RevokeKeyAsync(keyId);
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/trades/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /offers — cross-server isolation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOffers_ReturnsOnlyOffersForAuthenticatedServer()
    {
        const int serverAId = 20201;
        const int serverBId = 20202;
        string apiKeyA;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.AddRange(
                new TibiaServer { Id = serverAId, Name = "Cross-Server A", IsActive = true },
                new TibiaServer { Id = serverBId, Name = "Cross-Server B", IsActive = true });

            db.Users.Add(new ApplicationUser { Id = "seller-cross", UserName = "SellerCross" });

            db.ItemOffers.AddRange(
                new ItemOffer
                {
                    Id = 20201, ServerId = serverAId, ItemKey = "DRAGON LANCE",
                    ItemName = "Dragon Lance", Quantity = 2, UnitPrice = 0.01m,
                    PricingGateway = "btc", SellerUserId = "seller-cross",
                    IsActive = true, CreatedAt = DateTime.UtcNow
                },
                new ItemOffer
                {
                    Id = 20202, ServerId = serverBId, ItemKey = "DRAGON LANCE",
                    ItemName = "Dragon Lance", Quantity = 1, UnitPrice = 0.008m,
                    PricingGateway = "btc", SellerUserId = "seller-cross",
                    IsActive = true, CreatedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverAId, "cross-server-offers-test");
            apiKeyA = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKeyA);

        var response = await client.GetAsync("/api/v1/server/offers?itemKey=dragon%20lance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<OffersEnvelopeCrossServer>();
        Assert.NotNull(payload);
        Assert.Equal(serverAId, payload!.ServerId);
        Assert.Single(payload.Offers);
        Assert.Equal(20201, payload.Offers[0].OfferId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /offers — sort by price ascending
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOffers_ReturnsSortedByUnitPriceAscending()
    {
        const int serverId = 20301;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Price Sort Server", IsActive = true });
            db.Users.Add(new ApplicationUser { Id = "seller-sort", UserName = "SellerSort" });

            db.ItemOffers.AddRange(
                new ItemOffer { Id = 20301, ServerId = serverId, ItemKey = "GOLDEN ARMOR", ItemName = "Golden Armor", Quantity = 1, UnitPrice = 0.009m, PricingGateway = "btc", SellerUserId = "seller-sort", IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new ItemOffer { Id = 20302, ServerId = serverId, ItemKey = "GOLDEN ARMOR", ItemName = "Golden Armor", Quantity = 1, UnitPrice = 0.003m, PricingGateway = "btc", SellerUserId = "seller-sort", IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new ItemOffer { Id = 20303, ServerId = serverId, ItemKey = "GOLDEN ARMOR", ItemName = "Golden Armor", Quantity = 1, UnitPrice = 0.006m, PricingGateway = "btc", SellerUserId = "seller-sort", IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
            );
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "price-sort-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/offers?itemKey=golden%20armor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<OffersEnvelopeCrossServer>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload!.Offers.Count);
        Assert.Equal(20302, payload.Offers[0].OfferId); // 0.003 - cheapest
        Assert.Equal(20303, payload.Offers[1].OfferId); // 0.006
        Assert.Equal(20301, payload.Offers[2].OfferId); // 0.009 - most expensive
    }
}

// DTOs local para desserializar as respostas
file record OfferItemDto(int OfferId, string ItemKey, decimal UnitPrice);
file record OffersEnvelopeCrossServer(int ServerId, string ItemKey, List<OfferItemDto> Offers, string Message);
