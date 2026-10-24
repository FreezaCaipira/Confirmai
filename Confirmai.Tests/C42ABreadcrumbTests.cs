using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// C42-A item 4: mobile "back" affordance is a real link to the logical
/// parent (&lt;- {pai}), never history.back(). Desktop keeps the full trail.
/// </summary>
public class C42ABreadcrumbTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C42ABreadcrumbTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    private static HttpClient ClientFor(IntegrationTestWebAppFactory factory, string userId, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", name);
        return client;
    }

    private async Task<int> SeedGroupWithEventAsync(string adminId, string memberId)
    {
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId, "Racha C42");
        await _factory.EnsureUserAsync(memberId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = memberId,
            Role = GroupMemberRole.Member,
        });
        var ev = new Event
        {
            GroupId = groupId,
            Sport = Sport.Futsal,
            Location = "Quadra C42",
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 12,
            MaxGoalkeepers = 2,
            IsActive = true,
            CreatedByUserId = adminId,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    [Fact]
    public async Task TopLevelSubScreen_RendersMobileBackLink_ToHome()
    {
        await _factory.EnsureUserAsync("c42-crumb-1");
        var client = ClientFor(_factory, "c42-crumb-1", "u");

        var html = await (await client.GetAsync("/mailbox")).Content.ReadAsStringAsync();

        // single-segment route: parent is Home
        Assert.Contains("breadcrumb-back", html);
        Assert.Contains("class=\"breadcrumb-back\" href=\"/\"", html);
    }

    [Fact]
    public async Task EventDetail_RendersMobileBackLink_ToGroup()
    {
        const string adminId = "c42-crumb-admin";
        const string memberId = "c42-crumb-member";
        var eventId = await SeedGroupWithEventAsync(adminId, memberId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var groupId = (await db.Events.FindAsync(eventId))!.GroupId;

        var client = ClientFor(_factory, memberId, "member");
        var html = await (await client.GetAsync($"/futsal/{eventId}")).Content.ReadAsStringAsync();

        // event detail resolves the logical parent: the event's group
        Assert.Contains($"class=\"breadcrumb-back\" href=\"/grupo/{groupId}\"", html);
        // desktop trail shows Grupos > {group} > Futsal
        Assert.Contains("breadcrumb-nav", html);
        Assert.Contains("Racha C42", html);
    }

    [Fact]
    public async Task Home_RendersNoBackLink()
    {
        await _factory.EnsureUserAsync("c42-crumb-2");
        var client = ClientFor(_factory, "c42-crumb-2", "u");

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("breadcrumb-back", html);
    }

    private async Task<int> SeedConfirmationAsync(string adminId, string ownerId, string groupName)
    {
        var groupId = await _factory.SeedGroupWithAdminAsync(adminId, groupName);
        await _factory.EnsureUserAsync(ownerId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = ownerId, Role = GroupMemberRole.Member });
        var ev = new Event
        {
            GroupId = groupId,
            Sport = Sport.Futsal,
            Location = "Quadra C42",
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 12,
            MaxGoalkeepers = 2,
            IsActive = true,
            CreatedByUserId = adminId,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var conf = new EventConfirmation
        {
            EventId = ev.Id,
            UserId = ownerId,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield,
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        return conf.Id;
    }

    [Fact]
    public async Task PaymentPage_StrangerDoesNotSeeGroupNameInTrail()
    {
        var confId = await SeedConfirmationAsync("c42-pay-admin2", "c42-pay-owner2", "Racha Secreto C42");
        await _factory.EnsureUserAsync("c42-pay-stranger");
        var client = ClientFor(_factory, "c42-pay-stranger", "stranger");

        var html = await (await client.GetAsync($"/pagamento/evento/{confId}")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("Racha Secreto C42", html);
    }

    [Fact]
    public async Task Header_RendersAppVersion()
    {
        await _factory.EnsureUserAsync("c42-crumb-3");
        var client = ClientFor(_factory, "c42-crumb-3", "u");

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("shell-version", html);
        Assert.Contains($"v{Confirmai.Services.Core.AppVersion.Current}", html);
    }
}

/// <summary>C42-A item 9: AppVersion reads AssemblyInformationalVersion.</summary>
public class C42AppVersionTests
{
    [Fact]
    public void AppVersion_Current_IsSemverWithoutBuildSuffix()
    {
        var v = Confirmai.Services.Core.AppVersion.Current;
        Assert.Matches(@"^\d+\.\d+\.\d+$", v);
        Assert.Equal("0.42.0", v);
    }
}

/// <summary>
/// C42-A item 10: mobile menu order + affordances. Structural test — the
/// ordering lives in CSS (display:contents + order on marker classes) and the
/// Esc close in the layout markup.
/// </summary>
public class C42MobileMenuTests
{
    private static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return File.ReadAllText(Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray()));
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void MainLayout_NavItemsCarryOrderMarkers_AndEscClose()
    {
        var razor = RepoFile("Shared", "Components", "MainLayout.razor");
        foreach (var cls in new[]
                 {
                     "nav-item--home", "nav-item--groups", "nav-item--mailbox",
                     "nav-item--profile", "nav-item--admin", "nav-item--logout"
                 })
            Assert.Contains(cls, razor);
        Assert.Contains("nav-language-label", razor);
        Assert.Contains("OnNavKeyDown", razor);
    }

    [Fact]
    public void InviteBlock_WhatsAppCtaFirst_FieldLink_AndLiveFeedback()
    {
        var razor = RepoFile("Shared", "Components", "Groups", "GroupInviteBlock.razor");
        // CTA primario e o primeiro elemento interativo do bloco
        Assert.Contains("btn btn-whatsapp invite-cta", razor);
        Assert.True(razor.IndexOf("invite-cta", StringComparison.Ordinal)
                    < razor.IndexOf("invite-field", StringComparison.Ordinal),
            "WhatsApp CTA must come before the link field");
        Assert.Contains("invite-field-text", razor);
        Assert.Contains("aria-live=\"polite\"", razor);
        Assert.Contains("Group.InviteWhatsApp", razor);
    }

    [Fact]
    public void ShellCss_BrandMark_KeepsAccentTile()
    {
        var css = RepoFile("wwwroot", "css", "shell.css");
        var start = css.IndexOf(".shell-brand-mark {", StringComparison.Ordinal);
        var rule = css.Substring(start, css.IndexOf('}', start) - start);

        Assert.Contains("background: var(--accent)", rule);
        Assert.Contains("width: 28px", rule);
    }

    [Fact]
    public void ShellCss_MobileMenu_HasOrderedItems_AndL3Panel()
    {
        var css = RepoFile("wwwroot", "css", "shell.css");
        var mobile = css.Substring(css.IndexOf("@media (max-width: 768px)", StringComparison.Ordinal));

        Assert.Contains("display: contents", mobile);
        Assert.Contains(".nav-item--mailbox     { order: 2; }", mobile);
        Assert.Contains(".nav-item--logout      { order: 7; }", mobile);
        Assert.Contains("background: var(--surface-3)", mobile);
        Assert.Contains("min-height: 48px", mobile);
        Assert.Contains("var(--danger)", mobile);
    }
}
