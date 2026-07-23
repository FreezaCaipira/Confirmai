using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileEditForm
{
    [Parameter]
    public ProfileEditModel EditModel { get; set; } = new();

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string? SaveFeedback { get; set; }

    [Parameter]
    public EventCallback OnSaveProfile { get; set; }

    private async Task HandleSaveProfile()
        => await OnSaveProfile.InvokeAsync();

    public sealed class ProfileEditModel
    {
        [StringLength(120)] public string? InstagramHandle { get; set; }
        [StringLength(120)] public string? DiscordHandle { get; set; }
        [StringLength(160)] public string? PixKey { get; set; }
    }
}
