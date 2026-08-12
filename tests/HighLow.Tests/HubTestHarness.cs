using System.Collections.Concurrent;
using HighLow.Domain;
using HighLow.Game;
using HighLow.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HighLow.Tests;

public sealed class HubTestContext : IAsyncDisposable
{
    public FakeClock Clock { get; } = new();
    public FixedBotStrategy Bots { get; } = new();
    public TestServer Server { get; }
    public HttpClient Http { get; }

    private readonly WebApplicationFactory<Program> _factory;

    public HubTestContext()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IClock>();
                s.AddSingleton<IClock>(Clock);
                s.RemoveAll<IBotStrategy>();
                s.AddSingleton<IBotStrategy>(Bots);
                s.RemoveAll<Random>();
                s.AddSingleton(new Random(42));
                s.RemoveAll<IHostedService>();
            });
        });
        Server = _factory.Server;
        Http = _factory.CreateClient();
    }

    public async Task<HubConnection> ConnectAsync(string name)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost/hubs/game"), o =>
                o.HttpMessageHandlerFactory = _ => Server.CreateHandler())
            .AddJsonProtocol(o =>
                o.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
            .Build();
        await conn.StartAsync();
        return conn;
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}

public sealed class EventSink
{
    private readonly ConcurrentQueue<object> _all = new();
    private readonly ConcurrentQueue<string> _rejections = new();
    private readonly ConcurrentQueue<RoomView> _views = new();

    public EventSink(HubConnection conn)
    {
        conn.On<LobbyStateEvent>("LobbyState", e => _all.Enqueue(e));
        conn.On<GameStartedEvent>("GameStarted", e => _all.Enqueue(e));
        conn.On<RoundStartedEvent>("RoundStarted", e => _all.Enqueue(e));
        conn.On<SpecialsRevealedEvent>("SpecialsRevealed", e => _all.Enqueue(e));
        conn.On<CardsRevealedEvent>("CardsRevealed", e => _all.Enqueue(e));
        conn.On<RoundResolvedEvent>("RoundResolved", e => _all.Enqueue(e));
        conn.On<GiftPromptEvent>("GiftPrompt", e => _all.Enqueue(e));
        conn.On<GameFinishedEvent>("GameFinished", e => _all.Enqueue(e));
        conn.On<PlayerStatusEvent>("PlayerDisconnected", e => _all.Enqueue(e));
        conn.On<PlayerStatusEvent>("PlayerReconnected", e => _all.Enqueue(e));
        conn.On<PlayerStatusEvent>("PlayerBotControlled", e => _all.Enqueue(e));
        conn.On<string>("RejectedMessage", e => _rejections.Enqueue(e));
        conn.On<string>("RoomClosed", e => _all.Enqueue(e));
    }

    public async Task WaitAsync<T>() where T : class =>
        await WaitUntilAsync(() => _all.OfType<T>().Any());

    public async Task WaitCountAsync<T>(int count) where T : class =>
        await WaitUntilAsync(() => _all.OfType<T>().Count() >= count);

    public T? Last<T>() where T : class => _all.OfType<T>().LastOrDefault();
    public IReadOnlyList<T> All<T>() where T : class => _all.OfType<T>().ToList();
    public IReadOnlyList<string> Rejections => _rejections.ToList();

    private static async Task WaitUntilAsync(Func<bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!done())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("expected event was not received");
            await Task.Delay(25);
        }
    }
}