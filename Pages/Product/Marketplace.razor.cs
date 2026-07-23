using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Confirmai.Pages.Product;

public partial class Marketplace
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override Task OnInitializedAsync()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("serverId", out var queryServerId)
            && int.TryParse(queryServerId.LastOrDefault(), out var parsedServerId)
            && parsedServerId > 0)
        {
            Navigation.NavigateTo($"/servers/{parsedServerId}", replace: true);
            return Task.CompletedTask;
        }

        Navigation.NavigateTo("/servers", replace: true);
        return Task.CompletedTask;
    }
}
