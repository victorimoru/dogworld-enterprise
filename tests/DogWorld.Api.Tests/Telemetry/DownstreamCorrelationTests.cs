using System.Collections.Concurrent;
using System.Diagnostics;
using DogWorld.Api.Clients;
using DogWorld.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DogWorld.Api.Tests.Telemetry;

public class DownstreamCorrelationTests
{
    [Fact]
    public async Task ParallelHttpClientsPropagateOneTraceWithDistinctChildSpans()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "DogWorld.Test" or "System.Net.Http",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("DogWorld.Test");
        var headers = new ConcurrentBag<string>();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.MapGet("/api/breeds/beagle", (HttpContext context) =>
        {
            headers.Add(context.Request.Headers["traceparent"].ToString());
            return Results.Ok(new BreedProfileResponse("beagle", "Beagle", "Hound", "Friendly", "12 years", "10 kg", "England"));
        });
        app.MapGet("/api/dogs/7/vaccinations", (HttpContext context) =>
        {
            headers.Add(context.Request.Headers["traceparent"].ToString());
            return Results.Ok(Array.Empty<VaccinationRecordResponse>());
        });
        await app.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var root = source.StartActivity("dog-details")!;
            await Task.WhenAll(
                new BreedsApiClient(http).GetBreedByIdAsync("beagle"),
                new VaccinationsApiClient(http).GetByDogIdAsync(7));
            Assert.Equal(2, headers.Count);
            var contexts = headers.Select(value =>
            {
                Assert.True(ActivityContext.TryParse(value, null, out var context));
                Assert.Equal(root.TraceId, context.TraceId);
                Assert.NotEqual(root.SpanId, context.SpanId);
                return context;
            }).ToArray();
            Assert.NotEqual(contexts[0].SpanId, contexts[1].SpanId);
        }
        finally { await app.StopAsync(); }
    }
}
