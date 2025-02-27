using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Clients;

namespace Services.PriceFetcherService.Infrastructure.Strategies;

/// <summary>
/// Implementation of price fetching strategy for Binance Futures
/// </summary>
public class BinanceFuturesPriceFetchingStrategy : IPriceFetchingStrategy
{
    private readonly IBinanceFuturesClient _client;
    private readonly ILogger<BinanceFuturesPriceFetchingStrategy> _logger;

    public BinanceFuturesPriceFetchingStrategy(
        IBinanceFuturesClient client,
        ILogger<BinanceFuturesPriceFetchingStrategy> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<BinanceKlineData[]> GetKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching klines for contract type {ContractType} from Binance", contractType);
        var klines = await _client.GetFuturesKlinesAsync(contractType, interval, limit, cancellationToken);
        _logger.LogInformation("Retrieved {Count} klines for contract type {ContractType}", klines.Length, contractType);
        return klines;
    }
    
    public async Task<decimal> GetLatestPriceAsync(string contractType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching latest price for contract type {ContractType}", contractType);
        var klines = await GetKlinesAsync(contractType, "1h", 1, cancellationToken);
        var price = klines.Length > 0 ? klines[0].Close : 0;
        _logger.LogInformation("Latest price for {ContractType}: {Price}", contractType, price);
        return price;
    }
}