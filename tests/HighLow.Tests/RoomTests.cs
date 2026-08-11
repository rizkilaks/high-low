using HighLow.Domain;
using HighLow.Game;
using Xunit;

namespace HighLow.Tests;

public class RoomTests
{
    private static readonly int[] NormalDeck = { 2, 4, 6, 8, 10, 1, 3, 5, 7, 9 };
    private static readonly int[] NegativeFirstDeck = { -3, 2, 4, 6, 8, 10, 1, 5, 7, 9 };

    private static async Task<Room> HumanRoom(FakeClock clock, IReadOnlyList<int>? deck = null)
    {
        var room = TestRoomFactory.CreateRoom(deck: deck);
        await room.AddHumanAsync("a", "ta", clock.UtcNow);
        await room.AddHumanAsync("b", "tb", clock.UtcNow);
        await room.AddHumanAsync("c", "tc", clock.UtcNow);
        await room.AddHumanAsync("d", "td", clock.UtcNow);
        return room;
    }

    [Fact]
    public async Task Fourth_human_auto_starts_round_one()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        Assert.Equal(RoomPhase.Submitting, room.Phase);
        Assert.Equal(1, room.Round);
        var view = await room.ViewAsync(0);
        Assert.Equal(10, view.MyHand.Count);
        Assert.Equal(4, view.Seats.Count);
    }

    [Fact]
    public async Task All_submits_resolve_and_advance_to_round_two()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        var events = new List<object>();
        events.AddRange((await room.SubmitAsync(0, 1, Special.Normal, false, clock.UtcNow)).Events);
        events.AddRange((await room.SubmitAsync(1, 2, Special.Normal, false, clock.UtcNow)).Events);
        events.AddRange((await room.SubmitAsync(2, 3, Special.Normal, false, clock.UtcNow)).Events);
        events.AddRange((await room.SubmitAsync(3, 4, Special.Normal, false, clock.UtcNow)).Events);

        Assert.True(events.OfType<SpecialsRevealedEvent>().Any());
        Assert.True(events.OfType<CardsRevealedEvent>().Any());
        Assert.True(events.OfType<RoundResolvedEvent>().Any());
        var next = events.OfType<RoundStartedEvent>().Last();
        Assert.Equal(clock.UtcNow.ToUnixTimeMilliseconds() + Room.SubmitPhaseMs, next.PhaseDeadlineUtcMs);
        Assert.Equal(2, next.Round);
        Assert.Equal(2, room.Round);
        Assert.Equal(1, room.StartSeat);
    }

    [Fact]
    public async Task Submit_duplicate_and_wrong_card_are_rejected()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        var dup = await room.SubmitAsync(0, 1, Special.Normal, false, clock.UtcNow);
        Assert.True(dup.Ok);
        var again = await room.SubmitAsync(0, 2, Special.Normal, false, clock.UtcNow);
        Assert.False(again.Ok);

        var notInHand = await room.SubmitAsync(1, 99, Special.Normal, false, clock.UtcNow);
        Assert.False(notInHand.Ok);
    }

    [Fact]
    public async Task Reverse_credit_is_one_shot_per_game()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NormalDeck);
        var ok1 = await room.SubmitAsync(0, 1, Special.Reverse, false, clock.UtcNow);
        Assert.True(ok1.Ok);
        await room.SubmitAsync(1, 2, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 3, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 4, Special.Normal, false, clock.UtcNow);

        var spent = await room.SubmitAsync(0, 5, Special.Reverse, false, clock.UtcNow);
        Assert.False(spent.Ok);
        var plain = await room.SubmitAsync(0, 5, Special.Normal, false, clock.UtcNow);
        Assert.True(plain.Ok);
    }

    [Fact]
    public async Task Pass_is_forbidden_for_the_starting_player()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        var forcedPass = await room.SubmitAsync(0, 3, Special.Normal, true, clock.UtcNow);
        Assert.False(forcedPass.Ok);

        var anyoneElse = await room.SubmitAsync(1, 3, Special.Normal, true, clock.UtcNow);
        Assert.True(anyoneElse.Ok);
    }

    [Fact]
    public async Task Timeout_auto_submits_hidden_and_counts_a_miss()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        await room.SubmitAsync(0, 1, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 2, Special.Normal, false, clock.UtcNow);

        clock.Advance(TimeSpan.FromSeconds(61));
        var events = (await room.TickAsync(clock.UtcNow)).ToList();

        Assert.True(events.OfType<RoundResolvedEvent>().Any());
        var cards = events.OfType<CardsRevealedEvent>().Single();
        Assert.Null(cards.Cards.Single(c => c.Seat == 2).Card);
        var view = await room.ViewAsync(2);
        Assert.Equal(9, view.MyHand.Count);
    }

    [Fact]
    public async Task Timeout_on_forced_seat_reveals_the_random_card()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock);
        await room.SubmitAsync(0, 1, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 2, Special.Normal, true, clock.UtcNow);
        await room.SubmitAsync(2, 3, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 4, Special.Normal, false, clock.UtcNow);

        await room.SubmitAsync(0, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 6, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 7, Special.Normal, false, clock.UtcNow);
        clock.Advance(TimeSpan.FromSeconds(61));
        var events = (await room.TickAsync(clock.UtcNow)).ToList();

        var cards = events.OfType<CardsRevealedEvent>().Single();
        Assert.NotNull(cards.Cards.Single(c => c.Seat == 1).Card);
    }

    [Fact]
    public async Task All_void_burns_the_point_and_no_scores_change()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NegativeFirstDeck);
        await room.SubmitAsync(0, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 8, Special.Normal, false, clock.UtcNow);
        var events = (await room.SubmitAsync(3, 8, Special.Normal, false, clock.UtcNow)).Events;

        var resolved = events.OfType<RoundResolvedEvent>().Single();
        Assert.Null(resolved.WinnerSeat);
        Assert.Equal(-3, resolved.BurnedPrize);
        var view = await room.ViewAsync(0);
        Assert.All(view.Seats, s => Assert.Equal(0, s.Score));
    }

    [Fact]
    public async Task Negative_point_with_overlap_opens_gift_phase_and_gift_transfers()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NegativeFirstDeck);
        await room.SubmitAsync(0, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 1, Special.Normal, false, clock.UtcNow);
        var events = (await room.SubmitAsync(3, 2, Special.Normal, false, clock.UtcNow)).Events;

        Assert.Equal(RoomPhase.GiftDecision, room.Phase);
        Assert.Equal(new[] { 0, 1 }, events.OfType<GiftPromptEvent>().Single().TargetSeats);

        var notWinner = await room.ChooseGiftAsync(0, 1, clock.UtcNow);
        Assert.False(notWinner.Ok);
        var badTarget = await room.ChooseGiftAsync(3, 99, clock.UtcNow);
        Assert.False(badTarget.Ok);

        var gift = await room.ChooseGiftAsync(3, 0, clock.UtcNow);
        Assert.True(gift.Ok);
        var resolved = gift.Events.OfType<RoundResolvedEvent>().Single();
        Assert.Equal(3, resolved.WinnerSeat);
        Assert.Equal(0, resolved.GiftTargetSeat);
        var view = await room.ViewAsync(0);
        Assert.Equal(-3, view.Seats[0].Score);
        Assert.Equal(0, view.Seats[3].Score);
        Assert.Equal(RoomPhase.Submitting, room.Phase);
    }

    [Fact]
    public async Task Gift_phase_timeout_keeps_the_negative()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NegativeFirstDeck);
        await room.SubmitAsync(0, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 1, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 2, Special.Normal, false, clock.UtcNow);
        Assert.Equal(RoomPhase.GiftDecision, room.Phase);
        var submitDuringGift = await room.SubmitAsync(0, 2, Special.Normal, false, clock.UtcNow);
        Assert.False(submitDuringGift.Ok);

        clock.Advance(TimeSpan.FromSeconds(16));
        var events = (await room.TickAsync(clock.UtcNow)).ToList();
        var resolved = events.OfType<RoundResolvedEvent>().Single();
        Assert.Null(resolved.GiftTargetSeat);
        var view = await room.ViewAsync(3);
        Assert.Equal(-3, view.Seats[3].Score);
    }

    [Fact]
    public async Task Bot_winner_skips_gift_phase()
    {
        var clock = new FakeClock();
        var bots = new FixedBotStrategy();
        bots.Enqueue(1, 2, 9);
        var room = TestRoomFactory.CreateRoom(bots, NegativeFirstDeck);
        await room.AddHumanAsync("host", "th", clock.UtcNow);
        var start = await room.StartAsync(clock.UtcNow);
        Assert.True(start.Ok);
        Assert.Equal(RoomPhase.Submitting, room.Phase);
        Assert.Equal(1, room.Round);

        var events = (await room.SubmitAsync(0, 1, Special.Normal, false, clock.UtcNow)).Events;
        var resolved = events.OfType<RoundResolvedEvent>().Single();
        Assert.Equal(3, resolved.WinnerSeat);
        Assert.Null(resolved.GiftTargetSeat);
        Assert.Equal(RoomPhase.Submitting, room.Phase);
    }

    [Fact]
    public async Task Hidden_winner_card_only_surfaces_next_round_start()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NormalDeck);
        await room.SubmitAsync(0, 2, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(1, 8, Special.Normal, true, clock.UtcNow);
        await room.SubmitAsync(2, 4, Special.Normal, false, clock.UtcNow);
        var events = (await room.SubmitAsync(3, 6, Special.Normal, false, clock.UtcNow)).Events;

        var resolved = events.OfType<RoundResolvedEvent>().Single();
        Assert.Equal(1, resolved.WinnerSeat);
        Assert.Null(resolved.WinnerCardValue);
        var nextStart = events.OfType<RoundStartedEvent>().Single();
        Assert.Equal(8, nextStart.HiddenWinnerCardValue);
    }

    [Fact]
    public async Task Two_missed_rounds_triggers_bot_takeover()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NormalDeck);
        await room.SubmitAsync(1, 2, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 3, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 4, Special.Normal, false, clock.UtcNow);
        clock.Advance(TimeSpan.FromSeconds(61));
        var r1 = (await room.TickAsync(clock.UtcNow)).ToList();
        Assert.DoesNotContain(r1, e => e is PlayerStatusEvent { Status: "bot" });

        await room.SubmitAsync(1, 5, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(2, 6, Special.Normal, false, clock.UtcNow);
        await room.SubmitAsync(3, 7, Special.Normal, false, clock.UtcNow);
        clock.Advance(TimeSpan.FromSeconds(61));
        var r2 = (await room.TickAsync(clock.UtcNow)).ToList();

        Assert.Contains(r2, e => e is PlayerStatusEvent { Seat: 0, Status: "bot" });
        Assert.Equal(RoomPhase.Submitting, room.Phase);
        clock.Advance(TimeSpan.FromSeconds(61));
        var r3 = (await room.TickAsync(clock.UtcNow)).ToList();
        Assert.True(r3.OfType<RoundResolvedEvent>().Any());
    }

    [Fact]
    public async Task Disconnect_grace_then_bot_then_reconnect_reclaims()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NormalDeck);
        var disc = (await room.DisconnectAsync(0, clock.UtcNow)).ToList();
        Assert.Contains(disc, e => e is PlayerStatusEvent { Seat: 0, Status: "disconnected" });

        clock.Advance(TimeSpan.FromSeconds(30));
        var early = (await room.TickAsync(clock.UtcNow)).ToList();
        Assert.DoesNotContain(early, e => e is PlayerStatusEvent { Seat: 0, Status: "bot" });

        clock.Advance(TimeSpan.FromSeconds(31));
        var late = (await room.TickAsync(clock.UtcNow)).ToList();
        Assert.Contains(late, e => e is PlayerStatusEvent { Seat: 0, Status: "bot" });

        var reconnect = await room.ReconnectAsync("ta", clock.UtcNow);
        Assert.True(reconnect.Ok);
        var view = reconnect.View!;
        Assert.Equal(9, view.MyHand.Count); // bot played one round for this seat
        Assert.False(view.Seats[0].BotControlled);
    }

    [Fact]
    public async Task Nine_rounds_finish_the_game_with_two_winners()
    {
        var clock = new FakeClock();
        var room = await HumanRoom(clock, NormalDeck);
        int[][] perSeat = new[]
        {
            new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            new[] { 5, 6, 7, 8, 9, 10, 2, 3, 4 },
            new[] { 8, 9, 10, 1, 2, 3, 4, 5, 6 },
            new[] { 5, 6, 7, 8, 9, 10, 2, 3, 4 },
        };
        var events = new List<object>();
        for (var round = 1; round <= 9; round++)
        {
            events.AddRange((await room.SubmitAsync(0, perSeat[0][round - 1], Special.Normal, false, clock.UtcNow)).Events);
            events.AddRange((await room.SubmitAsync(1, perSeat[1][round - 1], Special.Normal, false, clock.UtcNow)).Events);
            events.AddRange((await room.SubmitAsync(2, perSeat[2][round - 1], Special.Normal, false, clock.UtcNow)).Events);
            events.AddRange((await room.SubmitAsync(3, perSeat[3][round - 1], Special.Normal, false, clock.UtcNow)).Events);
        }
        Assert.Equal(RoomPhase.Finished, room.Phase);
        var finished = events.OfType<GameFinishedEvent>().Single();
        Assert.Equal(2, finished.WinnerSeats.Count);
    }
}