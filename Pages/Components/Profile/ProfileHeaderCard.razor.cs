using System.Globalization;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileHeaderCard
{
    [Parameter]
    public ApplicationUser? User { get; set; }

    [Parameter]
    public bool IsOwnProfile { get; set; }

    [Parameter]
    public string? CurrentAvatarPath { get; set; }

    [Parameter]
    public IBrowserFile? SelectedFile { get; set; }

    [Parameter]
    public bool IsSavingAvatar { get; set; }

    [Parameter]
    public string? AvatarFeedback { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnAvatarSelected { get; set; }

    [Parameter]
    public EventCallback OnSaveAvatar { get; set; }

    private string DisplayName => User?.FullName?.Trim() is { Length: > 0 } name
        ? name
        : User?.UserName ?? "—";

    private string AvatarInitial => DisplayName.Length > 0
        ? DisplayName[0].ToString().ToUpperInvariant()
        : "?";

    private async Task HandleAvatarSelected(InputFileChangeEventArgs e)
        => await OnAvatarSelected.InvokeAsync(e.File);

    private async Task HandleSaveAvatar()
        => await OnSaveAvatar.InvokeAsync();
}
