using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class EscalacaoScoreEditor
{
    [Parameter]
    public int? ScoreTeamA { get; set; }

    [Parameter]
    public int? ScoreTeamB { get; set; }

    [Parameter]
    public int? ScoreInputA { get; set; }

    [Parameter]
    public EventCallback<int?> ScoreInputAChanged { get; set; }

    [Parameter]
    public int? ScoreInputB { get; set; }

    [Parameter]
    public EventCallback<int?> ScoreInputBChanged { get; set; }

    [Parameter]
    public bool EditingScore { get; set; }

    [Parameter]
    public string? ScoreError { get; set; }

    [Parameter]
    public string TeamAName { get; set; } = "Time A";

    [Parameter]
    public string TeamBName { get; set; } = "Time B";

    [Parameter]
    public bool IsMember { get; set; }

    [Parameter]
    public bool IsAdmin { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string? ScoreRegisteredByName { get; set; }

    [Parameter]
    public DateTime? ScoreRegisteredAt { get; set; }

    [Parameter]
    public EventCallback<bool> OnEditingScoreChanged { get; set; }

    [Parameter]
    public EventCallback OnSaveScore { get; set; }

    private async Task HandleEditingScore(bool value)
        => await OnEditingScoreChanged.InvokeAsync(value);

    private async Task HandleSaveScore()
        => await OnSaveScore.InvokeAsync();

    private async Task HandleScoreInputAChanged(ChangeEventArgs e)
    {
        ScoreInputA = int.TryParse(e.Value?.ToString(), out var v) ? v : null;
        await ScoreInputAChanged.InvokeAsync(ScoreInputA);
    }

    private async Task HandleScoreInputBChanged(ChangeEventArgs e)
    {
        ScoreInputB = int.TryParse(e.Value?.ToString(), out var v) ? v : null;
        await ScoreInputBChanged.InvokeAsync(ScoreInputB);
    }
}
