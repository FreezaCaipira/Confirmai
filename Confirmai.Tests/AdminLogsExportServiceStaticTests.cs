using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogsExportServiceStaticTests
{
    [Fact]
    public void NormalizeForFileName_Alphanumeric_ReturnsUnchanged()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test123" }) as string;

        // Assert
        Assert.Equal("test123", result);
    }

    [Fact]
    public void NormalizeForFileName_WithSpaces_ReplacesWithDashes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test Name" }) as string;

        // Assert
        Assert.Equal("test-name", result);
    }

    [Fact]
    public void NormalizeForFileName_WithSpecialChars_ReplacesWithDashes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test@#$%Name" }) as string;

        // Assert
        Assert.Equal("test----name", result);
    }

    [Fact]
    public void NormalizeForFileName_WithUnderscore_KeepsUnderscore()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test_Name" }) as string;

        // Assert
        Assert.Equal("test_name", result);
    }

    [Fact]
    public void NormalizeForFileName_WithDash_KeepsDash()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test-Name" }) as string;

        // Assert
        Assert.Equal("test-name", result);
    }

    [Fact]
    public void NormalizeForFileName_WithLeadingTrailingDashes_TrimsDashes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "-Test-Name-" }) as string;

        // Assert
        Assert.Equal("test-name", result);
    }

    [Fact]
    public void NormalizeForFileName_WithUppercase_ConvertsToLowercase()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("NormalizeForFileName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "TESTNAME" }) as string;

        // Assert
        Assert.Equal("testname", result);
    }

    [Fact]
    public void EscapeCsv_Null_ReturnsEmptyQuotes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("EscapeCsv", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { null }) as string;

        // Assert
        Assert.Equal("\"\"", result);
    }

    [Fact]
    public void EscapeCsv_Empty_ReturnsEmptyQuotes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("EscapeCsv", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "" }) as string;

        // Assert
        Assert.Equal("\"\"", result);
    }

    [Fact]
    public void EscapeCsv_WithQuotes_DoublesQuotes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("EscapeCsv", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test \"Value\"" }) as string;

        // Assert
        Assert.Equal("\"Test \"\"Value\"\"\"", result);
    }

    [Fact]
    public void EscapeCsv_WithComma_WrapsInQuotes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("EscapeCsv", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "Test,Value" }) as string;

        // Assert
        Assert.Equal("\"Test,Value\"", result);
    }

    [Fact]
    public void EscapeCsv_SimpleText_WrapsInQuotes()
    {
        // Act
        var method = typeof(AdminLogsExportService).GetMethod("EscapeCsv", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "TestValue" }) as string;

        // Assert
        Assert.Equal("\"TestValue\"", result);
    }
}
