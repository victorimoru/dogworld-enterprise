using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
namespace DogWorld.Desktop.Telemetry;
public static class DesktopTelemetry
{
    public static TracerProviderBuilder CreateBuilder(Uri apiAddress) =>
        Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(DesktopOperations.SourceName))
            .AddSource(DesktopOperations.SourceName)
            .AddHttpClientInstrumentation(options =>
                options.FilterHttpRequestMessage = request =>
                    request.RequestUri is { } uri && uri.Scheme == apiAddress.Scheme
                    && uri.Host == apiAddress.Host && uri.Port == apiAddress.Port);

    public static TracerProvider? Start(DesktopTelemetryOptions options, Uri apiAddress)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
            return null;
        return CreateBuilder(apiAddress)
            .AddAzureMonitorTraceExporter(exporter =>
            {
                exporter.ConnectionString = options.ConnectionString;
                exporter.DisableOfflineStorage = true;
            })
            .Build();
    }
}
