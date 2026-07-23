using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class EscalacaoDraftTeamBuilder
{
    [Parameter]
    public List<PlayerSlot> TeamA { get; set; } = new();

    [Parameter]
    public List<PlayerSlot> TeamB { get; set; } = new();

    [Parameter]
    public List<PlayerSlot> Reservas { get; set; } = new();

    [Parameter]
    public string TeamAName { get; set; } = "Time A";

    [Parameter]
    public string TeamBName { get; set; } = "Time B";

    [Parameter]
    public bool IsAdmin { get; set; }

    [Parameter]
    public EventCallback OnSaveTeamNames { get; set; }

    [Parameter]
    public EventCallback<(PlayerSlot Slot, bool ToTeamB)> OnMovePlayer { get; set; }

    private async Task SaveTeamNamesCallback()
        => await OnSaveTeamNames.InvokeAsync();

    private async Task HandleMovePlayer(PlayerSlot slot, bool toTeamB)
        => await OnMovePlayer.InvokeAsync((slot, toTeamB));
}
