using HighLow.Domain;
using HighLow.Game;
using Xunit;

namespace HighLow.Tests;

public class DefaultBotStrategyTests
{
    private static readonly int[] FullHand = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Fact]
    public void Plays_third_highest_card_when_deterministic()
    {
        var rng = new ScriptedRandom();
        rng.Script(0.8);
        var bot = new DefaultBotStrategy(rng);
        var s = bot.ChooseSubmission(FullHand, true, SpecialDirection.Highest, 2, 0);
        Assert.Equal(8, s.CardValue);
        Assert.Equal(Special.Normal, s.Special);
        Assert.False(s.Pass);
    }

    [Fact]
    public void Uses_reverse_on_negative_score_with_lowest_card()
    {
        var rng = new ScriptedRandom();
        rng.Script(0.8);
        var bot = new DefaultBotStrategy(rng);
        var s = bot.ChooseSubmission(FullHand, true, SpecialDirection.Highest, -3, -5);
        Assert.Equal(1, s.CardValue);
        Assert.Equal(Special.Reverse, s.Special);
    }

    [Fact]
    public void No_reverse_when_unavailable_even_if_losing()
    {
        var rng = new ScriptedRandom();
        rng.Script(0.8);
        var bot = new DefaultBotStrategy(rng);
        var s = bot.ChooseSubmission(FullHand, false, SpecialDirection.Highest, -3, -5);
        Assert.Equal(Special.Normal, s.Special);
        Assert.Equal(8, s.CardValue);
    }

    [Fact]
    public void Random_branch_plays_any_hand_card_without_special()
    {
        var rng = new ScriptedRandom();
        rng.Script(0.1);
        rng.Script(2);
        var bot = new DefaultBotStrategy(rng);
        var s = bot.ChooseSubmission(FullHand, true, SpecialDirection.Highest, 2, 0);
        Assert.Equal(3, s.CardValue);
        Assert.Equal(Special.Normal, s.Special);
        Assert.False(s.Pass);
    }

    [Fact]
    public void Late_game_small_hand_still_plays()
    {
        var rng = new ScriptedRandom();
        rng.Script(0.8);
        var bot = new DefaultBotStrategy(rng);
        var s = bot.ChooseSubmission(new[] { 4 }, false, SpecialDirection.Highest, 9, 0);
        Assert.Equal(4, s.CardValue);
    }
}