using System;
using System.Threading;
using System.Threading.Tasks;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Factories;
using Services.PriceFetcherService.Infrastructure.Strategies;

namespace Services.PriceFetcherService.Application.Services;

public class PriceFetcherManager
{
    private readonly IPriceFetchingStrategy _priceFetchingStrategy;
    private readonly ILogger<PriceFetcherManager> _logger;

    public PriceFetcherManager(
        IPriceFetchingStrategyFactory strategyFactory,
        ILogger<PriceFetcherManager> logger)
    {
        _priceFetchingStrategy = strategyFactory.CreateStrategy("binance");
        _logger = logger;
        _logger.LogInformation("PriceFetcherManager initialized with strategy: {StrategyType}", 
            _priceFetchingStrategy.GetType().Name);
    }

    public async Task<(BinanceKlineData[] Quarter, BinanceKlineData[] BiQuarter)> FetchCurrentPricesAsync(
        CancellationToken cancellationToken = default,
        string interval = "1h",
        int limit = 100)
    {
        _logger.LogInformation("Fetching prices with interval: {Interval}, limit: {Limit}", interval, limit);
        
        var quarterPrice = await _priceFetchingStrategy.GetKlinesAsync("CURRENT_QUARTER", interval, limit, cancellationToken);
        var biQuarterPrice = await _priceFetchingStrategy.GetKlinesAsync("NEXT_QUARTER", interval, limit, cancellationToken);

        _logger.LogInformation("Successfully fetched prices. Quarter data count: {QuarterCount}, BiQuarter data count: {BiQuarterCount}", 
            quarterPrice.Length, biQuarterPrice.Length);
            
        return (quarterPrice, biQuarterPrice);
    }
} 