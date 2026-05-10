using Confirmai.Data;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class OperationFeeCalculatorServiceTests
{
    [Fact]
    public async Task CalculateBreakdownAsync_UsesDefaultPercent_WhenSettingIsMissing()
    {
        await using var db = CreateDbContext();
        var settingsService = new AdminSettingsService(db);
        var service = new OperationFeeCalculatorService(settingsService);

        const decimal grossAmount = 0.005m;

        var breakdown = await service.CalculateBreakdownAsync(grossAmount);

        var expectedFee = Math.Round(
            grossAmount * (AdminSettingsService.DefaultOperationFeePercent / 100m),
            8,
            MidpointRounding.AwayFromZero);

        Assert.Equal(AdminSettingsService.DefaultOperationFeePercent, breakdown.Percent);
        Assert.Equal(expectedFee, breakdown.FeeAmount);
        Assert.Equal(grossAmount - expectedFee, breakdown.NetAmount);
    }

    [Fact]
    public async Task CalculateBreakdownAsync_UsesPersistedPercent_AndRoundsFeeToEightDecimals()
    {
        await using var db = CreateDbContext();
        var settingsService = new AdminSettingsService(db);
        var service = new OperationFeeCalculatorService(settingsService);

        var saved = await settingsService.SetOperationFeePercentAsync(3.257m);
        Assert.True(saved);

        const decimal grossAmount = 0.00123456789m;

        var breakdown = await service.CalculateBreakdownAsync(grossAmount);

        var expectedFee = Math.Round(grossAmount * (3.26m / 100m), 8, MidpointRounding.AwayFromZero);

        Assert.Equal(3.26m, breakdown.Percent);
        Assert.Equal(expectedFee, breakdown.FeeAmount);
        Assert.Equal(grossAmount - expectedFee, breakdown.NetAmount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"operation-fee-calculator-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}


