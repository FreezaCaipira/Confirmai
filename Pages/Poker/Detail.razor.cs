using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Poker;

public partial class Detail
{
    [Parameter] public int Id { get; set; }

    private Event? ev = null;
    private bool isLoading = true;
    private string? currentUserId = null;
    private bool homeGameUnlocked = false;
    private string codeInput = string.Empty;
    private bool codeWrong = false;
    private string actionError = string.Empty;
    private bool showCancelConfirm = false;
    private GroupJoinRequest? userJoinRequest = null;
    private bool requestingJoin = false;
    private string joinRequestError = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LoadEvent();
    }

    private async Task LoadEvent()
    {
        isLoading = true;
        await using var db = await DbFactory.CreateDbContextAsync();
        ev = await db.Events
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Poker);

        userJoinRequest = null;
        if (currentUserId is not null && ev is not null)
        {
            userJoinRequest = await db.GroupJoinRequests
                .Where(r => r.GroupId == ev.GroupId && r.UserId == currentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();
        }

        isLoading = false;
    }

    private async Task RequestToJoinAsync()
    {
        if (currentUserId is null || ev is null)
            return;

        requestingJoin = true;
        joinRequestError = string.Empty;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();

            var alreadyPending = await db.GroupJoinRequests
                .AnyAsync(r => r.GroupId == ev.GroupId && r.UserId == currentUserId && r.Status == JoinRequestStatus.Pending);

            if (!alreadyPending)
            {
                db.GroupJoinRequests.Add(new GroupJoinRequest
                {
                    GroupId = ev.GroupId,
                    UserId = currentUserId,
                    RequestedAt = DateTime.UtcNow,
                    Status = JoinRequestStatus.Pending,
                });

                await db.SaveChangesAsync();
            }

            await LoadEvent();
        }
        catch
        {
            joinRequestError = "Não foi possível enviar a solicitação agora. Tente novamente em instantes.";
        }
        finally
        {
            requestingJoin = false;
        }
    }

    private void TryUnlock()
    {
        codeWrong = false;
        if (ev?.HomeGameCode is null) return;
        if (string.Equals(codeInput.Trim(), ev.HomeGameCode, StringComparison.OrdinalIgnoreCase))
            homeGameUnlocked = true;
        else
            codeWrong = true;
    }

    private async Task HandleCodeKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") TryUnlock();
        await Task.CompletedTask;
    }

    private async Task ConfirmPresence()
    {
        if (currentUserId is null || ev is null) return;
        actionError = string.Empty;

        await using var db = await DbFactory.CreateDbContextAsync();
        var existing = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == Id && c.UserId == currentUserId);
        if (existing is not null) { actionError = "Você já está inscrito."; return; }

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = Id,
            UserId = currentUserId,
            ConfirmedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        await LoadEvent();
    }

    private async Task CancelConfirmation()
    {
        if (currentUserId is null) return;
        actionError = string.Empty;

        await using var db = await DbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == Id && c.UserId == currentUserId);
        if (conf is not null)
        {
            db.EventConfirmations.Remove(conf);
            await db.SaveChangesAsync();
        }
        await LoadEvent();
    }

    private async Task CancelEvent()
    {
        if (currentUserId is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var dbEv = await db.Events.FirstOrDefaultAsync(e => e.Id == Id);
        if (dbEv is null || dbEv.CreatedByUserId != currentUserId) return;
        dbEv.IsActive = false;
        await db.SaveChangesAsync();
        showCancelConfirm = false;
        await NotificationService.NotifyEventCancelledAsync(Id, currentUserId!);
        await LoadEvent();
    }

    private string PageTitle() => ev is null ? "Evento" : $"{ev.Group.Name} · {TypeLabel(ev.PokerEventType)}";

    private static string TypeLabel(PokerEventType? t) => t switch
    {
        PokerEventType.Tournament => "Torneio",
        PokerEventType.CashGame => "Cash Game",
        PokerEventType.HomeGame => "Home Game",
        _ => "Poker"
    };

    private static string TypeBadgeClass(PokerEventType? t) => t switch
    {
        PokerEventType.Tournament => "poker-type-badge--tourney",
        PokerEventType.CashGame => "poker-type-badge--cash",
        PokerEventType.HomeGame => "poker-type-badge--home",
        _ => ""
    };

    private static string ModalityLabel(PokerModality m) => m switch
    {
        PokerModality.Vanilla => "Vanilla (Texas NL)",
        PokerModality.PKO => "PKO",
        PokerModality.Freezeout => "Freezeout",
        PokerModality.PLO4 => "PLO 4 cartas",
        PokerModality.PLO5 => "PLO 5 cartas",
        PokerModality.PLO6 => "PLO 6 cartas",
        PokerModality.DealerChoice => "Dealer's Choice",
        _ => m.ToString()
    };
}
