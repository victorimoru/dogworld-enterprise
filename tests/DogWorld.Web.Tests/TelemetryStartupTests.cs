using System.Net;
using System.Text;
using BlazorApplicationInsights;
using Bunit;
using DogWorld.Web;
using DogWorld.Web.Telemetry;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class TelemetryStartupTests : TestContext
{
    [Fact]
    public async Task FirstApiRequestWaitsUntilTelemetryInitializationCompletes()
    {
        var startup = new TelemetryStartup();
        var handler = new Handler();
        Services.AddSingleton(startup);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new("http://api.example.test/") });
        JSInterop.Setup<bool>("dogWorldTelemetry.waitForSdk", _ => true).SetResult(true);
        ComponentFactories.AddStub<ApplicationInsightsInit>();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/dogs/1");
        var app = RenderComponent<App>();
        Assert.Equal(0, handler.Calls);
        Assert.Contains("Preparing DogWorld", app.Markup);
        await app.InvokeAsync(startup.Complete);
        app.WaitForAssertion(() => Assert.Equal(1, handler.Calls));
        app.WaitForAssertion(() => Assert.Contains("Bella", app.Find("h1").TextContent));
    }

    private sealed class Handler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":1,"name":"Bella","breed":"Beagle","ageInMonths":14,"status":"Available"}""", Encoding.UTF8, "application/json")
            });
        }
    }
}
