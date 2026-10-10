using System.Net;
using Polly;
using Polly.Retry;

namespace DogWorld.Api.Resilience;

public static class BreedsApiResiliencePolicy
{
    public static ResiliencePipeline<HttpResponseMessage> Create(TimeSpan? retryDelay = null, TimeSpan? totalTimeout = null) =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            // Outer timeout covers all attempts and retry delays.
            .AddTimeout(totalTimeout ?? TimeSpan.FromSeconds(5))
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = retryDelay ?? TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.RequestTimeout
                        || (int)response.StatusCode is >= 500 and <= 599),
                OnRetry = args =>
                {
                    args.Outcome.Result?.Dispose();
                    return default;
                }
            })
            .Build();
}
