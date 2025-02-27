namespace Services.PriceFetcherService.Infrastructure.Options;

public class BinanceOptions
{
    public const string SectionName = "Binance";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string QuarterSymbol { get; set; } = "BTCUSDT_QUARTER";
    public string BiQuarterSymbol { get; set; } = "BTCUSDT_BI-QUARTER";
} 