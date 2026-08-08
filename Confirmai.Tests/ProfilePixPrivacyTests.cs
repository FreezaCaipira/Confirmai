using Confirmai.Models;
using Confirmai.Pages.Components.Profile;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Tests that the Pix key is only visible to the profile owner, not to visitors.
/// Fase 0 — Ciclo 26: privacy fix for ProfileContactsDisplay.
/// </summary>
public class ProfilePixPrivacyTests
{
    [Fact]
    public void ShouldShowPix_OwnProfile_ReturnsTrue()
    {
        Assert.True(ProfilePixVisibility.ShouldShowPix(isOwnProfile: true, hasPixKey: true));
    }

    [Fact]
    public void ShouldShowPix_Visitor_ReturnsFalse()
    {
        Assert.False(ProfilePixVisibility.ShouldShowPix(isOwnProfile: false, hasPixKey: true));
    }

    [Fact]
    public void ShouldShowPix_NoPixKey_ReturnsFalse()
    {
        Assert.False(ProfilePixVisibility.ShouldShowPix(isOwnProfile: true, hasPixKey: false));
    }

    [Fact]
    public void ShouldShowPix_VisitorNoPixKey_ReturnsFalse()
    {
        Assert.False(ProfilePixVisibility.ShouldShowPix(isOwnProfile: false, hasPixKey: false));
    }
}
