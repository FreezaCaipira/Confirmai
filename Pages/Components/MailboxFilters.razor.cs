using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components;

public partial class MailboxFilters
{
    [Parameter]
    public string SearchTerm { get; set; } = string.Empty;

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public bool InboxUnreadOnly { get; set; }

    [Parameter]
    public EventCallback OnApplyFilters { get; set; }

    [Parameter]
    public EventCallback OnResetFilters { get; set; }

    [Inject] private UiTextService T { get; set; } = default!;

    private async Task HandleApplyFilters()
    {
        await OnApplyFilters.InvokeAsync();
    }

    private async Task HandleResetFilters()
    {
        await OnResetFilters.InvokeAsync();
    }
}
