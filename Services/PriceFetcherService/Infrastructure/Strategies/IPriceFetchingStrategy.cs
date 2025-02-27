using System.Threading;
using System.Threading.Tasks;
using Services.PriceFetcherService.Domain.Models;

namespace Services.PriceFetcherService.Infrastructure.Strategies;

/// <summary>
/// Defines the contract for different price fetching strategies
/// </summary>
public interface IPriceFetchingStrategy
{
    /// <summary>
    /// Gets the kline data for a specified contract type
    /// </summary>
    /// <param name="contractType">The type of contract to fetch data for</param>
    /// <param name="interval">The time interval for klines</param>
    /// <param name="limit">Maximum number of klines to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of kline data</returns>
    Task<BinanceKlineData[]> GetKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest price for a specified contract type
    /// </summary>
    /// <param name="contractType">The type of contract to fetch price for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest price</returns>
    Task<decimal> GetLatestPriceAsync(string contractType, CancellationToken cancellationToken = default);
}