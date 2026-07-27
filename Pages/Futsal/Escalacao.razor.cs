using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Futsal.Components;
using Confirmai.Services.Futsal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Futsal;

public partial class Escalacao : IAsyncDisposable
{
    [Inject] private EscalacaoService EscalacaoSvc { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private Event? ev;
    private bool isLoading = true;
    private string? currentUserId;
    private bool isAdmin;
    private List<PlayerSlot> teamA = new();
    private List<PlayerSlot> teamB = new();
    private List<PlayerSlot> reservas = new();
    private bool showConfirmModal;
    private bool showResetModal;
    private bool isSaving;
    private bool copied;
    private string? actionError;
    private CancellationTokenSource? _copyCts;
    private Task? _copyTask;

    private bool isPastEvent;
    private bool isMember;
    private List<PostMatchVote> allVotes = new();
    private PostMatchVote? myVote;
    private int? scoreInputA;
    private int? scoreInputB;
    private bool editingScore;
    private string? scoreError;
    private string? voteError;
    private string? scoreRegisteredByName;
    private bool isEditingVote;
    private string teamAName = "Time A";
    private string teamBName = "Time B";

    private bool TeamsAreBalanced =>
        Math.Abs(teamA.Count(s => !s.IsGoalkeeper) - teamB.Count(s => !s.IsGoalkeeper)) <= 1;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LoadEvent();
        if (ev?.LineupConfirmedAt is null && isAdmin)
            Randomize();
    }

    private async Task SaveTeamNamesCallback() => await SaveTeamNames();
    private Task MovePlayerCallback((PlayerSlot Slot, bool ToTeamB) args)
    { MovePlayer(args.Slot, args.ToTeamB); return Task.CompletedTask; }
    private Task RandomizeCallback() { Randomize(); return Task.CompletedTask; }
    private Task ShowConfirmModalChangedCallback(bool value) { showConfirmModal = value; return Task.CompletedTask; }
    private async Task ConfirmarEscalacaoCallback() => await ConfirmarEscalacao();
    private Task EditingScoreChangedCallback(bool value) { editingScore = value; return Task.CompletedTask; }
    private async Task SaveScoreCallback() => await SaveScore();
    private async Task CopyToClipboardCallback() => await CopyToClipboard();
    private async Task ShareOnWhatsAppCallback() => await ShareOnWhatsApp();

    private async Task LoadEvent()
    {
        isLoading = true;
        var result = await EscalacaoSvc.LoadAsync(Id, currentUserId);

        ev = result.Event;
        isAdmin = result.IsAdmin;
        isPastEvent = result.IsPastEvent;
        isMember = result.IsMember;
        allVotes = result.AllVotes;
        myVote = result.MyVote;
        scoreRegisteredByName = result.ScoreRegisteredByName;
        teamAName = result.TeamAName;
        teamBName = result.TeamBName;

        if (ev is not null)
        {
            scoreInputA = ev.ScoreTeamA;
            scoreInputB = ev.ScoreTeamB;
        }

        isLoading = false;
    }

    private async Task SaveTeamNames()
    {
        if (ev is null) return;
        await EscalacaoSvc.SaveTeamNamesAsync(Id, teamAName, teamBName);
        teamAName = string.IsNullOrWhiteSpace(teamAName) ? "Time A" : teamAName.Trim();
        teamBName = string.IsNullOrWhiteSpace(teamBName) ? "Time B" : teamBName.Trim();
    }

    private async Task HandleTeamNamesChanged((string TeamA, string TeamB) names)
    {
        teamAName = names.TeamA;
        teamBName = names.TeamB;
        await SaveTeamNames();
    }

