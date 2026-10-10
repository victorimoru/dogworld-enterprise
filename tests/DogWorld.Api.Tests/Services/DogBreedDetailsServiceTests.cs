using DogWorld.Api.Clients;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;
using DogWorld.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.Timeout;

namespace DogWorld.Api.Tests.Services;

public class DogDetailsEnrichmentTests
{
    [Fact]
    public async Task EnrichesDogUsingSlugAndForwardsCancellation()
    {
        var client = new Client();
        using var cancellation = new CancellationTokenSource();
        var result = await Service(client).GetByIdAsync(4, cancellation.Token);
        Assert.NotNull(result);
        Assert.Equal("Bella", result.Name);
        Assert.Equal("golden-retriever", client.Id);
        Assert.Equal(cancellation.Token, client.Token);
        Assert.Equal(client.Profile, result.BreedDetails);
    }

    [Fact]
    public async Task MissingDogSkipsDownstreamCall()
    {
        var client = new Client();
        var service = new DogDetailsService(new Repository { Missing = true }, client,
            NullLogger<DogDetailsService>.Instance);
        Assert.Null(await service.GetByIdAsync(99, default));
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task MissingProfileKeepsDogDetails()
    {
        var result = await Service(new Client { Missing = true }).GetByIdAsync(4, default);
        Assert.NotNull(result);
        Assert.Null(result.BreedDetails);
        Assert.Equal("Bella", result.Name);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("timeout")]
    [InlineData("json")]
    public async Task DownstreamFailureKeepsDogDetails(string failure)
    {
        Exception error = failure switch
        {
            "http" => new HttpRequestException("Private downstream error"),
            "timeout" => new TimeoutRejectedException(),
            _ => new System.Text.Json.JsonException("Invalid downstream body")
        };
        var result = await Service(new Client { Failure = error }).GetByIdAsync(4, default);
        Assert.NotNull(result);
        Assert.Null(result.BreedDetails);
        Assert.Equal("Bella", result.Name);
    }

    [Fact]
    public async Task CallerCancellationIsNotConvertedToSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(new Client()).GetByIdAsync(4, cancellation.Token));
    }

    private static DogDetailsService Service(Client client) =>
        new(new Repository(), client, NullLogger<DogDetailsService>.Instance);
    private sealed class Repository : IDogDetailsRepository
    {
        public bool Missing { get; init; }
        public Task<Dog?> GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Missing ? null :
            new Dog { Id = id, Name = "Bella", Breed = "Golden Retriever", AgeInMonths = 14, Status = DogStatus.Available });
    }
    private sealed class Client : IBreedsApiClient
    {
        public BreedProfileResponse Profile { get; } = new("golden-retriever", "Golden Retriever", "Sporting", "Friendly", "10–12 years", "25–34 kg", "Scotland");
        public string? Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public bool Missing { get; init; }
        public Exception? Failure { get; init; }
        public Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
        {
            Calls++; Id = breedId; Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is not null ? Task.FromException<BreedProfileResponse?>(Failure) : Task.FromResult(Missing ? null : Profile);
        }
    }
}
