using Polly;

namespace DogWorld.Api.Resilience;

public static class VaccinationsApiResiliencePolicy
{
    public static ResiliencePipeline<HttpResponseMessage> Create(TimeSpan? retryDelay = null,
        TimeSpan? totalTimeout = null, ILogger? logger = null, TimeProvider? timeProvider = null) =>
        DownstreamResiliencePipeline.Create("Vaccinations", retryDelay, totalTimeout, logger, timeProvider);
}
