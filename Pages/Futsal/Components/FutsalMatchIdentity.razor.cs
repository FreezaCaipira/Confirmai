using Confirmai.Models;
using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class FutsalMatchIdentity
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();

    [Parameter]
    public string SubFormat { get; set; } = "futsal";

    [Parameter]
    public Group? PreselectedGroup { get; set; }

    [Parameter]
    public EventCallback<string> OnSubFormatChanged { get; set; }

    private async Task HandleSubFormatChanged(ChangeEventArgs e)
    {
        var newFormat = e.Value?.ToString() ?? "futsal";
        await OnSubFormatChanged.InvokeAsync(newFormat);
    }
}
