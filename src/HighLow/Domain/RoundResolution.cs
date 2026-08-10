namespace HighLow.Domain;

public sealed record RoundResolution(
    int RoundNumber,
    int Prize,
    SpecialDirection Direction,
    IReadOnlyList<RevealedCard> Cards,
    IReadOnlyList<int> VoidedSeats,
    int? WinnerSeat,
    int? WinnerCardValue,
    int? HiddenWinnerCardValue,
    int? BurnedPrize,
    bool GiftEligible);