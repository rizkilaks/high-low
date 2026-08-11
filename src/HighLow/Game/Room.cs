using System.Runtime.InteropServices;
using HighLow.Domain;

namespace HighLow.Game;

public sealed class Room
{
    public const int TotalRounds = 9;
    public const long SubmitPhaseMs = 60_000;
    public const long GiftPhaseMs = 15_000;
    public const long DisconnectGraceMs = 60_000;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<object> _outbox = new();
    private readonly Random _rng;
    private readonly IBotStrategy _bots;
    private readonly List<Seat> _seats = new();
    private readonly List<int> _deck;

    public Room(string code, Random rng, IBotStrategy bots, bool isPublic, string hostToken,
        IReadOnlyList<int>? deck = null)
    {
        Code = code;
        IsPublic = isPublic;
        HostToken = hostToken;
        _rng = rng;
        _bots = bots;
        _deck = deck is not null ? new List<int>(deck) : Shuffle(PointDeck.Cards.ToList());
    }

    public string Code { get; }
    public bool IsPublic { get; }
    public string HostToken { get; }
    public RoomPhase Phase { get; private set; } = RoomPhase.Lobby;
    public int Round { get; private set; }
    public int StartSeat { get; private set; }
    public DateTimeOffset LastActivityUtc { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats;
    public bool IsFinished => Phase == RoomPhase.Finished;

    private int? _prize;
    private SpecialDirection? _direction;
    private int? _winnerSeat;
    private int? _winnerCardVisible;
    private int? _hiddenWinnerCard;
    private int? _giftTargetSeat;
    private int? _burnedPrize;
    private List<int> _forcedReveal = new();
    private List<int> _voidedThisRound = new();
    private Dictionary<int, Submission> _submissions = new();
    private long _deadlineUtcMs;

    private List<int> Shuffle(List<int> src)
    {
        _rng.Shuffle(CollectionsMarshal.AsSpan(src));
        return src;
    }

    private IReadOnlyList<object> DrainOutbox()
    {
        var events = _outbox.ToList();
        _outbox.Clear();
        return events;
    }

    private void Push(object e) => _outbox.Add(e);
    private long NowMs(DateTimeOffset now) => now.ToUnixTimeMilliseconds();
    private void Touch(DateTimeOffset now) => LastActivityUtc = now;

    public async Task<(int Seat, IReadOnlyList<object> Events)> AddHumanAsync(string name, string token, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            var seat = new Seat { Name = name, Token = token, IsBot = false, Connected = true };
            _seats.Add(seat);
            Touch(now);
            Push(new LobbyStateEvent(Code, SeatsInfo(), _seats.Count > 0));
            var events = DrainOutbox().ToList();
            if (_seats.Count == 4 && Phase == RoomPhase.Lobby)
                events.AddRange((await StartLockedAsync(now)).Events);
            return (_seats.Count - 1, events);
        }
        finally { _gate.Release(); }
    }

    public async Task<(bool Ok, string? Error, IReadOnlyList<object> Events)> StartAsync(DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try { return await StartLockedAsync(now); }
        finally { _gate.Release(); }
    }

    private async Task<(bool Ok, string? Error, IReadOnlyList<object> Events)> StartLockedAsync(DateTimeOffset now)
    {
        if (Phase != RoomPhase.Lobby) return (false, "already started", DrainOutbox());
        if (_seats.Count == 0) return (false, "no players", DrainOutbox());

        while (_seats.Count < 4)
            _seats.Add(new Seat { Name = $"Bot {_seats.Count}", Token = $"bot-{_seats.Count}", IsBot = true, Connected = true });

        Push(new GameStartedEvent(SeatsInfo(), 0));
        BeginRoundLocked(now);
        return (true, null, DrainOutbox());
    }

