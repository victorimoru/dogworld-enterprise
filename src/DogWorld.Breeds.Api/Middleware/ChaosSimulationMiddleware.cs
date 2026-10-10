using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DogWorld.Breeds.Api.Middleware;

/// <summary>
/// Chaos simulation middleware for local observability experiments (tail latency, HTTP 5xx failures).
/// Scoped to Development environment and triggered only via explicit request headers or query parameters.
/// </summary>
public class ChaosSimulationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChaosSimulationMiddleware> _logger;
    private readonly Random _random = new();

    public ChaosSimulationMiddleware(RequestDelegate next, ILogger<ChaosSimulationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers;
        var query = context.Request.Query;

        // 1. Check for Forced Status Code Injection
        if (TryGetStatusCode(headers, query, out var statusCode))
        {
            _logger.LogWarning("Chaos Simulation: Injecting forced status code {StatusCode} for path {Path}", statusCode, context.Request.Path);
            Activity.Current?.SetTag("simulated.fault", $"StatusCode_{statusCode}");

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = "Simulated Chaos Fault",
                Detail = $"Request failed due to an injected local chaos simulation status code ({statusCode}).",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        // 2. Check for Probabilistic Failure Injection
        if (TryGetFailureRate(headers, query, out var failureRate))
        {
            if (_random.NextDouble() < failureRate)
            {
                _logger.LogWarning("Chaos Simulation: Probabilistic failure triggered ({FailureRate:P}) for path {Path}", failureRate, context.Request.Path);
                Activity.Current?.SetTag("simulated.fault", $"Probabilistic_{failureRate:P}");

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Simulated Chaos Service Unavailable",
                    Detail = $"Request failed due to probabilistic chaos simulation (Rate: {failureRate:P0}).",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
                return;
            }
        }

        // 3. Check for Artificial Delay Injection
        if (TryGetDelayMs(headers, query, out var delayMs) && delayMs > 0)
        {
            _logger.LogWarning("Chaos Simulation: Injecting artificial delay of {DelayMs}ms for path {Path}", delayMs, context.Request.Path);
            Activity.Current?.SetTag("simulated.delay_ms", delayMs);
            context.Response.Headers["X-Chaos-Simulated-Delay-Ms"] = delayMs.ToString();

            await Task.Delay(delayMs, context.RequestAborted);
        }

        await _next(context);
    }

    private static bool TryGetStatusCode(IHeaderDictionary headers, IQueryCollection query, out int statusCode)
    {
        if (headers.TryGetValue("X-Simulate-StatusCode", out var headerVal) && int.TryParse(headerVal, out statusCode))
        {
            return true;
        }
        if (query.TryGetValue("simulateStatusCode", out var queryVal) && int.TryParse(queryVal, out statusCode))
        {
            return true;
        }
        statusCode = 0;
        return false;
    }

    private static bool TryGetFailureRate(IHeaderDictionary headers, IQueryCollection query, out double failureRate)
    {
        if (headers.TryGetValue("X-Simulate-Failure-Rate", out var headerVal) && double.TryParse(headerVal, out failureRate))
        {
            return true;
        }
        if (query.TryGetValue("simulateFailureRate", out var queryVal) && double.TryParse(queryVal, out failureRate))
        {
            return true;
        }
        failureRate = 0;
        return false;
    }

    private static bool TryGetDelayMs(IHeaderDictionary headers, IQueryCollection query, out int delayMs)
    {
        if (headers.TryGetValue("X-Simulate-Delay-Ms", out var headerVal) && int.TryParse(headerVal, out delayMs))
        {
            return true;
        }
        if (query.TryGetValue("simulateDelayMs", out var queryVal) && int.TryParse(queryVal, out delayMs))
        {
            return true;
        }
        delayMs = 0;
        return false;
    }
}
