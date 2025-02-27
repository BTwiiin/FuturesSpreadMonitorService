using Services.PriceFetcherService.Common.Exceptions;
using Services.PriceFetcherService.Domain.Models;
using System.Text.Json;
using System.Globalization;

namespace Services.PriceFetcherService.Infrastructure.Clients;

/// <summary>
/// Client for interacting with Binance Futures API endpoints.
/// Handles fetching price data for quarterly and bi-quarterly futures contracts.
/// </summary>
public class BinanceFuturesClient : IBinanceFuturesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinanceFuturesClient> _logger;
    private const string BaseUrl = "https://dapi.binance.com/dapi/v1/";
    private string? _quarterSymbol;
    private string? _biQuarterSymbol;

    /// <summary>
    /// Initializes a new instance of the BinanceFuturesClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for making API requests.</param>
    /// <param name="logger">The logger instance for recording operational data.</param>
    public BinanceFuturesClient(HttpClient httpClient, ILogger<BinanceFuturesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <summary>
    /// Initializes the futures symbols by fetching exchange information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="BinanceApiException">Thrown when unable to fetch or process exchange information.</exception>
    private async Task InitializeSymbolsAsync(CancellationToken cancellationToken)
    {
        if (_quarterSymbol != null && _biQuarterSymbol != null)
            return;

        _logger.LogInformation("Initializing Binance futures symbols");
        
        try
        {
            var exchangeInfo = await _httpClient.GetFromJsonAsync<BinanceExchangeInfo>(
                "exchangeInfo", 
                cancellationToken);
            
            if (exchangeInfo?.Symbols == null)
                throw new BinanceApiException("Failed to get exchange info");

            var quarterSymbol = exchangeInfo.Symbols
                .FirstOrDefault(s => s.ContractType == "CURRENT_QUARTER")?.Symbol;
            var biQuarterSymbol = exchangeInfo.Symbols
                .FirstOrDefault(s => s.ContractType == "NEXT_QUARTER")?.Symbol;

            if (string.IsNullOrEmpty(quarterSymbol) || string.IsNullOrEmpty(biQuarterSymbol))
                throw new BinanceApiException("Failed to find required futures symbols");

            _quarterSymbol = quarterSymbol;
            _biQuarterSymbol = biQuarterSymbol;

            _logger.LogInformation("Successfully initialized futures symbols. Quarter: {QuarterSymbol}, BiQuarter: {BiQuarterSymbol}", 
                quarterSymbol, biQuarterSymbol);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange info from Binance API");
            throw new BinanceApiException("Failed to fetch exchange info", ex);
        }
    }

    /// <summary>
    /// Retrieves kline (candlestick) data for specified futures contract.
    /// </summary>
    /// <param name="contractType">Type of futures contract (CURRENT_QUARTER or NEXT_QUARTER).</param>
    /// <param name="interval">Time interval for klines (default: "1h").</param>
    /// <param name="limit">Maximum number of klines to retrieve (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Array of kline data.</returns>
    /// <exception cref="ArgumentException">Thrown when contract type is invalid.</exception>
    /// <exception cref="BinanceApiException">Thrown when API request fails or response cannot be processed.</exception>
    public async Task<BinanceKlineData[]> GetFuturesKlinesAsync(string contractType, string interval = "1h", int limit = 100, CancellationToken cancellationToken = default)
    {
        await InitializeSymbolsAsync(cancellationToken);
        
        string? symbol = contractType switch
        {
            "CURRENT_QUARTER" => _quarterSymbol,
            "NEXT_QUARTER" => _biQuarterSymbol,
            _ => throw new ArgumentException($"Invalid contract type: {contractType}")
        };

        _logger.LogInformation("Fetching klines for symbol {Symbol}, interval {Interval}, limit {Limit}", 
            symbol, interval, limit);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<JsonElement[][]>(
                $"klines?symbol={symbol}&interval={interval}&limit={limit}",
                cancellationToken);
            
            if (response == null)
                return Array.Empty<BinanceKlineData>();
            
            var result = new List<BinanceKlineData>();
            
            foreach (var kline in response)
            {
                try
                {
                    var klineData = new BinanceKlineData
                    {
                        OpenTime = kline[0].GetInt64(),
                        Open = ParseDecimal(kline[1]),
                        High = ParseDecimal(kline[2]),
                        Low = ParseDecimal(kline[3]),
                        Close = ParseDecimal(kline[4]),
                        Volume = ParseDecimal(kline[5]),
                        CloseTime = kline[6].GetInt64(),
                        QuoteAssetVolume = ParseDecimal(kline[7]),
                        NumberOfTrades = kline[8].GetInt32(),
                        TakerBuyBaseAssetVolume = ParseDecimal(kline[9]),
                        TakerBuyQuoteAssetVolume = ParseDecimal(kline[10])
                    };
                    
                    result.Add(klineData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing individual kline data");
                }
            }
            
            _logger.LogInformation("Successfully retrieved {Count} klines for {Symbol}", result.Count, symbol);
            return result.ToArray();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching klines for {Symbol}", symbol);
            throw new BinanceApiException($"Failed to fetch klines for {symbol}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed while processing klines for {Symbol}", symbol);
            throw new BinanceApiException($"Failed to fetch klines for {symbol}", ex);
        }
    }

    /// <summary>
    /// Parses a JsonElement into a decimal value.
    /// </summary>
    /// <param name="element">The JsonElement to parse.</param>
    /// <returns>The parsed decimal value, or 0 if parsing fails.</returns>
    private static decimal ParseDecimal(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.GetDecimal();
            case JsonValueKind.String:
                var str = element.GetString();
                if (string.IsNullOrEmpty(str))
                    return 0;
                return decimal.Parse(str, CultureInfo.InvariantCulture);
            default:
                return 0;
        }
    }

    /// <summary>
    /// Gets the latest price for a specified futures contract.
    /// </summary>
    /// <param name="contractType">Type of futures contract (CURRENT_QUARTER or NEXT_QUARTER).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The latest price for the specified contract, or 0 if unavailable.</returns>
    public async Task<decimal> GetFuturePriceAsync(string contractType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching latest price for contract type {ContractType}", contractType);
        var klines = await GetFuturesKlinesAsync(contractType, "1h", 1, cancellationToken);
        var price = klines.Length > 0 ? klines[0].Close : 0;
        _logger.LogInformation("Latest price for {ContractType}: {Price}", contractType, price);
        return price;
    }
}