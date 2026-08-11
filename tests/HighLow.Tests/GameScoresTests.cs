using HighLow.Domain;
using Xunit;

namespace HighLow.Tests;

public class GameScoresTests
{
    private static readonly PlayerStanding S = new(0, 0, 0, 0);

    [Fact]
    public void ApplyPrize_updates_score_and_tie_accumulators()
    {
        var updated = new PlayerStanding(2, 0, 0, 0).ApplyPrize(-3);
        Assert.Equal(-3, updated.Score);
        Assert.Equal(3, updated.TieAbs);
        Assert.Equal(3, updated.TieMax);
    }

    [Fact]
    public void RankWinners_prefers_score_then_tie_accumulators_then_seat()
    {
        var standings = new[]
        {
            S with { Seat = 0, Score = 5, TieAbs = 5, TieMax = 5 },
            S with { Seat = 1, Score = 5, TieAbs = 8, TieMax = 8 }, // wins tie via TieAbs
            S with { Seat = 2, Score = 7 },                          // top score
            S with { Seat = 3, Score = 5, TieAbs = 8, TieMax = 6 }, // ties TieAbs, loses TieMax
        };
        Assert.Equal(new[] { 2, 1 }, GameScores.RankWinners(standings, 2));
    }

    [Fact]
    public void RankWinners_full_score_tie_uses_lower_seat()
    {
        var standings = new[]
        {
            S with { Seat = 1, Score = 4, TieAbs = 4 },
            S with { Seat = 3, Score = 4, TieAbs = 4 },
        };
        Assert.Equal(new[] { 1, 3 }, GameScores.RankWinners(standings, 2));
    }
}