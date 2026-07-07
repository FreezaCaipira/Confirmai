using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Detail : IAsyncDisposable
{
    [Parameter] public int Id { get; set; }

    private Group?                    group            = null;
    private List<GroupJoinRequest>    pendingRequests  = new();
    private GroupJoinRequest?         userJoinRequest  = null;
    private bool                      isLoading        = true;
    private string?                   currentUserId    = null;
    private bool                      copiedInvite     = false;
    private bool                      copiedCode       = false;
    private bool                      requestingJoin   = false;
    private string                    enteredCode      = string.Empty;
    private string                    codeError        = string.Empty;
    private string                    requestSortOrder = "newest";
    private HashSet<int>              selectedRequestIds = new();
    private bool                      isBulkProcessing = false;

    private IEnumerable<GroupJoinRequest> sortedPendingRequests => requestSortOrder switch
    {
        "newest" => pendingRequests.OrderByDescending(r => r.RequestedAt),
        "oldest" => pendingRequests.OrderBy(r => r.RequestedAt),
        "name" => pendingRequests.OrderBy(r => r.User?.FullName ?? r.User?.UserName ?? ""),
        _ => pendingRequests
    };

    private CancellationTokenSource? _copyInviteCts;
    private CancellationTokenSource? _copyCodeCts;
    private Task? _copyInviteTask = null;
    private Task? _copyCodeTask = null;
    private bool                      showAllMembers   = false;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LoadGroup();
    }

    private async Task LoadGroup()
    {
        isLoading = true;
        await using var db = await DbFactory.CreateDbContextAsync();

        group = await db.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == Id);

        if (group is not null)
        {
            showAllMembers = false;

            pendingRequests = await db.GroupJoinRequests
                .Where(r => r.GroupId == Id && r.Status == JoinRequestStatus.Pending)
                .Include(r => r.User)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            if (currentUserId is not null)
            {
                userJoinRequest = await db.GroupJoinRequests
                    .Where(r => r.GroupId == Id && r.UserId == currentUserId)
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefaultAsync();
            }
        }

        isLoading = false;
    }

    private async Task CopyInviteLink(string url)
    {
        try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", url); }
        catch { /* fallback: ignore if clipboard not available */ }

        _copyInviteCts?.Cancel();
        _copyInviteCts = new CancellationTokenSource();
        var ct = _copyInviteCts.Token;

        try
        {
            copiedInvite = true;
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested)
            {
                copiedInvite = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copiedInvite = false;
        }
    }

    private void ToggleMembersView() => showAllMembers = !showAllMembers;

    private async Task CopyCode(string code)
    {
        try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", code); }
        catch { }

        _copyCodeCts?.Cancel();
        _copyCodeCts = new CancellationTokenSource();
        var ct = _copyCodeCts.Token;

        try
        {
            copiedCode = true;
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested)
            {
                copiedCode = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copiedCode = false;
        }
    }

    private async Task ShareInviteOnWhatsApp(string url)
    {
        var shareUrl = $"https://wa.me/?text={Uri.EscapeDataString(url)}";
        await JS.InvokeVoidAsync("open", shareUrl, "_blank", "noopener,noreferrer");
    }

    private async Task RequestToJoin()
    {
        if (currentUserId is null || group is null) return;
        requestingJoin = true;
        await using var db = await DbFactory.CreateDbContextAsync();
        var already = await db.GroupJoinRequests
            .AnyAsync(r => r.GroupId == group.Id && r.UserId == currentUserId
                        && r.Status == JoinRequestStatus.Pending);
        if (!already)
        {
            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId     = group.Id,
                UserId      = currentUserId,
                RequestedAt = DateTime.UtcNow,
                Status      = JoinRequestStatus.Pending,
            });
            await db.SaveChangesAsync();
        }
        requestingJoin = false;
        await LoadGroup();
    }

    private async Task JoinWithCode()
    {
        if (currentUserId is null || group is null) return;
        codeError      = string.Empty;
        var typed = enteredCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(group.InviteCode) ||
            !string.Equals(typed, group.InviteCode, StringComparison.OrdinalIgnoreCase))
        {
            codeError       = "Código inválido. Verifique e tente novamente.";
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        var exists = await db.GroupMembers
            .AnyAsync(m => m.GroupId == group.Id && m.UserId == currentUserId);
        if (!exists)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId   = group.Id,
                UserId    = currentUserId,
                Role      = GroupMemberRole.Member,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await LoadGroup();
    }

    private async Task ApproveRequest(int requestId)
    {
        if (currentUserId is null || group is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;

        var alreadyMember = await db.GroupMembers
            .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
        if (!alreadyMember)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId   = req.GroupId,
                UserId    = req.UserId,
                Role      = GroupMemberRole.Member,
                CreatedAt = DateTime.UtcNow,
            });
        }
        req.Status            = JoinRequestStatus.Approved;
        req.RespondedAt       = DateTime.UtcNow;
        req.RespondedByUserId = currentUserId;

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId         = null,   // null = mensagem do sistema (Confirmai)
            RecipientUserId      = req.UserId,
            RecipientDisplayName = (await db.Users.FindAsync(req.UserId) as ApplicationUser)?.UserName,
            Subject              = $"Você foi aprovado em \"{group!.Name}\"",
            Body                 = $"Sua solicitação para entrar no grupo **{group!.Name}** foi aprovada! Você já pode acessar o grupo.",
            CreatedAt            = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        await LoadGroup();
    }

    private async Task RejectRequest(int requestId)
    {
        if (currentUserId is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;
        req.Status            = JoinRequestStatus.Rejected;
        req.RespondedAt       = DateTime.UtcNow;
        req.RespondedByUserId = currentUserId;
        await db.SaveChangesAsync();
        await LoadGroup();
    }

    private async Task ApproveSelected()
    {
        if (currentUserId is null || group is null || selectedRequestIds.Count == 0) return;
        isBulkProcessing = true;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var requests = await db.GroupJoinRequests
                .Where(r => selectedRequestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
                .Include(r => r.User)
                .ToListAsync();

            foreach (var req in requests)
            {
                var alreadyMember = await db.GroupMembers
                    .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
                if (!alreadyMember)
                {
                    db.GroupMembers.Add(new GroupMember
                    {
                        GroupId   = req.GroupId,
                        UserId    = req.UserId,
                        Role      = GroupMemberRole.Member,
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                req.Status            = JoinRequestStatus.Approved;
                req.RespondedAt       = DateTime.UtcNow;
                req.RespondedByUserId = currentUserId;

                db.UserMailboxMessages.Add(new UserMailboxMessage
                {
                    SenderUserId         = null,
                    RecipientUserId      = req.UserId,
                    RecipientDisplayName = req.User?.UserName,
                    Subject              = $"Você foi aprovado em \"{group.Name}\"",
                    Body                 = $"Sua solicitação para entrar no grupo **{group.Name}** foi aprovada! Você já pode acessar o grupo.",
                    CreatedAt            = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
            selectedRequestIds.Clear();
            await LoadGroup();
        }
        finally
        {
            isBulkProcessing = false;
        }
    }

    private async Task RejectSelected()
    {
        if (currentUserId is null || selectedRequestIds.Count == 0) return;
        isBulkProcessing = true;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var requests = await db.GroupJoinRequests
                .Where(r => selectedRequestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

            foreach (var req in requests)
            {
                req.Status            = JoinRequestStatus.Rejected;
                req.RespondedAt       = DateTime.UtcNow;
                req.RespondedByUserId = currentUserId;
            }

            await db.SaveChangesAsync();
            selectedRequestIds.Clear();
            await LoadGroup();
        }
        finally
        {
            isBulkProcessing = false;
        }
    }

    private void ClearSelection()
    {
        selectedRequestIds.Clear();
    }

    private void ToggleRequestSelection(int requestId, bool isSelected)
    {
        if (isSelected)
        {
            selectedRequestIds.Add(requestId);
        }
        else
        {
            selectedRequestIds.Remove(requestId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _copyInviteCts?.Cancel();
        _copyCodeCts?.Cancel();

        if (_copyInviteTask is not null)
        {
            try { await _copyInviteTask; }
            catch (OperationCanceledException) { }
        }

        if (_copyCodeTask is not null)
        {
            try { await _copyCodeTask; }
            catch (OperationCanceledException) { }
        }

        _copyInviteCts?.Dispose();
        _copyCodeCts?.Dispose();
    }
}
