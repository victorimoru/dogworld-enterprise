using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using DogWorld.Web;
using BlazorApplicationInsights;
using BlazorApplicationInsights.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var webBaseUri = new Uri(builder.HostEnvironment.BaseAddress);
var apiBaseUrl = (webBaseUri.Scheme == Uri.UriSchemeHttps
    ? builder.Configuration["ApiBaseUrlHttps"] : null)
    ?? builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var apiBaseUri = new Uri(apiBaseUrl, UriKind.Absolute);

// Register Blazor Application Insights
builder.Services.AddBlazorApplicationInsights(
    config =>
    {
        config.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
            ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        config.EnableCorsCorrelation = true;
        config.CorrelationHeaderDomains = [apiBaseUri.Authority];
    },
    async applicationInsights =>
    {
        var telemetryItem = new TelemetryItem()
        {
            Tags = new Dictionary<string, object?>()
            {
                { "ai.cloud.role", "DogWorld Web" },
                { "ai.cloud.roleInstance", "Blazor Wasm" },
            }
        };

        await applicationInsights.AddTelemetryInitializer(telemetryItem);
    },
    true,
    options =>
    {
        options.MinLogLevel = LogLevel.Information;
    }
);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUri });

await builder.Build().RunAsync();
