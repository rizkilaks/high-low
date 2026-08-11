namespace HighLow.Domain;

public sealed record PlayerStanding(int Seat, int Score, int TieAbs, int TieMax)
{
    public PlayerStanding ApplyPrize(int point) =>
        this with
        {
            Score = Score + point,
            TieAbs = TieAbs + Math.Abs(point),
            TieMax = Math.Max(TieMax, Math.Abs(point))
        };
}

public static class GameScores
{
    public static IReadOnlyList<int> RankWinners(IEnumerable<PlayerStanding> standings, int topN)
        => standings
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.TieAbs)
            .ThenByDescending(s => s.TieMax)
            .ThenBy(s => s.Seat)
            .Take(topN)
            .Select(s => s.Seat)
            .ToArray();
}