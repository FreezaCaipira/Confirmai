using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components;

public partial class EscalacaoConfirmed
{
    [Parameter]
    public required Event Event { get; set; }

    [Parameter]
    public required bool IsPastEvent { get; set; }

    [Parameter]
    public required bool IsAdmin { get; set; }

    [Parameter]
    public string TeamAName { get; set; } = "Time A";

    [Parameter]
    public string TeamBName { get; set; } = "Time B";

    [Parameter]
    public EventCallback<(string TeamA, string TeamB)> OnTeamNamesChanged { get; set; }

    [Parameter]
    public required List<PostMatchVote> AllVotes { get; set; }

    public List<EventConfirmation> ConfirmationsA => Event.Confirmations
        .Where(c => c.TeamId == 0)
        .OrderBy(c => c.Position)
        .ThenBy(c => c.ConfirmedAt)
        .ToList();

    public List<EventConfirmation> ConfirmationsB => Event.Confirmations
        .Where(c => c.TeamId == 1)
        .OrderBy(c => c.Position)
        .ThenBy(c => c.ConfirmedAt)
        .ToList();

    public List<EventConfirmation> ConfirmationsReserva => Event.Confirmations
        .Where(c => c.TeamId == null)
        .OrderBy(c => c.ConfirmedAt)
        .ToList();

    public string? MvpUserId
    {
        get
        {
            var mvpGroup = AllVotes.GroupBy(v => v.VotedForUserId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            return mvpGroup?.Key;
        }
    }

    public bool MvpRevealed
    {
        get
        {
            if (!IsPastEvent || MvpUserId is null)
                return false;

            int mvpTotalVoters = Event.Confirmations.Count(c => c.TeamId == 0 || c.TeamId == 1);
            return AllVotes.Count >= Math.Max(1, (int)Math.Ceiling(mvpTotalVoters * 0.5));
        }
    }

    private async Task HandleSaveTeamNames()
    {
        await OnTeamNamesChanged.InvokeAsync((TeamAName, TeamBName));
    }
}
