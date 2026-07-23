using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.MyConfirmations;

public partial class Index
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/eventos?tab=meus-jogos", replace: true);
    }
}
