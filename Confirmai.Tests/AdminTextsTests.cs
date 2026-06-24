using Confirmai.Services.Core.UiText;

namespace Confirmai.Tests;

public class AdminTextsTests
{
    [Fact]
    public void PtBr_ContainsRequiredKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.NotNull(dictionary);
        Assert.True(dictionary.Count > 0);
    }

    [Fact]
    public void PtBr_ContainsAdminNavKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("Admin.Nav.Users"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Products"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Payments"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Gateways"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Logs"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Languages"));
        Assert.True(dictionary.ContainsKey("Admin.Nav.Security"));
    }

    [Fact]
    public void PtBr_ContainsAdminProductsKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminProducts.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminProducts.Title"));
        Assert.True(dictionary.ContainsKey("AdminProducts.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminProducts.Aria"));
    }

    [Fact]
    public void PtBr_ContainsAdminPaymentsKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminPayments.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminPayments.Title"));
        Assert.True(dictionary.ContainsKey("AdminPayments.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminPayments.Aria"));
    }

    [Fact]
    public void PtBr_ContainsAdminUsersKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminUsers.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminUsers.Title"));
        Assert.True(dictionary.ContainsKey("AdminUsers.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminUsers.Aria"));
    }

    [Fact]
    public void PtBr_ContainsAdminLanguagesKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminLanguages.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminLanguages.Title"));
        Assert.True(dictionary.ContainsKey("AdminLanguages.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminLanguages.Aria"));
    }

    [Fact]
    public void PtBr_ContainsAdminCommonKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminCommon.CloseNotice"));
        Assert.True(dictionary.ContainsKey("AdminCommon.ExtraActions"));
    }

    [Fact]
    public void PtBr_ValuesAreNotEmpty()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has empty value");
        }
    }

    [Fact]
    public void PtBr_UsesCaseInsensitiveComparer()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("admin.nav.users"));
        Assert.True(dictionary.ContainsKey("ADMIN.NAV.USERS"));
    }

    [Fact]
    public void PtBr_ContainsAdminGatewaysKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminGateways.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminGateways.Title"));
        Assert.True(dictionary.ContainsKey("AdminGateways.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminGateways.Aria"));
    }

    [Fact]
    public void PtBr_ContainsAdminLogsKeys()
    {
        // Act
        var dictionary = AdminTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("AdminLogs.Kicker"));
        Assert.True(dictionary.ContainsKey("AdminLogs.Title"));
        Assert.True(dictionary.ContainsKey("AdminLogs.Subtitle"));
        Assert.True(dictionary.ContainsKey("AdminLogs.Aria"));
    }
}
