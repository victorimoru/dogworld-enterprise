using System.Collections.Concurrent;
using System.Diagnostics;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace DogWorld.Desktop.Tests;

[Collection("Desktop telemetry")]
public class DesktopTracePropagationTests
{
    [Fact]
    public async Task DetailsLoadsPropagateUniqueTracesThroughApiAndDownstream()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var server = builder.Build();
        var apiHeaders = new List<string>();
        var downstreamHeaders = new List<string>();
        using var downstreamHttp = new HttpClient();
        server.MapGet("/downstream", (HttpContext context) =>
        {
            downstreamHeaders.Add(context.Request.Headers["traceparent"].ToString());
            return Results.Ok();
        });
        server.MapGet("/api/dogs/{id:int}", async (HttpContext context, int id) =>
        {
            apiHeaders.Add(context.Request.Headers["traceparent"].ToString());
            using var response = await downstreamHttp.GetAsync(new Uri(new Uri(server.Urls.Single()), "/downstream"));
            response.EnsureSuccessStatusCode();
            return Results.Json(new { id, name = "Bella", breed = "Poodle", ageInMonths = 12, status = "Available" });
        });
        await server.StartAsync(TestContext.Current.CancellationToken);
        var address = new Uri(server.Urls.Single());
        var exporter = new RecordingExporter();
        using var provider = DesktopTelemetry.CreateBuilder(address)
            .AddSource("Microsoft.AspNetCore")
            .AddProcessor(new SimpleActivityExportProcessor(exporter))
            .Build();
        using var http = new HttpClient { BaseAddress = address };
        var client = new TracedDogDetailsClient(new DogDetailsClient(http));

        Assert.NotNull(await client.GetAsync(1, CancellationToken.None));
        Assert.NotNull(await client.GetAsync(2, CancellationToken.None));
        provider.ForceFlush();
        var operations = exporter.Spans.Where(span => span.OperationName == "dog.details.load").ToArray();
        Assert.Equal(2, operations.Length);
        Assert.NotEqual(operations[0].TraceId, operations[1].TraceId);
        for (var index = 0; index < 2; index++)
        {
            Assert.True(ActivityContext.TryParse(apiHeaders[index], null, out var apiParent));
            Assert.True(ActivityContext.TryParse(downstreamHeaders[index], null, out var downstreamParent));
            Assert.Equal(operations[index].TraceId, apiParent.TraceId);
            Assert.Equal(apiParent.TraceId, downstreamParent.TraceId);
            Assert.NotEqual(apiParent.SpanId, downstreamParent.SpanId);
            Assert.Contains(exporter.Spans, span => span.Kind == ActivityKind.Client
                && span.ParentSpanId == operations[index].SpanId && span.SpanId == apiParent.SpanId);
        }
        await server.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void LocalConfigurationRequiresOptInAndConnectionString()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"ApplicationInsights":{"Enabled":true,"ConnectionString":"InstrumentationKey=00000000-0000-0000-0000-000000000001"}}""");
            Assert.True(DesktopTelemetryOptions.Load(path, _ => null).Enabled);
            Assert.False(DesktopTelemetryOptions.Load(path, key => key == "ApplicationInsights__Enabled" ? "false" : null).Enabled);
            File.WriteAllText(path, """{"ApplicationInsights":{"Enabled":true}}""");
            Assert.False(DesktopTelemetryOptions.Load(path, _ => null).Enabled);
            File.WriteAllText(path, "{invalid");
            Assert.False(DesktopTelemetryOptions.Load(path, _ => null).Enabled);
            Assert.Null(DesktopTelemetry.Start(new(), new Uri("http://localhost:5157")));
        }
        finally { File.Delete(path); }
    }

    private sealed class RecordingExporter : BaseExporter<Activity>
    {
        public ConcurrentQueue<Activity> Spans { get; } = new();
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch) Spans.Enqueue(activity);
            return ExportResult.Success;
        }
    }
}

