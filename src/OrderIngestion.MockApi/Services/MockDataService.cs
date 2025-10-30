using OrderIngestion.MockApi.Models;

namespace OrderIngestion.MockApi.Services;

public class MockDataService
{
    private readonly Random _random = new();
    
    private const int MinExternalId = 1;
    private const int MaxExternalId = 1500;

    private readonly string[] _products = new[]
    {
        "Laptop Computer",
        "Wireless Mouse",
        "Mechanical Keyboard",
        "USB-C Cable",
        "External Hard Drive",
        "Monitor 27 inch",
        "Webcam HD",
        "Headphones",
        "Phone Charger",
        "Tablet Case"
    };

    public List<OrderDto> GenerateOrders(int count = 50)
    {
        var orders = new List<OrderDto>();

        for (int i = 0; i < count; i++)
        {
            orders.Add(new OrderDto
            {
                Id = _random.Next(MinExternalId, MaxExternalId + 1),
                UserId = _random.Next(1, 20),
                Title = $"Order for {_products[_random.Next(_products.Length)]}",
                Completed = _random.Next(0, 100) < 30
            });
        }

        return orders;
    }
}