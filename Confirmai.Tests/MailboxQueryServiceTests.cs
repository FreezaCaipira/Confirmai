using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class MailboxQueryServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, MailboxQueryService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"mbox-{Guid.NewGuid()}");
        var svc = new MailboxQueryService(factory);
        return (factory, svc);
    }

    private static async Task SeedUsersAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "user-a", UserName = "Alice", FullName = "Alice" });
        db.Users.Add(new ApplicationUser { Id = "user-b", UserName = "Bob", FullName = "Bob" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LoadConversationsAsync_ReturnsEmpty_WhenNoMessages()
    {
        var (factory, svc) = Setup();

        var result = await svc.LoadConversationsAsync("user-a", null, false, false, 1, 10);

        Assert.Empty(result.Contacts);
        Assert.Equal(0, result.TotalConversations);
    }

    [Fact]
    public async Task LoadConversationsAsync_ReturnsConversation_WhenMessagesExist()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Subject = "Test",
            Body = "Hello",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.LoadConversationsAsync("user-a", null, false, false, 1, 10);

        Assert.Single(result.Contacts);
        Assert.Equal("Bob", result.Contacts[0].ContactName);
        Assert.True(result.Contacts[0].HasUnread);
    }

    [Fact]
    public async Task LoadConversationsAsync_FiltersBySearchTerm()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Subject = "Test",
            Body = "Important message",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Subject = "Test",
            Body = "Random text",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.LoadConversationsAsync("user-a", "important", false, false, 1, 10);

        Assert.Single(result.Contacts);
        Assert.Contains("Important", result.Contacts[0].LastBody);
    }

    [Fact]
    public async Task LoadConversationsAsync_FiltersUnreadOnly()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Unread",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Read",
            IsRead = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.LoadConversationsAsync("user-a", null, true, false, 1, 10);

        Assert.Single(result.Contacts);
        Assert.Contains("Unread", result.Contacts[0].LastBody);
    }

    [Fact]
    public async Task LoadThreadAsync_ReturnsMessages_WhenConversationExists()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Message 1",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        });
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-a",
            RecipientUserId = "user-b",
            Body = "Reply 1",
            IsRead = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var thread = await svc.LoadThreadAsync("user-a", "user-b");

        Assert.Equal(2, thread.Count);
        Assert.True(thread[0].IsIncoming);
        Assert.False(thread[1].IsIncoming);
    }

    [Fact]
    public async Task LoadThreadAsync_ReturnsEmpty_WhenContactUserIdIsEmpty()
    {
        var (factory, svc) = Setup();

        var thread = await svc.LoadThreadAsync("user-a", "");

        Assert.Empty(thread);
    }

    [Fact]
    public async Task MarkConversationAsReadAsync_MarksUnreadMessages()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Unread",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await svc.MarkConversationAsReadAsync("user-a", "user-b");

        await using var db2 = factory.CreateDbContext();
        var msg = await db2.UserMailboxMessages.FirstAsync();
        Assert.True(msg.IsRead);
        Assert.NotNull(msg.ReadAt);
    }

    [Fact]
    public async Task SendQuickReplyAsync_PersistsMessage()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);

        await svc.SendQuickReplyAsync("user-a", "user-b", "Bob", "Quick reply");

        await using var db = factory.CreateDbContext();
        var msg = await db.UserMailboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(msg);
        Assert.Equal("user-a", msg!.SenderUserId);
        Assert.Equal("user-b", msg.RecipientUserId);
        Assert.Equal("Quick reply", msg.Body);
    }

    [Fact]
    public async Task SetConversationArchivedAsync_ArchivesMessages()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Test",
            IsRead = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await svc.SetConversationArchivedAsync("user-a", "user-b", true);

        await using var db2 = factory.CreateDbContext();
        var msg = await db2.UserMailboxMessages.FirstAsync();
        Assert.True(msg.IsArchivedByRecipient);
        Assert.NotNull(msg.ArchivedByRecipientAt);
    }

    [Fact]
    public async Task SetConversationArchivedAsync_UnarchivesMessages()
    {
        var (factory, svc) = Setup();
        await SeedUsersAsync(factory);
        await using var db = factory.CreateDbContext();
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "user-b",
            RecipientUserId = "user-a",
            Body = "Test",
            IsRead = true,
            IsArchivedByRecipient = true,
            ArchivedByRecipientAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await svc.SetConversationArchivedAsync("user-a", "user-b", false);

        await using var db2 = factory.CreateDbContext();
        var msg = await db2.UserMailboxMessages.FirstAsync();
        Assert.False(msg.IsArchivedByRecipient);
        Assert.Null(msg.ArchivedByRecipientAt);
    }
}
