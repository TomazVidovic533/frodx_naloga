using Microsoft.Extensions.Logging;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Models;
using OrderIngestion.Common.Metrics;
using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Interfaces;

namespace OrderIngestion.Application.Services;

public class OrderLoaderService
{
    private readonly IOrderRepository _repository;
    private readonly IOrderFileStorageService _fileStorage;
    private readonly ILogger<OrderLoaderService> _logger;

    public OrderLoaderService(
        IOrderRepository repository,
        IOrderFileStorageService fileStorage,
        ILogger<OrderLoaderService> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<OrderIngestionResult>> LoadOrdersAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting order loading from {FilePath}", filePath);

            var batchResult = await _fileStorage.LoadBatchAsync(filePath, cancellationToken);

            if (!batchResult.IsSuccess)
            {
                return Result<OrderIngestionResult>.Fail($"Failed to load batch: {batchResult.Error}");
            }

            var batch = batchResult.Value!;
            var savedCount = 0;
            var duplicateCount = 0;

            foreach (var order in batch.Orders)
            {
                var saveResult = await _repository.SaveOrderAsync(order, cancellationToken);

                if (!saveResult.IsSuccess)
                {
                    _logger.LogError("Failed to save order {ExternalId}: {Error}", order.ExternalId, saveResult.Error);
                    return Result<OrderIngestionResult>.Fail($"Failed to save order {order.ExternalId}: {saveResult.Error}");
                }

                if (saveResult.Value!)
                    savedCount++;
                else
                    duplicateCount++;
            }

            var totalOrders = await _repository.GetTotalOrdersCountAsync(cancellationToken);

            var result = new OrderIngestionResult
            {
                StartedAt = DateTime.UtcNow,
                SavedCount = savedCount,
                DuplicateCount = duplicateCount,
                TotalOrdersInDatabase = totalOrders,
                CompletedAt = DateTime.UtcNow,
                IsSuccess = true
            };

            var deleteResult = await _fileStorage.DeleteBatchAsync(filePath, cancellationToken);

            if (!deleteResult.IsSuccess)
            {
                _logger.LogWarning("Failed to delete batch file {FilePath}: {Error}", filePath, deleteResult.Error);
            }

            _logger.LogInformation(
                "Order loading completed. Saved: {Saved}, Duplicates: {Duplicates}, Total in DB: {Total}",
                savedCount, duplicateCount, totalOrders);

            OrderIngestionMetrics.OrdersSaved.Add(savedCount);
            OrderIngestionMetrics.OrdersDuplicate.Add(duplicateCount);

            return Result<OrderIngestionResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during order loading: {Message}", ex.Message);
            return Result<OrderIngestionResult>.Fail($"Loading failed: {ex.Message}");
        }
    }
}