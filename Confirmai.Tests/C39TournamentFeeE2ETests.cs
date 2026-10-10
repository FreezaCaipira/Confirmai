using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.Poker;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C39-A F7 — a taxa percentual do torneio de poker, ponta a ponta:
/// inscricao carimba preco + %, o QR sai com entrada + taxa, o comprovante
/// e aprovado, a taxa acumula na aba do grupo e o repasse e aprovado.
/// Espelha <see cref="ManualPlatformFeeFlowE2ETests"/> (futsal, taxa fixa).
/// </summary>
public class C39TournamentFeeE2ETests
{
    private static byte[] FakeImage(int size = 1024)
    {
        var bytes = Enumerable.Range(0, size).Select(_ => (byte)0xFF).ToArray();
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF; // JPEG signature
        return bytes;
    }

    private sealed class TestInMemoryDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;
        public TestInMemoryDbContextFactory(string dbName) => _dbName = dbName;
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new AppDbContext(options);
        }
    }

    private static async Task<string> SeedSystemAdminAsync(AppDbContext db, string userId = "sysadmin-1")
    {
        var role = new IdentityRole("admin") { NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
        return userId;
    }

    private sealed record Wired(
        AppDbContext Db,
        TestInMemoryDbContextFactory Factory,
        PokerDetailService DetailSvc,
        PixProofUploadService UploadSvc,
        PlatformFeeLedgerService FeeLedger,
        AdminConfirmationService ConfirmSvc,
        PlatformFeeSettlementService SettlementSvc);

    private static Wired Wire(decimal feeMin = 5m, decimal feeMax = 10m)
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);

        var feeOptions = Options.Create(new FeeOptions
        {
            ManualPlatformFeeFixed = 0.75m,
            PokerFeePercentMin = feeMin,
            PokerFeePercentMax = feeMax,
        });
        var policy = new PlatformFeePolicy(feeOptions);
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        return new Wired(
            db, factory,
            new PokerDetailService(factory, policy),
            new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance, logService),
            new PlatformFeeLedgerService(factory, policy),
            new AdminConfirmationService(factory, logService,
                new PlatformFeeLedgerService(factory, policy), NullLogger<AdminConfirmationService>.Instance),
            new PlatformFeeSettlementService(factory, NullLogger<PlatformFeeSettlementService>.Instance,
                new UiTextService(new LanguagePreferenceService()), logService));
    }

    private static async Task<(int groupId, int eventId)> SeedPokerTournamentAsync(
        AppDbContext db, string organizerId, string playerId,
        decimal buyIn, decimal feePercent, DateTime? waivedUntil = null)
    {
        var group = new Group
        {
            Name = "Liga Poker", Sport = Sport.Poker,
            CreatedByUserId = organizerId, InviteCode = "POKER123",
            PlatformFeeWaivedUntil = waivedUntil,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.Users.Add(new ApplicationUser { Id = organizerId, UserName = "Org", PixKey = "org@pix" });
        db.Users.Add(new ApplicationUser { Id = playerId, UserName = "P1", FullName = "Player One" });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = organizerId, Role = GroupMemberRole.Admin });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = playerId, Role = GroupMemberRole.Member });
        var ev = new Event
        {
            GroupId = group.Id, Sport = Sport.Poker, PokerEventType = PokerEventType.Tournament,
            StartsAt = DateTime.UtcNow.AddDays(-1), MaxPlayers = 9,
            Price = buyIn, PlatformFeePercent = feePercent,
            IsActive = true, CreatedByUserId = organizerId,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return (group.Id, ev.Id);
    }

    [Fact]
    public async Task FullFlow_Tournament200At5Percent_Qr210_Fee10Accrues_SettlementCloses()
    {
        var w = Wire();

        var (groupId, eventId) = await SeedPokerTournamentAsync(w.Db, "org-1", "player-1", 200m, 5m);
        var sysAdminId = await SeedSystemAdminAsync(w.Db);

        // ── Inscricao: carimbo de preco (200) e taxa (5% = 10) ──
        var confirm = await w.DetailSvc.ConfirmAsync(eventId, "player-1");
        Assert.Equal(PokerConfirmStatus.Confirmed, confirm.Status);

        var confId = await w.Db.EventConfirmations.Select(c => c.Id).SingleAsync();
        var conf = await w.Db.EventConfirmations.FindAsync(confId);
        Assert.Equal(200m, conf!.ChargedPrice);
        Assert.Equal(10m, conf.PlatformFeeAmount);

        // ── QR: entrada + taxa = R$ 210 no payload Pix ──
        var total = ManualPlatformFee.TotalToPay(gatewaysEnabled: false, basePrice: 200m, fee: 10m);
        var payload = EventPaymentService.BuildPixStaticPayload("org@pix", "Liga Poker", "Muzambinho", total);
        Assert.Equal(210m, total);
        Assert.Contains("5406210.00", payload);

        // ── Comprovante -> aprovacao do admin do grupo ──
        var upload = await w.UploadSvc.UploadProofAsync(confId, FakeImage(), "image/jpeg", "player-1");
        Assert.True(upload.Success);
        var toggle = await w.ConfirmSvc.TogglePaidAsync(confId, "org-1");
        Assert.True(toggle.Updated);

        // ── Taxa acumulada na aba do grupo: R$ 10 ──
        var (accrued, settledBefore, dueBefore) = await w.FeeLedger.GetGroupBalanceAsync(groupId);
        Assert.Equal(10m, accrued);
        Assert.Equal(0m, settledBefore);
        Assert.Equal(10m, dueBefore);

        // ── Repasse submetido pelo organizador e aprovado pelo admin ──
        var submit = await w.SettlementSvc.SubmitSettlementAsync(
            groupId, "org-1", 10m, FakeImage(), "image/jpeg", new[] { eventId });
        Assert.True(submit.Success);
        var review = await w.SettlementSvc.ReviewSettlementAsync(
            submit.SettlementId!.Value, sysAdminId, approved: true);
        Assert.True(review.Success);

        var (accruedAfter, settledAfter, dueAfter) = await w.FeeLedger.GetGroupBalanceAsync(groupId);
        Assert.Equal(10m, accruedAfter);
        Assert.Equal(10m, settledAfter);
        Assert.Equal(0m, dueAfter);
    }

    [Fact]
    public async Task WaivedGroup_Tournament_StampsZeroFee_QrIsEntryOnly()
    {
        var w = Wire();
        var (groupId, eventId) = await SeedPokerTournamentAsync(
            w.Db, "org-1", "player-1", 200m, 10m,
            waivedUntil: DateTime.UtcNow.AddDays(30));

        var confirm = await w.DetailSvc.ConfirmAsync(eventId, "player-1");
        Assert.Equal(PokerConfirmStatus.Confirmed, confirm.Status);

        var conf = await w.Db.EventConfirmations.SingleAsync();
        Assert.Equal(200m, conf.ChargedPrice);
        Assert.Equal(0m, conf.PlatformFeeAmount); // isencao carimbada, nao pulada

        var total = ManualPlatformFee.TotalToPay(false, 200m, conf.PlatformFeeAmount!.Value);
        Assert.Equal(200m, total); // jogador paga so a entrada
    }

    [Fact]
    public async Task StampTardio_LegacyRow_UsesEventPercent()
    {
        var w = Wire();
        var (_, eventId) = await SeedPokerTournamentAsync(w.Db, "org-1", "player-1", 200m, 5m);

        // Linha legada sem carimbo: o stamp tardio resolve pelo % do evento.
        w.Db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = eventId, UserId = "player-1",
            ConfirmedAt = DateTime.UtcNow.AddHours(-1),
            HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            MarkedPaidByUserId = "org-1",
        });
        await w.Db.SaveChangesAsync();

        var stamped = await w.FeeLedger.StampFeeOnPaidAsync(
            await w.Db.EventConfirmations.Select(c => c.Id).SingleAsync());

        Assert.True(stamped);
        // Fresh context: w.Db rastreia a entidade que eu mesmo inseri.
        await using var fresh = w.Factory.CreateDbContext();
        var conf = await fresh.EventConfirmations.SingleAsync();
        Assert.Equal(10m, conf.PlatformFeeAmount);
    }
}
