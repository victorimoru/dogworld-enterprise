using System.Diagnostics;
using DogWorld.Desktop.Telemetry;
namespace DogWorld.Desktop.Tests;
[Collection("Desktop telemetry")]
public class DesktopTelemetryTests
{
    [Fact]
    public void MissingConfigurationDisablesTelemetry() => Assert.False(DesktopTelemetryOptions.Load("missing-telemetry.json", _ => null).Enabled);
    [Fact]
    public void EnvironmentCanEnableOrDisableLocalTelemetry()
    {
        var options = DesktopTelemetryOptions.Load("missing-telemetry.json", name => name switch
        {
            "ApplicationInsights__Enabled" => "true",
            "APPLICATIONINSIGHTS_CONNECTION_STRING" => "InstrumentationKey=00000000-0000-0000-0000-000000000001",
            _ => null
        });
        Assert.True(options.Enabled);
        Assert.Contains("InstrumentationKey",options.ConnectionString);
    }
    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("cancelled")]
    public async Task OperationRecordsOutcomeAndRestoresAmbientContext(string outcome)
    {
        Activity? stopped=null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DesktopOperations.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stopped=activity
        };
        ActivitySource.AddActivityListener(listener);
        var before=Activity.Current;
        using var cancellation=new CancellationTokenSource();
        var error=await Record.ExceptionAsync(() => DesktopOperations.RunAsync<int>("dog.details.load", () =>
        {
            if(outcome == "failure") throw new HttpRequestException("Private error text");
            if(outcome == "cancelled") { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); }
            return Task.FromResult(42);
        },cancellation.Token));
        Assert.NotNull(stopped);
        Assert.Equal(outcome,stopped.GetTagItem("dogworld.outcome"));
        Assert.Equal(outcome == "failure" ? ActivityStatusCode.Error : ActivityStatusCode.Unset,stopped.Status);
        Assert.Same(before,Activity.Current);
        if(outcome == "success") Assert.Null(error); else Assert.NotNull(error);
        Assert.DoesNotContain(stopped.TagObjects,tag => Equals(tag.Value,"Private error text"));
    }
}

