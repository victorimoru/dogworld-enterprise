using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using DogWorld.Web;
using BlazorApplicationInsights;
using BlazorApplicationInsights.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// WebAssembly cannot read the server's environment variables or user secrets.
using (var settingsClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
    using var settings = await settingsClient.GetAsync("telemetry.local.json");
    if (settings.IsSuccessStatusCode)
        builder.Configuration.AddJsonStream(await settings.Content.ReadAsStreamAsync());
    else if (settings.StatusCode != System.Net.HttpStatusCode.NotFound)
        settings.EnsureSuccessStatusCode();
}
var telemetryConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
var telemetryEnabled = builder.Configuration.GetValue("ApplicationInsights:Enabled", true)
    && !string.IsNullOrWhiteSpace(telemetryConnectionString);
var telemetryStartup = new DogWorld.Web.Telemetry.TelemetryStartup();
builder.Services.AddSingleton(telemetryStartup);
if (!telemetryEnabled) telemetryStartup.Complete();
var webBaseUri = new Uri(builder.HostEnvironment.BaseAddress);
var apiBaseUrl = (webBaseUri.Scheme == Uri.UriSchemeHttps
    ? builder.Configuration["ApiBaseUrlHttps"] : null)
    ?? builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var apiBaseUri = new Uri(apiBaseUrl, UriKind.Absolute);

// Register Blazor Application Insights
builder.Services.AddBlazorApplicationInsights(
    config =>
    {
        config.ConnectionString = telemetryConnectionString;
        config.DisableTelemetry = !telemetryEnabled;
        config.DisableFetchTracking = false;
        config.SamplingPercentage = 100; // Short manual tracing lessons; not a spend cap.
        config.EnableCorsCorrelation = true;
        config.CorrelationHeaderDomains = [apiBaseUri.Authority];
    },
    async applicationInsights =>
    {
        var telemetryItem = new TelemetryItem()
        {
            Tags = new Dictionary<string, object?>()
            {
                { "ai.cloud.role", "DogWorld.Web" },
                { "ai.cloud.roleInstance", "Blazor Wasm" },
            }
        };

        try { await applicationInsights.AddTelemetryInitializer(telemetryItem); }
        catch (Microsoft.JSInterop.JSException)
        {
            Console.Error.WriteLine("Browser telemetry could not initialize. Check the SDK network request or content blockers.");
        }
        finally { telemetryStartup.Complete(); }
    },
    telemetryEnabled,
    options =>
    {
        options.MinLogLevel = LogLevel.Warning;
    }
);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUri });

await builder.Build().RunAsync();
