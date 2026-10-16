using System.Text.Json;
namespace DogWorld.Desktop.Telemetry;
public sealed record DesktopTelemetryOptions(bool Enabled = false, string? ConnectionString = null)
{
    public static DesktopTelemetryOptions Load(string path, Func<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariable;
        var options = new DesktopTelemetryOptions();
        try
        {
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("ApplicationInsights", out var section))
                    options = section.Deserialize<DesktopTelemetryOptions>() ?? options;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            // Optional telemetry configuration must not prevent the desktop from starting.
        }
        var enabled = bool.TryParse(environment("ApplicationInsights__Enabled"), out var flag) ? flag : options.Enabled;
        var connection = environment("APPLICATIONINSIGHTS_CONNECTION_STRING")
            ?? environment("ApplicationInsights__ConnectionString") ?? options.ConnectionString;
        return new(enabled && !string.IsNullOrWhiteSpace(connection), connection);
    }
}
