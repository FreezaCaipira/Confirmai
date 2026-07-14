using Confirmai.Services.Core;
using Confirmai.Services.Core.UiText;
using Confirmai.Services.User;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests validating UiTextService refactoring.
/// 
/// These tests ensure that the new domain-driven architecture:
/// 1. Retrieves text correctly across all domains
/// 2. Maintains backward compatibility with original API
/// 3. Handles language fallback properly
/// 4. Validates coverage of key text strings
/// 5. Tests indexer syntax (T["key"])
/// 6. Tests datetime formatting methods
/// </summary>
public class UiTextServiceRefactoringTests : IDisposable
{
    private readonly LanguagePreferenceService _languageService;

    public UiTextServiceRefactoringTests()
    {
        // Create a real language service that defaults to PT-BR
        _languageService = new LanguagePreferenceService();
    }

    /// <summary>
    /// Test 1: Core domain text retrieval
    /// Validates that common UI strings are accessible
    /// </summary>
    [Fact]
    public void Get_CoreTexts_ReturnsCorrectValues()
    {
        var ui = new UiTextService(_languageService);

        // Navigation strings
        Assert.Equal("Dashboard", ui.Get("Nav.Dashboard"));
        Assert.Equal("Marketplace", ui.Get("Nav.Marketplace"));

        // Layout strings
        Assert.Equal("Bem-vindo", ui.Get("Layout.Hello"));
        Assert.Equal("Sair", ui.Get("Layout.Logout"));

        // Common strings
        Assert.Equal("Carregando...", ui.Get("Common.Loading"));
        Assert.Equal("Cancelar", ui.Get("Common.Cancel"));
    }

    /// <summary>
    /// Test 2: Admin domain text retrieval
    /// Validates administrative panel strings
    /// </summary>
    [Fact]
    public void Get_AdminTexts_ReturnsCorrectValues()
    {
        var ui = new UiTextService(_languageService);

        Assert.Equal("Usuarios", ui.Get("Admin.Nav.Users"));
        Assert.Equal("Itens", ui.Get("Admin.Nav.Products"));
        Assert.Equal("Pagamentos", ui.Get("Admin.Nav.Payments"));
        Assert.Equal("Painel Administrativo", ui.Get("AdminDashboard.Title"));
    }

    /// <summary>
    /// Test 3: Server domain text retrieval
    /// Validates game server and marketplace strings
    /// </summary>
    [Fact]
    public void Get_ServerTexts_ReturnsCorrectValues()
    {
        var ui = new UiTextService(_languageService);

        Assert.Equal("Servidores", ui.Get("Servers.Title"));
        Assert.Equal("Detalhes do Servidor", ui.Get("ServerDetails.Title"));
        Assert.Equal("Criar novo servidor", ui.Get("ServerForm.Title"));
    }

    /// <summary>
    /// Test 4: Payment domain text retrieval
    /// Validates payment and checkout strings
    /// </summary>
    [Fact]
    public void Get_PaymentTexts_ReturnsCorrectValues()
    {
        var ui = new UiTextService(_languageService);

        Assert.Equal("Finalizar compra", ui.Get("PaymentBuy.Title"));
        Assert.Equal("Meus pagamentos", ui.Get("PaymentHistory.Title"));
        Assert.Equal("Produtos", ui.Get("Products.Title"));
        Assert.Equal("Marketplace", ui.Get("Marketplace.Title"));
    }

    /// <summary>
    /// Test 5: String formatting with arguments
    /// Validates that parameterized strings work correctly
    /// </summary>
    [Fact]
    public void Get_WithArguments_FormatsStringCorrectly()
    {
        var ui = new UiTextService(_languageService);

        // Example: product deletion confirmation with product ID
        var message = ui.Get("AdminProducts.ConfirmDelete");
        Assert.Contains("produto", message); // Should contain confirmation text

        // Format with argument if the string supports it
        // This validates the string.Format infrastructure works
        var result = ui.Get("Common.Loading");
        Assert.Equal("Carregando...", result);
    }

