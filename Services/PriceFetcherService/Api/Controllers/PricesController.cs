using Microsoft.AspNetCore.Mvc;
using Services.PriceFetcherService.Application.Services;

namespace Services.PriceFetcherService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricesController : ControllerBase
    {
        private readonly PriceFetcherManager _priceFetcherService;

        public PricesController(PriceFetcherManager priceFetcherService)
        {
            _priceFetcherService = priceFetcherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentPrices(CancellationToken cancellationToken)
        {
            var prices = await _priceFetcherService.FetchCurrentPricesAsync(cancellationToken);
            
            return Ok(new
            {
                CurrentPrices = new 
                { 
                    Quarter = prices.Quarter.FirstOrDefault()?.Close ?? 0,
                    BiQuarter = prices.BiQuarter.FirstOrDefault()?.Close ?? 0
                },
                QuarterKlines = prices.Quarter,
                BiQuarterKlines = prices.BiQuarter
            });
        }

        [HttpGet("klines")]
        public async Task<IActionResult> GetKlines(
            [FromQuery] string interval = "1h",
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var prices = await _priceFetcherService.FetchCurrentPricesAsync(cancellationToken, interval, limit);
            
            return Ok(new
            {
                Quarter = prices.Quarter,
                BiQuarter = prices.BiQuarter
            });
        }
    }
} 