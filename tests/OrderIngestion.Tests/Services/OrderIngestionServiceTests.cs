using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Services;
using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Tests.Services;

public class OrderIngestionServiceTests
{
    [Fact]
    public async Task ExtractOrders_WhenApiReturnsOrders_ShouldSaveToFile()
    {
        var mockApiClient = new Mock<IOrderApiClient>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderIngestionService>>();

        var orders = new List<Order>
        {
            new() { ExternalId = 1, UserId = 1, Title = "Test Order" }
        };

        mockApiClient
            .Setup(x => x.FetchOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<Order>>.Ok(orders));

        mockFileStorage
            .Setup(x => x.SaveBatchAsync(It.IsAny<OrderIngestion.Application.Models.OrderBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("/tmp/test.json"));

        var service = new OrderIngestionService(mockApiClient.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.ExtractOrdersAsync();

        result.Should().BeEquivalentTo(Result<string>.Ok("/tmp/test.json"));
        mockFileStorage.Verify(x => x.SaveBatchAsync(It.IsAny<OrderIngestion.Application.Models.OrderBatch>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractOrders_WhenApiFails_ShouldReturnFailure()
    {
        var mockApiClient = new Mock<IOrderApiClient>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderIngestionService>>();

        mockApiClient
            .Setup(x => x.FetchOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<Order>>.Fail("API Error"));

        var service = new OrderIngestionService(mockApiClient.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.ExtractOrdersAsync();

        result.Should().BeEquivalentTo(Result<string>.Fail("Failed to fetch orders: API Error"));
        mockFileStorage.Verify(x => x.SaveBatchAsync(It.IsAny<OrderIngestion.Application.Models.OrderBatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtractOrders_WhenFileStorageFails_ShouldReturnFailure()
    {
        var mockApiClient = new Mock<IOrderApiClient>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderIngestionService>>();

        var orders = new List<Order> { new() { ExternalId = 1 } };

        mockApiClient
            .Setup(x => x.FetchOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<Order>>.Ok(orders));

        mockFileStorage
            .Setup(x => x.SaveBatchAsync(It.IsAny<OrderIngestion.Application.Models.OrderBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Fail("Disk full"));

        var service = new OrderIngestionService(mockApiClient.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.ExtractOrdersAsync();

        result.Should().BeEquivalentTo(Result<string>.Fail("Failed to save batch: Disk full"));
    }
}