using Microsoft.Extensions.Logging;
using Moq;
using Services.PriceFetcherService.Infrastructure.Clients;
using Services.PriceFetcherService.Infrastructure.Decorators;
using Services.PriceFetcherService.Infrastructure.Factories;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Xunit;

namespace PriceFetcherService.Tests;

public class PriceFetchingStrategyFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IBinanceFuturesClient> _mockBinanceClient;
    private readonly Mock<ILogger<BinanceFuturesPriceFetchingStrategy>> _mockStrategyLogger;
    private readonly Mock<ILogger<RetryPriceFetchingDecorator>> _mockRetryLogger;
    private readonly Mock<ILogger<RateLimitingPriceFetchingDecorator>> _mockRateLimitingLogger;
    private readonly PriceFetchingStrategyFactory _sut;

    public PriceFetchingStrategyFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockBinanceClient = new Mock<IBinanceFuturesClient>();
        _mockStrategyLogger = new Mock<ILogger<BinanceFuturesPriceFetchingStrategy>>();
        _mockRetryLogger = new Mock<ILogger<RetryPriceFetchingDecorator>>();
        _mockRateLimitingLogger = new Mock<ILogger<RateLimitingPriceFetchingDecorator>>();

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IBinanceFuturesClient)))
            .Returns(_mockBinanceClient.Object);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(BinanceFuturesPriceFetchingStrategy)))
            .Returns(new BinanceFuturesPriceFetchingStrategy(_mockBinanceClient.Object, _mockStrategyLogger.Object));

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.Is<string>(s => s.Contains(nameof(BinanceFuturesPriceFetchingStrategy)))))
            .Returns(_mockStrategyLogger.Object);
            
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.Is<string>(s => s.Contains(nameof(RetryPriceFetchingDecorator)))))
            .Returns(_mockRetryLogger.Object);
            
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.Is<string>(s => s.Contains(nameof(RateLimitingPriceFetchingDecorator)))))
            .Returns(_mockRateLimitingLogger.Object);

        _sut = new PriceFetchingStrategyFactory(_mockServiceProvider.Object, _mockLoggerFactory.Object);
    }

    [Fact]
    public void CreateStrategy_WithBinance_ShouldReturnDecoratedStrategy()
    {
        // Act
        var strategy = _sut.CreateStrategy("binance");

        // Assert
        Assert.NotNull(strategy);
        
        // Verify it's a RetryPriceFetchingDecorator wrapping a RateLimitingPriceFetchingDecorator
        Assert.IsType<RetryPriceFetchingDecorator>(strategy);
        
        // Verify the correct service was requested
        _mockServiceProvider.Verify(x => x.GetService(typeof(IBinanceFuturesClient)), Times.Once);
        _mockLoggerFactory.Verify(x => x.CreateLogger(It.Is<string>(s => s.Contains(nameof(RetryPriceFetchingDecorator)))), Times.Once);
        _mockLoggerFactory.Verify(x => x.CreateLogger(It.Is<string>(s => s.Contains(nameof(RateLimitingPriceFetchingDecorator)))), Times.Once);
    }

    [Fact]
    public void CreateStrategy_WithInvalidExchange_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _sut.CreateStrategy("invalid"));
        Assert.Contains("Unsupported exchange", exception.Message);
    }

    [Fact]
    public async Task CreatedStrategy_ShouldApplyDecorators()
    {
        // Arrange
        var contractType = "CURRENT_QUARTER";
        
        // Setup the client to throw an exception when called
        _mockBinanceClient
            .Setup(x => x.GetFuturesKlinesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NullReferenceException("Test exception"));
        
        // Act
        var strategy = _sut.CreateStrategy("binance");
        
        // Assert - we can test that the strategy works end-to-end
        // This is an integration test of the factory's output
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            strategy.GetKlinesAsync(contractType));
    }
} 