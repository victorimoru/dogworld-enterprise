using System.Net;
using System.Text;
using Bunit;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class AvailableDogsPageTests : TestContext
{
    private const string Dogs = """
        [{"id":1,"name":"Bella","breed":"Poodle","ageInMonths":14},
         {"id":2,"name":"Max","breed":"Beagle","ageInMonths":36}]
        """;

    [Fact]
    public void ShowsLoadingUntilRequestCompletes()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        Register(_ => pending.Task);
        var page = RenderComponent<Home>();
        Assert.Contains("Loading", page.Find("[role=status]").TextContent);
        pending.SetResult(Json("[]"));
        page.WaitForAssertion(() => Assert.Contains("No dogs available", page.Markup));
    }

    [Fact]
    public void FetchesDogsAndDisplaysNamesBreedsAndAgeInApiOrder()
    {
        var handler = Register(_ => Task.FromResult(Json(Dogs)));
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
        Assert.Equal("/api/dogs", Assert.Single(handler.Paths));
        var cards = page.FindAll("article");
        Assert.Contains("Bella", cards[0].TextContent);
        Assert.Contains("Poodle", cards[0].TextContent);
        Assert.Contains("14 months", cards[0].TextContent);
        Assert.Contains("Max", cards[1].TextContent);
        Assert.Contains("Beagle", cards[1].TextContent);
        Assert.Contains("36 months", cards[1].TextContent);
    }

    [Fact]
    public void ShowsEmptyMessageForEmptyArray()
    {
        Register(_ => Task.FromResult(Json("[]")));
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Contains("No dogs available", page.Markup));
        Assert.Empty(page.FindAll("article"));
        Assert.Empty(page.FindAll("[role=alert]"));
    }

    [Fact]
    public async Task FailureShowsFriendlyErrorAndRetryLoadsDogsWithoutReload()
    {
        var attempt = 0;
        var pendingRetry = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Register(_ => ++attempt == 1
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                { Content = new StringContent("PRIVATE_SQL_ERROR") })
            : pendingRetry.Task);
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Contains("Unable to load dogs", page.Find("[role=alert]").TextContent));
        Assert.DoesNotContain("PRIVATE_SQL_ERROR", page.Markup);
        var retry = page.Find("button");
        Assert.Equal("Retry", retry.TextContent.Trim());
        var click = retry.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        page.WaitForAssertion(() => Assert.Contains("Loading", page.Find("[role=status]").TextContent));
        pendingRetry.SetResult(Json(Dogs));
        await click;
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
        Assert.Empty(page.FindAll("[role=alert]"));
        Assert.Equal(2, handler.Paths.Count);
    }

    [Fact]
    public void NetworkFailureShowsRetry()
    {
        Register(_ => Task.FromException<HttpResponseMessage>(new HttpRequestException("Network unavailable")));
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Contains("Unable to load dogs", page.Find("[role=alert]").TextContent));
        Assert.Equal("Retry", page.Find("button").TextContent.Trim());
    }

    private Handler Register(Func<CancellationToken, Task<HttpResponseMessage>> respond)
    {
        var handler = new Handler(respond);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") });
        return handler;
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class Handler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            return respond(cancellationToken);
        }
    }
}
