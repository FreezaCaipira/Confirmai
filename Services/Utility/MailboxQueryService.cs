using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Pages.Components;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Utility;

public sealed record MailboxConversationPageResult(
    List<ConversationContactView> Contacts,
    int TotalConversations,
    int ActiveConversationTotal,
    int ArchivedConversationTotal);

public sealed class MailboxQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MailboxQueryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<MailboxConversationPageResult> LoadConversationsAsync(
        string currentUserId,
        string? searchTerm,
        bool inboxUnreadOnly,
        bool showArchived,
        int page,
        int pageSize)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : searchTerm.Trim().ToLower();

        var allRows = await db.UserMailboxMessages
            .AsNoTracking()
            .Include(m => m.SenderUser)
            .Include(m => m.RecipientUser)
            .Where(m => m.RecipientUserId == currentUserId || m.SenderUserId == currentUserId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        IEnumerable<UserMailboxMessage> candidateRows = allRows;

        if (inboxUnreadOnly)
        {
            candidateRows = candidateRows.Where(m => m.RecipientUserId == currentUserId && !m.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            candidateRows = candidateRows.Where(m =>
                !string.IsNullOrWhiteSpace(m.Body) && m.Body.ToLower().Contains(normalizedSearch));
        }

        var candidateMessages = candidateRows
            .Select(m => new MailboxMessageView
            {
                Id = m.Id,
                Subject = m.Subject ?? string.Empty,
                Body = m.Body,
                SenderUserId = m.SenderUserId,
                SenderName = m.SenderUserId == null
                    ? "Confirmai"
                    : (m.SenderUser == null
                        ? (m.SenderDisplayName ?? "(conta excluída)")
                        : (string.IsNullOrWhiteSpace(m.SenderUser.UserName) ? m.SenderDisplayName ?? m.SenderUserId : m.SenderUser.UserName!)),
                RecipientUserId = m.RecipientUserId,
                RecipientName = m.RecipientUser == null
                    ? (m.RecipientDisplayName ?? "(conta excluída)")
                    : (string.IsNullOrWhiteSpace(m.RecipientUser.UserName) ? m.RecipientDisplayName ?? m.RecipientUserId : m.RecipientUser.UserName!),
                IsArchivedBySender = m.IsArchivedBySender,
                IsArchivedByRecipient = m.IsArchivedByRecipient,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToList();

        var (activeTotal, archivedTotal) = CountConversationFolders(candidateMessages, currentUserId);

        var allMessages = showArchived
            ? candidateMessages.Where(m =>
                (m.RecipientUserId == currentUserId && m.IsArchivedByRecipient)
                || (m.SenderUserId == currentUserId && m.IsArchivedBySender))
            : candidateMessages.Where(m =>
                (m.RecipientUserId == currentUserId && !m.IsArchivedByRecipient)
                || (m.SenderUserId == currentUserId && !m.IsArchivedBySender));

        var groupedContacts = allMessages
            .GroupBy(m => BuildConversationKey(m, currentUserId))
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.CreatedAt).First();
                var contactId = latest.SenderUserId == currentUserId ? latest.RecipientUserId : (latest.SenderUserId ?? "SYSTEM");
                var contactName = latest.SenderUserId == currentUserId ? latest.RecipientName : latest.SenderName;
                var title = string.IsNullOrWhiteSpace(contactName) ? contactId : contactName;

                return new ConversationContactView
                {
                    ConversationKey = $"contact:{contactId}",
                    ConversationTitle = title,
                    ContactUserId = contactId,
                    ContactName = string.IsNullOrWhiteSpace(contactName) ? contactId : contactName,
                    LastBody = latest.Body,
                    LastCreatedAt = latest.CreatedAt,
                    UnreadCount = g.Count(x => x.RecipientUserId == currentUserId && !x.IsRead),
                    HasUnread = g.Any(x => x.RecipientUserId == currentUserId && !x.IsRead)
                };
            })
            .OrderByDescending(c => c.LastCreatedAt)
            .ToList();

        var totalConversations = groupedContacts.Count;
        var skip = (Math.Max(1, page) - 1) * pageSize;
        var contacts = groupedContacts
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new MailboxConversationPageResult(contacts, totalConversations, activeTotal, archivedTotal);
    }

    public async Task<List<ConversationMessageView>> LoadThreadAsync(
        string currentUserId,
        string contactUserId)
    {
        if (string.IsNullOrWhiteSpace(contactUserId))
        {
            return new List<ConversationMessageView>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.UserMailboxMessages
            .AsNoTracking()
            .Include(m => m.SenderUser)
            .Where(m => m.RecipientUserId == currentUserId || m.SenderUserId == currentUserId);

        if (contactUserId == "SYSTEM")
        {
            query = query.Where(m => m.SenderUserId == null && m.RecipientUserId == currentUserId);
        }
        else
        {
            query = query.Where(m =>
                (m.SenderUserId == currentUserId && m.RecipientUserId == contactUserId)
                || (m.SenderUserId == contactUserId && m.RecipientUserId == currentUserId));
        }

        return await query
            .OrderBy(m => m.CreatedAt)
            .Take(120)
            .Select(m => new ConversationMessageView
            {
                Id = m.Id,
                Body = m.Body,
                SenderUserId = m.SenderUserId,
                AuthorName = m.SenderUserId == currentUserId
                    ? "Voce"
                    : (m.SenderUserId == null
                        ? "Confirmai"
                        : (m.SenderUser == null
                            ? (m.SenderDisplayName ?? "(conta excluída)")
                            : (string.IsNullOrWhiteSpace(m.SenderUser.UserName) ? m.SenderDisplayName ?? m.SenderUserId : m.SenderUser.UserName!))),
                IsIncoming = m.SenderUserId != currentUserId,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task MarkConversationAsReadAsync(string currentUserId, string contactUserId)
    {
        if (string.IsNullOrWhiteSpace(contactUserId))
        {
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<UserMailboxMessage> unreadQuery = db.UserMailboxMessages
            .Where(m => m.RecipientUserId == currentUserId && !m.IsRead);

        if (contactUserId == "SYSTEM")
        {
            unreadQuery = unreadQuery.Where(m => m.SenderUserId == null);
        }
        else
        {
            unreadQuery = unreadQuery.Where(m => m.SenderUserId == contactUserId);
        }

        var unreadMessages = await unreadQuery.ToListAsync();
        if (!unreadMessages.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task SendQuickReplyAsync(
        string currentUserId,
        string recipientUserId,
        string recipientDisplayName,
        string body)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var senderUser = await db.Users.FindAsync(currentUserId) as ApplicationUser;
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = currentUserId,
            SenderDisplayName = senderUser?.UserName,
            RecipientUserId = recipientUserId,
            RecipientDisplayName = recipientDisplayName,
            Subject = "Chat pelo perfil",
            Body = body,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public async Task SetConversationArchivedAsync(
        string currentUserId,
        string contactUserId,
        bool archive)
    {
        if (string.IsNullOrWhiteSpace(contactUserId))
        {
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<UserMailboxMessage> query = db.UserMailboxMessages
            .Where(m => m.RecipientUserId == currentUserId || m.SenderUserId == currentUserId);

        if (contactUserId == "SYSTEM")
        {
            query = query.Where(m => m.SenderUserId == null && m.RecipientUserId == currentUserId);
        }
        else
        {
            query = query.Where(m =>
                (m.SenderUserId == currentUserId && m.RecipientUserId == contactUserId)
                || (m.SenderUserId == contactUserId && m.RecipientUserId == currentUserId));
        }

        var rows = await query.ToListAsync();
        if (rows.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            if (row.SenderUserId == currentUserId)
            {
                row.IsArchivedBySender = archive;
                row.ArchivedBySenderAt = archive ? now : null;
            }

            if (row.RecipientUserId == currentUserId)
            {
                row.IsArchivedByRecipient = archive;
                row.ArchivedByRecipientAt = archive ? now : null;
            }
        }

        await db.SaveChangesAsync();
    }

    private static string BuildConversationKey(MailboxMessageView message, string currentUserId)
    {
        var contactUserId = message.SenderUserId == currentUserId ? message.RecipientUserId : (message.SenderUserId ?? "SYSTEM");
        return $"contact:{contactUserId}";
    }

    private static (int Active, int Archived) CountConversationFolders(
        IEnumerable<MailboxMessageView> projected,
        string currentUserId)
    {
        var active = projected
            .Where(m =>
                (m.SenderUserId == currentUserId && !m.IsArchivedBySender)
                || (m.RecipientUserId == currentUserId && !m.IsArchivedByRecipient))
            .GroupBy(m => BuildConversationKey(m, currentUserId))
            .Count();

        var archived = projected
            .Where(m =>
                (m.SenderUserId == currentUserId && m.IsArchivedBySender)
                || (m.RecipientUserId == currentUserId && m.IsArchivedByRecipient))
            .GroupBy(m => BuildConversationKey(m, currentUserId))
            .Count();

        return (active, archived);
    }
}

internal sealed class MailboxMessageView
{
    public int Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? SenderUserId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string RecipientUserId { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public bool IsArchivedBySender { get; init; }
    public bool IsArchivedByRecipient { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
