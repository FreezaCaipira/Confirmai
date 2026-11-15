using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Shared.Helpers;

namespace Confirmai.Tests;

/// <summary>C45: saudacao rotativa, logo local da quadra e guardioes CSS dos 9 pontos.</summary>
public class C45UiPolishTests
{
    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }

    private static string Read(string rel) =>
        File.ReadAllText(Path.Combine(RepoRoot().FullName, rel.Replace('/', Path.DirectorySeparatorChar)));

    private static string Block(string css, string selector)
    {
        var i = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(i >= 0, $"selector not found: {selector}");
        return css.Substring(i, css.IndexOf('}', i) - i);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public void Greetings_AllRotatingKeysExist_InEveryLanguage(string lang)
    {
        var pref = new LanguagePreferenceService();
        pref.SetLanguage(lang);
        var ui = new UiTextService(pref);
        for (var i = 1; i <= HomeToday.GreetingCount; i++)
        {
            var key = $"Index.Greeting{i}";
            Assert.NotEqual(key, ui[key]);
        }
    }

    [Fact]
    public void Greeting_IsStableWithinTheDay_AndRotatesAcrossDays()
    {
        var morning = new DateTime(2026, 11, 15, 8, 0, 0);
        var night = new DateTime(2026, 11, 15, 23, 0, 0);
        Assert.Equal(HomeToday.GreetingOfTheDay(morning), HomeToday.GreetingOfTheDay(night));
        Assert.NotEqual(HomeToday.GreetingOfTheDay(morning), HomeToday.GreetingOfTheDay(morning.AddDays(1)));
        Assert.InRange(HomeToday.GreetingOfTheDay(morning), 0, HomeToday.GreetingCount - 1);
    }

    [Theory]
    [InlineData("/images/venues/bandolim.webp", true)]
    [InlineData("https://evil.example/x.png", false)]
    [InlineData("//evil.example/x.png", false)]
    [InlineData("images/x.png", false)]
    [InlineData("/x\\..\\y.png", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void VenueLogo_OnlyAcceptsLocalPaths(string? path, bool expected)
        => Assert.Equal(expected, VenueLogo.IsLocalPath(path));

    [Fact]
    public void BandolimLogo_IsShippedLocally()
        => Assert.True(File.Exists(Path.Combine(RepoRoot().FullName, "wwwroot", "images", "venues", "bandolim.webp")));

    [Fact]
    public void PlayerGroupHeaders_ShareOnePattern()
    {
        var css = Read("wwwroot/css/event-detail.css");
        var header = Block(css, ".players-group .players-group-header {");
        Assert.Contains("background: var(--players-soft);", header);
        Assert.Contains("border-color: var(--players-border);", header);
        Assert.DoesNotContain(".players-group--line .players-group-header", css);
        Assert.DoesNotContain(".players-group--gk .players-group-header", css);
        Assert.DoesNotContain("rgba(", Block(css, ".players-group--line {"));
    }

    [Theory]
    [InlineData("Shared/Components/SidebarVenuesCard.razor.css")]
    [InlineData("Pages/VenueManager/VenueEdit.razor.css")]
    public void LogoFrames_UseToken_NotHardcodedWhite(string rel)
    {
        var css = Read(rel);
        Assert.Contains("var(--logo-bg)", css);
        Assert.DoesNotContain("#fff", css);
    }

    [Fact]
    public void GroupHeader_HasBackLinkWithRealParentUrl()
    {
        var razor = Read("Shared/Components/Groups/GroupHeader.razor");
        Assert.Contains("group-header-back", razor);
        Assert.Contains("\"/grupos\"", razor);
        Assert.DoesNotContain("history.back", razor);
    }

    [Fact]
    public void Home_NoLongerListsGroups_ScheduleMovedToGroupsPage()
    {
        Assert.DoesNotContain("home-group-chip", Read("Pages/Index.razor"));
        Assert.DoesNotContain("home-group-chip", Read("Pages/Index.razor.css"));
        Assert.Contains("group-row-schedule", Read("Pages/Groups/Index.razor"));
    }

    [Fact]
    public void GroupEntryCompact_CentersActions()
    {
        var css = Read("Shared/Components/Groups/GroupEntryPanel.razor.css");
        var actions = Block(css, ".group-entry--compact .group-entry-option > .btn,");
        Assert.Contains("align-self: center;", actions);
        Assert.Contains("margin-top: 0;", actions);
    }

    [Fact]
    public void SidebarVenuesCard_TimerNotTiedToFirstRender_ReloadsOnNavigation_AndDisposes()
    {
        var razor = Read("Shared/Components/SidebarVenuesCard.razor");
        Assert.Contains("OnAfterRender(bool firstRender) => EnsureTimer();", razor);
        Assert.DoesNotContain("if (firstRender", razor);
        Assert.Contains("Navigation.LocationChanged += OnLocationChanged;", razor);
        Assert.Contains("Navigation.LocationChanged -= OnLocationChanged;", razor);
        Assert.Contains("timer?.Dispose()", razor);
        Assert.Contains("VenueLogo.IsLocalPath", razor);
    }
}
