using Confirmai.Pages.Admin;
using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Review C39-C: picking a group in the partner-share form must load its current share,
/// otherwise saving without retyping silently resets the partnership to 0%.
/// </summary>
public class C39CReviewPartnerShareFormTests
{
    private static readonly IReadOnlyList<GroupPartnerShareRow> Shares =
    [
        new(1, "Casa A", 50m),
        new(2, "Casa B", 100m),
    ];

    [Theory]
    [InlineData(1, 50)]
    [InlineData(2, 100)]
    [InlineData(3, 0)]
    [InlineData(0, 0)]
    public void CurrentPartnerShare_LoadsGroupShare(int groupId, int expected)
        => Assert.Equal((decimal)expected, AdminRevenue.CurrentPartnerShare(Shares, groupId));

    [Fact]
    public void PartnerGroupSelect_RefreshesPercentOnChange()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot(), "Pages/Admin/AdminRevenue.razor"));
        Assert.Contains("@bind=\"partnerGroupId\" @bind:after=\"OnPartnerGroupChanged\"", src);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }
}
