using System.Net;
using System.Net.Http.Json;
using DogWorld.Api.Clients;
using DogWorld.Contracts;

namespace DogWorld.Api.Tests.Clients;

public class BreedsApiClientTests
{
    private static readonly BreedProfileResponse Profile = new("beagle", "Beagle", "Hound", "Friendly", "12–15 years", "9–11 kg", "England");

    [Fact]
    public async Task GetsProfileFromBreedEndpoint()
    {
        string? path = null;
        using var http = Http((request, _) =>
        {
            path = request.RequestUri!.AbsolutePath;
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Profile) });
        });
        var result = await new BreedsApiClient(http).GetBreedByIdAsync("beagle", default);
        Assert.Equal("/api/breeds/beagle", path);
        Assert.Equal(Profile, result);
    }

    [Fact]
    public async Task NotFoundReturnsNull()
    {
        using var http = Http((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        Assert.Null(await new BreedsApiClient(http).GetBreedByIdAsync("missing", default));
    }

    [Fact]
    public async Task ServerFailureIsNotDisguisedAsMissingBreed()
    {
        using var http = Http((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new BreedsApiClient(http).GetBreedByIdAsync("beagle", default));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
    }

    [Fact]
    public async Task CallerCanCancelPendingRequest()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = Http(async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cancellation = new CancellationTokenSource();
        var pending = new BreedsApiClient(http).GetBreedByIdAsync("beagle", cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static HttpClient Http(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
        new(new Handler(send)) { BaseAddress = new Uri("http://breeds.example.test/") };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
