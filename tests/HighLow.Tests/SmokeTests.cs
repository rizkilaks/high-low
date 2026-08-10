using Xunit;

namespace HighLow.Tests;

public class SmokeTests
{
    [Fact]
    public void PointDeck_has_exactly_10_cards()
    {
        Assert.Equal(10, HighLow.Domain.PointDeck.Cards.Count);
        Assert.Equal(-1, HighLow.Domain.PointDeck.Cards[0]);
        Assert.Equal(10, HighLow.Domain.PointDeck.Cards[9]);
    }
}