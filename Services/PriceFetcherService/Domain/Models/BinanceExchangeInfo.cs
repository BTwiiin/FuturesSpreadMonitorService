namespace Services.PriceFetcherService.Domain.Models;

public class BinanceExchangeInfo
{
    public List<SymbolInfo> Symbols { get; set; } = new();

    public class SymbolInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
    }
} 