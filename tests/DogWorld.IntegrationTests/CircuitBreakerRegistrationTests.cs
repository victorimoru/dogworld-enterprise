using System.Net;
using System.Reflection;
using DogWorld.Api.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace DogWorld.IntegrationTests;

public class CircuitBreakerRegistrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task SeparateClientScopesShareOnlyTheirOwnCircuit(bool failBreeds)
    {
        var entry = Assembly.Load("DogWorld.Api").EntryPoint!.DeclaringType!;
        return (Task)typeof(CircuitBreakerRegistrationTests).GetMethod(nameof(Verify), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entry).Invoke(null, [failBreeds])!;
    }

    private static async Task Verify<TEntry>(bool failBreeds) where TEntry : class
    {
        var breeds = new Handler(failBreeds, true);
        var vaccinations = new Handler(!failBreeds, false);
        using var factory = new WebApplicationFactory<TEntry>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient("Breeds").ConfigurePrimaryHttpMessageHandler(() => breeds);
                services.AddHttpClient("Vaccinations").ConfigurePrimaryHttpMessageHandler(() => vaccinations);
            }));

        for (var i = 0; i < 4; i++)
        {
            using var scope = factory.Services.CreateScope();
            await Assert.ThrowsAsync<HttpRequestException>(() => Call(scope.ServiceProvider, failBreeds));
        }
        var failingHandler = failBreeds ? breeds : vaccinations;
        Assert.Equal(12, failingHandler.Calls);
        using var last = factory.Services.CreateScope();
        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => Call(last.ServiceProvider, failBreeds));
        Assert.Equal(12, failingHandler.Calls);
        await Call(last.ServiceProvider, !failBreeds);
        Assert.Equal(1, (failBreeds ? vaccinations : breeds).Calls);
    }

    private static async Task Call(IServiceProvider services, bool breeds)
    {
        if (breeds) await services.GetRequiredService<BreedsApiClient>().GetBreedByIdAsync("beagle");
        else await services.GetRequiredService<IVaccinationsApiClient>().GetByDogIdAsync(1);
    }

    private sealed class Handler(bool fail, bool breed) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(fail ? HttpStatusCode.ServiceUnavailable : breed ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
