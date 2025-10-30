using OrderIngestion.Application.Models;
using OrderIngestion.Common.Results;

namespace OrderIngestion.Application.Interfaces;

public interface IOrderFileStorageService
{
    Task<Result<string>> SaveBatchAsync(OrderBatch batch, CancellationToken cancellationToken = default);
    Task<Result<OrderBatch>> LoadBatchAsync(string filePath, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteBatchAsync(string filePath, CancellationToken cancellationToken = default);
}