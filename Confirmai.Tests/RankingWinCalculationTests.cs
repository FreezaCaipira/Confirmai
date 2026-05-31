namespace Confirmai.Tests;

/// <summary>
/// Tests for the win-determination logic used in Groups/Ranking.razor BuildRanking().
/// Formula: (TeamId == 0 &amp;&amp; ScoreTeamA &gt; ScoreTeamB) || (TeamId == 1 &amp;&amp; ScoreTeamB &gt; ScoreTeamA)
/// </summary>
public class RankingWinCalculationTests
{
    // Helper mirrors the lambda used in BuildRanking
    private static bool IsWin(int teamId, int? scoreTeamA, int? scoreTeamB)
        => (teamId == 0 && scoreTeamA > scoreTeamB)
        || (teamId == 1 && scoreTeamB > scoreTeamA);

    [Fact]
    public void TeamA_Wins_WhenScoreTeamAHigher()
    {
        Assert.True(IsWin(teamId: 0, scoreTeamA: 3, scoreTeamB: 1));
    }

    [Fact]
    public void TeamB_Wins_WhenScoreTeamBHigher()
    {
        Assert.True(IsWin(teamId: 1, scoreTeamA: 1, scoreTeamB: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NoWin_OnDraw(int teamId)
    {
        Assert.False(IsWin(teamId, scoreTeamA: 2, scoreTeamB: 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NoWin_WhenScoreIsNull(int teamId)
    {
        Assert.False(IsWin(teamId, scoreTeamA: null, scoreTeamB: null));
    }

    [Fact]
    public void TeamA_Player_DoesNotGetWin_WhenTeamBScoreHigher()
    {
        // Team A player when A lost
        Assert.False(IsWin(teamId: 0, scoreTeamA: 1, scoreTeamB: 3));
    }

    [Fact]
    public void TeamB_Player_DoesNotGetWin_WhenTeamAScoreHigher()
    {
        // Team B player when B lost
        Assert.False(IsWin(teamId: 1, scoreTeamA: 3, scoreTeamB: 1));
    }

    [Fact]
    public void TeamA_Wins_MinimalMargin()
    {
        Assert.True(IsWin(teamId: 0, scoreTeamA: 1, scoreTeamB: 0));
    }

    [Fact]
    public void TeamB_Wins_MinimalMargin()
    {
        Assert.True(IsWin(teamId: 1, scoreTeamA: 0, scoreTeamB: 1));
    }
}
