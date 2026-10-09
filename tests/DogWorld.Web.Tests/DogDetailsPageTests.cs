using System.Net;
using System.Text;
using Bunit;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class DogDetailsPageTests : TestContext
{
    private static HttpResponseMessage Dog(string name = "Bella", string status = "Available") => new(HttpStatusCode.OK)
    {
        Content = new StringContent($$"""{"id":2,"name":"{{name}}","breed":"Poodle","ageInMonths":14,"status":"{{status}}"}""", Encoding.UTF8, "application/json")
    };

    [Fact]
    public void LoadingThenDetailsAndBackLink()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>();
        Register(_ => pending.Task);
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 2));
        Assert.Contains("Loading", page.Find("[role=status]").TextContent);
        pending.SetResult(Dog());
        page.WaitForAssertion(() => Assert.Equal("Bella", page.Find("h1").TextContent));
        Assert.Contains("Poodle", page.Markup);
        Assert.Contains("14 months", page.Markup);
        Assert.Contains("Available", page.Markup);
        Assert.Equal("/", page.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void MissingDogShowsNotFoundWithoutRetry()
    {
        Register(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 99));
        page.WaitForAssertion(() => Assert.Equal("Dog not found", page.Find("h1").TextContent));
        Assert.Empty(page.FindAll("button"));
    }

    [Fact]
    public void FailureThenRetryShowsAdoptedDog()
    {
        var calls = 0;
        Register(_ => Task.FromResult(++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("PRIVATE_SQL") }
            : Dog(status: "Adopted")));
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 2));
        page.WaitForAssertion(() => Assert.Contains("Unable to load", page.Find("[role=alert]").TextContent));
        Assert.DoesNotContain("PRIVATE_SQL", page.Markup);
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Contains("Adopted", page.Markup));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ParameterChangeFetchesNewDog()
    {
        var paths = new List<string>();
        Register(request => { paths.Add(request.RequestUri!.AbsolutePath); return Task.FromResult(Dog(paths.Count == 1 ? "Bella" : "Max")); });
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 2));
        page.WaitForAssertion(() => Assert.Equal("Bella", page.Find("h1").TextContent));
        page.SetParametersAndRender(p => p.Add(x => x.Id, 3));
        page.WaitForAssertion(() => Assert.Equal("Max", page.Find("h1").TextContent));
        Assert.Equal(new[] { "/api/dogs/2", "/api/dogs/3" }, paths);
    }

    private void Register(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) =>
        Services.AddSingleton(new HttpClient(new Handler(respond)) { BaseAddress = new Uri("https://api.example.test/") });

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
