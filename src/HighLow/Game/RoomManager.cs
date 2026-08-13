namespace HighLow.Game;

public sealed record JoinResult(bool Ok, string? Error, Room? Room, int Seat, string Token, IReadOnlyList<object> Events)
{
    public static JoinResult Fail(string error) => new(false, error, null, -1, "", Array.Empty<object>());
}

public sealed class RoomManager
{
    public const int MaxRoomsPerIp = 3;
    public const long LobbyIdleTtlMs = 10 * 60_000;
    public const long FinishedTtlMs = 30 * 60_000;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static int CountRooms;
    public static int CountPlayers;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Random _rng;
    private readonly IBotStrategy _bots;
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<Room, HashSet<string>> _ipsByRoom = new();

    public RoomManager(Random rng, IBotStrategy bots)
    {
        _rng = rng;
        _bots = bots;
    }

    public async Task<JoinResult> CreateRoomAsync(string name, string ip, bool isPublic, string? requestedCode, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            if (CountRoomsForIp(ip) >= MaxRoomsPerIp) return JoinResult.Fail("too many rooms from this address");

            var code = requestedCode?.ToUpperInvariant() ?? NewCodeLocked();
            if (_rooms.ContainsKey(code)) return JoinResult.Fail("code already taken");

            return await CreateAndJoinLockedAsync(code, name, ip, isPublic, now);
        }
        finally { _gate.Release(); }
    }

    public async Task<JoinResult> JoinRoomAsync(string code, string name, string ip, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            if (CountRoomsForIp(ip) >= MaxRoomsPerIp) return JoinResult.Fail("too many rooms from this address");

            if (!_rooms.TryGetValue(code.ToUpperInvariant(), out var room)) return JoinResult.Fail("room not found");

            if (room.Phase != RoomPhase.Lobby || room.Seats.Count >= 4) return JoinResult.Fail("room full or already started");

            return await AddHumanLockedAsync(room, name, ip, now);
        }
        finally { _gate.Release(); }
    }

    public async Task<JoinResult> QuickMatchAsync(string name, string ip, DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            if (CountRoomsForIp(ip) >= MaxRoomsPerIp) return JoinResult.Fail("too many rooms from this address");

            var room = _rooms.Values.FirstOrDefault(r => r.IsPublic && r.Phase == RoomPhase.Lobby && r.Seats.Count < 4);
            if (room is not null) return await AddHumanLockedAsync(room, name, ip, now);

            var code = NewCodeLocked();
            return await CreateAndJoinLockedAsync(code, name, ip, isPublic: true, now);
        }
        finally { _gate.Release(); }
    }

    public async Task<Room?> FindRoomAsync(string code)
    {
        await _gate.WaitAsync();
        try { return _rooms.GetValueOrDefault(code.ToUpperInvariant()); }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<Room>> GetRooms()
    {
        await _gate.WaitAsync();
        try { return _rooms.Values.ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<Room>> SweepAsync(DateTimeOffset now)
    {
        await _gate.WaitAsync();
        try
        {
            var closed = new List<Room>();
            foreach (var room in _rooms.Values.ToList())
            {
                var idle = (now - room.LastActivityUtc).TotalMilliseconds;
                var stale = room.Phase == RoomPhase.Lobby
                    ? idle >= LobbyIdleTtlMs
                    : room.IsFinished && idle >= FinishedTtlMs;
                if (stale)
                {
                    _rooms.Remove(room.Code);
                    _ipsByRoom.Remove(room);
                    CountRooms--;
                    CountPlayers -= room.Seats.Count(s => !s.IsBot);
                    closed.Add(room);
                }
            }
            return closed;
        }
        finally { _gate.Release(); }
    }

    private int CountRoomsForIp(string ip) => _ipsByRoom.Values.Count(set => set.Contains(ip));

    private string NewCodeLocked()
    {
        var code = new char[4];
        do
        {
            for (var i = 0; i < code.Length; i++)
                code[i] = Alphabet[_rng.Next(Alphabet.Length)];
        } while (_rooms.ContainsKey(new string(code)));
        return new string(code);
    }

    private async Task<JoinResult> CreateAndJoinLockedAsync(string code, string name, string ip, bool isPublic, DateTimeOffset now)
    {
        var token = Guid.NewGuid().ToString("N");
        var room = new Room(code, _rng, _bots, isPublic, token);
        _rooms[code] = room;
        _ipsByRoom[room] = new HashSet<string>();
        CountRooms++;
        return await AddHumanLockedAsync(room, name, ip, now, token);
    }

    private async Task<JoinResult> AddHumanLockedAsync(Room room, string name, string ip, DateTimeOffset now, string? tokenOverride = null)
    {
        var token = tokenOverride ?? Guid.NewGuid().ToString("N");
        var (seat, events) = await room.AddHumanAsync(name, token, now);
        _ipsByRoom[room].Add(ip);
        CountPlayers++;
        return new JoinResult(true, null, room, seat, token, events);
    }
}