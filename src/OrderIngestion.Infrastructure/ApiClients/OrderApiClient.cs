using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Infrastructure.ApiClients;

public class OrderApiClient : IOrderApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderApiClient> _logger;

    public OrderApiClient(HttpClient httpClient, ILogger<OrderApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<Order>>> FetchOrdersAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = _httpClient.BaseAddress;

        try
        {
            _logger.LogInformation("Fetching orders from API: {Url}", requestUri);

            var response = await _httpClient.GetAsync("", cancellationToken);

            _logger.LogDebug("API response: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"API returned {(int)response.StatusCode} {response.StatusCode}";
                _logger.LogError("{ErrorMessage} from {Url}", errorMessage, requestUri);
                return Result<IEnumerable<Order>>.Fail(errorMessage);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var apiOrders = JsonSerializer.Deserialize<List<OrderDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiOrders == null || !apiOrders.Any())
            {
                _logger.LogWarning("No orders received from API at {Url}", requestUri);
                return Result<IEnumerable<Order>>.Ok(Enumerable.Empty<Order>());
            }

            var orders = apiOrders.Select(dto => new Order
            {
                ExternalId = dto.Id,
                UserId = dto.UserId,
                Title = dto.Title ?? string.Empty,
                Completed = dto.Completed
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} orders from {Url}", orders.Count, requestUri);
            return Result<IEnumerable<Order>>.Ok(orders);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout while fetching orders from {Url}", requestUri);
            return Result<IEnumerable<Order>>.Fail($"Request to {requestUri} timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while fetching orders from {Url}", requestUri);
            return Result<IEnumerable<Order>>.Fail($"Failed to connect to {requestUri}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response from {Url}", requestUri);
            return Result<IEnumerable<Order>>.Fail($"Invalid JSON response from {requestUri}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching orders from {Url}", requestUri);
            return Result<IEnumerable<Order>>.Fail($"Unexpected error fetching orders from {requestUri}");
        }
    }

    private class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Title { get; set; }
        public bool Completed { get; set; }
    }
}