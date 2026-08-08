namespace Confirmai.Pages.Components.Profile;

/// <summary>
/// Determines whether the Pix key should be displayed on a profile.
/// Pix is a payment key — it must only be visible to the profile owner,
/// never to visitors (Fase 0, Ciclo 26).
/// </summary>
public static class ProfilePixVisibility
{
    public static bool ShouldShowPix(bool isOwnProfile, bool hasPixKey)
        => isOwnProfile && hasPixKey;
}
