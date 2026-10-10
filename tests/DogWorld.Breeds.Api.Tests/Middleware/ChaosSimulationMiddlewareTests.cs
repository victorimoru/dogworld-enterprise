using System.Diagnostics;
using System.Net;
using DogWorld.Breeds.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogWorld.Breeds.Api.Tests.Middleware;

public class ChaosSimulationMiddlewareTests
{
    private readonly ILogger<ChaosSimulationMiddleware> _logger = NullLogger<ChaosSimulationMiddleware>.Instance;

    [Fact]
    public async Task InvokeAsync_WithoutChaosHeaders_ExecutesNextMiddlewareNormally()
    {
        // Arrange
        var nextExecuted = false;
        RequestDelegate next = (ctx) =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new ChaosSimulationMiddleware(next, _logger);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextExecuted);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithForcedStatusCodeHeader_ReturnsProblemDetailsAndDoesNotCallNext()
    {
        // Arrange
        var nextExecuted = false;
        RequestDelegate next = (ctx) =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new ChaosSimulationMiddleware(next, _logger);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Simulate-StatusCode"] = "503";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextExecuted);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithSimulatedDelayHeader_AppliesDelayAndAddsHeader()
    {
        // Arrange
        var nextExecuted = false;
        RequestDelegate next = (ctx) =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new ChaosSimulationMiddleware(next, _logger);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Simulate-Delay-Ms"] = "50";

        var stopwatch = Stopwatch.StartNew();

        // Act
        await middleware.InvokeAsync(context);
        stopwatch.Stop();

        // Assert
        Assert.True(nextExecuted);
        Assert.True(stopwatch.ElapsedMilliseconds >= 40, $"Expected delay of ~50ms, got {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal("50", context.Response.Headers["X-Chaos-Simulated-Delay-Ms"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_With100PercentFailureRate_Returns503ServiceUnavailable()
    {
        // Arrange
        var nextExecuted = false;
        RequestDelegate next = (ctx) =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new ChaosSimulationMiddleware(next, _logger);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Simulate-Failure-Rate"] = "1.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextExecuted);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }
}
