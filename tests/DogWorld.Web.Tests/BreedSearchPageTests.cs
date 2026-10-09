using System.Net;
using System.Net.Http.Json;
using Bunit;
using DogWorld.Contracts;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class BreedSearchPageTests : TestContext
{
    [Fact]
    public void SearchResetsPageAndKeepsFilterWhenPagingAndClearing()
    {
        var handler = Register(_ => Result(30));
        var page = RenderComponent<Home>();
        page.Find("button[aria-label='Next page']").Click();
        page.Find("#breed-search").Input("  beagle  ");
        page.Find("form[role=search]").Submit();
        Assert.Equal("?page=1&pageSize=12&breed=beagle", handler.Queries.Last());
        page.Find("button[aria-label='Next page']").Click();
        Assert.Equal("?page=2&pageSize=12&breed=beagle", handler.Queries.Last());
        page.Find("input[value='Clear search']").Click();
        Assert.Equal("?page=1&pageSize=12", handler.Queries.Last());
        Assert.Equal("", page.Find("#breed-search").GetAttribute("value") ?? "");
    }

    [Fact]
    public void NoMatchesShowsSearchMessageAndClearAction()
    {
        var handler = Register(query => Result(query.Contains("breed=") ? 0 : 1));
        var page = RenderComponent<Home>();
        page.Find("#breed-search").Input("Poodle & mix");
        page.Find("form[role=search]").Submit();
        Assert.Equal("?page=1&pageSize=12&breed=Poodle%20%26%20mix", handler.Queries.Last());
        Assert.Contains("No dogs match", page.Find("[role=status]").TextContent);
        Assert.Empty(page.FindAll("article"));
        page.Find("input[value='Clear search']").Click();
        Assert.Single(page.FindAll("article"));
    }

    [Fact]
    public void FailedSearchRetriesAppliedFilterRatherThanUnsubmittedInput()
    {
        var attempt = 0;
        var handler = Register(query => query.Contains("breed=") && ++attempt == 1
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Result(1));
        var page = RenderComponent<Home>();
        page.Find("#breed-search").Input("Beagle");
        page.Find("form[role=search]").Submit();
        Assert.Single(page.FindAll("[role=alert]"));
        page.Find("#breed-search").Input("Poodle");
        page.Find("[role=alert] button").Click();
        Assert.Equal("?page=1&pageSize=12&breed=Beagle", handler.Queries.Last());
        Assert.Empty(page.FindAll("[role=alert]"));
    }

    private Handler Register(Func<string, HttpResponseMessage> respond)
    {
        var handler = new Handler(respond);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") });
        return handler;
    }
    private static HttpResponseMessage Result(int total)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Enumerable.Range(1, Math.Min(total, 12)).Select(id => new AvailableDog(id, $"Dog {id}", "Beagle", 12)))
        };
        response.Headers.Add("X-Total-Count", total.ToString());
        return response;
    }
    private sealed class Handler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Queries { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var query = request.RequestUri!.Query;
            Queries.Add(query);
            return Task.FromResult(respond(query));
        }
    }
}