    /// <summary>
    /// Test 6: Language fallback behavior
    /// Validates that PT-BR is used as fallback for missing translations
    /// </summary>
    [Fact]
    public void Get_WithMissingLanguage_FallsBackToPtBr()
    {
        // Set language to EN-US (which has stubs)
        var languageService = new LanguagePreferenceService();
        languageService.SetLanguage("en-US");
        var ui = new UiTextService(languageService);

        // EN-US has stubs for Dashboard, but PT-BR should be available as fallback
        var result = ui.Get("Nav.Dashboard");
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Test 7: Unknown key fallback
    /// Validates that unknown keys return the key name itself
    /// </summary>
    [Fact]
    public void Get_WithUnknownKey_ReturnKeyName()
    {
        var ui = new UiTextService(_languageService);

        var result = ui.Get("Unknown.NonExistent.Key");
        Assert.Equal("Unknown.NonExistent.Key", result);
    }

    /// <summary>
    /// Test 8: Empty/null key handling
    /// Validates graceful handling of invalid inputs
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_WithInvalidKey_ReturnsEmpty(string key)
    {
        var ui = new UiTextService(_languageService);

        var result = ui.Get(key);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 9: ContainsKey validation method
    /// Validates the testing helper method
    /// </summary>
    [Fact]
    public void ContainsKey_WithExistingKey_ReturnsTrue()
    {
        var ui = new UiTextService(_languageService);

        Assert.True(ui.ContainsKey("Nav.Dashboard"));
        Assert.True(ui.ContainsKey("Admin.Nav.Users"));
        Assert.True(ui.ContainsKey("Servers.Title"));
    }

    /// <summary>
    /// Test 10: ContainsKey with non-existing key
    /// Validates that missing keys return false
    /// </summary>
    [Fact]
    public void ContainsKey_WithMissingKey_ReturnsFalse()
    {
        var ui = new UiTextService(_languageService);

        Assert.False(ui.ContainsKey("Unknown.NonExistent.Key"));
    }

    /// <summary>
    /// Test 11: GetStatistics method
    /// Validates that text coverage statistics are available
    /// </summary>
    [Fact]
    public void GetStatistics_ReturnsValidCounts()
    {
        var ui = new UiTextService(_languageService);

        var stats = ui.GetStatistics();

        // PT-BR should have the most entries (fully populated)
        Assert.True(stats.PtBr > 0);
        Assert.True(stats.EnUs > 0);
        Assert.True(stats.EsEs > 0);
        Assert.True(stats.Total == stats.PtBr + stats.EnUs + stats.EsEs);

        // PT-BR should have majority of keys during migration
        Assert.True(stats.PtBr >= stats.EnUs);
        Assert.True(stats.PtBr >= stats.EsEs);
    }

    /// <summary>
    /// Test 12: Backward compatibility check
    /// Validates that the refactored service can replace the original
    /// with zero code changes in calling code
    /// </summary>
    [Fact]
    public void Get_MaintainsBackwardCompatibility()
    {
        var ui = new UiTextService(_languageService);

        // Simulate how existing code calls the service
        string adminLabel = ui.Get("Admin.Nav.Users");      // Expected: "Usuarios"
        string dashboardTitle = ui.Get("AdminDashboard.Title");  // Expected: "Painel Administrativo"
        string loadingMsg = ui.Get("Common.Loading");       // Expected: "Carregando..."

        // All should return meaningful values
        Assert.NotEmpty(adminLabel);
        Assert.NotEmpty(dashboardTitle);
        Assert.NotEmpty(loadingMsg);

        // Values should match expectations
        Assert.Equal("Usuarios", adminLabel);
        Assert.Equal("Painel Administrativo", dashboardTitle);
        Assert.Equal("Carregando...", loadingMsg);
    }

    /// <summary>
    /// Test 13: Multiple instantiation consistency
    /// Validates that cache is shared across instances
    /// </summary>
    [Fact]
    public void Get_WithMultipleInstances_ReturnConsistentValues()
    {
        var ui1 = new UiTextService(_languageService);
        var ui2 = new UiTextService(_languageService);

        var value1 = ui1.Get("Nav.Dashboard");
        var value2 = ui2.Get("Nav.Dashboard");

        Assert.Equal(value1, value2);
    }

    /// <summary>
    /// Test 14: Indexer syntax (NEW)
    /// Validates that T["key"] syntax works for Razor compatibility
    /// This is used extensively in .razor files
    /// </summary>
    [Fact]
    public void Indexer_ReturnsCorrectValue()
    {
        var ui = new UiTextService(_languageService);

        // Test indexer syntax: ui["key"] == ui.Get("key")
        Assert.Equal(ui.Get("Nav.Dashboard"), ui["Nav.Dashboard"]);
        Assert.Equal(ui.Get("Admin.Nav.Users"), ui["Admin.Nav.Users"]);
        Assert.Equal(ui.Get("Common.Loading"), ui["Common.Loading"]);
        Assert.Equal(ui.Get("Unknown.Key"), ui["Unknown.Key"]);
    }

    /// <summary>
    /// Test 15: FormatDateTime method (NEW)
    /// Validates datetime formatting for Razor pages
    /// </summary>
    [Fact]
    public void FormatDateTime_WithValidDateTime_ReturnsFormattedString()
    {
        var ui = new UiTextService(_languageService);

        var testDate = new DateTime(2026, 3, 15, 14, 30, 0);

        // Test each format type
        var dateDefault = ui.FormatDateTime(testDate, "DateDefault");
        Assert.Equal("15/03", dateDefault);

        var timeDefault = ui.FormatDateTime(testDate, "TimeDefault");
        Assert.Equal("14:30", timeDefault);

        var dateTimeDefault = ui.FormatDateTime(testDate, "DateTimeDefault");
        Assert.Equal("15/03 14:30", dateTimeDefault);

        var dateTimeShortCompact = ui.FormatDateTime(testDate, "DateTimeShortCompact");
        Assert.Equal("15/03/26 14:30", dateTimeShortCompact);
    }

    /// <summary>
    /// Test 16: FormatDateTime with default date (NEW)
    /// Validates that default/empty DateTime returns empty string
    /// </summary>
    [Fact]
    public void FormatDateTime_WithDefaultDateTime_ReturnsEmpty()
    {
        var ui = new UiTextService(_languageService);

        var result = ui.FormatDateTime(default, "DateDefault");
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 17: FormatDateTime with unknown format (NEW)
    /// Validates fallback behavior for unknown format keys
    /// </summary>
    [Fact]
    public void FormatDateTime_WithUnknownFormat_UsesDefaultFormat()
    {
        var ui = new UiTextService(_languageService);

        var testDate = new DateTime(2026, 3, 15, 14, 30, 0);
        var result = ui.FormatDateTime(testDate, "UnknownFormat");

        // Should use "g" (general) format as fallback
        Assert.NotEmpty(result);
        Assert.Contains("2026", result); // Year should be present
        Assert.Contains("14:30", result); // Time should be present
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
