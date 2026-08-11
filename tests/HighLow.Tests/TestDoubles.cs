using HighLow.Domain;
using HighLow.Game;

namespace HighLow.Tests;

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;
    public void Advance(TimeSpan t) => UtcNow += t;
}

public sealed class FixedBotStrategy : IBotStrategy
{
    private readonly Queue<int> _cards = new();
    public void Enqueue(params int[] cards) { foreach (var c in cards) _cards.Enqueue(c); }

    public (int Card, Special Special, bool Pass) ChooseSubmission(
        IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int point, int ownScore)
        => (_cards.Count > 0 ? _cards.Dequeue() : hand[0], Special.Normal, false);
}

public static class TestRoomFactory
{
    public static Room CreateRoom(FakeClock clock, IBotStrategy? bots = null, IReadOnlyList<int>? deck = null)
        => new("AB12", new Random(42), bots ?? new FixedBotStrategy(), isPublic: true, hostToken: "t0", deck);
}