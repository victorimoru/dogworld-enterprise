using System.Net;
using DogWorld.Desktop.Services;
namespace DogWorld.Desktop.Tests;
public class CatalogueClientTests
{
    [Fact]
    public async Task UsesApiPagingContract()
    {
        var handler = new Handler("50");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var result = await new DogCatalogueClient(http).GetPageAsync(2,12,TestContext.Current.CancellationToken);
        Assert.Equal("/api/dogs?page=2&pageSize=12",handler.Path);
        Assert.Equal(50,result.TotalCount);
        Assert.Equal("Bella",Assert.Single(result.Dogs).Name);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("-1")]
    public async Task MalformedTotalIsRejected(string? total)
    {
        using var http = new HttpClient(new Handler(total)) { BaseAddress = new Uri("http://localhost/") };
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => new DogCatalogueClient(http).GetPageAsync(1,12,TestContext.Current.CancellationToken));
    }
    private sealed class Handler(string? total) : HttpMessageHandler
    {
        public string? Path;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Path=request.RequestUri!.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"id\":1,\"name\":\"Bella\",\"breed\":\"Poodle\",\"ageInMonths\":14}]",System.Text.Encoding.UTF8,"application/json") };
            if(total is not null) response.Headers.TryAddWithoutValidation("X-Total-Count",total);
            return Task.FromResult(response);
        }
    }
}
