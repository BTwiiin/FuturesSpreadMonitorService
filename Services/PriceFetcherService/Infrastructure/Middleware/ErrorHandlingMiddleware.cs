using System.Net;
using System.Text.Json;
using Services.PriceFetcherService.Common.Exceptions;

namespace Services.PriceFetcherService.Infrastructure.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unexpected error occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            BinanceApiException => (HttpStatusCode.BadGateway, "Error communicating with Binance API"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new { message });
        await response.WriteAsync(result);
    }
} 