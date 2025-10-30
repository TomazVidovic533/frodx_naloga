using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Interfaces;
using OrderIngestion.Domain.Models;
using OrderIngestion.Infrastructure.Data;

namespace OrderIngestion.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(OrderContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await OrderExistsByExternalIdAsync(order.ExternalId, cancellationToken))
            {
                _logger.LogDebug("Order with ExternalId {ExternalId} already exists. Skipping.", order.ExternalId);
                return Result<bool>.Ok(false);
            }

            await _context.Orders.AddAsync(order, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully saved order with ExternalId {ExternalId}", order.ExternalId);
            return Result<bool>.Ok(true);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while saving order with ExternalId {ExternalId}", order.ExternalId);
            return Result<bool>.Fail($"Failed to save order {order.ExternalId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving order with ExternalId {ExternalId}", order.ExternalId);
            return Result<bool>.Fail($"Unexpected database error saving order {order.ExternalId}: {ex.Message}");
        }
    }

    public async Task<Order?> GetByExternalIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.ExternalId == externalId, cancellationToken);
    }

    public async Task<int> GetTotalOrdersCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders.CountAsync(cancellationToken);
    }

    public async Task<bool> OrderExistsByExternalIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AnyAsync(o => o.ExternalId == externalId, cancellationToken);
    }
}