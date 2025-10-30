using Microsoft.AspNetCore.Mvc;
using OrderIngestion.MockApi.Services;

namespace OrderIngestion.MockApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MockDataService _mockDataService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(MockDataService mockDataService, ILogger<OrdersController> logger)
    {
        _mockDataService = mockDataService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetOrders([FromQuery] int count = 50)
    {
        _logger.LogInformation("Generating {Count} mock orders", count);
        return Ok(_mockDataService.GenerateOrders(count));
    }
}