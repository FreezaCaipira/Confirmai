using Confirmai.Models;

namespace Confirmai.Services.Groups;

/// <summary>
/// Pure, side-effect-free business rules for group feature toggles.
/// Extracted from the Features code-behind so the cascade behaviour can be
/// unit-tested without a Blazor circuit or database.
/// </summary>
public static class GroupFeatureRules
{
    /// <summary>
    /// Applies the payment-gateways toggle to <paramref name="group"/>. When the
    /// gateways are turned OFF, the group falls back to the organizer's manual Pix
    /// and the paid extras (post-match ranking and best-player voting) are disabled
    /// as well. Turning gateways ON never re-enables those extras automatically.
    /// </summary>
    public static void ApplyPaymentGatewaysToggle(Group group, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.EnablePaymentGateways = enabled;

        if (!enabled)
        {
            group.EnablePostMatchRanking = false;
            group.EnableBestPlayerVoting = false;
        }
    }
}
