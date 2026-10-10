using System.Net;
using DogWorld.Api.Resilience;

namespace DogWorld.Api.Tests.Resilience;

public class BreedsApiResiliencePolicyTests
{
    [Theory]
    [InlineData(408)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task TransientStatusGetsAtMostTwoRetries(int status)
    {
        var pipeline = BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero, totalTimeout: TimeSpan.FromSeconds(5));
        var calls = 0;
        using var result = await pipeline.ExecuteAsync(_ =>
        {
            calls++;
            return ValueTask.FromResult(new HttpResponseMessage((HttpStatusCode)status));
        }, CancellationToken.None);
        Assert.Equal(3, calls);
        Assert.Equal((HttpStatusCode)status, result.StatusCode);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    public async Task SuccessAndNonTransientClientErrorsAreNotRetried(int status)
    {
        var pipeline = BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero, totalTimeout: TimeSpan.FromSeconds(5));
        var calls = 0;
        using var result = await pipeline.ExecuteAsync(_ =>
        {
            calls++;
            return ValueTask.FromResult(new HttpResponseMessage((HttpStatusCode)status));
        }, CancellationToken.None);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task NetworkFailureCanRecoverOnRetry()
    {
        var pipeline = BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero, totalTimeout: TimeSpan.FromSeconds(5));
        var calls = 0;
        using var result = await pipeline.ExecuteAsync(_ => ++calls == 1
            ? ValueTask.FromException<HttpResponseMessage>(new HttpRequestException("Network unavailable"))
            : ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), CancellationToken.None);
        Assert.Equal(2, calls);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task TotalTimeoutCancelsPendingCall()
    {
        var pipeline = BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero, totalTimeout: TimeSpan.FromMilliseconds(100));
        var calls = 0;
        var cancelled = false;
        var error = await Record.ExceptionAsync(async () =>
        {
            using var result = await pipeline.ExecuteAsync(async token =>
            {
                calls++;
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                catch (OperationCanceledException) { cancelled = true; throw; }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }, CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        });
        Assert.NotNull(error);
        Assert.Equal("Polly.Timeout.TimeoutRejectedException", error.GetType().FullName);
        Assert.True(cancelled);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CallerCancellationStopsWithoutRetry()
    {
        var pipeline = BreedsApiResiliencePolicy.Create(retryDelay: TimeSpan.Zero, totalTimeout: TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            using var result = await pipeline.ExecuteAsync(token =>
            {
                calls++;
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, cancellation.Token);
        });
        Assert.Equal(1, calls);
    }
}
