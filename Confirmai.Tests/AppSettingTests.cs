using Confirmai.Models;

namespace Confirmai.Tests;

public class AppSettingTests
{
    [Fact]
    public void Constructor_InitializesPropertiesWithEmptyStrings()
    {
        var setting = new AppSetting();

        Assert.Equal(string.Empty, setting.Key);
        Assert.Equal(string.Empty, setting.Value);
    }

    [Fact]
    public void Key_CanBeSet()
    {
        var setting = new AppSetting();
        setting.Key = "test-key";

        Assert.Equal("test-key", setting.Key);
    }

    [Fact]
    public void Value_CanBeSet()
    {
        var setting = new AppSetting();
        setting.Value = "test-value";

        Assert.Equal("test-value", setting.Value);
    }

    [Fact]
    public void Key_AcceptsNull()
    {
        var setting = new AppSetting();
        setting.Key = null!;

        Assert.Null(setting.Key);
    }

    [Fact]
    public void Value_AcceptsNull()
    {
        var setting = new AppSetting();
        setting.Value = null!;

        Assert.Null(setting.Value);
    }

    [Fact]
    public void Key_AcceptsEmptyString()
    {
        var setting = new AppSetting();
        setting.Key = string.Empty;

        Assert.Equal(string.Empty, setting.Key);
    }

    [Fact]
    public void Value_AcceptsEmptyString()
    {
        var setting = new AppSetting();
        setting.Value = string.Empty;

        Assert.Equal(string.Empty, setting.Value);
    }
}
