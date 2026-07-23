using System.Reflection;
using Confirmai.Pages;
using Xunit;

namespace Confirmai.Tests;

public class ProfileCharacterizationTests
{
    private static readonly Type ProfileType = typeof(Profile);

    private static string? InvokeNormalizeOptional(string? value)
    {
        var method = ProfileType.GetMethod("NormalizeOptional",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return (string?)method!.Invoke(null, [value]);
    }

    [Fact]
    public void NormalizeOptional_NullValue_ReturnsNull()
    {
        Assert.Null(InvokeNormalizeOptional(null));
    }

    [Fact]
    public void NormalizeOptional_EmptyString_ReturnsNull()
    {
        Assert.Null(InvokeNormalizeOptional(""));
    }

    [Fact]
    public void NormalizeOptional_WhitespaceOnly_ReturnsNull()
    {
        Assert.Null(InvokeNormalizeOptional("   "));
    }

    [Fact]
    public void NormalizeOptional_TabOnly_ReturnsNull()
    {
        Assert.Null(InvokeNormalizeOptional("\t\t"));
    }

    [Fact]
    public void NormalizeOptional_SimpleValue_ReturnsTrimmed()
    {
        Assert.Equal("hello", InvokeNormalizeOptional("hello"));
    }

    [Fact]
    public void NormalizeOptional_ValueWithSpaces_ReturnsTrimmed()
    {
        Assert.Equal("hello", InvokeNormalizeOptional("  hello  "));
    }

    [Fact]
    public void NormalizeOptional_ValueWithTabs_ReturnsTrimmed()
    {
        Assert.Equal("hello", InvokeNormalizeOptional("\thello\t"));
    }

    [Fact]
    public void NormalizeOptional_InternalSpaces_Preserved()
    {
        Assert.Equal("hello world", InvokeNormalizeOptional("  hello world  "));
    }
}
