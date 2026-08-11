using HighLow.Domain;
using HighLow.Game;
using HighLow.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace HighLow.Tests;

public class GameHubTests
{
    [Fact]
    public async Task Two_humans_and_bots_play_a_full_round()
    {
        await using var ctx = new HubTestContext();
        ctx.Bots.Enqueue(7, 8);
        await using var host = await ctx.ConnectAsync("host");
        await using var guest = await ctx.ConnectAsync("guest");
        var sinkH = new EventSink(host);
        var sinkG = new EventSink(guest);

        var created = await host.InvokeAsync<JoinedRoom>("CreateRoom", "host", true);
        Assert.NotNull(created);
        Assert.Equal(0, created!.Seat);
        var joined = await guest.InvokeAsync<JoinedRoom>("JoinRoom", created.RoomCode, "guest");
        Assert.NotNull(joined);
        Assert.Equal(1, joined!.Seat);

        Assert.True(await host.InvokeAsync<bool>("StartWithBots", created.RoomCode, created.Token));
        await sinkH.WaitAsync<GameStartedEvent>();
        await sinkG.WaitAsync<GameStartedEvent>();

        Assert.True(await host.InvokeAsync<bool>("Submit", created.RoomCode, created.Token, 5, Special.Normal, false));
        Assert.True(await guest.InvokeAsync<bool>("Submit", created.RoomCode, joined.Token, 6, Special.Normal, false));
        await sinkH.WaitCountAsync<RoundResolvedEvent>(1);
        await sinkG.WaitCountAsync<RoundResolvedEvent>(1);
        await sinkH.WaitCountAsync<RoundStartedEvent>(2);
        await sinkG.WaitCountAsync<RoundStartedEvent>(2);

        var resolved = sinkH.Last<RoundResolvedEvent>()!;
        Assert.Equal(3, resolved.WinnerSeat);
        Assert.Equal(8, resolved.WinnerCardValue);
        var nextStart = sinkH.Last<RoundStartedEvent>()!;
        Assert.Equal(2, nextStart.Round);
    }

    [Fact]
    public async Task Hidden_cards_and_hidden_winner_never_leave_the_server()
    {
        await using var ctx = new HubTestContext();
        ctx.Bots.Enqueue(3, 4);
        await using var host = await ctx.ConnectAsync("host");
        await using var guest = await ctx.ConnectAsync("guest");
        var sinkH = new EventSink(host);
        var sinkG = new EventSink(guest);

        var created = await host.InvokeAsync<JoinedRoom>("CreateRoom", "host", true);
        var joined = await guest.InvokeAsync<JoinedRoom>("JoinRoom", created!.RoomCode, "guest");
        Assert.True(await host.InvokeAsync<bool>("StartWithBots", created.RoomCode, created.Token));
        await sinkH.WaitAsync<GameStartedEvent>();

        Assert.True(await host.InvokeAsync<bool>("Submit", created.RoomCode, created.Token, 5, Special.Normal, false));
        Assert.True(await guest.InvokeAsync<bool>("Submit", created.RoomCode, joined!.Token, 6, Special.Normal, true));
        await sinkH.WaitAsync<RoundResolvedEvent>();
        await sinkG.WaitAsync<RoundResolvedEvent>();
        await sinkH.WaitCountAsync<RoundStartedEvent>(2);
        await sinkG.WaitCountAsync<RoundStartedEvent>(2);

        foreach (var sink in new[] { sinkH, sinkG })
        {
            var cards = sink.Last<CardsRevealedEvent>()!;
            Assert.Equal(5, cards.Cards.Single(c => c.Seat == 0).Card);
            Assert.Null(cards.Cards.Single(c => c.Seat == 1).Card);

            var resolved = sink.Last<RoundResolvedEvent>()!;
            Assert.Equal(1, resolved.WinnerSeat);
            Assert.Null(resolved.WinnerCardValue);

            var nextStart = sink.Last<RoundStartedEvent>()!;
            Assert.Equal(6, nextStart.HiddenWinnerCardValue);
        }
    }

    [Fact]
    public async Task Invalid_calls_are_rejected_with_reason()
    {
        await using var ctx = new HubTestContext();
        await using var host = await ctx.ConnectAsync("host");
        await using var guest = await ctx.ConnectAsync("guest");
        var sink = new EventSink(guest);

        var created = await host.InvokeAsync<JoinedRoom>("CreateRoom", "host", true);
        var joined = await guest.InvokeAsync<JoinedRoom>("JoinRoom", created!.RoomCode, "guest");

        Assert.False(await guest.InvokeAsync<bool>("StartWithBots", created.RoomCode, joined!.Token));
        Assert.False(await guest.InvokeAsync<bool>("Submit", "ZZZZ", joined.Token, 1, Special.Normal, false));
        Assert.False(await guest.InvokeAsync<bool>("Submit", created.RoomCode, "bogus", 1, Special.Normal, false));
        Assert.False(await guest.InvokeAsync<bool>("Submit", created.RoomCode, joined.Token, 1, Special.Normal, false));
        await Task.Delay(100);

        Assert.Contains("not the host", sink.Rejections);
        Assert.Contains("room not found", sink.Rejections);
        Assert.Contains("unknown token", sink.Rejections);
        Assert.Contains("not in submit phase", sink.Rejections);
    }

    [Fact]
    public async Task Reconnect_view_contains_only_own_hand()
    {
        await using var ctx = new HubTestContext();
        ctx.Bots.Enqueue(7, 8);
        await using var host = await ctx.ConnectAsync("host");
        await using var guest = await ctx.ConnectAsync("guest");
        var created = await host.InvokeAsync<JoinedRoom>("CreateRoom", "host", true);
        var joined = await guest.InvokeAsync<JoinedRoom>("JoinRoom", created!.RoomCode, "guest");
        Assert.True(await host.InvokeAsync<bool>("StartWithBots", created.RoomCode, created.Token));
        Assert.True(await host.InvokeAsync<bool>("Submit", created.RoomCode, created.Token, 5, Special.Normal, false));
        Assert.True(await guest.InvokeAsync<bool>("Submit", created.RoomCode, joined!.Token, 6, Special.Normal, false));

        var view = await guest.InvokeAsync<RoomView>("Reconnect", created.RoomCode, joined.Token);

        Assert.NotNull(view);
        Assert.Equal(9, view!.MyHand.Count);
        Assert.DoesNotContain(6, view.MyHand);
        Assert.Equal(9, view.Seats[1].HandCount);
    }

    [Fact]
    public async Task Index_html_is_served()
    {
        await using var ctx = new HubTestContext();
        var index = await ctx.Http.GetAsync("/");
        Assert.Equal(System.Net.HttpStatusCode.OK, index.StatusCode);
        Assert.Contains("highlow", await index.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Metrics_endpoint_reports_live_counters()
    {
        await using var ctx = new HubTestContext();
        await using var host = await ctx.ConnectAsync("host");
        var created = await host.InvokeAsync<JoinedRoom>("CreateRoom", "host", true);
        Assert.True(await host.InvokeAsync<bool>("StartWithBots", created!.RoomCode, created.Token));

        var resp = await ctx.Http.GetAsync("/metrics");
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Contains("\"rooms\":", body);
        Assert.Contains("\"players\":", body);
        Assert.Contains("\"gamesStarted\":", body);
        Assert.Contains("\"gamesFinished\":", body);
        Assert.Contains("\"submissions\":", body);
        Assert.Contains("\"reconnects\":", body);
    }
}