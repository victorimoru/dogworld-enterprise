using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace DogWorld.Api.Resilience;

internal static class DownstreamResiliencePipeline
{
    public static ResiliencePipeline<HttpResponseMessage> Create(string service, TimeSpan? retryDelay,
        TimeSpan? totalTimeout, ILogger? logger, TimeProvider? timeProvider)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        if (timeProvider is not null) builder.TimeProvider = timeProvider;

        return builder
            // Count final logical outcomes, including timeout, rather than individual retries.
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(IsTransient),
                OnOpened = args =>
                {
                    logger?.LogWarning("Downstream circuit {CircuitState} for {DownstreamService}; break duration {BreakDurationMs} ms",
                        "Open", service, args.BreakDuration.TotalMilliseconds);
                    return default;
                },
                OnHalfOpened = args =>
                {
                    logger?.LogWarning("Downstream circuit {CircuitState} for {DownstreamService}", "HalfOpen", service);
                    return default;
                },
                OnClosed = args =>
                {
                    logger?.LogInformation("Downstream circuit {CircuitState} for {DownstreamService}", "Closed", service);
                    return default;
                }
            })
            .AddTimeout(totalTimeout ?? TimeSpan.FromSeconds(5))
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = retryDelay ?? TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(IsTransient),
                OnRetry = args =>
                {
                    args.Outcome.Result?.Dispose();
                    return default;
                }
            })
            .Build();
    }

    private static bool IsTransient(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode is >= 500 and <= 599;
}
