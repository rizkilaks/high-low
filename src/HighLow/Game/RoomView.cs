using HighLow.Domain;

namespace HighLow.Game;

public sealed record RoomView(
    string RoomCode, RoomPhase Phase, int Round, int TotalRounds,
    int? Prize, SpecialDirection? Direction, int? WinnerSeat, int? WinnerCardValue, int? GiftTargetSeat,
    int? BurnedPrize, int StartSeat, IReadOnlyList<int> ForcedRevealSeats, int? HiddenWinnerCardValue,
    long PhaseDeadlineUtcMs, IReadOnlyList<SeatInfo> Seats, IReadOnlyList<int> MyHand,
    IReadOnlyList<int>? WinnerSeats, IReadOnlyList<int>? GiftTargets);