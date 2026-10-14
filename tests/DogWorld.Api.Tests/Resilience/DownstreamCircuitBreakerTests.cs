using System.Net;
using DogWorld.Api.Resilience;
using Polly;
using Polly.CircuitBreaker;

namespace DogWorld.Api.Tests.Resilience;

public class DownstreamCircuitBreakerTests
{
    [Theory]
    [InlineData("breeds", 408)]
    [InlineData("breeds", 503)]
    [InlineData("vaccinations", 408)]
    [InlineData("vaccinations", 503)]
    public async Task FourFailedLogicalCallsOpenCircuitAndNextCallDoesNotReachDownstream(string service, int status)
    {
        var pipeline = Create(service);
        var attempts = 0;
        ValueTask<HttpResponseMessage> Send(CancellationToken _) =>
            ValueTask.FromResult(Response((HttpStatusCode)status));
        HttpResponseMessage Response(HttpStatusCode code)
        {
            attempts++;
            return new HttpResponseMessage(code);
        }

        for (var call = 0; call < 4; call++)
        {
            using var response = await pipeline.ExecuteAsync(Send, CancellationToken.None);
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
        }
        Assert.Equal(12, attempts); // Each logical call retains its original + two retries.

        await Assert.ThrowsAnyAsync<BrokenCircuitException>(async () =>
        {
            using var response = await pipeline.ExecuteAsync(Send, CancellationToken.None);
        });
        Assert.Equal(12, attempts);
    }

    [Theory]
    [InlineData("breeds")]
    [InlineData("vaccinations")]
    public async Task TwoFailuresAndTwoSuccessesReachFiftyPercentThreshold(string service)
    {
        var pipeline = Create(service);
        foreach (var status in new[] { 200, 200, 503, 503 })
        {
            using var response = await pipeline.ExecuteAsync(_ =>
                ValueTask.FromResult(new HttpResponseMessage((HttpStatusCode)status)), CancellationToken.None);
        }
        var called = false;
        await Assert.ThrowsAnyAsync<BrokenCircuitException>(async () =>
        {
            using var response = await pipeline.ExecuteAsync(_ =>
            {
                called = true;
                return ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, CancellationToken.None);
        });
        Assert.False(called);
    }

    [Theory]
    [InlineData("breeds")]
    [InlineData("vaccinations")]
    public async Task RecoveringRetriesDoNotCountAsFailedLogicalCalls(string service)
    {
        var pipeline = Create(service);
        var attempts = 0;
        for (var call = 0; call < 5; call++)
        {
            using var response = await pipeline.ExecuteAsync(_ =>
                ValueTask.FromResult(new HttpResponseMessage(++attempts % 3 == 0
                    ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.Equal(15, attempts);
    }

    [Theory]
    [InlineData("breeds")]
    [InlineData("vaccinations")]
    public async Task NotFoundDoesNotOpenCircuit(string service)
    {
        var pipeline = Create(service);
        for (var call = 0; call < 5; call++)
        {
            using var response = await pipeline.ExecuteAsync(_ =>
                ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)), CancellationToken.None);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("breeds", "vaccinations")]
    [InlineData("vaccinations", "breeds")]
    public async Task OpeningOneCircuitLeavesOtherServiceAvailable(string failing, string healthy)
    {
        var failingPipeline = Create(failing);
        var healthyPipeline = Create(healthy);
        for (var call = 0; call < 4; call++)
        {
            using var response = await failingPipeline.ExecuteAsync(_ =>
                ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
        }
        await Assert.ThrowsAnyAsync<BrokenCircuitException>(async () =>
        {
            using var response = await failingPipeline.ExecuteAsync(_ =>
                ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), CancellationToken.None);
        });
        using var healthyResponse = await healthyPipeline.ExecuteAsync(_ =>
            ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, healthyResponse.StatusCode);
    }

    private static ResiliencePipeline<HttpResponseMessage> Create(string service) => service == "breeds"
        ? BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero)
        : VaccinationsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero);
}
