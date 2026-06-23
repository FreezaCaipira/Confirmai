using Confirmai.Services.Admin;
using Microsoft.Extensions.Primitives;

namespace Confirmai.Tests;

public class AdminLogsQueryOverridesParserStaticTests
{
    [Fact]
    public void ReadString_ValidKey_ReturnsValue()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "testValue" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as string;

        Assert.Equal("testValue", result);
    }

    [Fact]
    public void ReadString_MissingKey_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "testValue" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "missingKey" }) as string;

        Assert.Null(result);
    }

    [Fact]
    public void ReadString_EmptyValue_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as string;

        Assert.Null(result);
    }

    [Fact]
    public void ReadString_WhitespaceValue_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "   " }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as string;

        Assert.Null(result);
    }

    [Fact]
    public void ReadDate_ValidDate_ReturnsDateTime()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "2026-05-21" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as DateTime?;

        Assert.Equal(new DateTime(2026, 5, 21), result);
    }

    [Fact]
    public void ReadDate_InvalidDate_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "2026-99-99" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as DateTime?;

        Assert.Null(result);
    }

    [Fact]
    public void ReadDate_MissingKey_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "2026-05-21" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "missingKey" }) as DateTime?;

        Assert.Null(result);
    }

    [Fact]
    public void ReadDate_EmptyValue_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as DateTime?;

        Assert.Null(result);
    }

    [Fact]
    public void ReadDate_WrongFormat_ReturnsNull()
    {
        var query = new Dictionary<string, StringValues>
        {
            { "testKey", "01/01/2026" }
        };

        var method = typeof(AdminLogsQueryOverridesParser).GetMethod("ReadDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { query, "testKey" }) as DateTime?;

        Assert.Null(result);
    }
}
