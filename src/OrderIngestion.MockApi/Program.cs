using OrderIngestion.MockApi.Middleware;
using OrderIngestion.MockApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<MockDataService>();

var app = builder.Build();

app.UseMiddleware<ErrorSimulationMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
