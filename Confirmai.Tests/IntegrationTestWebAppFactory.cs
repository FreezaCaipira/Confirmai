using Confirmai.Services.Crypto;
using Confirmai.Services.Admin;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Confirmai.Tests;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private string? _schemaName;
    private string? _connectionString;
    private string? _adminConnectionString;

    /// <summary>
    /// Lazily creates the per-factory citest_* schema in the shared Postgres
    /// backend and applies migrations. Called inside ConfigureWebHost (sync)
    /// because the app's own startup MigrateAsync runs in the entry-point
    /// thread after the test host is captured — it cannot be relied on.
    /// Derived factories (WithWebHostBuilder) reuse the same schema, matching
    /// the old shared InMemory database semantics.
    /// </summary>
    private void EnsurePostgresSchema()
    {
        if (_connectionString is not null)
        {
            return;
        }

        var backend = PostgresTestBackend.Get();
        _adminConnectionString = backend.ConnectionString;
        _schemaName = $"citest_{DateTime.UtcNow:yyMMddHHmm}_{Guid.NewGuid():N}";

        using (var conn = new NpgsqlConnection(_adminConnectionString))
        {
            conn.Open();
            using var cmd = new NpgsqlCommand($"CREATE SCHEMA \"{_schemaName}\"", conn);
            cmd.ExecuteNonQuery();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            SearchPath = _schemaName
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        using var db = new AppDbContext(options);
        db.Database.Migrate();
    }

    /// <summary>
    /// Drops the per-factory schema after the host stops. Leftover citest_*
    /// schemas from killed runs are swept by PostgresTestBackend.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_schemaName is not null && _adminConnectionString is not null)
        {
            try
            {
                using var conn = new NpgsqlConnection(_adminConnectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE", conn);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Best-effort cleanup; stale schemas are swept by PostgresTestBackend.
            }

            _schemaName = null;
            _connectionString = null;
            _adminConnectionString = null;
        }
    }

    /// <summary>
    /// Seeds a minimal Futsal group + event. Returns the created event ID.
    /// When <paramref name="lineupConfirmed"/> is true, sets LineupConfirmedAt
    /// so the confirmed-lineup state is rendered.
    /// </summary>
    public async Task<int> SeedFutsalEventAsync(
        bool lineupConfirmed = false,
        string creatorId = "test-creator-1")
    {
        await EnsureUserAsync(creatorId);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = new Confirmai.Models.Group
        {
            Name           = "Racha de Teste",
            Sport          = Confirmai.Enums.Sport.Futsal,
            City           = "Pouso Alegre",
            StateCode      = "MG",
            CreatedByUserId = creatorId
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var ev = new Confirmai.Models.Event
        {
            GroupId         = group.Id,
            Sport           = Confirmai.Enums.Sport.Futsal,
            Location        = "Quadra de Teste",
            StartsAt        = DateTime.UtcNow.AddDays(1),
            MaxPlayers      = 12,
            MaxGoalkeepers  = 2,
            IsActive        = true,
            CreatedByUserId = creatorId,
            LineupConfirmedAt = lineupConfirmed ? DateTime.UtcNow : null
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return ev.Id;
    }

    /// <summary>
    /// Seeds a Futsal group and adds the given user as Admin. Returns the group ID.
    /// </summary>
    public async Task<int> SeedGroupWithAdminAsync(string adminUserId, string groupName = "Grupo de Teste")
    {
        await EnsureUserAsync(adminUserId);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = new Confirmai.Models.Group
        {
            Name            = groupName,
            Sport           = Confirmai.Enums.Sport.Futsal,
            City            = "Pouso Alegre",
            StateCode       = "MG",
            CreatedByUserId = adminUserId,
            InviteCode      = Guid.NewGuid().ToString("N")[..8].ToUpper(),
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        db.GroupMembers.Add(new Confirmai.Models.GroupMember
        {
            GroupId = group.Id,
            UserId  = adminUserId,
            Role    = Confirmai.Enums.GroupMemberRole.Admin,
        });
        await db.SaveChangesAsync();

        return group.Id;
    }

    /// <summary>
    /// Inserts a minimal ApplicationUser with the given Id when absent.
    /// Postgres enforces FK on UserId columns — seeds that used to get away
    /// with bare ids under InMemory must call this first.
    /// </summary>
    public async Task EnsureUserAsync(string userId, string? email = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email ?? $"{userId}@test.local",
            NormalizedUserName = (email ?? $"{userId}@test.local").ToUpperInvariant(),
            Email = email ?? $"{userId}@test.local",
            NormalizedEmail = (email ?? $"{userId}@test.local").ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures the LuaDeliveryEnabled feature flag is set to "true" for tests
    /// that exercise the /api/v1/server/trades/* endpoints. Safe to call
    /// multiple times � it inserts the row only when missing.
    /// </summary>
    public async Task EnsureLuaDeliveryEnabledAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var setting = await db.AppSettings.FindAsync(AdminSettingsService.LuaDeliveryEnabledKey);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = AdminSettingsService.LuaDeliveryEnabledKey,
                Value = "true"
            });
            await db.SaveChangesAsync();
        }
        else if (!string.Equals(setting.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            setting.Value = "true";
            await db.SaveChangesAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        EnsurePostgresSchema();

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["AdminSeed:Email"] = "admin@test.local",
                ["AdminSeed:Password"] = "Admin123!Aa",
                ["AdminSeed:FullName"] = "Admin Test",
                ["BtcPay:WebhookSecret"] = "expected-secret",
                ["BtcPay:WebhookMaxBodyBytes"] = "256"
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptors = services
                .Where(service => service.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();

            foreach (var descriptor in dbContextOptionsDescriptors)
                services.Remove(descriptor);

            var dbContextOptionsConfigType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var dbContextOptionsConfigDescriptors = services
                .Where(service => service.ServiceType == dbContextOptionsConfigType)
                .ToList();

            foreach (var descriptor in dbContextOptionsConfigDescriptors)
                services.Remove(descriptor);

            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(AppDbContext));

            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services.RemoveAll<BitcoinQuoteService>();
            services.RemoveAll<CryptoQuoteService>();

            var quoteHttpFactory = new StubHttpClientFactory(_ =>
                HttpTestResponses.Json("{\"bitcoin\":{\"brl\":500000,\"usd\":100000},\"ethereum\":{\"brl\":15000,\"usd\":3000},\"solana\":{\"brl\":800,\"usd\":160}}"));

            services.AddSingleton<BitcoinQuoteService>(provider =>
                new BitcoinQuoteService(
                    quoteHttpFactory,
                    provider.GetRequiredService<IConfiguration>(),
                    provider.GetService<IServiceScopeFactory>()));

            services.AddSingleton<CryptoQuoteService>(provider =>
                new CryptoQuoteService(
                    quoteHttpFactory,
                    provider.GetRequiredService<IConfiguration>(),
                    provider.GetService<IServiceScopeFactory>()));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ =>
                    {
                    });
        });
    }
}



