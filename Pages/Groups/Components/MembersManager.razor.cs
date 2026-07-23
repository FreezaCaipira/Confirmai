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

    private async Task HandleSetMemberRole(int memberId, GroupMemberRole newRole)
        => await OnSetMemberRole.InvokeAsync((memberId, newRole));

    private async Task HandleInitiatePromote(int memberId)
        => await OnInitiatePromote.InvokeAsync(memberId);

    private async Task HandleCancelPromote()
        => await OnCancelPromote.InvokeAsync();
}
