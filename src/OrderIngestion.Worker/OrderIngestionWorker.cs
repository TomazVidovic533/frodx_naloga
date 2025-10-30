using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderIngestion.Application.Services;
using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;

namespace OrderIngestion.Worker;

public class OrderIngestionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderIngestionWorker> _logger;
    private readonly IConfiguration _configuration;

    public OrderIngestionWorker(
        IServiceProvider serviceProvider,
        ILogger<OrderIngestionWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetRequired<int>(ConfigKeys.Ingestion.IntervalSeconds);

        _logger.LogInformation("Order Ingestion Worker started. Running every {Seconds} seconds", intervalSeconds);

        await ProcessOrdersAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOrdersAsync(stoppingToken);
        }
    }

    private async Task ProcessOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<OrderIngestionService>();
        var loaderService = scope.ServiceProvider.GetRequiredService<OrderLoaderService>();

        var extractResult = await ingestionService.ExtractOrdersAsync(cancellationToken);

        if (!extractResult.IsSuccess)
        {
            _logger.LogWarning("Extract phase failed: {Error}", extractResult.Error);
            return;
        }

        _logger.LogInformation("Extract phase completed. File: {FilePath}", extractResult.Value);

        var loadResult = await loaderService.LoadOrdersAsync(extractResult.Value!, cancellationToken);

        if (!loadResult.IsSuccess)
        {
            _logger.LogError("Load phase failed: {Error}. File preserved: {FilePath}", loadResult.Error, extractResult.Value);
            return;
        }

        _logger.LogInformation("Load phase completed: {Result}", loadResult.Value!.ToString());
    }
}