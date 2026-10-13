namespace DogWorld.Web.Telemetry;

public sealed class TelemetryStartup
{
    public bool IsReady { get; private set; }
    public event Action? Ready;

    public void Complete()
    {
        if (IsReady) return;
        IsReady = true;
        Ready?.Invoke();
    }
}
