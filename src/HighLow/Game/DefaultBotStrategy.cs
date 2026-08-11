using HighLow.Domain;

namespace HighLow.Game;

public sealed class DefaultBotStrategy : IBotStrategy
{
    private const double RandomChance = 0.7;
    private readonly Random _rng;

    public DefaultBotStrategy(Random? rng = null) => _rng = rng ?? Random.Shared;

    public Submission ChooseSubmission(IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int point, int ownScore)
    {
        if (_rng.NextDouble() < RandomChance)
            return new Submission(hand[_rng.Next(hand.Count)], Special.Normal, false);

        var sorted = hand.OrderBy(c => c).ToArray();
        if (reverseAvailable && ownScore < 0)
            return new Submission(sorted[0], Special.Reverse, false);

        return new Submission(sorted[Math.Max(0, sorted.Length - 3)], Special.Normal, false);
    }
}