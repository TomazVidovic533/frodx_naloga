using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Models;
using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;
using OrderIngestion.Common.Results;

namespace OrderIngestion.Application.Services;

public class OrderFileStorageService : IOrderFileStorageService
{
    private readonly string _downloadPath;
    private readonly ILogger<OrderFileStorageService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OrderFileStorageService(
        IConfiguration configuration,
        ILogger<OrderFileStorageService> logger)
    {
        _downloadPath = configuration.GetRequired<string>(ConfigKeys.Ingestion.DownloadPath);
        _logger = logger;

        EnsureDirectoryExists();
    }

    public async Task<Result<string>> SaveBatchAsync(OrderBatch batch, CancellationToken cancellationToken = default)
    {
        try
        {
            var timestamp = batch.ExtractedAt.ToString("yyyyMMddHHmmss");
            var hash = GenerateHash(timestamp);
            var fileName = $"order_batch_{timestamp}_{hash}.json";
            var filePath = Path.Combine(_downloadPath, fileName);

            var json = JsonSerializer.Serialize(batch, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("Saved order batch to {FilePath} with {Count} orders", filePath, batch.Orders.Count);

            return Result<string>.Ok(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch: {Message}", ex.Message);
            return Result<string>.Fail($"Failed to save batch: {ex.Message}");
        }
    }

    public async Task<Result<OrderBatch>> LoadBatchAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var batch = JsonSerializer.Deserialize<OrderBatch>(json, _jsonOptions);

            if (batch == null)
            {
                return Result<OrderBatch>.Fail($"Failed to deserialize batch from {filePath}");
            }

            _logger.LogInformation("Loaded order batch from {FilePath} with {Count} orders", filePath, batch.Orders.Count);

            return Result<OrderBatch>.Ok(batch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading batch from {FilePath}: {Message}", filePath, ex.Message);
            return Result<OrderBatch>.Fail($"Failed to load batch: {ex.Message}");
        }
    }

    public Task<Result<bool>> DeleteBatchAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted batch file {FilePath}", filePath);
            }

            return Task.FromResult(Result<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting batch file {FilePath}: {Message}", filePath, ex.Message);
            return Task.FromResult(Result<bool>.Fail($"Failed to delete batch: {ex.Message}"));
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_downloadPath))
        {
            Directory.CreateDirectory(_downloadPath);
            _logger.LogInformation("Created download directory at {Path}", _downloadPath);
        }
    }

    private static string GenerateHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }
}