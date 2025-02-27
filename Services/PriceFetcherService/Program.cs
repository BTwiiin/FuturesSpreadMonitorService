using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Services.PriceFetcherService.Application.Services;
using Services.PriceFetcherService.Infrastructure.Clients;
using Services.PriceFetcherService.Infrastructure.Factories;
using Services.PriceFetcherService.Infrastructure.Middleware;
using Services.PriceFetcherService.Infrastructure.Options;
using Services.PriceFetcherService.Infrastructure.Strategies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Binance options
builder.Services.Configure<BinanceOptions>(
    builder.Configuration.GetSection(BinanceOptions.SectionName));

// Configure HttpClient for Binance
builder.Services.AddHttpClient<IBinanceFuturesClient, BinanceFuturesClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BinanceOptions>>();
    client.BaseAddress = new Uri(options.Value.BaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register strategies and decorators
builder.Services.AddScoped<BinanceFuturesPriceFetchingStrategy>();

// Configure PriceFetchingStrategyFactory
builder.Services.AddSingleton<IPriceFetchingStrategyFactory, PriceFetchingStrategyFactory>();

// Register application services
builder.Services.AddScoped<PriceFetcherManager>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("BinanceAPI", () => HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();
