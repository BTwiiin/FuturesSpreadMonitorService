namespace Services.PriceFetcherService.Domain.Models;

public class FuturesPrice
{
    public string? Symbol { get; set; }
    public decimal? Price { get; set; }
    public DateTime? Timestamp { get; set; }
}