using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Services.PriceFetcherService.Common.Exceptions;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Decorators;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Xunit;

namespace PriceFetcherService.Tests;

public class RetryPriceFetchingDecoratorTests
{
    private readonly Mock<IPriceFetchingStrategy> _mockInner;
    private readonly Mock<ILogger<RetryPriceFetchingDecorator>> _mockLogger;
    private readonly RetryPriceFetchingDecorator _sut;
    private readonly CancellationToken _cancellationToken;

    public RetryPriceFetchingDecoratorTests()
    {
        _mockInner = new Mock<IPriceFetchingStrategy>();
        _mockLogger = new Mock<ILogger<RetryPriceFetchingDecorator>>();
        _sut = new RetryPriceFetchingDecorator(_mockInner.Object, _mockLogger.Object, maxRetries: 2);
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task GetKlinesAsync_WhenSuccessful_ShouldReturnData()
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

        // Act
        var result = await _sut.GetKlinesAsync(contractType, interval, limit, _cancellationToken);

        // Assert
        Assert.Same(expectedKlines, result);
        _mockInner.Verify(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetKlinesAsync_WhenFailsOnceAndThenSucceeds_ShouldRetry()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var interval = "1h";
        var limit = 100;
        var expectedKlines = new[]
        {
            new BinanceKlineData { Close = 50000m }
        };

        var callCount = 0;
        _mockInner
            .Setup(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new HttpRequestException("API error");
                }
                return Task.FromResult(expectedKlines);
            });

        // Act
        var result = await _sut.GetKlinesAsync(contractType, interval, limit, _cancellationToken);

        // Assert
        Assert.Same(expectedKlines, result);
        _mockInner.Verify(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken), Times.Exactly(2));
        VerifyLoggerCalled(LogLevel.Warning, "Failed to fetch data. Retry attempt 1");
    }

    [Fact]
    public async Task GetKlinesAsync_WhenFailsRepeatedly_ShouldThrowAfterMaxRetries()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var interval = "1h";
        var limit = 100;

        _mockInner
            .Setup(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken))
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.GetKlinesAsync(contractType, interval, limit, _cancellationToken));
        
        Assert.Equal("API error", exception.Message);
        _mockInner.Verify(x => x.GetKlinesAsync(contractType, interval, limit, _cancellationToken), Times.Exactly(3)); // Initial + 2 retries
        VerifyLoggerCalled(LogLevel.Warning, "Failed to fetch data. Retry attempt 1");
        VerifyLoggerCalled(LogLevel.Warning, "Failed to fetch data. Retry attempt 2");
    }

    [Fact]
    public async Task GetLatestPriceAsync_WhenSuccessful_ShouldReturnPrice()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var expectedPrice = 50000m;

        _mockInner
            .Setup(x => x.GetLatestPriceAsync(contractType, _cancellationToken))
            .ReturnsAsync(expectedPrice);

        // Act
        var result = await _sut.GetLatestPriceAsync(contractType, _cancellationToken);

        // Assert
        Assert.Equal(expectedPrice, result);
        _mockInner.Verify(x => x.GetLatestPriceAsync(contractType, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetLatestPriceAsync_WhenFailsOnceAndThenSucceeds_ShouldRetry()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var expectedPrice = 50000m;

        var callCount = 0;
        _mockInner
            .Setup(x => x.GetLatestPriceAsync(contractType, _cancellationToken))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new BinanceApiException("API error");
                }
                return Task.FromResult(expectedPrice);
            });

        // Act
        var result = await _sut.GetLatestPriceAsync(contractType, _cancellationToken);

        // Assert
        Assert.Equal(expectedPrice, result);
        _mockInner.Verify(x => x.GetLatestPriceAsync(contractType, _cancellationToken), Times.Exactly(2));
        VerifyLoggerCalled(LogLevel.Warning, "Failed to fetch data. Retry attempt 1");
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