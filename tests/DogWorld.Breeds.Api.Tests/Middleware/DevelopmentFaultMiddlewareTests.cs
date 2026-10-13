using DogWorld.ResilienceLab;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogWorld.Breeds.Api.Tests.Middleware;

public class DevelopmentFaultMiddlewareTests
{
    [Theory]
    [InlineData("Production", "Unavailable")]
    [InlineData("Development", "Off")]
    public async Task DoesNotInjectOutsideEnabledDevelopmentScenario(string environment, string scenario)
    {
        var middleware = Create(environment, scenario);
        var context = Context();
        await middleware.InvokeAsync(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("X-Resilience-Lab"));
    }

    [Fact]
    public async Task RecoverFailsFirstTwoAttemptsThenSucceeds()
    {
        var middleware = Create("Development", "Recover");
        foreach (var expected in new[] { 503, 503, 200, 200 })
        {
            var context = Context();
            await middleware.InvokeAsync(context);
            Assert.Equal(expected, context.Response.StatusCode);
        }
    }

    [Fact]
    public async Task UnavailableReturnsMarkedProblemDetails()
    {
        var context = Context();
        await Create("Development", "Unavailable").InvokeAsync(context);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Equal("Unavailable", context.Response.Headers["X-Resilience-Lab"]);
    }

    [Fact]
    public async Task DelayHonorsRequestCancellation()
    {
        var context = Context();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        context.RequestAborted = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create("Development", "Timeout").InvokeAsync(context));
    }

    [Fact]
    public async Task HealthEndpointIsNotAffected()
    {
        var context = Context();
        context.Request.Path = "/health";
        await Create("Development", "Unavailable").InvokeAsync(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private static DevelopmentFaultMiddleware Create(string environment, string scenario) => new(
        _ => Task.CompletedTask,
        new TestEnvironment { EnvironmentName = environment },
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ResilienceLab:Scenario"] = scenario }).Build(),
        NullLogger<DevelopmentFaultMiddleware>.Instance);

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/breeds/beagle";
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        return context;
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
