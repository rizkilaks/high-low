namespace HighLow.Domain;

public static class RoundResolver
{
    public static RoundResolution Resolve(int roundNumber, int prize, IReadOnlyDictionary<int, Submission> submissions)
    {
        var reverses = submissions.Values.Count(s => s.Special == Special.Reverse);
        var direction = reverses % 2 == 1 ? SpecialDirection.Lowest : SpecialDirection.Highest;

        var byValue = submissions.GroupBy(kv => kv.Value.CardValue);
        var voided = byValue.Where(g => g.Count() > 1).SelectMany(g => g.Select(kv => kv.Key)).ToArray();

        int? winnerSeat = null;
        int? winnerCard = null;
        var survivors = submissions.Where(kv => !voided.Contains(kv.Key)).ToArray();
        if (survivors.Length > 0)
        {
            var best = direction == SpecialDirection.Highest
                ? survivors.MaxBy(kv => kv.Value.CardValue)
                : survivors.MinBy(kv => kv.Value.CardValue);
            winnerSeat = best.Key;
            winnerCard = best.Value.CardValue;
        }

        var hidden = winnerSeat.HasValue && submissions[winnerSeat.Value].Pass ? winnerCard : null;
        var visible = winnerSeat.HasValue && !submissions[winnerSeat.Value].Pass ? winnerCard : null;

        return new RoundResolution(
            roundNumber,
            prize,
            direction,
            submissions.OrderBy(kv => kv.Key).Select(kv => new RevealedCard(kv.Key, kv.Value.CardValue, kv.Value.Pass)).ToArray(),
            voided,
            winnerSeat,
            visible,
            hidden,
            winnerSeat.HasValue ? null : prize,
            prize < 0 && voided.Length > 0 && winnerSeat.HasValue);
    }
}