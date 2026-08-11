using System.Collections.Concurrent;
using HighLow.Domain;
using HighLow.Game;
using Microsoft.AspNetCore.SignalR;

namespace HighLow.Hubs;

public sealed record JoinedRoom(string RoomCode, int Seat, string Token);

public sealed class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, (string Code, int Seat)> Connections = new();

    private readonly RoomManager _rooms;
    private readonly IClock _clock;

    public GameHub(RoomManager rooms, IClock clock)
    {
        _rooms = rooms;
        _clock = clock;
    }

    public static string RoomGroup(string code) => "room-" + code;

    public async Task<JoinedRoom?> CreateRoom(string name, bool isPublic)
    {
        var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var r = await _rooms.CreateRoomAsync(name, ip, isPublic, null, _clock.UtcNow);
        return await CompleteJoinAsync(r);
    }

    public async Task<JoinedRoom?> JoinRoom(string code, string name)
    {
        var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var r = await _rooms.JoinRoomAsync(code, name, ip, _clock.UtcNow);
        return await CompleteJoinAsync(r);
    }

    public async Task<JoinedRoom?> QuickMatch(string name)
    {
        var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var r = await _rooms.QuickMatchAsync(name, ip, _clock.UtcNow);
        return await CompleteJoinAsync(r);
    }

    public async Task<bool> StartWithBots(string roomCode, string token)
    {
        var room = await _rooms.FindRoomAsync(roomCode);
        if (room is null || room.HostToken != token) return await RejectAsync("not the host");

        var (ok, error, events) = await room.StartAsync(_clock.UtcNow);
        if (!ok) return await RejectAsync(error!);

        await PushToRoomAsync(roomCode, events);
        return true;
    }

    public async Task<bool> Submit(string roomCode, string token, int card, Special special, bool pass)
    {
        var room = await _rooms.FindRoomAsync(roomCode);
        if (room is null) return await RejectAsync("room not found");

        var seat = room.Seats.ToList().FindIndex(s => s.Token == token);
        if (seat < 0) return await RejectAsync("unknown token");

        var (ok, error, events) = await room.SubmitAsync(seat, card, special, pass, _clock.UtcNow);
        if (!ok) return await RejectAsync(error!);

        Metrics.Submissions++;
        await PushToRoomAsync(roomCode, events);
        return true;
    }

    public async Task<bool> ChooseGift(string roomCode, string token, int? target)
    {
        var room = await _rooms.FindRoomAsync(roomCode);
        if (room is null) return await RejectAsync("room not found");

        var seat = room.Seats.ToList().FindIndex(s => s.Token == token);
        if (seat < 0) return await RejectAsync("unknown token");

        var (ok, error, events) = await room.ChooseGiftAsync(seat, target, _clock.UtcNow);
        if (!ok) return await RejectAsync(error!);

        Metrics.GiftsChosen++;
        await PushToRoomAsync(roomCode, events);
        return true;
    }

    public async Task<RoomView?> Reconnect(string roomCode, string token)
    {
        var room = await _rooms.FindRoomAsync(roomCode);
        if (room is null) { await SendRejectedAsync("room not found"); return null; }

        var seat = room.Seats.ToList().FindIndex(s => s.Token == token);
        if (seat < 0) { await SendRejectedAsync("unknown token"); return null; }

        var (ok, error, view, events) = await room.ReconnectAsync(token, _clock.UtcNow);
        if (!ok) { await SendRejectedAsync(error!); return null; }

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomCode));
        Connections[Context.ConnectionId] = (roomCode, seat);

        Metrics.Reconnects++;
        await PushToRoomAsync(roomCode, events);
        return view;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var entry))
        {
            var room = await _rooms.FindRoomAsync(entry.Code);
            if (room is not null)
            {
                var events = await room.DisconnectAsync(entry.Seat, _clock.UtcNow);
                await PushToRoomAsync(entry.Code, events);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<JoinedRoom?> CompleteJoinAsync(JoinResult r)
    {
        if (!r.Ok) { await SendRejectedAsync(r.Error!); return null; }

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(r.Room!.Code));
        Connections[Context.ConnectionId] = (r.Room.Code, r.Seat);
        await PushToRoomAsync(r.Room.Code, r.Events);
        return new JoinedRoom(r.Room.Code, r.Seat, r.Token);
    }

    private async Task<bool> RejectAsync(string reason)
    {
        await SendRejectedAsync(reason);
        return false;
    }

    private Task SendRejectedAsync(string reason) => Clients.Caller.SendAsync("RejectedMessage", reason);

    private async Task PushToRoomAsync(string code, IReadOnlyList<object> events)
    {
        var group = Clients.Group(RoomGroup(code));
        foreach (var e in events)
        {
            if (e is GameStartedEvent) Metrics.GamesStarted++;
            else if (e is GameFinishedEvent) Metrics.GamesFinished++;
            await group.SendAsync(SignalRName(e), e);
        }
    }

    private static string SignalRName(object e) => e switch
    {
        LobbyStateEvent => "LobbyState",
        GameStartedEvent => "GameStarted",
        RoundStartedEvent => "RoundStarted",
        SpecialsRevealedEvent => "SpecialsRevealed",
        CardsRevealedEvent => "CardsRevealed",
        RoundResolvedEvent => "RoundResolved",
        GiftPromptEvent => "GiftPrompt",
        GameFinishedEvent => "GameFinished",
        PlayerStatusEvent p => p.Status switch
        {
            "disconnected" => "PlayerDisconnected",
            "reconnected" => "PlayerReconnected",
            _ => "PlayerBotControlled",
        },
        _ => e.GetType().Name,
    };
}