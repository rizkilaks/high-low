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

    public Submission ChooseSubmission(
        IReadOnlyList<int> hand, bool reverseAvailable, SpecialDirection direction, int point, int ownScore)
    {
        var card = _cards.Count > 0 ? _cards.Dequeue() : hand[0];
        return new Submission(card, Special.Normal, false);
    }
}

public sealed class ScriptedRandom : Random
{
    private readonly Queue<(double? Double, int? Int)> _values = new();
    public void Script(double d) => _values.Enqueue((d, null));
    public void Script(int i) => _values.Enqueue((null, i));
    public override double NextDouble() => _values.Dequeue().Double!.Value;
    public override int Next(int maxValue) => _values.Dequeue().Int!.Value;
}

public static class TestRoomFactory
{
    public static Room CreateRoom(IBotStrategy? bots = null, IReadOnlyList<int>? deck = null)
        => new("AB12", new Random(42), bots ?? new FixedBotStrategy(), isPublic: true, hostToken: "t0", deck);
}