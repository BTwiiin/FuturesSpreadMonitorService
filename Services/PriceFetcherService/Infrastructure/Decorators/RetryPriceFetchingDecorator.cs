using Services.PriceFetcherService.Common.Exceptions;
using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Strategies;

namespace Services.PriceFetcherService.Infrastructure.Decorators;

/// <summary>
/// Decorator that adds retry capability to any price fetching strategy
/// </summary>
public class RetryPriceFetchingDecorator : IPriceFetchingStrategy
{
    private readonly IPriceFetchingStrategy _inner;
    private readonly ILogger<RetryPriceFetchingDecorator> _logger;
    private readonly int _maxRetries;

    public RetryPriceFetchingDecorator(
        IPriceFetchingStrategy inner,
        ILogger<RetryPriceFetchingDecorator> logger,
        int maxRetries = 3)
    {
        _inner = inner;
        _logger = logger;
        _maxRetries = maxRetries;
    }

    private void OnRetry(Exception exception, int retryCount)
    {
        _logger.LogWarning(
            exception,
            "Failed to fetch data. Retry attempt {RetryCount} after {DelaySeconds} seconds",
            retryCount,
            Math.Pow(2, retryCount));
    }

    public async Task<BinanceKlineData[]> GetKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await _inner.GetKlinesAsync(contractType, interval, limit, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is BinanceApiException)
            {
                retryCount++;
                if (retryCount > _maxRetries)
                {
                    throw;
                }
                
                OnRetry(ex, retryCount);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }
    }
    
    public async Task<decimal> GetLatestPriceAsync(string contractType, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await _inner.GetLatestPriceAsync(contractType, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is BinanceApiException)
            {
                retryCount++;
                if (retryCount > _maxRetries)
                {
                    throw;
                }
                
                OnRetry(ex, retryCount);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }
    }
}