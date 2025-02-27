using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Decorators;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Xunit;

namespace PriceFetcherService.Tests;

public class RateLimitingPriceFetchingDecoratorTests
{
    private readonly Mock<IPriceFetchingStrategy> _mockInner;
    private readonly Mock<ILogger<RateLimitingPriceFetchingDecorator>> _mockLogger;
    private readonly CancellationToken _cancellationToken;

    public RateLimitingPriceFetchingDecoratorTests()
    {
        _mockInner = new Mock<IPriceFetchingStrategy>();
        _mockLogger = new Mock<ILogger<RateLimitingPriceFetchingDecorator>>();
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task GetKlinesAsync_ShouldDelegateToInner()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var interval = "1h";
        var limit = 100;
        var expectedKlines = new[]
        {
            new BinanceKlineData { Close = 50000m }
        };

        _mockInner
            .Setup(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken))
            .ReturnsAsync(expectedKlines);

        using var sut = new RateLimitingPriceFetchingDecorator(_mockInner.Object, _mockLogger.Object);

        // Act
        var result = await sut.GetKlinesAsync(contractType, interval, limit, _cancellationToken);

        // Assert
        Assert.Same(expectedKlines, result);
        _mockInner.Verify(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetLatestPriceAsync_ShouldDelegateToInner()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var expectedPrice = 50000m;

        _mockInner
            .Setup(x => x.GetLatestPriceAsync(contractType, _cancellationToken))
            .ReturnsAsync(expectedPrice);

        using var sut = new RateLimitingPriceFetchingDecorator(_mockInner.Object, _mockLogger.Object);

        // Act
        var result = await sut.GetLatestPriceAsync(contractType, _cancellationToken);

        // Assert
        Assert.Equal(expectedPrice, result);
        _mockInner.Verify(x => x.GetLatestPriceAsync(contractType, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task MultipleRequests_ShouldBeRateLimited()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var expectedPrice = 50000m;
        var maxRequestsPerSecond = 10;
        var requestCount = 20;

        _mockInner
            .Setup(x => x.GetLatestPriceAsync(contractType, _cancellationToken))
            .ReturnsAsync(expectedPrice);

        using var sut = new RateLimitingPriceFetchingDecorator(
            _mockInner.Object, 
            _mockLogger.Object, 
            maxRequestsPerSecond);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = new List<Task<decimal>>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(sut.GetLatestPriceAsync(contractType, _cancellationToken));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var expectedMinimumDuration = TimeSpan.FromMilliseconds(1000.0 * (requestCount - maxRequestsPerSecond) / maxRequestsPerSecond);
        
        // Allow some tolerance for test execution overhead
        Assert.True(
            stopwatch.Elapsed >= expectedMinimumDuration.Subtract(TimeSpan.FromMilliseconds(50)), 
            $"Expected minimum duration: {expectedMinimumDuration}, Actual: {stopwatch.Elapsed}");
        
        _mockInner.Verify(x => x.GetLatestPriceAsync(contractType, _cancellationToken), Times.Exactly(requestCount));
        VerifyLoggerCalled(LogLevel.Information, "Rate limiting applied");
    }

    private void VerifyLoggerCalled(LogLevel level, string contains)
    {
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == level),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(contains)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
} 