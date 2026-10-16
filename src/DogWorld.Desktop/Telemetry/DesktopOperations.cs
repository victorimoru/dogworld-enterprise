using System.Diagnostics;
namespace DogWorld.Desktop.Telemetry;
public static class DesktopOperations
{
    public const string SourceName = "DogWorld.Desktop";
    private static readonly ActivitySource Source = new(SourceName);
    public static async Task<T> RunAsync<T>(string name, Func<Task<T>> action, CancellationToken token)
    {
        using var activity = Source.StartActivity(name, ActivityKind.Internal);
        try
        {
            var result = await action();
            activity?.SetTag("dogworld.outcome", "success");
            return result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            activity?.SetTag("dogworld.outcome", "cancelled");
            throw;
        }
        catch (Exception error)
        {
            activity?.SetTag("dogworld.outcome", "failure");
            activity?.SetTag("error.type", error.GetType().FullName);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}
