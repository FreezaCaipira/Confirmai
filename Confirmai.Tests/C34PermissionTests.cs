using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

/// <summary>
/// C34 Fase 3b — testes de permissão dos FALTA restantes do mapa C33:
/// UC-O-24/26/27 (guarda de editar/cancelar evento) e UC-A-08/09/12
/// (lockout e papel venue_manager), agora executados/auditados no service layer.
/// </summary>
public class C34PermissionTests
{
    private static LogService NewLog(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<LogService>.Instance);

    private static EventCancellationService NewCancellation(IDbContextFactory<AppDbContext> factory)
        => new(factory,
               new EventNotificationService(factory, Mock.Of<IEmailSender>(),
                   NullLogger<EventNotificationService>.Instance),
               NewLog(factory));

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
        => new(new Mock<IUserStore<ApplicationUser>>().Object,
               null!, null!, null!, null!, null!, null!, null!, null!);

    private static async Task<int> SeedEventAsync(AppDbContext db, string creatorId)
    {
        db.Groups.Add(new Group { Id = 900, Name = "G", CreatedByUserId = creatorId });
        var ev = new Event
        {
            GroupId = 900, CreatedByUserId = creatorId, Sport = Enums.Sport.Poker,
            StartsAt = DateTime.UtcNow.AddDays(1), IsActive = true,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    // ---- UC-O-24 / UC-O-27: cancelar evento (futsal/poker) ----

    [Fact]
    public async Task CancelEvent_NonCreatorNonAdmin_Forbidden_EventStaysActive()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var eventId = await SeedEventAsync(db, "creator-1");
        var svc = NewCancellation(factory);

        var result = await svc.CancelAsync(eventId, "intruder-9", isAdmin: false);

        Assert.Equal(EventCancellationResult.Forbidden, result);
        await using var check = await factory.CreateDbContextAsync();
        Assert.True(check.Events.Find(eventId)!.IsActive);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(verify.Logs.Where(l => l.EventType == AuditEvents.EventCancelled));
    }

    [Fact]
    public async Task CancelEvent_Creator_Succeeds_AndAudits()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var eventId = await SeedEventAsync(db, "creator-1");
        var svc = NewCancellation(factory);

        var result = await svc.CancelAsync(eventId, "creator-1", isAdmin: false);

        Assert.Equal(EventCancellationResult.Success, result);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.False(verify.Events.Find(eventId)!.IsActive);
        var audit = Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.EventCancelled));
        Assert.Equal("creator-1", audit.UserId);
        Assert.Equal(eventId.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CancelEvent_SysAdmin_Succeeds_OnNonOwnedEvent()
    {
        // Poker agora permite cancelamento por sysadmin (antes: só criador,
        // divergente do futsal). Guarda unificada no service.
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var eventId = await SeedEventAsync(db, "creator-1");
        var svc = NewCancellation(factory);

        var result = await svc.CancelAsync(eventId, "sysadmin-1", isAdmin: true);

        Assert.Equal(EventCancellationResult.Success, result);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.False(verify.Events.Find(eventId)!.IsActive);
    }

    [Fact]
    public async Task CancelEvent_MissingEvent_NotFound()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var svc = NewCancellation(factory);

        Assert.Equal(EventCancellationResult.NotFound,
            await svc.CancelAsync(999_999, "anyone", isAdmin: true));
    }

    [Theory]
    [InlineData("creator-1", false, true)]   // criador
    [InlineData("other-1",    true,  true)]  // sysadmin
    [InlineData("other-1",    false, false)] // terceiro
    [InlineData(null,         false, false)] // anônimo
    public void CanManage_CreatorOrAdminOnly(string? userId, bool isAdmin, bool expected)
    {
        var ev = new Event { CreatedByUserId = "creator-1" };
        Assert.Equal(expected, EventCancellationService.CanManage(ev, userId, isAdmin));
    }

    // ---- UC-A-08 / UC-A-09: lockout de usuário ----

    [Fact]
    public async Task SetLockout_Lock_WritesAudit_WithAdminAsActor()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.SetLockoutEndDateAsync(target, It.IsAny<DateTimeOffset?>()))
           .ReturnsAsync(IdentityResult.Success);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var result = await svc.SetLockoutAsync("target-1", locked: true, actorUserId: "admin-1");

        Assert.Equal(AdminUserMutationResult.Success, result);
        mgr.Verify(m => m.SetLockoutEndDateAsync(target, DateTimeOffset.MaxValue), Times.Once);
        await using var verify = await factory.CreateDbContextAsync();
        var audit = Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.UserLockedOut));
        Assert.Equal("admin-1", audit.UserId);   // ator = admin, não o alvo
        Assert.Equal("target-1", audit.EntityId);
    }

    [Fact]
    public async Task SetLockout_Unlock_WritesAudit()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.SetLockoutEndDateAsync(target, null))
           .ReturnsAsync(IdentityResult.Success);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var result = await svc.SetLockoutAsync("target-1", locked: false, actorUserId: "admin-1");

        Assert.Equal(AdminUserMutationResult.Success, result);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.UserUnlocked));
    }

    [Fact]
    public async Task SetLockout_UnknownUser_NotFound_NoAudit()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var result = await svc.SetLockoutAsync("ghost", locked: true, actorUserId: "admin-1");

        Assert.Equal(AdminUserMutationResult.NotFound, result);
        mgr.Verify(m => m.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<DateTimeOffset?>()), Times.Never);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(verify.Logs);
    }

    // ---- UC-A-10: excluir usuário ----

    [Fact]
    public async Task DeleteUser_Success_AuditsWithAdminAsActor()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.DeleteAsync(target)).ReturnsAsync(IdentityResult.Success);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var (result, _) = await svc.DeleteUserAsync("target-1", "admin-1");

        Assert.Equal(AdminUserMutationResult.Success, result);
        await using var verify = await factory.CreateDbContextAsync();
        var audit = Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.UserDeleted));
        Assert.Equal("admin-1", audit.UserId);
        Assert.Equal("target-1", audit.EntityId);
    }

    // ---- UC-A-12: papel venue_manager ----

    [Fact]
    public async Task ToggleVenueManager_Grant_AuditsRoleAssigned()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.IsInRoleAsync(target, "venue_manager")).ReturnsAsync(false);
        mgr.Setup(m => m.AddToRoleAsync(target, "venue_manager"))
           .ReturnsAsync(IdentityResult.Success);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var (result, isManager, _) = await svc.ToggleVenueManagerAsync("target-1", "admin-1");

        Assert.Equal(AdminUserMutationResult.Success, result);
        Assert.True(isManager);
        mgr.Verify(m => m.AddToRoleAsync(target, "venue_manager"), Times.Once);
        await using var verify = await factory.CreateDbContextAsync();
        var audit = Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.UserRoleAssigned));
        Assert.Equal("admin-1", audit.UserId);
    }

    [Fact]
    public async Task ToggleVenueManager_Revoke_AuditsRoleRemoved()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.IsInRoleAsync(target, "venue_manager")).ReturnsAsync(true);
        mgr.Setup(m => m.RemoveFromRoleAsync(target, "venue_manager"))
           .ReturnsAsync(IdentityResult.Success);
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var (result, isManager, _) = await svc.ToggleVenueManagerAsync("target-1", "admin-1");

        Assert.Equal(AdminUserMutationResult.Success, result);
        Assert.False(isManager);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Single(verify.Logs.Where(l => l.EventType == AuditEvents.UserRoleRemoved));
    }

    [Fact]
    public async Task ToggleVenueManager_FailedResult_NoAudit()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var target = new ApplicationUser { Id = "target-1", UserName = "t1" };
        var mgr = MockUserManager();
        mgr.Setup(m => m.FindByIdAsync("target-1")).ReturnsAsync(target);
        mgr.Setup(m => m.IsInRoleAsync(target, "venue_manager")).ReturnsAsync(false);
        mgr.Setup(m => m.AddToRoleAsync(target, "venue_manager"))
           .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));
        var svc = new AdminUserService(mgr.Object, NewLog(factory));

        var (result, _, errors) = await svc.ToggleVenueManagerAsync("target-1", "admin-1");

        Assert.Equal(AdminUserMutationResult.Failed, result);
        Assert.Equal("boom", errors);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(verify.Logs);
    }

    // ---- Convenção: toda página /admin exige papel admin ----

    [Fact]
    public void AdminPages_AllRequireAdminRoleAttribute()
    {
        var pagesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "Pages", "Admin");
        Assert.True(Directory.Exists(pagesDir), $"Pages/Admin não encontrado em {pagesDir}");

        var offenders = Directory.GetFiles(pagesDir, "*.razor")
            .Where(f => File.ReadAllText(f).Contains("@page "))
            .Where(f => !File.ReadAllText(f).Contains("[Authorize(Roles = \"admin\")]"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(offenders.Count == 0,
            "Páginas admin sem [Authorize(Roles = \"admin\")]: " + string.Join(", ", offenders));
    }
}
