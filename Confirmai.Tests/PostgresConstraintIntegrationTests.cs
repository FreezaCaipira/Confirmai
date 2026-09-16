using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Confirmai.Tests;

/// <summary>
/// Proves the integration-test infrastructure runs on real Postgres, where
/// foreign keys are enforced — the class of bug that InMemory let through in
/// #107 (audit log written with an external ProviderKey instead of a real
/// AspNetUsers.Id silently "worked" until Postgres rejected it).
/// </summary>
public class PostgresConstraintIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PostgresConstraintIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuditAsync_WithNonExistentUserId_ThrowsForeignKeyViolation()
    {
        using var scope = _factory.Services.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<LogService>();

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            logService.AuditAsync(
                eventType: "test.fk_violation",
                entityType: "User",
                entityId: "1",
                message: "audit com UserId que nao existe em AspNetUsers (bug da #107)",
                actorUserId: "google-provider-key-1234567890"));

        var pg = ex.InnerException as PostgresException;
        Assert.NotNull(pg);
        Assert.Equal("23503", pg.SqlState);
    }

    [Fact]
    public async Task AuditAsync_WithValidUserId_Persists()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logService = scope.ServiceProvider.GetRequiredService<LogService>();

        var user = new ApplicationUser
        {
            UserName = $"fk-valid-{Guid.NewGuid():N}@test.local",
            Email = $"fk-valid-{Guid.NewGuid():N}@test.local",
            EmailConfirmed = true,
        };
        var create = await userManager.CreateAsync(user);
        Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(e => e.Description)));

        await logService.AuditAsync(
            eventType: "test.fk_ok",
            entityType: "User",
            entityId: user.Id,
            message: "audit com UserId valido",
            actorUserId: user.Id);

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Logs.FirstOrDefaultAsync(l => l.EventType == "test.fk_ok" && l.UserId == user.Id);

        Assert.NotNull(row);
        Assert.Equal(user.Id, row.UserId);
    }
}
