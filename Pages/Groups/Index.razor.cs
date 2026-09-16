using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Groups;

public partial class Index
{
    private string?      currentUserId = null;
    private List<Group>  groups        = new();
    private bool         isLoading     = true;
    private Dictionary<int, int> pendingJoinRequestsByGroup = new();
    private Dictionary<int, List<DayOfWeek>> weeklySchedulesByGroup = new();

    private static readonly string[] WeekdayNamesPt =
    { "Domingos", "Segundas", "Terças", "Quartas", "Quintas", "Sextas", "Sábados" };

    private string       joinCode      = string.Empty;
    private string       joinCodeError = string.Empty;
    private bool         isProcessingApproval = false;
    private int?         processingGroupId = null;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private Confirmai.Services.Groups.GroupDetailService GroupDetailService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LoadGroups();
    }

    private async Task LoadGroups()
    {
        if (currentUserId is null) return;
        isLoading = true;
        await using var db = await DbFactory.CreateDbContextAsync();

        // First: get group IDs
        var userGroupIds = await db.GroupMembers
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.GroupId)
            .Distinct()
            .ToListAsync();

        if (!userGroupIds.Any())
        {
            groups = new List<Group>();
            isLoading = false;
            return;
        }

        // Second: load groups by ID - separate Groups from Events first
        groups = await db.Groups
            .Where(g => userGroupIds.Contains(g.Id))
            .OrderByDescending(g => g.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin))
            .ThenBy(g => g.Name)
            .ToListAsync();

        // Third: explicitly load Members for each group
        foreach (var group in groups)
        {
            // Force load Members
            await db.Entry(group).Collection(g => g.Members).LoadAsync();
            // Force load Events
            await db.Entry(group).Collection(g => g.Events).LoadAsync();
        }

        // Load weekly schedules for groups
        var schedules = await db.RachaSchedules
            .Where(rs => userGroupIds.Contains(rs.GroupId) && rs.IsActive)
            .Select(rs => new { rs.GroupId, rs.DayOfWeek })
            .ToListAsync();
        weeklySchedulesByGroup = schedules
            .GroupBy(s => s.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.DayOfWeek).Distinct().OrderBy(d => (int)d).ToList());

        var adminGroupIds = groups
            .Where(IsCurrentUserAdmin)
            .Select(g => g.Id)
            .ToList();

        pendingJoinRequestsByGroup = adminGroupIds.Count == 0
            ? new Dictionary<int, int>()
            : await db.GroupJoinRequests
                .Where(r => adminGroupIds.Contains(r.GroupId) && r.Status == JoinRequestStatus.Pending)
                .GroupBy(r => r.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupId, x => x.Count);

        groups = groups
            .OrderByDescending(g => GetPendingRequestsCount(g.Id) > 0)
            .ThenByDescending(IsCurrentUserAdmin)
            .ThenBy(g => g.Name)
            .ToList();

        isLoading = false;
    }

    private bool IsCurrentUserAdmin(Group group)
        => currentUserId is not null && group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);

    private int GetPendingRequestsCount(int groupId)
        => pendingJoinRequestsByGroup.TryGetValue(groupId, out var pendingCount) ? pendingCount : 0;

    private bool GroupNeedsPixSetup(Group group)
        => IsCurrentUserAdmin(group)
        && !group.Members.Any(m => m.Role == GroupMemberRole.Admin && !string.IsNullOrWhiteSpace(m.User?.PixKey));

    private void JoinWithCode()
    {
        var code = joinCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            joinCodeError = Ui["GroupEntry.CodeRequired"];
            return;
        }
        NavigationManager.NavigateTo($"/convite/{code}");
    }

    private string GetDefaultBackgroundStyle(Sport sport)
    {
        return sport == Sport.Futsal
            ? "background: linear-gradient(135deg, #1a472e 0%, #0f2619 100%);"  // Green for futsal
            : "background: linear-gradient(135deg, #2d1a47 0%, #1a0f26 100%);"  // Purple for poker
        ;
    }

    private async Task ApproveAllPending(int groupId)
    {
        if (currentUserId is null) return;
        isProcessingApproval = true;
        processingGroupId = groupId;

        try
        {
            await GroupDetailService.ApproveAllPendingAsync(groupId, currentUserId);
            await LoadGroups();
        }
        finally
        {
            isProcessingApproval = false;
            processingGroupId = null;
        }
    }
}
