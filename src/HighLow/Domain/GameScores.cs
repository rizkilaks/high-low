namespace HighLow.Domain;

public static class GameScores
{
    public static (int Seat, int Score, int TieAbs, int TieMax) ApplyPrize(
        (int Seat, int Score, int TieAbs, int TieMax) standing, int point)
        => (standing.Seat, standing.Score + point, standing.TieAbs + Math.Abs(point), Math.Max(standing.TieMax, Math.Abs(point)));

    public static IReadOnlyList<int> RankWinners(
        IEnumerable<(int Seat, int Score, int TieAbs, int TieMax)> standings, int topN)
        => standings
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.TieAbs)
            .ThenByDescending(s => s.TieMax)
            .ThenBy(s => s.Seat)
            .Take(topN)
            .Select(s => s.Seat)
            .ToArray();
}