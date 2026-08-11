namespace HighLow.Domain;

public sealed record RoundResolution(
    int RoundNumber,
    int Point,
    SpecialDirection Direction,
    IReadOnlyList<RevealedCard> Cards,
    IReadOnlyList<int> VoidedSeats,
    int? WinnerSeat,
    int? WinnerCardValue,
    int? HiddenWinnerCardValue,
    bool PointBurned,
    bool GiftEligible);