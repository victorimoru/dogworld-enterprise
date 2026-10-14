using System.Net;
using DogWorld.Api.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace DogWorld.Api.Tests.Resilience;

public class CircuitBreakerRecoveryTests
{
    [Theory]
    [InlineData("breeds", true)]
    [InlineData("vaccinations", true)]
    [InlineData("breeds", false)]
    [InlineData("vaccinations", false)]
    public async Task HalfOpenAllowsOneProbeAndThenClosesOrReopens(string service, bool recover)
    {
        var clock = new Clock();
        var logger = new RecordingLogger();
        var pipeline = Create(service, clock, logger);
        await Open(pipeline);
        await Blocked(pipeline);
        clock.Advance(TimeSpan.FromSeconds(14));
        await Blocked(pipeline);
        clock.Advance(TimeSpan.FromSeconds(2));

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = pipeline.ExecuteAsync(async token =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return new HttpResponseMessage(recover ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        }, CancellationToken.None).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try { await Blocked(pipeline); }
        finally { release.TrySetResult(); }
        using var result = await probe;

        if (recover)
        {
            using var next = await pipeline.ExecuteAsync(_ => ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, next.StatusCode);
            Assert.Equal(new[] { "Open", "HalfOpen", "Closed" }, logger.States);
        }
        else
        {
            await Blocked(pipeline);
            Assert.Equal(new[] { "Open", "HalfOpen", "Open" }, logger.States);
        }
    }

    [Theory]
    [InlineData("breeds")]
    [InlineData("vaccinations")]
    public async Task FailuresOutsideSamplingWindowDoNotCombine(string service)
    {
        var clock = new Clock();
        var pipeline = Create(service, clock, new RecordingLogger());
        for (var i = 0; i < 6; i++)
        {
            using var response = await pipeline.ExecuteAsync(_ => ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
            clock.Advance(TimeSpan.FromSeconds(31));
        }
    }

    [Theory]
    [InlineData("breeds", false)]
    [InlineData("vaccinations", false)]
    [InlineData("breeds", true)]
    [InlineData("vaccinations", true)]
    public async Task NetworkErrorsAndTotalTimeoutsOpenCircuit(string service, bool timeout)
    {
        var pipeline = service == "breeds"
            ? BreedsApiResiliencePolicy.Create(TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
            : VaccinationsApiResiliencePolicy.Create(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        for (var i = 0; i < 4; i++)
        {
            var error = await Record.ExceptionAsync(async () =>
            {
                using var response = await pipeline.ExecuteAsync<HttpResponseMessage>(async token =>
                {
                    if (timeout) await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    throw new HttpRequestException("Network unavailable");
                }, CancellationToken.None);
            });
            if (timeout) Assert.IsType<Polly.Timeout.TimeoutRejectedException>(error);
            else Assert.IsType<HttpRequestException>(error);
        }
        await Blocked(pipeline);
    }

    [Theory]
    [InlineData("breeds")]
    [InlineData("vaccinations")]
    public async Task CallerCancellationsDoNotOpenCircuit(string service)
    {
        var pipeline = Create(service, new Clock(), new RecordingLogger());
        for (var i = 0; i < 4; i++)
        {
            using var cancellation = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                using var response = await pipeline.ExecuteAsync(token =>
                {
                    cancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                    return ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }, cancellation.Token);
            });
        }
        using var result = await pipeline.ExecuteAsync(_ => ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
    private static ResiliencePipeline<HttpResponseMessage> Create(string service, Clock clock, RecordingLogger logger) =>
        service == "breeds" ? BreedsApiResiliencePolicy.Create(TimeSpan.Zero, logger: logger, timeProvider: clock)
                            : VaccinationsApiResiliencePolicy.Create(TimeSpan.Zero, logger: logger, timeProvider: clock);

    private static async Task Open(ResiliencePipeline<HttpResponseMessage> pipeline)
    {
        for (var i = 0; i < 4; i++)
        {
            using var response = await pipeline.ExecuteAsync(_ => ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
        }
    }

    private static async Task Blocked(ResiliencePipeline<HttpResponseMessage> pipeline)
    {
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

    // Circuit transition time advances without real sleeps; no pending timeout is advanced.
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public override long GetTimestamp() => now.UtcTicks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public void Advance(TimeSpan duration) => now += duration;
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> States { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> fields)
                States.Add(fields.Single(field => field.Key == "CircuitState").Value!.ToString()!);
        }
    }
}
