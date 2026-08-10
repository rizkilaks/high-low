using HighLow.Domain;
using Xunit;

namespace HighLow.Tests;

public class RandomizedInvariantTests
{
    [Theory]
    [InlineData(200)]
    public void Invariants_hold_on_random_rounds(int iterations)
    {
        var rng = new Random(42);
        for (var i = 0; i < iterations; i++)
        {
            var subs = new Dictionary<int, Submission>();
            for (var seat = 0; seat < 4; seat++)
                subs[seat] = new(rng.Next(1, 11), rng.Next(2) == 0 ? Special.Normal : Special.Reverse, rng.Next(2) == 0);

            var prize = PointDeck.Cards[rng.Next(PointDeck.Cards.Count)];
            var r = RoundResolver.Resolve(1, prize, subs);

            Assert.Equal(4, r.Cards.Count);
            Assert.Equal(Enumerable.Range(0, 4), r.Cards.Select(c => c.Seat));

            if (r.WinnerSeat is int w)
            {
                Assert.DoesNotContain(w, r.VoidedSeats);
                var survivors = subs.Where(kv => !r.VoidedSeats.Contains(kv.Key)).Select(kv => kv.Value.CardValue);
                var expected = r.Direction == SpecialDirection.Highest ? survivors.Max() : survivors.Min();
                if (subs[w].Pass)
                {
                    Assert.Null(r.WinnerCardValue);               // hidden winner stays secret
                    Assert.Equal(expected, r.HiddenWinnerCardValue);
                }
                else
                {
                    Assert.Equal(expected, r.WinnerCardValue);
                }
                Assert.Null(r.BurnedPrize);
            }
            else
            {
                Assert.Equal(prize, r.BurnedPrize);
                Assert.Null(r.WinnerCardValue);
            }

            var voidGroups = subs.GroupBy(kv => kv.Value.CardValue).Where(g => g.Count() > 1);
            Assert.Equal(voidGroups.SelectMany(g => g.Select(kv => kv.Key)).OrderBy(x => x), r.VoidedSeats.OrderBy(x => x));
        }
    }
}