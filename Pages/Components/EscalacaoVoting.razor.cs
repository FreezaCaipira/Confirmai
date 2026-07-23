using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components;

public partial class EscalacaoVoting
{
    [Parameter]
    public required Event Event { get; set; }

    [Parameter]
    public required bool IsPastEvent { get; set; }

    [Parameter]
    public required bool IsMember { get; set; }

    [Parameter]
    public required bool IsAdmin { get; set; }

    [Parameter]
    public string? CurrentUserId { get; set; }

    [Parameter]
    public required List<PostMatchVote> AllVotes { get; set; }

    [Parameter]
    public PostMatchVote? MyVote { get; set; }

    [Parameter]
    public bool IsEditingVote { get; set; }

    [Parameter]
    public string? VoteError { get; set; }

    [Parameter]
    public EventCallback<string> OnCastVote { get; set; }

    [Parameter]
    public EventCallback<bool> OnEditingVoteChanged { get; set; }

    private async Task HandleCastVote(string votedForUserId)
    {
        await OnCastVote.InvokeAsync(votedForUserId);
    }

    private async Task HandleEditingVoteChanged(bool isEditing)
    {
        await OnEditingVoteChanged.InvokeAsync(isEditing);
    }
}
