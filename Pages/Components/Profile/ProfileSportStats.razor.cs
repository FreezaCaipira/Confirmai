using Confirmai.Enums;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileSportStats
{
    [Parameter]
    public Sport ActiveSport { get; set; } = Sport.Futsal;

    [Parameter]
    public int FutsalTotal { get; set; }

    [Parameter]
    public int FutsalOutfieldTotal { get; set; }

    [Parameter]
    public int FutsalGoalkeeperTotal { get; set; }

    [Parameter]
    public int PokerTotal { get; set; }

    [Parameter]
    public EventCallback<Sport> OnSetSport { get; set; }

    private async Task HandleSetSport(Sport sport)
        => await OnSetSport.InvokeAsync(sport);
}
