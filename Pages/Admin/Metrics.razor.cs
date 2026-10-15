using Confirmai.Services.Admin;

namespace Confirmai.Pages.Admin;

public partial class Metrics
{
    private AdminProductMetricsService.Result? result;

    protected override async Task OnInitializedAsync()
    {
        result = await MetricsService.LoadAsync();
    }
}
