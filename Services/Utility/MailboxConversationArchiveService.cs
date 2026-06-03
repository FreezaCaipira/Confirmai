using Microsoft.EntityFrameworkCore;
using Confirmai.Data;

namespace Confirmai.Services.Utility;

public static class MailboxConversationArchiveService
{
    public static async Task ArchiveOrderConversationForAllParticipantsAsync(AppDbContext db, int orderId)
    {
        var subject = $"Pedido {orderId}".ToLower();
        var now = DateTime.UtcNow;

        var rows = await db.UserMailboxMessages
            .Where(m => m.Subject != null && m.Subject.ToLower() == subject)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            row.IsArchivedBySender = true;
            row.IsArchivedByRecipient = true;
            row.ArchivedBySenderAt ??= now;
            row.ArchivedByRecipientAt ??= now;
        }

        await db.SaveChangesAsync();
    }
}
