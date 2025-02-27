using Services.PriceFetcherService.Domain.Models;

namespace Services.PriceFetcherService.Infrastructure.Clients;

public interface IBinanceFuturesClient
{
    Task<BinanceKlineData[]> GetFuturesKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default);
} 