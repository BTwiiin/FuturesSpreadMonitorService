using Services.PriceFetcherService.Infrastructure.Decorators;
using Services.PriceFetcherService.Infrastructure.Strategies;
using Services.PriceFetcherService.Infrastructure.Clients;

namespace Services.PriceFetcherService.Infrastructure.Factories;

/// <summary>
/// Factory for creating price fetching strategies with appropriate decorators
/// </summary>
public class PriceFetchingStrategyFactory : IPriceFetchingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    private readonly ILoggerFactory _loggerFactory;

    public PriceFetchingStrategyFactory(
        IServiceProvider serviceProvider,

        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;

        _loggerFactory = loggerFactory;
    }

    public IPriceFetchingStrategy CreateStrategy(string exchange)
    {
        // Create the base strategy
        IPriceFetchingStrategy strategy = exchange.ToLower() switch
        {
            "binance" => new BinanceFuturesPriceFetchingStrategy(
                _serviceProvider.GetRequiredService<IBinanceFuturesClient>(),
                _loggerFactory.CreateLogger<BinanceFuturesPriceFetchingStrategy>()),
            _ => throw new ArgumentException($"Unsupported exchange: {exchange}")
        };

        // Add rate limiting (innermost decorator)
        strategy = new RateLimitingPriceFetchingDecorator(
            strategy,
            _loggerFactory.CreateLogger<RateLimitingPriceFetchingDecorator>()
        );

        // Add retry logic
        strategy = new RetryPriceFetchingDecorator(
            strategy,
            _loggerFactory.CreateLogger<RetryPriceFetchingDecorator>()
        );

        return strategy;
    }
} 