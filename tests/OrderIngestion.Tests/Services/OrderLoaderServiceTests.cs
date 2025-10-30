using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderIngestion.Application.Interfaces;
using OrderIngestion.Application.Models;
using OrderIngestion.Application.Services;
using OrderIngestion.Common.Results;
using OrderIngestion.Domain.Interfaces;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Tests.Services;

public class OrderLoaderServiceTests
{
    [Fact]
    public async Task LoadOrders_WhenFileExists_ShouldSaveToDatabase()
    {
        var mockRepository = new Mock<IOrderRepository>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderLoaderService>>();

        var batch = new OrderBatch
        {
            ExtractedAt = DateTime.UtcNow,
            BatchId = "test-123",
            Orders = new List<Order>
            {
                new() { ExternalId = 1, UserId = 1, Title = "Order 1" },
                new() { ExternalId = 2, UserId = 2, Title = "Order 2" }
            }
        };

        mockFileStorage
            .Setup(x => x.LoadBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrderBatch>.Ok(batch));

        mockRepository
            .Setup(x => x.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        mockRepository
            .Setup(x => x.GetTotalOrdersCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        mockFileStorage
            .Setup(x => x.DeleteBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        var service = new OrderLoaderService(mockRepository.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.LoadOrdersAsync("/tmp/test.json");

        result.Value.Should().BeEquivalentTo(new OrderIngestionResult
        {
            IsSuccess = true,
            SavedCount = 2,
            DuplicateCount = 0,
            TotalOrdersInDatabase = 2
        }, options => options.Excluding(x => x.StartedAt).Excluding(x => x.CompletedAt));

        mockRepository.Verify(x => x.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockFileStorage.Verify(x => x.DeleteBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadOrders_WhenFileLoadFails_ShouldReturnFailure()
    {
        var mockRepository = new Mock<IOrderRepository>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderLoaderService>>();

        mockFileStorage
            .Setup(x => x.LoadBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrderBatch>.Fail("File not found"));

        var service = new OrderLoaderService(mockRepository.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.LoadOrdersAsync("/tmp/test.json");

        result.Should().BeEquivalentTo(Result<OrderIngestionResult>.Fail("Failed to load batch: File not found"));
        mockRepository.Verify(x => x.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadOrders_WhenDatabaseSaveFails_ShouldReturnFailure()
    {
        var mockRepository = new Mock<IOrderRepository>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderLoaderService>>();

        var batch = new OrderBatch
        {
            Orders = new List<Order> { new() { ExternalId = 1 } }
        };

        mockFileStorage
            .Setup(x => x.LoadBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrderBatch>.Ok(batch));

        mockRepository
            .Setup(x => x.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail("Database connection failed"));

        var service = new OrderLoaderService(mockRepository.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.LoadOrdersAsync("/tmp/test.json");

        result.Should().BeEquivalentTo(Result<OrderIngestionResult>.Fail("Failed to save order 1: Database connection failed"));
        mockFileStorage.Verify(x => x.DeleteBatchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadOrders_WithDuplicates_ShouldCountCorrectly()
    {
        var mockRepository = new Mock<IOrderRepository>();
        var mockFileStorage = new Mock<IOrderFileStorageService>();
        var mockLogger = new Mock<ILogger<OrderLoaderService>>();

        var batch = new OrderBatch
        {
            Orders = new List<Order>
            {
                new() { ExternalId = 1 },
                new() { ExternalId = 2 },
                new() { ExternalId = 3 }
            }
        };

        mockFileStorage
            .Setup(x => x.LoadBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrderBatch>.Ok(batch));

        mockRepository
            .SetupSequence(x => x.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true))
            .ReturnsAsync(Result<bool>.Ok(false))
            .ReturnsAsync(Result<bool>.Ok(true));

        mockRepository
            .Setup(x => x.GetTotalOrdersCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        mockFileStorage
            .Setup(x => x.DeleteBatchAsync("/tmp/test.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        var service = new OrderLoaderService(mockRepository.Object, mockFileStorage.Object, mockLogger.Object);

        var result = await service.LoadOrdersAsync("/tmp/test.json");

        result.Value.Should().BeEquivalentTo(new OrderIngestionResult
        {
            IsSuccess = true,
            SavedCount = 2,
            DuplicateCount = 1,
            TotalOrdersInDatabase = 5
        }, options => options.Excluding(x => x.StartedAt).Excluding(x => x.CompletedAt));
    }
}