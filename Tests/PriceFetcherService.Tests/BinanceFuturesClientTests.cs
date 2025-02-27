using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Services.PriceFetcherService.Common.Exceptions;
using Services.PriceFetcherService.Infrastructure.Clients;

using Xunit;

namespace PriceFetcherService.Tests;

public class BinanceFuturesClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<BinanceFuturesClient>> _mockLogger;
    private readonly BinanceFuturesClient _sut;

    public BinanceFuturesClientTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockLogger = new Mock<ILogger<BinanceFuturesClient>>();
        _sut = new BinanceFuturesClient(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetFuturesKlinesAsync_WhenSuccessful_ShouldReturnKlineData()
    {
        // Arrange
        var exchangeInfoResponse = new
        {
            symbols = new[]
            {
                new { symbol = "BTCUSD_250328", contractType = "CURRENT_QUARTER" },
                new { symbol = "BTCUSD_250628", contractType = "NEXT_QUARTER" }
            }
        };

        var klinesResponse = new[]
        {
            new object[]
            {
                1234567890000, // OpenTime
                "50000.1", // Open
                "51000.2", // High
                "49000.3", // Low
                "50500.4", // Close
                "100.5", // Volume
                1234567899999, // CloseTime
                "5000000.6", // QuoteAssetVolume
                1000, // NumberOfTrades
                "50.7", // TakerBuyBaseAssetVolume
                "2500.8" // TakerBuyQuoteAssetVolume
            }
        };

        SetupMockResponse("exchangeInfo", exchangeInfoResponse);
        SetupMockResponse("klines?symbol=BTCUSD_250328&interval=1h&limit=100", klinesResponse);

        // Act
        var result = await _sut.GetFuturesKlinesAsync("CURRENT_QUARTER");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(50500.4m, result[0].Close);
        Assert.Equal(1234567890000, result[0].OpenTime);
        
        // Verify logging
        VerifyLoggerCalled(LogLevel.Information, "Initializing Binance futures symbols");
        VerifyLoggerCalled(LogLevel.Information, "Successfully initialized futures symbols");
        VerifyLoggerCalled(LogLevel.Information, "Fetching klines for symbol");
    }

    [Fact]
    public async Task GetFuturesKlinesAsync_WhenExchangeInfoFails_ShouldThrowException()
    {
        // Arrange
        SetupMockResponse("exchangeInfo", statusCode: HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<BinanceApiException>(
            () => _sut.GetFuturesKlinesAsync("CURRENT_QUARTER"));
            
        // Verify error logging
        VerifyLoggerCalled(LogLevel.Error, "Failed to fetch exchange info");
    }

    private void SetupMockResponse(string path, object? content = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = new StringContent(JsonSerializer.Serialize(content));
        }

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
    
    private void VerifyLoggerCalled(LogLevel level, string contains)
    {
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == level),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(contains)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
} 