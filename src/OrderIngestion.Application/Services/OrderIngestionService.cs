using Microsoft.Extensions.Logging;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Models;
using OrderIngestion.Common.Metrics;
using OrderIngestion.Common.Results;

namespace OrderIngestion.Application.Services;

public class OrderIngestionService
{
    private readonly IOrderApiClient _apiClient;
    private readonly IOrderFileStorageService _fileStorage;
    private readonly ILogger<OrderIngestionService> _logger;

    public OrderIngestionService(
        IOrderApiClient apiClient,
        IOrderFileStorageService fileStorage,
        ILogger<OrderIngestionService> logger)
    {
        _apiClient = apiClient;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<string>> ExtractOrdersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting order extraction from API at {Time}", DateTime.UtcNow);

            var ordersResult = await _apiClient.FetchOrdersAsync(cancellationToken);

            if (!ordersResult.IsSuccess)
            {
                return Result<string>.Fail($"Failed to fetch orders: {ordersResult.Error}");
            }

            var batch = new OrderBatch
            {
                ExtractedAt = DateTime.UtcNow,
                BatchId = Guid.NewGuid().ToString(),
                Orders = ordersResult.Value!.ToList()
            };

            var saveResult = await _fileStorage.SaveBatchAsync(batch, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return Result<string>.Fail($"Failed to save batch: {saveResult.Error}");
            }

            _logger.LogInformation("Extracted {Count} orders to {FilePath}", batch.Orders.Count, saveResult.Value);

            OrderIngestionMetrics.OrdersExtracted.Add(batch.Orders.Count);

            return Result<string>.Ok(saveResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during order extraction: {Message}", ex.Message);
            return Result<string>.Fail($"Extraction failed: {ex.Message}");
        }
    }
}