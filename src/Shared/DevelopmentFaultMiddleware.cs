using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DogWorld.ResilienceLab;

// Compiled into each downstream API. No shared runtime state between services.
public sealed class DevelopmentFaultMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<DevelopmentFaultMiddleware> logger;
    private readonly string scenario;
    private int attempts;

    public DevelopmentFaultMiddleware(RequestDelegate next, IHostEnvironment environment,
        IConfiguration configuration, ILogger<DevelopmentFaultMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
        scenario = environment.IsDevelopment() ? configuration["ResilienceLab:Scenario"] ?? "Off" : "Off";
        if (scenario is not ("Off" or "Unavailable" or "Timeout" or "Recover"))
            throw new InvalidOperationException("ResilienceLab:Scenario must be Off, Unavailable, Timeout, or Recover.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (scenario == "Off" || !HttpMethods.IsGet(context.Request.Method)
            || !context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var attempt = Interlocked.Increment(ref attempts);
        Activity.Current?.SetTag("resilience.lab.scenario", scenario);
        Activity.Current?.SetTag("resilience.lab.attempt", attempt);
        context.Response.Headers["X-Resilience-Lab"] = scenario;
        logger.LogWarning("Resilience lab {Scenario}, service attempt {Attempt}, path {Path}", scenario, attempt, context.Request.Path);

        if (scenario == "Timeout")
            await Task.Delay(TimeSpan.FromSeconds(7), context.RequestAborted);

        if (scenario == "Unavailable" || (scenario == "Recover" && attempt <= 2))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 503,
                title = "Simulated downstream unavailability",
                detail = "Development resilience lab; no database operation was performed."
            }, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
            return;
        }
        await next(context);
    }
}
