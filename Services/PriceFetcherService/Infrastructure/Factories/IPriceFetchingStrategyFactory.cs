using Services.PriceFetcherService.Infrastructure.Strategies;

namespace Services.PriceFetcherService.Infrastructure.Factories;

/// <summary>
/// Factory interface for creating price fetching strategies
/// </summary>
public interface IPriceFetchingStrategyFactory
{
    /// <summary>
    /// Creates a price fetching strategy for the specified exchange
    /// </summary>
    /// <param name="exchange">The exchange to create strategy for</param>
    /// <returns>A price fetching strategy</returns>
    IPriceFetchingStrategy CreateStrategy(string exchange);
} 