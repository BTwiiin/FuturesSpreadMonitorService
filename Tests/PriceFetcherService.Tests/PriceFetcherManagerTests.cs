using Moq;
using Xunit;
using Services.PriceFetcherService.Application.Services;
using Services.PriceFetcherService.Infrastructure.Factories;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Microsoft.Extensions.Logging;

namespace PriceFetcherService.Tests;

public class PriceFetcherManagerTests
{
    private readonly Mock<IPriceFetchingStrategyFactory> _mockStrategyFactory;
    private readonly Mock<IPriceFetchingStrategy> _mockStrategy;
    private readonly Mock<ILogger<PriceFetcherManager>> _mockLogger;
    private readonly PriceFetcherManager _sut; // System Under Test

    public PriceFetcherManagerTests()
    {
        _mockStrategyFactory = new Mock<IPriceFetchingStrategyFactory>();
        _mockStrategy = new Mock<IPriceFetchingStrategy>();
        _mockLogger = new Mock<ILogger<PriceFetcherManager>>();
        
        // Setup the factory to return our mock strategy
        _mockStrategyFactory
            .Setup(x => x.CreateStrategy("binance"))
            .Returns(_mockStrategy.Object);
            
        _sut = new PriceFetcherManager(_mockStrategyFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task FetchCurrentPricesAsync_ShouldReturnBothPrices()
    {
        // Arrange
        var quarterKlines = new[]
        {
            new BinanceKlineData { Close = 50000m, OpenTime = 1234567890000 }
        };
        var biQuarterKlines = new[]
        {
            new BinanceKlineData { Close = 51000m, OpenTime = 1234567890000 }
        };

        _mockStrategy
            .Setup(x => x.GetKlinesAsync("CURRENT_QUARTER", "1h", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quarterKlines);
        _mockStrategy
            .Setup(x => x.GetKlinesAsync("NEXT_QUARTER", "1h", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(biQuarterKlines);

        // Act
        var result = await _sut.FetchCurrentPricesAsync();

        // Assert
        Assert.NotNull(result.Quarter);
        Assert.NotNull(result.BiQuarter);
        Assert.Single(result.Quarter);
        Assert.Single(result.BiQuarter);
        Assert.Equal(50000m, result.Quarter[0].Close);
        Assert.Equal(51000m, result.BiQuarter[0].Close);
        
        // Verify the factory was used to create the strategy
        _mockStrategyFactory.Verify(x => x.CreateStrategy("binance"), Times.Once);
    }

    [Fact]
    public async Task FetchCurrentPricesAsync_WithCustomInterval_ShouldUseProvidedInterval()
    {
        // Arrange
        var interval = "4h";
        var limit = 50;

        // Act
        await _sut.FetchCurrentPricesAsync(default, interval, limit);

        // Assert
        _mockStrategy.Verify(
            x => x.GetKlinesAsync("CURRENT_QUARTER", interval, limit, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockStrategy.Verify(
            x => x.GetKlinesAsync("NEXT_QUARTER", interval, limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }
} 