using HighLow.Domain;
using Xunit;

namespace HighLow.Tests;

public class RoundResolverTests
{
    private static RoundResolution Resolve(int prize, Dictionary<int, Submission> subs)
        => RoundResolver.Resolve(1, prize, subs);

    [Fact]
    public void Single_reverse_flips_to_lowest_wins()
    {
        var r = Resolve(5, new()
        {
            [0] = new(9, Special.Normal, false),
            [1] = new(6, Special.Normal, false),
            [2] = new(3, Special.Normal, false),
            [3] = new(1, Special.Reverse, false),
        });
        Assert.Equal(SpecialDirection.Lowest, r.Direction);
        Assert.Equal(3, r.WinnerSeat);
        Assert.Equal(1, r.WinnerCardValue);
    }

    [Fact]
    public void Two_reverses_keep_highest_wins()
    {
        var r = Resolve(5, new()
        {
            [0] = new(9, Special.Normal, false),
            [1] = new(6, Special.Reverse, false),
            [2] = new(3, Special.Reverse, false),
            [3] = new(1, Special.Normal, false),
        });
        Assert.Equal(SpecialDirection.Highest, r.Direction);
        Assert.Equal(0, r.WinnerSeat);
        Assert.Equal(9, r.WinnerCardValue);
    }

    [Fact]
    public void Overlapping_numbers_are_voided_and_survivor_wins()
    {
        var r = Resolve(5, new()
        {
            [0] = new(7, Special.Normal, false),
            [1] = new(7, Special.Normal, false),
            [2] = new(4, Special.Normal, false),
            [3] = new(9, Special.Normal, false),
        });
        Assert.Equal(new[] { 0, 1 }, r.VoidedSeats);
        Assert.Equal(3, r.WinnerSeat);
        Assert.Equal(9, r.WinnerCardValue);
    }

    [Fact]
    public void All_void_means_no_winner_and_prize_burned()
    {
        var r = Resolve(-3, new()
        {
            [0] = new(5, Special.Normal, false),
            [1] = new(5, Special.Normal, false),
            [2] = new(8, Special.Normal, false),
            [3] = new(8, Special.Normal, false),
        });
        Assert.Null(r.WinnerSeat);
        Assert.True(r.PointBurned);
        Assert.False(r.GiftEligible);
    }

    [Fact]
    public void Hidden_winner_card_is_only_reported_internally()
    {
        var r = Resolve(6, new()
        {
            [0] = new(2, Special.Normal, false),
            [1] = new(8, Special.Normal, true),
            [2] = new(4, Special.Normal, false),
            [3] = new(6, Special.Normal, false),
        });
        Assert.Equal(1, r.WinnerSeat);
        Assert.Equal(8, r.HiddenWinnerCardValue);
        Assert.Null(r.WinnerCardValue);
    }

    [Fact]
    public void Gift_eligible_only_when_negative_prize_and_overlap_and_winner()
    {
        Assert.False(Resolve(-3, new()
        {
            [0] = new(5, Special.Normal, false), [1] = new(5, Special.Normal, false),
            [2] = new(8, Special.Normal, false), [3] = new(8, Special.Normal, false),
        }).GiftEligible);

        Assert.True(Resolve(-3, new()
        {
            [0] = new(5, Special.Normal, false), [1] = new(5, Special.Normal, false),
            [2] = new(8, Special.Normal, false), [3] = new(2, Special.Normal, false),
        }).GiftEligible);

        Assert.False(Resolve(6, new()
        {
            [0] = new(5, Special.Normal, false), [1] = new(5, Special.Normal, false),
            [2] = new(8, Special.Normal, false), [3] = new(2, Special.Normal, false),
        }).GiftEligible);

        Assert.False(Resolve(-3, new()
        {
            [0] = new(5, Special.Normal, false), [1] = new(6, Special.Normal, false),
            [2] = new(7, Special.Normal, false), [3] = new(8, Special.Normal, false),
        }).GiftEligible);
    }

    [Fact]
    public void Cards_are_ordered_by_seat_and_pass_is_marked_hidden()
    {
        var r = Resolve(5, new()
        {
            [3] = new(1, Special.Normal, true),
            [0] = new(9, Special.Normal, false),
            [2] = new(3, Special.Normal, false),
            [1] = new(6, Special.Normal, false),
        });
        Assert.Equal(new[] { 0, 1, 2, 3 }, r.Cards.Select(c => c.Seat));
        Assert.Equal(new[] { false, false, false, true }, r.Cards.Select(c => c.Hidden));
    }
}