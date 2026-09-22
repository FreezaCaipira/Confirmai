using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Groups.Components;

public partial class MembersManager
{
    [Parameter]
    public List<GroupMember> Members { get; set; } = new();

    [Parameter]
    public string CurrentUserId { get; set; } = string.Empty;

    [Parameter]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Parameter]
    public bool CanManageAdminRoles { get; set; }

    [Parameter]
    public int AdminCount { get; set; }

    [Parameter]
    public bool IsSavingRole { get; set; }

    [Parameter]
    public string RoleMessage { get; set; } = string.Empty;

    [Parameter]
    public bool RoleError { get; set; }

    [Parameter]
    public int? ConfirmPromoteMemberId { get; set; }

    [Parameter]
    public EventCallback<(int, GroupMemberRole)> OnSetMemberRole { get; set; }

    [Parameter]
    public EventCallback<int> OnInitiatePromote { get; set; }

    [Parameter]
    public EventCallback OnCancelPromote { get; set; }

    private int? openMenuMemberId;

    private void ToggleMenu(int memberId)
        => openMenuMemberId = openMenuMemberId == memberId ? null : memberId;

    private async Task PromoteFromMenu(int memberId)
    {
        openMenuMemberId = null;
        await OnInitiatePromote.InvokeAsync(memberId);
    }

    private async Task DemoteFromMenu(int memberId)
    {
        openMenuMemberId = null;
        await OnSetMemberRole.InvokeAsync((memberId, GroupMemberRole.Member));
    }

    private async Task HandleSetMemberRole(int memberId, GroupMemberRole newRole)
        => await OnSetMemberRole.InvokeAsync((memberId, newRole));

    private async Task HandleCancelPromote()
        => await OnCancelPromote.InvokeAsync();

    private static string Initials(GroupMember m)
    {
        var name = m.User?.FullName ?? m.User?.UserName ?? "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant(),
        };
    }
}
