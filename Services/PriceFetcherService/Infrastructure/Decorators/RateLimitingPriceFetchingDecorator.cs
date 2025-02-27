using Services.PriceFetcherService.Domain.Models;
using Services.PriceFetcherService.Infrastructure.Strategies;

namespace Services.PriceFetcherService.Infrastructure.Decorators;

/// <summary>
/// Decorator that adds rate limiting capability to any price fetching strategy
/// </summary>
public class RateLimitingPriceFetchingDecorator : IPriceFetchingStrategy, IDisposable
{
    private readonly IPriceFetchingStrategy _inner;
    private readonly ILogger<RateLimitingPriceFetchingDecorator> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _interval;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public RateLimitingPriceFetchingDecorator(
        IPriceFetchingStrategy inner,
        ILogger<RateLimitingPriceFetchingDecorator> logger,
        int maxRequestsPerSecond = 5)
    {
        _inner = inner;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        _interval = TimeSpan.FromMilliseconds(1000.0 / maxRequestsPerSecond);
    }

    public async Task<BinanceKlineData[]> GetKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);
        return await _inner.GetKlinesAsync(contractType, interval, limit, cancellationToken);
    }
    
    public async Task<decimal> GetLatestPriceAsync(string contractType, CancellationToken cancellationToken = default)
    {
        await EnforceRateLimitAsync(cancellationToken);
        return await _inner.GetLatestPriceAsync(contractType, cancellationToken);
    }
    
    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            
            if (timeSinceLastRequest < _interval)
            {
                var delayTime = _interval - timeSinceLastRequest;
                _logger.LogInformation("Rate limiting applied. Delaying request for {DelayMs}ms", delayTime.TotalMilliseconds);
                await Task.Delay(delayTime, cancellationToken);
            }
            
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Dispose()
    {
        _semaphore.Dispose();
    }
} 