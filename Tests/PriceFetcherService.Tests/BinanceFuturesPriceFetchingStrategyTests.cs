using Microsoft.Extensions.Logging;
using Moq;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Clients;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Xunit;

namespace PriceFetcherService.Tests;

public class BinanceFuturesPriceFetchingStrategyTests
{
    private readonly Mock<IBinanceFuturesClient> _mockClient;
    private readonly Mock<ILogger<BinanceFuturesPriceFetchingStrategy>> _mockLogger;
    private readonly BinanceFuturesPriceFetchingStrategy _sut;

    public BinanceFuturesPriceFetchingStrategyTests()
    {
        _mockClient = new Mock<IBinanceFuturesClient>();
        _mockLogger = new Mock<ILogger<BinanceFuturesPriceFetchingStrategy>>();
        _sut = new BinanceFuturesPriceFetchingStrategy(_mockClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetKlinesAsync_ShouldCallClientAndReturnData()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        var interval = "2h";
        var limit = 50;
        var expectedKlines = new[]
        {
            new BinanceKlineData { Close = 50000m, OpenTime = 1234567890000 }
        };

        _mockClient
            .Setup(x => x.GetFuturesKlinesAsync(contractType, interval, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedKlines);

        // Act
        var result = await _sut.GetKlinesAsync(contractType, interval, limit);

        // Assert
        Assert.Same(expectedKlines, result);
        _mockClient.Verify(x => x.GetFuturesKlinesAsync(contractType, interval, limit, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLoggerCalled(LogLevel.Information, $"Fetching klines for contract type {contractType}");
        VerifyLoggerCalled(LogLevel.Information, $"Retrieved {expectedKlines.Length} klines");
    }

    [Fact]
    public async Task GetLatestPriceAsync_ShouldReturnClosePrice()
    {
        // Arrange
        var contractType = "NEXT_QUARTER";
        var expectedKlines = new[]
        {
            new BinanceKlineData { Close = 51000m, OpenTime = 1234567890000 }
        };

        _mockClient
            .Setup(x => x.GetFuturesKlinesAsync(contractType, "1h", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedKlines);

        // Act
        var result = await _sut.GetLatestPriceAsync(contractType);

        // Assert
        Assert.Equal(51000m, result);
        _mockClient.Verify(x => x.GetFuturesKlinesAsync(contractType, "1h", 1, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLoggerCalled(LogLevel.Information, $"Fetching latest price for contract type {contractType}");
        VerifyLoggerCalled(LogLevel.Information, $"Latest price for {contractType}: {expectedKlines[0].Close}");
    }

    [Fact]
    public async Task GetLatestPriceAsync_WhenNoData_ShouldReturnZero()
    {
        // Arrange
        var contractType = "NEXT_QUARTER";
        var emptyKlines = Array.Empty<BinanceKlineData>();

        _mockClient
            .Setup(x => x.GetFuturesKlinesAsync(contractType, "1h", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyKlines);

        // Act
        var result = await _sut.GetLatestPriceAsync(contractType);

        // Assert
        Assert.Equal(0m, result);
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