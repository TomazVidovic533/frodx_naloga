using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;

namespace OrderIngestion.MockApi.Middleware;

public class ErrorSimulationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorSimulationMiddleware> _logger;
    private readonly Random _random = new();
    private int _requestCount = 0;

    public ErrorSimulationMiddleware(RequestDelegate next, ILogger<ErrorSimulationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        _requestCount++;

        var enableErrors = configuration.GetRequired<bool>(ConfigKeys.MockApi.EnableRandomErrors);
        var errorRate = configuration.GetRequired<double>(ConfigKeys.MockApi.ErrorRate);
        var minDelay = configuration.GetRequired<int>(ConfigKeys.MockApi.MinDelayMs);
        var maxDelay = configuration.GetRequired<int>(ConfigKeys.MockApi.MaxDelayMs);

        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var delay = _random.Next(minDelay, maxDelay);
        await Task.Delay(delay);

        if (enableErrors && _random.NextDouble() < errorRate)
        {
            var errorStatusCodes = new[]
            {
                (StatusCodes.Status500InternalServerError, "Internal Server Error"),
                (StatusCodes.Status503ServiceUnavailable, "Service Unavailable")
            };

            var (statusCode, errorMessage) = errorStatusCodes[_random.Next(errorStatusCodes.Length)];

            _logger.LogWarning(
                "Simulating {StatusCode} for request #{Count}",
                statusCode, _requestCount);

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                error = errorMessage,
                requestCount = _requestCount,
                statusCode = statusCode
            });
            return;
        }

        _logger.LogInformation(
            "Request #{Count} succeeded (Delay: {Delay}ms)",
            _requestCount, delay);
        await _next(context);
    }
}