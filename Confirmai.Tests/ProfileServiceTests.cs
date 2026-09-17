using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class ProfileServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, ProfileService svc, UserManager<ApplicationUser> userManager) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"profile-{Guid.NewGuid()}");
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        userStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new ApplicationUser { Id = id, UserName = id });
        var userManager = new UserManager<ApplicationUser>(
            userStoreMock.Object, null!, new PasswordHasher<ApplicationUser>(),
            null!, null!, null!, null!, null!, null!);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(x => x.WebRootPath).Returns(Path.GetTempPath());
        var svc = new ProfileService(factory, userManager, envMock.Object,
            new Confirmai.Services.Core.LogService(factory, NullLogger<Confirmai.Services.Core.LogService>.Instance));
        return (factory, svc, userManager);
    }

    private static async Task SeedUserAndConfirmationsAsync(IDbContextFactory<AppDbContext> factory, string userId)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "Player" });
        var group = new Group { Id = 1, Name = "Group", Sport = Sport.Futsal };
        db.Groups.Add(group);

        var futsalEvent = new Event { Id = 1, GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow, Location = "Quadra", MaxPlayers = 10 };
        var pokerEvent = new Event { Id = 2, GroupId = 1, Sport = Sport.Poker, StartsAt = DateTime.UtcNow, Location = "Poker House", MaxPlayers = 10 };
        db.Events.AddRange(futsalEvent, pokerEvent);

        db.EventConfirmations.Add(new EventConfirmation { EventId = 1, UserId = userId, Position = FutsalPosition.Goalkeeper, ConfirmedAt = DateTime.UtcNow });
        db.EventConfirmations.Add(new EventConfirmation { EventId = 1, UserId = userId, Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow });
        db.EventConfirmations.Add(new EventConfirmation { EventId = 2, UserId = userId, Position = null, ConfirmedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LoadSportStatsAsync_ReturnsZero_WhenNoConfirmations()
    {
        var (factory, svc, _) = Setup();
        var (futsalTotal, gk, outfield, pokerTotal) = await svc.LoadSportStatsAsync("user-1");
        Assert.Equal(0, futsalTotal);
        Assert.Equal(0, gk);
        Assert.Equal(0, outfield);
        Assert.Equal(0, pokerTotal);
    }

    [Fact]
    public async Task LoadSportStatsAsync_ReturnsCorrectCounts_WithConfirmations()
    {
        var (factory, svc, _) = Setup();
        await SeedUserAndConfirmationsAsync(factory, "user-1");

        var (futsalTotal, gk, outfield, pokerTotal) = await svc.LoadSportStatsAsync("user-1");

        Assert.Equal(2, futsalTotal);
        Assert.Equal(1, gk);
        Assert.Equal(1, outfield);
        Assert.Equal(1, pokerTotal);
    }

    [Fact]
    public async Task SendMessageAsync_CreatesMailboxMessage()
    {
        var (factory, svc, _) = Setup();

        await svc.SendMessageAsync("sender-1", "recipient-1", "Hello there!");

        await using var db = factory.CreateDbContext();
        var msg = await db.UserMailboxMessages.FirstOrDefaultAsync(m => m.RecipientUserId == "recipient-1");
        Assert.NotNull(msg);
        Assert.Equal("Hello there!", msg!.Body);
        Assert.Equal("sender-1", msg.SenderUserId);
    }

    [Fact]
    public async Task SendMessageAsync_TrimsBody()
    {
        var (factory, svc, _) = Setup();

        await svc.SendMessageAsync("sender-1", "recipient-1", "  padded message  ");

        await using var db = factory.CreateDbContext();
        var msg = await db.UserMailboxMessages.FirstAsync();
        Assert.Equal("padded message", msg.Body);
    }

    [Fact]
    public async Task LoadChatMessagesAsync_ReturnsMessages_WhenExist()
    {
        var (factory, svc, _) = Setup();
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-1", RecipientUserId = "user-2",
            Body = "Hi", CreatedAt = DateTime.UtcNow
        });
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-2", RecipientUserId = "user-1",
            Body = "Hello", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var messages = await svc.LoadChatMessagesAsync("user-1", "user-2");

        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task LoadChatMessagesAsync_ReturnsEmpty_WhenNoMessages()
    {
        var (factory, svc, _) = Setup();
        var messages = await svc.LoadChatMessagesAsync("user-1", "user-2");
        Assert.Empty(messages);
    }

    [Fact]
    public async Task LoadChatMessagesAsync_MarksIncomingCorrectly()
    {
        var (factory, svc, _) = Setup();
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-2", RecipientUserId = "user-1",
            Body = "From other", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var messages = await svc.LoadChatMessagesAsync("user-1", "user-2");

        Assert.Single(messages);
        Assert.True(messages[0].IsIncoming);
    }
}
