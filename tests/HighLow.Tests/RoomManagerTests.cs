using HighLow.Domain;
using HighLow.Game;
using Xunit;

namespace HighLow.Tests;

public class RoomManagerTests
{
    private static RoomManager CreateManager() => new(new Random(42), new FixedBotStrategy());
    private static readonly FakeClock Clock = new();

    [Fact]
    public async Task CreateRoom_makes_room_with_host_seat_zero_and_code()
    {
        var m = CreateManager();
        var r = await m.CreateRoomAsync("rizki", "1.2.3.4", isPublic: true, null, Clock.UtcNow);
        Assert.True(r.Ok);
        Assert.Equal(4, r.Room!.Code.Length);
        Assert.Equal(0, r.Seat);
        Assert.NotEmpty(r.Token);
        Assert.Equal(RoomPhase.Lobby, r.Room.Phase);
    }

    [Fact]
    public async Task CreateRoom_rejects_duplicate_requested_code()
    {
        var m = CreateManager();
        var first = await m.CreateRoomAsync("a", "1.1.1.1", true, "AB12", Clock.UtcNow);
        Assert.True(first.Ok);
        var dup = await m.CreateRoomAsync("b", "2.2.2.2", true, "AB12", Clock.UtcNow);
        Assert.False(dup.Ok);
        var other = await m.CreateRoomAsync("c", "3.3.3.3", true, "CD34", Clock.UtcNow);
        Assert.True(other.Ok);
    }

    [Fact]
    public async Task JoinRoom_joins_by_code_and_rejects_unknown()
    {
        var m = CreateManager();
        var host = await m.CreateRoomAsync("a", "1.1.1.1", true, null, Clock.UtcNow);
        var join = await m.JoinRoomAsync(host.Room!.Code, "b", "2.2.2.2", Clock.UtcNow);
        Assert.True(join.Ok);
        Assert.Equal(1, join.Seat);
        Assert.Equal(host.Room, join.Room);
        var miss = await m.JoinRoomAsync("ZZZZ", "b", "2.2.2.2", Clock.UtcNow);
        Assert.False(miss.Ok);
    }

    [Fact]
    public async Task JoinRoom_rejects_full_or_started_room()
    {
        var m = CreateManager();
        var host = await m.CreateRoomAsync("a", "1.1.1.1", true, null, Clock.UtcNow);
        await m.JoinRoomAsync(host.Room!.Code, "b", "2.2.2.2", Clock.UtcNow);
        await m.JoinRoomAsync(host.Room!.Code, "c", "3.3.3.3", Clock.UtcNow);
        await m.JoinRoomAsync(host.Room!.Code, "d", "4.4.4.4", Clock.UtcNow);
        var full = await m.JoinRoomAsync(host.Room!.Code, "e", "5.5.5.5", Clock.UtcNow);
        Assert.False(full.Ok);

        await host.Room.StartAsync(Clock.UtcNow);
        var started = await m.JoinRoomAsync(host.Room!.Code, "f", "6.6.6.6", Clock.UtcNow);
        Assert.False(started.Ok);
    }

    [Fact]
    public async Task Per_ip_cap_blocks_fourth_room_from_same_address()
    {
        var m = CreateManager();
        await m.CreateRoomAsync("a", "9.9.9.9", true, null, Clock.UtcNow);
        await m.CreateRoomAsync("b", "9.9.9.9", true, null, Clock.UtcNow);
        await m.CreateRoomAsync("c", "9.9.9.9", true, null, Clock.UtcNow);
        var fourth = await m.CreateRoomAsync("d", "9.9.9.9", true, null, Clock.UtcNow);
        Assert.False(fourth.Ok);

        var otherIp = await m.CreateRoomAsync("e", "8.8.8.8", true, null, Clock.UtcNow);
        Assert.True(otherIp.Ok);
    }

    [Fact]
    public async Task QuickMatch_joins_public_lobby_else_creates_new_room()
    {
        var m = CreateManager();
        var host = await m.CreateRoomAsync("a", "1.1.1.1", true, null, Clock.UtcNow);
        await m.CreateRoomAsync("p", "2.2.2.2", isPublic: false, null, Clock.UtcNow);

        var q1 = await m.QuickMatchAsync("b", "7.7.7.7", Clock.UtcNow);
        Assert.True(q1.Ok);
        Assert.Equal(host.Room, q1.Room);

        await m.JoinRoomAsync(host.Room!.Code, "c", "3.3.3.3", Clock.UtcNow);
        await m.JoinRoomAsync(host.Room!.Code, "d", "4.4.4.4", Clock.UtcNow);
        var q2 = await m.QuickMatchAsync("e", "7.7.7.7", Clock.UtcNow);
        Assert.True(q2.Ok);
        Assert.NotEqual(host.Room!.Code, q2.Room!.Code);
        Assert.True(q2.Room.IsPublic);
        Assert.Equal(0, q2.Seat);
    }

    [Fact]
    public async Task QuickMatch_respects_ip_cap()
    {
        var m = CreateManager();
        await m.CreateRoomAsync("a", "6.6.6.6", true, null, Clock.UtcNow);
        await m.CreateRoomAsync("b", "6.6.6.6", true, null, Clock.UtcNow);
        await m.CreateRoomAsync("c", "6.6.6.6", true, null, Clock.UtcNow);
        var q = await m.QuickMatchAsync("d", "6.6.6.6", Clock.UtcNow);
        Assert.False(q.Ok);
    }

    [Fact]
    public async Task Sweep_removes_stale_lobby_and_finished_rooms_only()
    {
        var m = CreateManager();
        var lobby = await m.CreateRoomAsync("a", "1.1.1.1", true, null, Clock.UtcNow);
        var active = await m.CreateRoomAsync("b", "2.2.2.2", true, null, Clock.UtcNow);
        await active.Room!.StartAsync(Clock.UtcNow);

        var finished = await m.CreateRoomAsync("c", "3.3.3.3", true, null, Clock.UtcNow);
        await finished.Room!.StartAsync(Clock.UtcNow);
        for (var round = 1; round <= 9; round++)
            await finished.Room.SubmitAsync(0, round, Special.Normal, false, Clock.UtcNow);
        Assert.True(finished.Room.IsFinished);

        Clock.Advance(TimeSpan.FromMinutes(11));
        var closed = await m.SweepAsync(Clock.UtcNow);
        Assert.Contains(lobby.Room, closed);
        Assert.DoesNotContain(active.Room, closed);
        Assert.DoesNotContain(finished.Room, closed);
        Assert.Null(await m.FindRoomAsync(lobby.Room!.Code));

        Clock.Advance(TimeSpan.FromMinutes(20));
        closed = await m.SweepAsync(Clock.UtcNow);
        Assert.Contains(finished.Room, closed);
        Assert.DoesNotContain(active.Room, closed);
        Assert.Null(await m.FindRoomAsync(finished.Room!.Code));
    }

    [Fact]
    public async Task GetRooms_returns_snapshot_of_all_rooms_until_swept()
    {
        var m = CreateManager();
        var a = await m.CreateRoomAsync("a", "1.1.1.1", true, null, Clock.UtcNow);
        var b = await m.CreateRoomAsync("b", "2.2.2.2", true, null, Clock.UtcNow);

        var rooms = await m.GetRooms();
        Assert.Equal(2, rooms.Count);
        Assert.Contains(a.Room, rooms);
        Assert.Contains(b.Room, rooms);

        Clock.Advance(TimeSpan.FromMinutes(11));
        await m.SweepAsync(Clock.UtcNow);
        Assert.Empty(await m.GetRooms());
    }
}