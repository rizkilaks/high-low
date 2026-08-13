using HighLow.Game;
using Microsoft.AspNetCore.SignalR;

namespace HighLow.Hubs;

public sealed class RoomSweeper : BackgroundService
{
    private readonly RoomManager _rooms;
    private readonly IClock _clock;
    private readonly IHubContext<GameHub> _hub;

    public RoomSweeper(RoomManager rooms, IClock clock, IHubContext<GameHub> hub)
    {
        _rooms = rooms;
        _clock = clock;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var now = _clock.UtcNow;

            var closed = await _rooms.SweepAsync(now);
            foreach (var room in closed)
                await _hub.Clients.Group(GameHub.RoomGroup(room.Code)).SendAsync("RoomClosed", room.Code, ct);

            var rooms = await _rooms.GetRooms();
            foreach (var room in rooms)
            {
                var events = await room.TickAsync(now);
                foreach (var e in events)
                    await _hub.Clients.Group(GameHub.RoomGroup(room.Code)).SendAsync(GameHub.SignalRName(e), e, ct);
            }
        }
    }
}