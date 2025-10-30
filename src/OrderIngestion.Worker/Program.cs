using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Services;
using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;
using OrderIngestion.Domain.Interfaces;
using OrderIngestion.Infrastructure.ApiClients;
using OrderIngestion.Infrastructure.Data;
using OrderIngestion.Infrastructure.Repositories;
using OrderIngestion.Worker;
using OrderIngestion.Worker.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = LoggingExtensions.ConfigureLogging(builder.Configuration);

builder.Services.AddSerilog();

var connectionString = builder.Configuration.GetRequired<string>(ConfigKeys.ConnectionStrings.OrderDb);
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<IOrderApiClient, OrderApiClient>(client =>
{
    var baseUrl = builder.Configuration.GetRequired<string>(ConfigKeys.ApiSettings.BaseUrl);
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(PolicyExtensions.GetRetryPolicy(builder.Configuration))
.AddPolicyHandler(PolicyExtensions.GetCircuitBreakerPolicy());

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderFileStorageService, OrderFileStorageService>();
builder.Services.AddScoped<OrderIngestionService>();
builder.Services.AddScoped<OrderLoaderService>();

builder.Services.AddHostedService<OrderIngestionWorker>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("OrderIngestion.Metrics")
        .AddRuntimeInstrumentation()
        .AddPrometheusHttpListener(options => options.UriPrefixes = new string[] { "http://+:8081/" }));

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
    await context.Database.EnsureCreatedAsync();
    Log.Information("Database initialized successfully");
}

try
{
    Log.Information("Starting Order Ingestion Worker with metrics on http://+:8081/metrics");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}