using System.Globalization;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileHeaderCard
{
    [Parameter]
    public ApplicationUser? User { get; set; }

    private string DisplayName => User?.FullName?.Trim() is { Length: > 0 } name
        ? name
        : User?.UserName ?? "—";

    private string AvatarInitial => DisplayName.Length > 0
        ? DisplayName[0].ToString().ToUpperInvariant()
        : "?";
}
