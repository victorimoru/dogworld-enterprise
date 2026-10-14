using Polly;

namespace DogWorld.Api.Resilience;

public static class BreedsApiResiliencePolicy
{
    public static ResiliencePipeline<HttpResponseMessage> Create(TimeSpan? retryDelay = null,
        TimeSpan? totalTimeout = null, ILogger? logger = null, TimeProvider? timeProvider = null) =>
        DownstreamResiliencePipeline.Create("Breeds", retryDelay, totalTimeout, logger, timeProvider);
}
