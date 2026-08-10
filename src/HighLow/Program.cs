var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/livez");
app.MapHealthChecks("/readyz");

app.Run();

public partial class Program { }