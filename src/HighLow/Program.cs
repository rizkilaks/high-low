using HighLow.Game;
using HighLow.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<Random>();
builder.Services.AddSingleton<IBotStrategy, DefaultBotStrategy>();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddHostedService<RoomSweeper>();

var app = builder.Build();

app.MapHealthChecks("/livez");
app.MapHealthChecks("/readyz");
app.MapHub<GameHub>("/hubs/game");
app.MapGet("/metrics", () => Results.Json(new
{
    rooms = RoomManager.CountRooms,
    players = RoomManager.CountPlayers,
    gamesStarted = Metrics.GamesStarted,
    gamesFinished = Metrics.GamesFinished,
    submissions = Metrics.Submissions,
    giftsChosen = Metrics.GiftsChosen,
    reconnects = Metrics.Reconnects,
}));

app.Run();

public partial class Program { }