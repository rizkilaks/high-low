using HighLow.Domain;

namespace HighLow.Game;

public sealed class DefaultBotStrategy : IBotStrategy
{
    public Submission ChooseSubmission(
        IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int point, int ownScore)
    {
        var sorted = hand.OrderBy(c => c).ToArray();
        if (reverseAvailable && ownScore < 0)
            return new Submission(sorted[0], Special.Reverse, false);
        return new Submission(sorted[Math.Max(0, sorted.Length - 3)], Special.Normal, false);
    }
}