    public async Task<(bool Ok, string? Error, IReadOnlyList<object> Events)> SubmitAsync(int seat, int card, Special special, bool pass, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            if (Phase != RoomPhase.Submitting) return (false, "not in submit phase", DrainOutbox());
            var s = _seats[seat];
            if (s.IsBot || s.BotControlled) return (false, "bot seat", DrainOutbox());
            if (s.HasSubmitted) return (false, "already submitted", DrainOutbox());
            if (!s.Hand.Contains(card)) return (false, "card not in hand", DrainOutbox());
            if (special == Special.Reverse && s.ReverseLeft <= 0) return (false, "no reverse left", DrainOutbox());
            if (pass && _forcedReveal.Contains(seat)) return (false, "forced to reveal", DrainOutbox());

            s.Hand.Remove(card);
            if (special == Special.Reverse) s.ReverseLeft--;
            s.Submission = new Submission(card, special, pass);
            s.HasSubmitted = true;
            _submissions[seat] = s.Submission;
            Touch(now);

            var events = DrainOutbox().ToList();
            if (_seats.All(x => x.HasSubmitted))
                events.AddRange(ResolveRoundLocked(now));
            return (true, null, events);
        }
        finally { _gate.Release(); }
    }

    public async Task<(bool Ok, string? Error, IReadOnlyList<object> Events)> ChooseGiftAsync(int seat, int? target, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            if (Phase != RoomPhase.GiftDecision) return (false, "no gift decision pending", DrainOutbox());
            if (_seats[seat].IsBot || _seats[seat].BotControlled) return (false, "bot seat", DrainOutbox());
            if (seat != _winnerSeat) return (false, "not the winner", DrainOutbox());
            if (target is int t && !_voidedThisRound.Contains(t)) return (false, "target not overlapped", DrainOutbox());
            return (true, null, ApplyResolutionLocked(target, now));
        }
        finally { _gate.Release(); }
    }

    public async Task<(bool Ok, string? Error, RoomView? View, IReadOnlyList<object> Events)> ReconnectAsync(string token, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            var seat = _seats.FindIndex(s => s.Token == token);
            if (seat < 0) return (false, "unknown token", null, DrainOutbox());
            if (Phase == RoomPhase.Finished) return (false, "game over", null, DrainOutbox());
            var s = _seats[seat];
            s.Connected = true;
            s.BotControlled = false;
            s.DisconnectedAtUtc = null;
            Touch(now);
            Push(new PlayerStatusEvent(seat, "reconnected"));
            return (true, null, GetViewLocked(seat), DrainOutbox());
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<object>> DisconnectAsync(int seat, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            var s = _seats[seat];
            if (!s.Connected && !s.IsBot) return DrainOutbox();
            s.Connected = false;
            s.DisconnectedAtUtc = now;
            if (!s.IsBot) Push(new PlayerStatusEvent(seat, "disconnected"));
            return DrainOutbox();
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<object>> TickAsync(DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            // resign disconnected humans past the grace window
            for (var i = 0; i < _seats.Count; i++)
            {
                var s = _seats[i];
                if (!s.IsBot && !s.BotControlled && !s.Connected && s.DisconnectedAtUtc is { } d &&
                    (now - d).TotalMilliseconds >= DisconnectGraceMs)
                {
                    s.BotControlled = true;
                    Push(new PlayerStatusEvent(i, "bot"));
                }
            }

            if (Phase == RoomPhase.Submitting && now.ToUnixTimeMilliseconds() >= _deadlineUtcMs)
            {
                for (var i = 0; i < _seats.Count; i++)
                {
                    var s = _seats[i];
                    if (s.HasSubmitted || s.IsBot || s.BotControlled) continue;
                    var card = s.Hand[_rng.Next(s.Hand.Count)];
                    s.Hand.Remove(card);
                    s.Submission = new Submission(card, Special.Normal, !_forcedReveal.Contains(i));
                    s.HasSubmitted = true;
                    s.MissedSubmissions++;
                    _submissions[i] = s.Submission;
                }
                return DrainOutbox().Concat(ResolveRoundLocked(now)).ToList();
            }

            if (Phase == RoomPhase.GiftDecision && now.ToUnixTimeMilliseconds() >= _deadlineUtcMs)
                return ApplyResolutionLocked(null, now);

            return DrainOutbox();
        }
        finally { _gate.Release(); }
    }

    public async Task<RoomView> ViewAsync(int selfSeat)
    {
        await _gate.WaitAsync();
        try { return GetViewLocked(selfSeat); }
        finally { _gate.Release(); }
    }

    private SeatInfo[] SeatsInfo() => _seats.Select(s => new SeatInfo(s.Name, s.IsBot, s.BotControlled, s.Connected,
        s.Score, s.TieAbs, s.TieMax, s.ReverseLeft, s.Hand.Count)).ToArray();

    private RoomView GetViewLocked(int selfSeat)
    {
        var mine = _seats[selfSeat].IsBot ? Array.Empty<int>() : _seats[selfSeat].Hand.ToArray();
        return new RoomView(Code, Phase, Round, TotalRounds,
            _prize, _direction, _winnerSeat, _winnerCardVisible, _giftTargetSeat, _burnedPrize,
            StartSeat, _forcedReveal.ToArray(), _hiddenWinnerCard, _deadlineUtcMs, SeatsInfo(), mine);
    }

    private void BeginRoundLocked(DateTimeOffset now)
    {
        Round++;
        StartSeat = (Round - 1) % 4;
        _forcedReveal = _seats.Select((_, i) => i).Where(i => i == StartSeat || _seats[i].PassedLastRound).ToList();
        _prize = _deck[Round - 1];
        _direction = null;
        _winnerSeat = null;
        _winnerCardVisible = null;
        _giftTargetSeat = null;
        _burnedPrize = null;
        _voidedThisRound = new List<int>();
        _submissions = new Dictionary<int, Submission>();
        foreach (var s in _seats) { s.HasSubmitted = false; s.Submission = null; s.PassedLastRound = false; }
        Phase = RoomPhase.Submitting;
        _deadlineUtcMs = NowMs(now) + SubmitPhaseMs;
        Touch(now);
        Push(new RoundStartedEvent(Round, TotalRounds, _prize.Value, StartSeat,
            _forcedReveal.ToArray(), _hiddenWinnerCard));
        _hiddenWinnerCard = null;

        BotSubmitsLocked();
    }

    private void BotSubmitsLocked()
    {
        for (var i = 0; i < _seats.Count; i++)
        {
            var s = _seats[i];
            if (!s.IsBot && !s.BotControlled) continue;
            var (card, special, pass) = _bots.ChooseSubmission(s.Hand, s.ReverseLeft > 0,
                _direction ?? SpecialDirection.Highest, _prize!.Value, s.Score);
            if (!s.Hand.Contains(card)) card = s.Hand[0];
            s.Hand.Remove(card);
            if (special == Special.Reverse && s.ReverseLeft > 0) s.ReverseLeft--;
            s.Submission = new Submission(card, special, pass);
            s.HasSubmitted = true;
            _submissions[i] = s.Submission;
        }
    }

    private IReadOnlyList<object> ResolveRoundLocked(DateTimeOffset now)
    {
        var resolution = RoundResolver.Resolve(Round, _prize!.Value, _submissions);
        foreach (var s in _seats) s.PassedLastRound = s.Submission?.Pass ?? false;
        _direction = resolution.Direction;
        _voidedThisRound = resolution.VoidedSeats.ToList();
        _winnerSeat = resolution.WinnerSeat;
        _winnerCardVisible = resolution.WinnerCardValue;
        _hiddenWinnerCard = resolution.HiddenWinnerCardValue;
        _burnedPrize = resolution.PointBurned ? _prize : null;

        Push(new SpecialsRevealedEvent(
            _submissions.Values.Count(v => v.Special == Special.Reverse),
            resolution.Direction,
            _submissions.Where(kv => kv.Value.Special == Special.Reverse).Select(kv => kv.Key).ToArray()));
        Push(new CardsRevealedEvent(
            resolution.Cards.Select(c => new PublicCard(c.Seat, c.Hidden ? null : c.CardValue, c.Hidden)).ToArray(),
            resolution.VoidedSeats));
        Touch(now);

        // bot takeover after 2 consecutive misses
        for (var i = 0; i < _seats.Count; i++)
        {
            var s = _seats[i];
            if (!s.IsBot && !s.BotControlled && s.MissedSubmissions >= 2)
            {
                s.BotControlled = true;
                Push(new PlayerStatusEvent(i, "bot"));
            }
        }

        var winnerIsHuman = _winnerSeat is int w &&
            !_seats[w].IsBot && !_seats[w].BotControlled;
        if (resolution.GiftEligible && winnerIsHuman)
        {
            Phase = RoomPhase.GiftDecision;
            _deadlineUtcMs = NowMs(now) + GiftPhaseMs;
            Push(new GiftPromptEvent(resolution.VoidedSeats));
            return DrainOutbox();
        }

        return ApplyResolutionLocked(null, now);
    }

    private IReadOnlyList<object> ApplyResolutionLocked(int? target, DateTimeOffset now)
    {
        var prize = _prize!.Value;
        var recipient = target ?? _winnerSeat;
        if (recipient is int r)
        {
            var seat = _seats[r];
            seat.Score += prize;
            seat.TieAbs += Math.Abs(prize);
            seat.TieMax = Math.Max(seat.TieMax, Math.Abs(prize));
        }
        _giftTargetSeat = target;

        Push(new RoundResolvedEvent(_winnerSeat, _winnerCardVisible, _giftTargetSeat, _burnedPrize,
            _seats.Select((s, i) => new ScoreLine(i, s.Score, s.TieAbs, s.TieMax)).ToArray()));
        Touch(now);

        if (Round >= TotalRounds)
        {
            Phase = RoomPhase.Finished;
            var winners = GameScores.RankWinners(
                _seats.Select((s, i) => new PlayerStanding(i, s.Score, s.TieAbs, s.TieMax)).ToArray(), 2);
            Push(new GameFinishedEvent(winners,
                _seats.Select((s, i) => new ScoreLine(i, s.Score, s.TieAbs, s.TieMax)).ToArray()));
        }
        else
        {
            BeginRoundLocked(now);
        }
        return DrainOutbox();
    }
}