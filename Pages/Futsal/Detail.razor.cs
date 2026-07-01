using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Confirmai.Services;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Futsal;

public partial class Detail
{
    [Parameter] public int Id { get; set; }

    private Event?      ev             = null;
    private bool        isLoading         = true;
    private string?     currentUserId     = null;
    private ApplicationUser? creatorUser   = null;
    private FutsalPosition chosenPos      = FutsalPosition.Outfield;
    private string      actionError       = string.Empty;
    private bool        isSystemAdmin     = false;
    private int?        confirmRemoveId      = null;
    private int?        confirmPayId         = null;
    private int         eventNumber          = 0;
    private bool        confirmCancel        = false;
    private WaitingList? userWaitlistEntry   = null;
    private bool        confirmLeaveWaitlist = false;
    private GroupJoinRequest? userJoinRequest = null;
    private bool        requestingJoin      = false;
    private string      joinRequestError    = string.Empty;
    private List<PostMatchVote> detailMvpVotes = new();

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        isSystemAdmin = auth.User.IsInRole("admin");
        await LoadEvent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || ev is null) return;
        var mapsKey    = Config["Google:MapsApiKey"];
        var hasMapsKey = !string.IsNullOrWhiteSpace(mapsKey) && !mapsKey!.Contains("SET_VIA");
        if (!hasMapsKey) return;
        var mapWrapId = $"map-wrap-{ev.Id}";
        var mapFbId   = $"map-fallback-{ev.Id}";
        try
        {
            await JS.InvokeVoidAsync("ConfirmaiSetupMapFallback", mapWrapId, mapFbId);
        }
        catch (JSDisconnectedException)
        {
            // Blazor circuit was disconnected during render — safely ignore
        }
        catch (OperationCanceledException)
        {
            // User navigated away before render completed — safely ignore
        }
        catch (Exception)
        {
            // Function may not be available in certain prerender scenarios — safely ignore
        }
    }

    private async Task LoadEvent()
    {
        isLoading = true;
        await using var db = await DbFactory.CreateDbContextAsync();
        ev = await db.Events
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .Include(e => e.Venue)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .Include(e => e.WaitingList)
                .ThenInclude(w => w.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Futsal);
        if (ev?.CreatedByUserId is not null)
            creatorUser = await db.Users.FindAsync(ev.CreatedByUserId) as ApplicationUser;
        userWaitlistEntry = currentUserId is not null
            ? ev?.WaitingList.FirstOrDefault(w => w.UserId == currentUserId)
            : null;

        userJoinRequest = null;
        if (currentUserId is not null && ev is not null)
        {
            userJoinRequest = await db.GroupJoinRequests
                .Where(r => r.GroupId == ev.GroupId && r.UserId == currentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();
        }

        detailMvpVotes = new();
        if (ev is not null && ev.Group.EnablePostMatchRanking)
        {
            var evEndsAt = ev.StartsAt.AddMinutes(ev.DurationMinutes ?? 120);
            if (evEndsAt < DateTime.UtcNow)
                detailMvpVotes = await db.PostMatchVotes.Where(v => v.EventId == ev.Id).ToListAsync();
        }

        if (ev is not null)
            eventNumber = 1 + await db.Events
                .CountAsync(e => e.GroupId == ev.GroupId && e.StartsAt < ev.StartsAt);

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

    private bool IsGroupAdmin() => EventAccess.IsAdmin(ev, currentUserId);

    private async Task ConfirmAs(FutsalPosition pos)
    {
        chosenPos = pos;
        await ConfirmPresence();
    }

    private async Task ConfirmPresence()
    {
        if (currentUserId is null || ev is null) return;
        actionError = string.Empty;

        await using var db = await DbFactory.CreateDbContextAsync();

        // Avoid double-confirm
        var existing = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == Id && c.UserId == currentUserId);
        if (existing is not null) { actionError = "Você já está confirmado."; return; }

        // Avoid duplicate waitlist entry
        var existingWait = await db.WaitingLists
            .FirstOrDefaultAsync(w => w.EventId == Id && w.UserId == currentUserId);
        if (existingWait is not null) { actionError = "Você já está na lista de espera."; return; }

        // Re-check slot availability at DB level (not stale UI state)
        var eventEntity = await db.Events
            .Include(e => e.Confirmations)
            .FirstOrDefaultAsync(e => e.Id == Id);
        if (eventEntity is null) return;

        bool isGk       = chosenPos == FutsalPosition.Goalkeeper;
        int  currentGk  = eventEntity.Confirmations.Count(c => c.Position == FutsalPosition.Goalkeeper);
        int  currentOut = eventEntity.Confirmations.Count(c => c.Position != FutsalPosition.Goalkeeper);
        int  maxOutfield = eventEntity.MaxGoalkeepers.HasValue
            ? eventEntity.MaxPlayers - eventEntity.MaxGoalkeepers.Value
            : eventEntity.MaxPlayers;

        bool slotFull = isGk
            ? (eventEntity.MaxGoalkeepers > 0 && currentGk >= eventEntity.MaxGoalkeepers.Value)
            : (maxOutfield > 0 && currentOut >= maxOutfield);

        if (slotFull)
        {
            var wCount = await db.WaitingLists.CountAsync(w => w.EventId == Id);
            db.WaitingLists.Add(new WaitingList
            {
                EventId         = Id,
                UserId          = currentUserId,
                Position        = wCount + 1,
                DesiredPosition = eventEntity.MaxGoalkeepers > 0 ? chosenPos : null,
                JoinedAt        = DateTime.UtcNow,
            });
        }
        else
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId     = Id,
                UserId      = currentUserId,
                Position    = eventEntity.MaxGoalkeepers > 0 ? chosenPos : FutsalPosition.Outfield,
                ConfirmedAt = DateTime.UtcNow,
            });
        }

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
            var position = conf.Position ?? FutsalPosition.Outfield;
            db.EventConfirmations.Remove(conf);
            await db.SaveChangesAsync();
            await PromoteFromWaitlistAsync(db, position);
        }
        confirmCancel = false;
        await LoadEvent();
    }

    private async Task LeaveWaitlist()
    {
        if (currentUserId is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var entry = await db.WaitingLists
            .FirstOrDefaultAsync(w => w.EventId == Id && w.UserId == currentUserId);
        if (entry is not null)
        {
            db.WaitingLists.Remove(entry);
            await db.SaveChangesAsync();
        }
        confirmLeaveWaitlist = false;
        await LoadEvent();
    }

    /// <summary>Promove o primeiro da lista de espera para a posição liberada.</summary>
    private async Task PromoteFromWaitlistAsync(AppDbContext db, FutsalPosition position)
    {
        var next = await db.WaitingLists
            .Where(w => w.EventId == Id &&
                        (w.DesiredPosition == null || w.DesiredPosition == position))
            .OrderBy(w => w.Position)
            .ThenBy(w => w.JoinedAt)
            .FirstOrDefaultAsync();
        if (next is null) return;

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId     = Id,
            UserId      = next.UserId,
            Position    = position,
            ConfirmedAt = DateTime.UtcNow,
        });
        db.WaitingLists.Remove(next);
        await db.SaveChangesAsync();

        _ = Task.Run(() => NotificationService.NotifyWaitlistPromotedAsync(Id, next.UserId));
    }

    private async Task AdminTogglePaid(int confirmationId)
    {
        if (!IsGroupAdmin()) return;
        var result = await AdminConfirmationService.TogglePaidAsync(confirmationId, currentUserId!);
        if (result.Found && result.Updated)
        {
            confirmPayId = null;
            await LoadEvent();
        }
    }

    private async Task AdminRemoveConfirmation(int confirmationId)
    {
        if (!IsGroupAdmin()) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations.FindAsync(confirmationId);
        if (conf is null) return;
        var position = conf.Position ?? FutsalPosition.Outfield;
        var targetUserId = conf.UserId;
        db.EventConfirmations.Remove(conf);
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, position);
        await LogService.AuditAsync(AuditEvents.EventConfirmationRemoved, AuditEntities.EventConfirmation, confirmationId.ToString(),
            $"Admin removeu jogador {targetUserId} da confirmação #{confirmationId} (evento #{ev?.Id})", currentUserId, "EventAdmin");
        confirmRemoveId = null;
        await LoadEvent();
    }

    private async Task AdminRemoveFromWaitlist(int waitlistId)
    {
        if (!IsGroupAdmin()) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var entry = await db.WaitingLists.FindAsync(waitlistId);
        if (entry is not null)
        {
            var wlUserId = entry.UserId;
            db.WaitingLists.Remove(entry);
            await db.SaveChangesAsync();
            await LogService.AuditAsync(AuditEvents.EventWaitlistRemoved, AuditEntities.EventConfirmation, waitlistId.ToString(),
                $"Admin removeu jogador {wlUserId} da lista de espera (evento #{ev?.Id})", currentUserId, "EventAdmin");
        }
        await LoadEvent();
    }

    private async Task AdminAddOutfieldSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(ev.Id);
        if (evt is null) return;
        evt.MaxPlayers += 1;
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, FutsalPosition.Outfield);
        await LoadEvent();
    }

    private async Task AdminRemoveOutfieldSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        var minInfo = EventMinimums.Get(ev.Sport);
        var currentMaxOut = ev.MaxGoalkeepers.HasValue ? ev.MaxPlayers - ev.MaxGoalkeepers.Value : ev.MaxPlayers;
        var currentConfOut = ev.Confirmations.Count(c => c.Position != FutsalPosition.Goalkeeper);
        if (currentMaxOut <= minInfo.MinOutfield || currentMaxOut <= currentConfOut) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(ev.Id);
        if (evt is null) return;
        evt.MaxPlayers -= 1;
        await db.SaveChangesAsync();
        await LoadEvent();
    }

    private async Task AdminAddGoalkeeperSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(ev.Id);
        if (evt is null) return;
        evt.MaxGoalkeepers = (evt.MaxGoalkeepers ?? 0) + 1;
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, FutsalPosition.Goalkeeper);
        await LoadEvent();
    }

    private async Task AdminRemoveGoalkeeperSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        var minInfo = EventMinimums.Get(ev.Sport);
        var currentGk = ev.MaxGoalkeepers ?? 0;
        var currentConfGk = ev.Confirmations.Count(c => c.Position == FutsalPosition.Goalkeeper);
        if (currentGk <= minInfo.MinGoalkeepers || currentGk <= currentConfGk) return;
        await using var db = await DbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(ev.Id);
        if (evt is null) return;
        evt.MaxGoalkeepers = currentGk - 1;
        await db.SaveChangesAsync();
        await LoadEvent();
    }

    private void NavigateToPaymentsModal(int confirmationId)
    {
        NavigationManager.NavigateTo($"/pagamento/evento/{confirmationId}");
    }

    // Callback wrappers for child components
    private async Task ConfirmAsCallback()
        => await ConfirmPresence();

    private Task ConfirmPayIdChangedCallback(int? value)
    { confirmPayId = value; return Task.CompletedTask; }

    private Task ConfirmRemoveIdChangedCallback(int? value)
    { confirmRemoveId = value; return Task.CompletedTask; }

    private Task ConfirmCancelChangedCallback(bool value)
    { confirmCancel = value; return Task.CompletedTask; }

    private async Task CancelConfirmationCallback()
        => await CancelConfirmation();

    private async Task AdminTogglePaidCallback(int confirmationId)
        => await AdminTogglePaid(confirmationId);

    private async Task AdminRemoveConfirmationCallback(int confirmationId)
        => await AdminRemoveConfirmation(confirmationId);

    private async Task AdminAddOutfieldSlotCallback()
        => await AdminAddOutfieldSlot();

    private async Task AdminRemoveOutfieldSlotCallback()
        => await AdminRemoveOutfieldSlot();

    private async Task AdminAddGoalkeeperSlotCallback()
        => await AdminAddGoalkeeperSlot();

    private async Task AdminRemoveGoalkeeperSlotCallback()
        => await AdminRemoveGoalkeeperSlot();

    private Task NavigateToPaymentsModalCallback(int confirmationId)
    { NavigateToPaymentsModal(confirmationId); return Task.CompletedTask; }
}
