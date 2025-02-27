using System;

namespace Services.PriceFetcherService.Common.Exceptions;
public class BinanceApiException : Exception
{
    public BinanceApiException(string message) : base(message)
    {
    }

    public BinanceApiException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
} 