using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Result<bool>> SaveOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByExternalIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<int> GetTotalOrdersCountAsync(CancellationToken cancellationToken = default);
    Task<bool> OrderExistsByExternalIdAsync(int externalId, CancellationToken cancellationToken = default);
}