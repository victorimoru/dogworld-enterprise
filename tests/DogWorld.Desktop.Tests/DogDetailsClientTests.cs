using System.Net;
using DogWorld.Desktop.Services;
namespace DogWorld.Desktop.Tests;
public class DogDetailsClientTests
{
    [Fact]
    public async Task LoadsAggregateAndUsesCorrectRoute()
    {
        var handler = new Handler(HttpStatusCode.OK,"{\"id\":7,\"name\":\"Bella\",\"breed\":\"Poodle\",\"ageInMonths\":14,\"status\":\"Available\",\"vaccinations\":[]}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var dog = await new DogDetailsClient(http).GetAsync(7,TestContext.Current.CancellationToken);
        Assert.Equal("/api/dogs/7",handler.Path);
        Assert.Equal("Bella",dog!.Name);
        Assert.Empty(dog.Vaccinations!);
    }
    [Fact]
    public async Task NotFoundReturnsNull()
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.NotFound,"")) { BaseAddress = new Uri("http://localhost/") };
        Assert.Null(await new DogDetailsClient(http).GetAsync(1,TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("{\"id\":2,\"name\":\"Other dog\"}")]
    [InlineData("not json")]
    public async Task InvalidOrWrongDogResponseIsRejected(string body)
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.OK,body)) { BaseAddress = new Uri("http://localhost/") };
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => new DogDetailsClient(http).GetAsync(1,TestContext.Current.CancellationToken));
    }
    private sealed class Handler(HttpStatusCode code,string body) : HttpMessageHandler
    {
        public string? Path;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Path=request.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body,System.Text.Encoding.UTF8,"application/json") });
        }
    }
}
