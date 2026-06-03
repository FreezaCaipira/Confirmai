using Confirmai.Services.Admin;
namespace Confirmai.Services.Core;

public class OperationFeeCalculatorService
{
    private readonly AdminSettingsService _adminSettingsService;

    public OperationFeeCalculatorService(AdminSettingsService adminSettingsService)
    {
        _adminSettingsService = adminSettingsService;
    }

    public async Task<OperationFeeBreakdown> CalculateBreakdownAsync(decimal grossAmount)
    {
        var percent = await _adminSettingsService.GetOperationFeePercentAsync();
        var feeAmount = Math.Round(grossAmount * (percent / 100m), 8, MidpointRounding.AwayFromZero);
        var netAmount = grossAmount - feeAmount;

        return new OperationFeeBreakdown(percent, feeAmount, netAmount);
    }
}

public sealed record OperationFeeBreakdown(decimal Percent, decimal FeeAmount, decimal NetAmount);


