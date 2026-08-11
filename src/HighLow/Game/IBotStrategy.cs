using HighLow.Domain;

namespace HighLow.Game;

public interface IBotStrategy
{
    (int Card, Special Special, bool Pass) ChooseSubmission(
        IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int prize, int ownScore);
}