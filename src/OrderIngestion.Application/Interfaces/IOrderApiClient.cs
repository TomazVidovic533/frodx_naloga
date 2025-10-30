using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Application.Interfaces;

public interface IOrderApiClient
{
    Task<Result<IEnumerable<Order>>> FetchOrdersAsync(CancellationToken cancellationToken = default);
}