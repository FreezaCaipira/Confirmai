using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Groups;

public partial class Join
{
    [Parameter] public string Code { get; set; } = string.Empty;

    private Group?      group         = null;
    private List<Event> recentEvents  = new();
    private bool        isLoading     = true;
    private string?     currentUserId = null;
    private bool        alreadyMember = false;
    private bool        joined        = false;
    private string      actionError   = string.Empty;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private Confirmai.Services.Groups.GroupDetailService GroupDetailService { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;

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
            .FirstOrDefaultAsync(g => g.InviteCode == Code && g.IsActive);

        // C42-B: evento de metrica — abertura do convite (funil H2).
        // Sem o codigo nem o usuario anonimo: so o grupo quando valido.
        await LogService.AuditAsync(
            AuditEvents.GroupInviteOpened,
            AuditEntities.Group,
            group?.Id.ToString(),
            group is not null
                ? $"Invite page opened for group #{group.Id}."
                : "Invite page opened with an invalid code.",
            actorUserId: currentUserId,
            source: "Metrics",
            metadata: new { groupId = group?.Id, valid = group is not null, loggedIn = currentUserId is not null });

        if (group is not null)
        {
            recentEvents = await db.Events
                .Where(e => e.GroupId == group.Id)
                .Include(e => e.Confirmations)
                .OrderByDescending(e => e.StartsAt)
                .Take(10)
                .ToListAsync();

            if (currentUserId is not null)
            {
                alreadyMember = await db.GroupMembers
                    .AnyAsync(m => m.GroupId == group.Id && m.UserId == currentUserId);
            }
        }

        isLoading = false;
    }

    private async Task JoinGroup()
    {
        if (currentUserId is null || group is null) return;
        actionError = string.Empty;

        try
        {
            var result = await GroupDetailService.JoinWithCodeAsync(group.Id, currentUserId, Code);
            if (result == Confirmai.Services.Groups.JoinWithCodeResult.Joined)
            {
                joined = true;
            }
            else if (result == Confirmai.Services.Groups.JoinWithCodeResult.AlreadyMember)
            {
                alreadyMember = true;
            }
            else
            {
                actionError = Ui["GroupEntry.InvalidCode"];
            }
        }
        catch (Exception)
        {
            actionError = Ui["Group.JoinError"];
        }
    }
}
