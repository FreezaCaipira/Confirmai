using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Confirmai.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class EventPaymentServiceTests
{
    private static EventPaymentService CreateService(
        AppDbContext db,
        EventPaymentGatewayFactory? gatewayFactory = null,
        FeeOptions? feeOptions = null)
    {
        var factory = new TestDbContextFactoryForEventPayment(db);
        gatewayFactory ??= CreateGatewayFactory(db);
        feeOptions ??= new FeeOptions();

        var feeOptionsMock = new Mock<IOptions<FeeOptions>>();
        feeOptionsMock.Setup(x => x.Value).Returns(feeOptions);

        return new EventPaymentService(factory, gatewayFactory, feeOptionsMock.Object);
    }

    private static EventPaymentGatewayFactory CreateGatewayFactory(AppDbContext db)
    {
        var gatewayService = new GatewayService(new TestDbContextFactoryForEventPayment(db));
        return new EventPaymentGatewayFactory(Array.Empty<IEventPaymentGateway>(), gatewayService);
    }

    private static (AppDbContext db, EventConfirmation conf, Group group, ApplicationUser user) SeedConfirmation(
        bool enablePaymentGateways = false,
        decimal? price = 50m,
        FutsalPosition position = FutsalPosition.Outfield,
        EventConfirmationPaymentStatus paymentStatus = EventConfirmationPaymentStatus.Pending)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var group = new Group
        {
            Id = 1,
            Name = "Test Group",
            City = "São Paulo",
            StateCode = "SP",
            EnablePaymentGateways = enablePaymentGateways,
            IsActive = true
        };

        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "Test User",
            Email = "user@email.com",
            PixKey = "user1pixkey@email.com"
        };

        var adminUser = new ApplicationUser
        {
            Id = "admin1",
            UserName = "Admin User",
            Email = "admin@email.com",
            PixKey = "adminpixkey@email.com"
        };

        var adminMember = new GroupMember
        {
            GroupId = 1,
            UserId = "admin1",
            Role = GroupMemberRole.Admin,
            User = adminUser,
            CreatedAt = DateTime.UtcNow
        };

        var evt = new Event
        {
            Id = 1,
            GroupId = 1,
            Group = group,
            StartsAt = DateTime.UtcNow.AddDays(7),
            Price = price,
            Sport = Sport.Futsal,
            IsActive = true
        };

        var conf = new EventConfirmation
        {
            Id = 1,
            EventId = 1,
            Event = evt,
            UserId = "user1",
            User = user,
            Position = position,
            PaymentStatus = paymentStatus
        };

        db.Groups.Add(group);
        db.Users.Add(user);
        db.Users.Add(adminUser);
        db.GroupMembers.Add(adminMember);
        db.Events.Add(evt);
        db.EventConfirmations.Add(conf);
        db.SaveChanges();

        return (db, conf, group, user);
    }

    // ── LoadConfirmationAsync ────────────────────────────────────────────

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsConfirmation_ForOwner()
    {
        var (db, conf, _, user) = SeedConfirmation();
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, user.Id);

        Assert.NotNull(result.Confirmation);
        Assert.Equal(conf.Id, result.Confirmation!.Id);
        Assert.False(result.IsAdminViewing);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsNull_ForUnauthorizedUser()
    {
        var (db, conf, _, _) = SeedConfirmation();
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, "random-user-id");

        Assert.Null(result.Confirmation);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsIsAdminViewing_ForAdminNotOwner()
    {
        var (db, conf, _, _) = SeedConfirmation();
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, "admin1");

        Assert.NotNull(result.Confirmation);
        Assert.True(result.IsAdminViewing);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsRedirectUrl_ForGoalkeeper()
    {
        var (db, conf, _, user) = SeedConfirmation(position: FutsalPosition.Goalkeeper);
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, user.Id);

        Assert.NotNull(result.RedirectUrl);
        Assert.Contains("/futsal/", result.RedirectUrl);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsRedirectUrl_ForNullPrice()
    {
        var (db, conf, _, user) = SeedConfirmation(price: null);
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, user.Id);

        Assert.NotNull(result.RedirectUrl);
    }

    [Fact]
    public async Task LoadConfirmationAsync_DoesNotRedirect_ForAdminViewingGoalkeeper()
    {
        var (db, conf, _, _) = SeedConfirmation(position: FutsalPosition.Goalkeeper);
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, "admin1");

        Assert.Null(result.RedirectUrl);
        Assert.True(result.IsAdminViewing);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsPendingCharge_WhenPendingPayment()
    {
        var (db, conf, _, user) = SeedConfirmation();
        conf.PixTxId = "tx123";
        conf.PixBrCode = "br-code-123";
        conf.PaymentGatewayName = "EfiBank";
        conf.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        db.EventConfirmations.Update(conf);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, user.Id);

        Assert.Equal("br-code-123", result.PendingBrCode);
        Assert.Equal("tx123", result.PendingChargeId);
        Assert.Equal("EfiBank", result.PendingGatewayName);
    }

    [Fact]
    public async Task LoadConfirmationAsync_ReturnsGroupGatewaysEnabled_WhenGroupHasGateways()
    {
        var (db, conf, _, user) = SeedConfirmation(enablePaymentGateways: true);
        var service = CreateService(db);

        var result = await service.LoadConfirmationAsync(conf.Id, user.Id);

        Assert.True(result.GroupGatewaysEnabled);
    }

    // ── GeneratePixChargeAsync ───────────────────────────────────────────

    [Fact]
    public async Task GeneratePixChargeAsync_ReturnsError_WhenGatewaysDisabled()
    {
        var (db, conf, _, _) = SeedConfirmation(enablePaymentGateways: false);
        var service = CreateService(db);

        var result = await service.GeneratePixChargeAsync(conf.Id, "EfiBank", false);

        Assert.False(result.Success);
        Assert.Contains("desativados", result.ErrorMessage);
    }

    [Fact]
    public async Task GeneratePixChargeAsync_ReturnsError_WhenConfirmationNotFound()
    {
        var (db, _, _, _) = SeedConfirmation(enablePaymentGateways: true);
        var service = CreateService(db);

        var result = await service.GeneratePixChargeAsync(999, "EfiBank", true);

        Assert.False(result.Success);
        Assert.Contains("não encontrada", result.ErrorMessage);
    }

    [Fact]
    public async Task GeneratePixChargeAsync_ReturnsError_WhenFeeConfiguredButGatewayNotSupported()
    {
        var (db, conf, _, _) = SeedConfirmation(enablePaymentGateways: true);
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            SupportedGateways = new[] { "EfiBank" }
        };
        var service = CreateService(db, feeOptions: feeOptions);

        var result = await service.GeneratePixChargeAsync(conf.Id, "UnsupportedGateway", true);

        Assert.False(result.Success);
        Assert.Contains("não suporta cobrança de taxa", result.ErrorMessage);
    }

    [Fact]
    public async Task GeneratePixChargeAsync_ReturnsError_WhenFeeConfiguredButNoPayoutAccount()
    {
        var (db, conf, _, _) = SeedConfirmation(enablePaymentGateways: true);
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            SupportedGateways = new[] { "EfiBank" }
        };
        var service = CreateService(db, feeOptions: feeOptions);

        var result = await service.GeneratePixChargeAsync(conf.Id, "EfiBank", true);

        Assert.False(result.Success);
        Assert.Contains("chave PIX para repasse", result.ErrorMessage);
    }

    [Fact]
    public async Task GeneratePixChargeAsync_ReturnsError_WhenGatewayUnavailable()
    {
        var (db, conf, _, _) = SeedConfirmation(enablePaymentGateways: true);
        var service = CreateService(db);

        var result = await service.GeneratePixChargeAsync(conf.Id, "NonExistentGateway", true);

        Assert.False(result.Success);
        Assert.Contains("Gateway indisponível", result.ErrorMessage);
    }

    // ── CheckPaymentStatusAsync ──────────────────────────────────────────

    [Fact]
    public async Task CheckPaymentStatusAsync_ReturnsPaid_WhenDbShowsPaid()
    {
        var (db, conf, _, _) = SeedConfirmation(paymentStatus: EventConfirmationPaymentStatus.Paid);
        var service = CreateService(db);

        var result = await service.CheckPaymentStatusAsync(conf.Id, "tx123", "EfiBank");

        Assert.True(result.IsPaid);
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_ReturnsNotPaid_WhenDbShowsPending()
    {
        var (db, conf, _, _) = SeedConfirmation(paymentStatus: EventConfirmationPaymentStatus.Pending);
        var service = CreateService(db);

        var result = await service.CheckPaymentStatusAsync(conf.Id, "tx123", "EfiBank");

        Assert.False(result.IsPaid);
    }

    // ── GetGroupAdminPixKey ──────────────────────────────────────────────

    [Fact]
    public void GetGroupAdminPixKey_ReturnsReceiverPixKey_WhenSet()
    {
        var group = new Group
        {
            Id = 1,
            Name = "Test",
            PixReceiverUserId = "admin1",
            Members = new List<GroupMember>
            {
                new()
                {
                    UserId = "admin1",
                    Role = GroupMemberRole.Admin,
                    User = new ApplicationUser { Id = "admin1", PixKey = "receiver-key@email.com" },
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        var result = EventPaymentService.GetGroupAdminPixKey(group);

        Assert.Equal("receiver-key@email.com", result);
    }

    [Fact]
    public void GetGroupAdminPixKey_FallsBackToFirstAdminWithPixKey()
    {
        var group = new Group
        {
            Id = 1,
            Name = "Test",
            PixReceiverUserId = null,
            Members = new List<GroupMember>
            {
                new()
                {
                    UserId = "admin2",
                    Role = GroupMemberRole.Admin,
                    User = new ApplicationUser { Id = "admin2", PixKey = "admin2-key@email.com" },
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    UserId = "admin1",
                    Role = GroupMemberRole.Admin,
                    User = new ApplicationUser { Id = "admin1", PixKey = "admin1-key@email.com" },
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        var result = EventPaymentService.GetGroupAdminPixKey(group);

        Assert.Equal("admin2-key@email.com", result);
    }

    [Fact]
    public void GetGroupAdminPixKey_ReturnsNull_WhenNoAdminHasPixKey()
    {
        var group = new Group
        {
            Id = 1,
            Name = "Test",
            Members = new List<GroupMember>
            {
                new()
                {
                    UserId = "admin1",
                    Role = GroupMemberRole.Admin,
                    User = new ApplicationUser { Id = "admin1", PixKey = null },
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        var result = EventPaymentService.GetGroupAdminPixKey(group);

        Assert.Null(result);
    }

    // ── BuildPixStaticPayload ────────────────────────────────────────────

    [Fact]
    public void BuildPixStaticPayload_ReturnsValidPayload()
    {
        var result = EventPaymentService.BuildPixStaticPayload(
            "test-key@email.com", "Test Group", "São Paulo", 50.00m);

        Assert.StartsWith("000201", result);
        Assert.Contains("br.gov.bcb.pix", result);
        Assert.Contains("test-key@email.com", result);
        Assert.Contains("Test Group", result);
        Assert.Contains("50.00", result);
        Assert.True(result.Length >= 100);
    }

    [Fact]
    public void BuildPixStaticPayload_TruncatesGroupName_WhenOver25Chars()
    {
        var longName = new string('A', 30);

        var result = EventPaymentService.BuildPixStaticPayload(
            "key@email.com", longName, "City", 10m);

        Assert.Contains(new string('A', 25), result);
        Assert.DoesNotContain(new string('A', 30), result);
    }

    [Fact]
    public void BuildPixStaticPayload_TruncatesCity_WhenOver15Chars()
    {
        var longCity = new string('B', 20);

        var result = EventPaymentService.BuildPixStaticPayload(
            "key@email.com", "Group", longCity, 10m);

        Assert.Contains(new string('B', 15), result);
        Assert.DoesNotContain(new string('B', 20), result);
    }

    [Fact]
    public void BuildPixStaticPayload_UsesBrasil_WhenCityIsNull()
    {
        var result = EventPaymentService.BuildPixStaticPayload(
            "key@email.com", "Group", null, 10m);

        Assert.Contains("Brasil", result);
    }

    [Fact]
    public void BuildPixStaticPayload_GeneratesConsistentCrc()
    {
        var result1 = EventPaymentService.BuildPixStaticPayload(
            "key@email.com", "Group", "City", 10m);
        var result2 = EventPaymentService.BuildPixStaticPayload(
            "key@email.com", "Group", "City", 10m);

        Assert.Equal(result1, result2);
    }

    // ── Helper ───────────────────────────────────────────────────────────

    private sealed class TestDbContextFactoryForEventPayment : IDbContextFactory<AppDbContext>
    {
        private readonly AppDbContext _context;

        public TestDbContextFactoryForEventPayment(AppDbContext context)
        {
            _context = context;
        }

        public AppDbContext CreateDbContext() => _context;
    }
}
