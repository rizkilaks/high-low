using HighLow.Domain;

namespace HighLow.Game;

public sealed record SeatInfo(string Name, bool IsBot, bool BotControlled, bool Connected,
    int Score, int TieAbs, int TieMax, int ReverseLeft, int HandCount);

public sealed record LobbyStateEvent(string RoomCode, IReadOnlyList<SeatInfo> Seats, bool CanStart);
public sealed record GameStartedEvent(IReadOnlyList<SeatInfo> Seats, int StartSeat);
public sealed record RoundStartedEvent(int Round, int TotalRounds, int Prize, int StartSeat,
    IReadOnlyList<int> ForcedRevealSeats, int? HiddenWinnerCardValue);
public sealed record SpecialsRevealedEvent(int Reverses, SpecialDirection Direction, IReadOnlyList<int> ReverseSeats);
public sealed record PublicCard(int Seat, int? Card, bool Hidden);
public sealed record CardsRevealedEvent(IReadOnlyList<PublicCard> Cards, IReadOnlyList<int> VoidedSeats);
public sealed record ScoreLine(int Seat, int Score, int TieAbs, int TieMax);
public sealed record RoundResolvedEvent(int? WinnerSeat, int? WinnerCardValue, int? GiftTargetSeat,
    int? BurnedPrize, IReadOnlyList<ScoreLine> Scores);
public sealed record GiftPromptEvent(IReadOnlyList<int> TargetSeats);
public sealed record GameFinishedEvent(IReadOnlyList<int> WinnerSeats, IReadOnlyList<ScoreLine> Scores);
public sealed record PlayerStatusEvent(int Seat, string Status); // "disconnected" | "reconnected" | "bot"