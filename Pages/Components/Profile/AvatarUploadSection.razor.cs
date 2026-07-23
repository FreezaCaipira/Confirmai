using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Confirmai.Pages.Components.Profile;

public partial class AvatarUploadSection
{
    [Parameter]
    public string? CurrentAvatarPath { get; set; }

    [Parameter]
    public IBrowserFile? SelectedFile { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string? Feedback { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnAvatarSelected { get; set; }

    [Parameter]
    public EventCallback OnSaveAvatar { get; set; }

    private async Task HandleAvatarSelected(InputFileChangeEventArgs e)
        => await OnAvatarSelected.InvokeAsync(e.File);

    private async Task HandleSaveAvatar()
        => await OnSaveAvatar.InvokeAsync();
}
