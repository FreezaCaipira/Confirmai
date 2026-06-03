using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class MailboxConversationArchiveServiceTests
{
    [Fact]
    public async Task ArchiveOrderConversation_ArchivesAllMatchingMessages()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.UserMailboxMessages.AddRange(
            new UserMailboxMessage { SenderUserId = "u1", RecipientUserId = "u2", Subject = "Pedido 42", Body = "msg1" },
            new UserMailboxMessage { SenderUserId = "u2", RecipientUserId = "u1", Subject = "Pedido 42", Body = "msg2" },
            new UserMailboxMessage { SenderUserId = "u1", RecipientUserId = "u3", Subject = "Pedido 99", Body = "other" }
        );
        await db.SaveChangesAsync();

        await MailboxConversationArchiveService.ArchiveOrderConversationForAllParticipantsAsync(db, 42);

        var archived = db.UserMailboxMessages.Where(m => m.IsArchivedBySender && m.IsArchivedByRecipient).ToList();
        Assert.Equal(2, archived.Count);
        Assert.All(archived, m => Assert.Contains("pedido 42", m.Subject!.ToLower()));

        var untouched = db.UserMailboxMessages.First(m => m.Subject == "Pedido 99");
        Assert.False(untouched.IsArchivedBySender);
        Assert.False(untouched.IsArchivedByRecipient);
    }

    [Fact]
    public async Task ArchiveOrderConversation_SetsTimestamps_OnlyOnce()
    {
        using var db = TestDataFactory.CreateDbContext();

        var earlier = DateTime.UtcNow.AddDays(-1);
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "u1",
            RecipientUserId = "u2",
            Subject = "Pedido 10",
            Body = "msg",
            IsArchivedBySender = true,
            ArchivedBySenderAt = earlier
        });
        await db.SaveChangesAsync();

        await MailboxConversationArchiveService.ArchiveOrderConversationForAllParticipantsAsync(db, 10);

        var msg = db.UserMailboxMessages.First();
        Assert.True(msg.IsArchivedBySender);
        Assert.True(msg.IsArchivedByRecipient);
        // Existing timestamp should be preserved (??= operator)
        Assert.Equal(earlier, msg.ArchivedBySenderAt);
        Assert.NotNull(msg.ArchivedByRecipientAt);
    }

    [Fact]
    public async Task ArchiveOrderConversation_DoesNothing_WhenNoMatchingMessages()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "u1",
            RecipientUserId = "u2",
            Subject = "Pedido 99",
            Body = "msg"
        });
        await db.SaveChangesAsync();

        await MailboxConversationArchiveService.ArchiveOrderConversationForAllParticipantsAsync(db, 42);

        var msg = db.UserMailboxMessages.First();
        Assert.False(msg.IsArchivedBySender);
        Assert.False(msg.IsArchivedByRecipient);
    }

    [Fact]
    public async Task ArchiveOrderConversation_MatchesSubject_CaseInsensitively()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = "u1",
            RecipientUserId = "u2",
            Subject = "PEDIDO 5",
            Body = "msg"
        });
        await db.SaveChangesAsync();

        await MailboxConversationArchiveService.ArchiveOrderConversationForAllParticipantsAsync(db, 5);

        var msg = db.UserMailboxMessages.First();
        Assert.True(msg.IsArchivedBySender);
        Assert.True(msg.IsArchivedByRecipient);
    }
}
