using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Confirmai.Tests;

public class C43BMockFillTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
        => TestDbContextFactory.CreateInMemoryFactory($"c43b-{Guid.NewGuid()}");

    private static MockFillService Create(IDbContextFactory<AppDbContext> factory, bool enabled)
        => new(factory, Options.Create(new Confirmai.Configuration.TestingOptions { MockFillEnabled = enabled }),
               TestLogService.Create(factory));

    private static ClaimsPrincipal Admin() => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1"), new Claim(ClaimTypes.Role, "admin") }, "test"));

    private static async Task<int> SeedEventAsync(IDbContextFactory<AppDbContext> factory, bool past = false, bool active = true)
    {
        await using var db = factory.CreateDbContext();
        db.Groups.Add(new Group { Id = 1, Name = "G" });
        db.Events.Add(new Event
        {
            GroupId = 1, Sport = Sport.Futsal, MaxPlayers = 4, MaxGoalkeepers = 1,
            StartsAt = DateTime.UtcNow.AddDays(past ? -1 : 1), IsActive = active,
        });
        await db.SaveChangesAsync();
        return 1;
    }

    [Fact]
    public async Task Fill_RefusesWithoutFlag()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory);
        var svc = Create(factory, enabled: false);
        var (status, _) = await svc.FillAsync(eventId, Admin());
        Assert.Equal(MockFillStatus.Disabled, status);
    }

    [Fact]
    public async Task Fill_RefusesNonAdmin()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory);
        var svc = Create(factory, enabled: true);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "u-1") }, "test"));
        var (status, _) = await svc.FillAsync(eventId, user);
        Assert.Equal(MockFillStatus.Forbidden, status);
    }

    [Fact]
    public async Task Fill_RefusesPastOrCancelled()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory, past: true);
        var svc = Create(factory, enabled: true);
        var (status, _) = await svc.FillAsync(eventId, Admin());
        Assert.Equal(MockFillStatus.EventFinished, status);
    }

    [Fact]
    public async Task Fill_FillsToCapacity_AndIsIdempotent()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory);
        var svc = Create(factory, enabled: true);

        var (status, added) = await svc.FillAsync(eventId, Admin());
        Assert.Equal(MockFillStatus.Filled, status);
        Assert.Equal(4, added); // 1 GK + 3 linha (MaxPlayers=4, MaxGK=1)

        await using var db = factory.CreateDbContext();
        var confs = await db.EventConfirmations.Where(c => c.EventId == eventId).ToListAsync();
        Assert.Equal(4, confs.Count);
        Assert.Single(confs, c => c.Position == FutsalPosition.Goalkeeper);
        Assert.All(confs, c =>
        {
            Assert.Equal(0m, c.ChargedPrice);
            Assert.Equal(0m, c.PlatformFeeAmount);
            Assert.Equal(EventConfirmationPaymentStatus.Paid, c.PaymentStatus);
        });
        Assert.Equal(4, await db.GroupMembers.CountAsync(m => m.GroupId == 1));
        Assert.Equal(4, await db.Users.CountAsync(u => u.IsMock));

        var (status2, added2) = await svc.FillAsync(eventId, Admin());
        Assert.Equal(MockFillStatus.NothingToFill, status2);
        Assert.Equal(0, added2);
        Assert.Single(await db.Logs.Where(l => l.EventType == Confirmai.Services.Core.AuditEvents.EventMockFilled).ToListAsync());
    }

    [Fact]
    public async Task Clear_RemovesOnlyMockConfirmations_AndAudits()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory);
        var svc = Create(factory, enabled: true);
        await svc.FillAsync(eventId, Admin());

        var (status, removed) = await svc.ClearAsync(eventId, Admin());
        Assert.Equal(MockFillStatus.Cleared, status);
        Assert.Equal(4, removed);

        await using var db = factory.CreateDbContext();
        Assert.Empty(await db.EventConfirmations.Where(c => c.EventId == eventId).ToListAsync());
        Assert.Equal(4, await db.Users.CountAsync(u => u.IsMock)); // pool fica
        Assert.Empty(await db.GroupMembers.Where(m => m.GroupId == 1 && m.User.IsMock).ToListAsync());
        Assert.Single(await db.Logs.Where(l => l.EventType == Confirmai.Services.Core.AuditEvents.EventMockCleared).ToListAsync());
    }

    [Fact]
    public async Task Clear_KeepsMockMembership_WhenConfirmedInAnotherEventOfGroup()
    {
        var factory = CreateFactory();
        var eventId = await SeedEventAsync(factory);
        int otherId;
        await using (var db = factory.CreateDbContext())
        {
            var other = new Event
            {
                GroupId = 1, Sport = Sport.Futsal, MaxPlayers = 4, MaxGoalkeepers = 1,
                StartsAt = DateTime.UtcNow.AddDays(2), IsActive = true,
            };
            db.Events.Add(other);
            await db.SaveChangesAsync();
            otherId = other.Id;
        }
        var svc = Create(factory, enabled: true);
        await svc.FillAsync(eventId, Admin());
        await svc.FillAsync(otherId, Admin());

        await svc.ClearAsync(eventId, Admin());

        await using var check = factory.CreateDbContext();
        Assert.Equal(4, await check.EventConfirmations.CountAsync(c => c.EventId == otherId));
        Assert.Equal(4, await check.GroupMembers.CountAsync(m => m.GroupId == 1 && m.User.IsMock));
    }
}
