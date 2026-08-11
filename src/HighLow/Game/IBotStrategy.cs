using HighLow.Domain;

namespace HighLow.Game;

public interface IBotStrategy
{
    Submission ChooseSubmission(
        IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int point, int ownScore);
}