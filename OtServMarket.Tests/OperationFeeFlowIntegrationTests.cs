using Confirmai.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class OperationFeeFlowIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public OperationFeeFlowIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveFee_ThenGetFee_ReturnsPersistedValue()
    {
        using var saveScope = _factory.Services.CreateScope();
        var settings = saveScope.ServiceProvider.GetRequiredService<AdminSettingsService>();

        var saved = await settings.SetOperationFeePercentAsync(3.75m);
        Assert.True(saved);

        using var readScope = _factory.Services.CreateScope();
        var readSettings = readScope.ServiceProvider.GetRequiredService<AdminSettingsService>();
        var persisted = await readSettings.GetOperationFeePercentAsync();

        Assert.Equal(3.75m, persisted);
    }

    [Fact]
    public async Task SavedFee_IsReflectedInReleaseBreakdownCalculation()
    {
        const decimal grossAmount = 0.00030000m;

        using var saveScope = _factory.Services.CreateScope();
        var settings = saveScope.ServiceProvider.GetRequiredService<AdminSettingsService>();
        var saveOk = await settings.SetOperationFeePercentAsync(2m);
        Assert.True(saveOk);

        using var calcScope = _factory.Services.CreateScope();
        var calculator = calcScope.ServiceProvider.GetRequiredService<OperationFeeCalculatorService>();
        var breakdown = await calculator.CalculateBreakdownAsync(grossAmount);

        Assert.Equal(2m, breakdown.Percent);
        Assert.Equal(0.00000600m, breakdown.FeeAmount);
        Assert.Equal(0.00029400m, breakdown.NetAmount);
    }

    [Fact]
    public async Task InvalidFeeValue_DoesNotOverridePreviousSavedValue()
    {
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<AdminSettingsService>();

        var saveOk = await settings.SetOperationFeePercentAsync(4.25m);
        Assert.True(saveOk);

        var invalidSave = await settings.SetOperationFeePercentAsync(101m);
        Assert.False(invalidSave);

        var stillPersisted = await settings.GetOperationFeePercentAsync();
        Assert.Equal(4.25m, stillPersisted);
    }
}


