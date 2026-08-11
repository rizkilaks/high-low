using HighLow.Domain;

namespace HighLow.Game;

public sealed class Seat
{
    public required string Name { get; init; }
    public required string Token { get; init; }
    public bool IsBot { get; set; }
    public bool BotControlled { get; set; }
    public bool Connected { get; set; }
    public DateTimeOffset? DisconnectedAtUtc { get; set; }
    public int Score { get; set; }
    public int TieAbs { get; set; }
    public int TieMax { get; set; }
    public int ReverseLeft { get; set; } = 1;
    public List<int> Hand { get; } = Enumerable.Range(1, 10).ToList();
    public bool HasSubmitted { get; set; }
    public Submission? Submission { get; set; }
    public int MissedSubmissions { get; set; }
    public bool PassedLastRound { get; set; }
}