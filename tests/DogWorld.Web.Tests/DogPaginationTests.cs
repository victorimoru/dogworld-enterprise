using System.Net;
using System.Net.Http.Json;
using Bunit;
using DogWorld.Contracts;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class DogPaginationTests : TestContext
{
    private static readonly string[] expected = new[] { "?page=1&pageSize=12", "?page=2&pageSize=12", "?page=3&pageSize=12", "?page=1&pageSize=12" };

    [Fact]
    public void FetchesTwelveDogsPerPageFromApi()
    {
        var handler = Register(30);
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Equal(12, page.FindAll("article").Count));
        Assert.Contains("Dog 1", page.Find("article h2").TextContent);
        Assert.True(page.Find("button[aria-label='Previous page']").HasAttribute("disabled"));
        page.Find("button[aria-label='Next page']").Click();
        Assert.Equal(12, page.FindAll("article").Count);
        Assert.Equal("Dog 13", page.Find("article h2").TextContent);
        Assert.Equal("2", page.Find("[aria-current=page]").TextContent.Trim());
        page.Find("button[aria-label='Next page']").Click();
        Assert.Equal(6, page.FindAll("article").Count);
        Assert.Contains("Showing 25–30 of 30", page.Markup);
        Assert.True(page.Find("button[aria-label='Next page']").HasAttribute("disabled"));
        page.Find("button[aria-label='Go to page 1']").Click();
        Assert.Equal("Dog 1", page.Find("article h2").TextContent);
        Assert.Equal(4, handler.Calls);
        Assert.Equal(expected, handler.Queries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void SinglePageDoesNotShowPagination(int count)
    {
        Register(count);
        var page = RenderComponent<Home>();
        page.WaitForAssertion(() => Assert.Equal(count, page.FindAll("article").Count));
        Assert.Empty(page.FindAll("nav[aria-label='Dog pagination']"));
    }

    private Handler Register(int count)
    {
        var handler = new Handler(count);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") });
        return handler;
    }

    private sealed class Handler(int count) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public List<string> Queries { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Queries.Add(request.RequestUri!.Query);
            var pageValue = request.RequestUri.Query.TrimStart('?').Split('&')
                .FirstOrDefault(part => part.StartsWith("page="))?.Split('=')[1];
            var page = pageValue is null ? 1 : int.Parse(pageValue);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Enumerable.Range(1, count).Skip((page - 1) * 12).Take(12).Select(id => new AvailableDog(id, $"Dog {id}", "Beagle", 12)))
            };
            response.Headers.Add("X-Total-Count", count.ToString());
            return Task.FromResult(response);
        }
    }
}
