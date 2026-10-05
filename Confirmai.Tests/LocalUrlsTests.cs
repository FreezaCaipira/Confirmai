using Confirmai.Services.Core;
using Xunit;

namespace Confirmai.Tests;

/// <summary>C38 F5 — o returnUrl do profile nunca pode virar open redirect.</summary>
public class LocalUrlsTests
{
    [Theory]
    [InlineData("/futsal/create?groupId=3", true)]
    [InlineData("/poker/create?groupId=1", true)]
    [InlineData("~/futsal/create", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("https://evil.com/x", false)]
    [InlineData("http://evil.com", false)]
    [InlineData("//evil.com/x", false)]
    [InlineData("/\\evil.com", false)]
    [InlineData("futsal/create", false)]
    [InlineData("javascript:alert(1)", false)]
    public void IsLocal_OnlyRelativePaths(string? url, bool expected)
        => Assert.Equal(expected, LocalUrls.IsLocal(url));
}
