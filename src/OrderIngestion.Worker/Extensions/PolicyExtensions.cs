using Microsoft.Extensions.Configuration;
using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace OrderIngestion.Worker.Extensions;

public static class PolicyExtensions
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration)
    {
        var retryCount = configuration.GetRequired<int>(ConfigKeys.RetryPolicy.RetryCount);

        Log.Information("Configuring retry policy with {RetryCount} retries and exponential backoff", retryCount);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Log.Warning("Retry {RetryCount}/{MaxRetries} after {Delay}s due to {Error}",
                        retryCount, configuration.GetRequired<int>(ConfigKeys.RetryPolicy.RetryCount),
                        timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Log.Error("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    Log.Information("Circuit breaker reset");
                });
    }
}