    private Task HandleEditingVoteChanged(bool isEditing)
    {
        isEditingVote = isEditing;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void Randomize()
    {
        if (ev is null) return;

        var gks = ev.Confirmations
            .Where(c => c.Position == FutsalPosition.Goalkeeper)
            .OrderBy(c => c.ConfirmedAt)
            .ToList();

        var outfield = ev.Confirmations
            .Where(c => c.Position != FutsalPosition.Goalkeeper)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        teamA.Clear(); teamB.Clear(); reservas.Clear();

        if (gks.Count >= 1)
            teamA.Add(new PlayerSlot(gks[0].UserId, gks[0].User.FullName ?? gks[0].User.Email!, true));
        if (gks.Count >= 2)
            teamB.Add(new PlayerSlot(gks[1].UserId, gks[1].User.FullName ?? gks[1].User.Email!, true));

        for (int i = 0; i < outfield.Count; i++)
        {
            var slot = new PlayerSlot(outfield[i].UserId, outfield[i].User.FullName ?? outfield[i].User.Email!, false);
            if (i % 2 == 0) teamA.Add(slot);
            else             teamB.Add(slot);
        }

        if (outfield.Count % 2 != 0 && teamA.Count > 0)
        {
            var last = teamA.Last(s => !s.IsGoalkeeper);
            teamA.Remove(last);
            reservas.Add(last);
        }
    }

    private void MovePlayer(PlayerSlot slot, bool toTeamB)
    {
        if (toTeamB)
        {
            teamA.Remove(slot);
            if (slot.IsGoalkeeper)
            {
                var existing = teamB.FirstOrDefault(s => s.IsGoalkeeper);
                if (existing is not null) { teamB.Remove(existing); teamA.Add(existing); }
            }
            teamB.Add(slot);
        }
        else
        {
            teamB.Remove(slot);
            if (slot.IsGoalkeeper)
            {
                var existing = teamA.FirstOrDefault(s => s.IsGoalkeeper);
                if (existing is not null) { teamA.Remove(existing); teamB.Add(existing); }
            }
            teamA.Add(slot);
        }
    }

    private async Task ConfirmarEscalacao()
    {
        if (ev is null) return;
        isSaving = true;
        actionError = null;

        var userIdToTeam = new Dictionary<string, int?>();
        foreach (var s in teamA)    userIdToTeam[s.UserId] = 0;
        foreach (var s in teamB)    userIdToTeam[s.UserId] = 1;
        foreach (var s in reservas) userIdToTeam[s.UserId] = null;

        await EscalacaoSvc.ConfirmLineupAsync(Id, userIdToTeam);
        showConfirmModal = false;
        isSaving = false;
        await LoadEvent();
    }

    private async Task CastVote(string votedForUserId)
    {
        if (currentUserId is null || votedForUserId == currentUserId) return;
        voteError = null;
        isSaving = true;
        await EscalacaoSvc.CastVoteAsync(Id, currentUserId, votedForUserId);
        isEditingVote = false;
        isSaving = false;
        await LoadEvent();
    }

    private async Task SaveScore()
    {
        if (scoreInputA is null || scoreInputB is null) return;
        scoreError = null;
        isSaving = true;
        await EscalacaoSvc.SaveScoreAsync(Id, scoreInputA.Value, scoreInputB.Value, currentUserId);
        editingScore = false;
        isSaving = false;
        await LoadEvent();
    }

    private async Task ResetarEscalacao()
    {
        if (ev is null) return;
        isSaving = true;
        await EscalacaoSvc.ResetLineupAsync(Id);
        showResetModal = false;
        isSaving = false;
        await LoadEvent();
        Randomize();
    }

    private async Task CopyToClipboard()
    {
        var text = BuildShareText();
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        _copyCts?.Cancel();
        _copyCts = new CancellationTokenSource();
        var ct = _copyCts.Token;
        try
        {
            copied = true;
            StateHasChanged();
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested) copied = false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copied = false;
        }
    }

    private async Task ShareOnWhatsApp()
    {
        var url = $"https://wa.me/?text={Uri.EscapeDataString(BuildWhatsAppText())}";
        await JS.InvokeVoidAsync("open", url, "_blank", "noopener,noreferrer");
    }

    private string BuildWhatsAppText() => ev is null ? string.Empty : EscalacaoTextFormatter.BuildWhatsAppText(ev);
    private string BuildShareText() => ev is null ? string.Empty : EscalacaoTextFormatter.BuildShareText(ev);

    public async ValueTask DisposeAsync()
    {
        _copyCts?.Cancel();
        if (_copyTask is not null)
        {
            try { await _copyTask; }
            catch (OperationCanceledException) { }
        }
        _copyCts?.Dispose();
    }
}
