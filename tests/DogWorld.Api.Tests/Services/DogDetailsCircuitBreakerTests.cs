using DogWorld.Api.Clients;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;
using DogWorld.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;

namespace DogWorld.Api.Tests.Services;

public class DogDetailsCircuitBreakerTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task OpenCircuitsPreserveDogAndAnyHealthyEnrichment(bool breedOpen, bool vaccinationsOpen)
    {
        var breeds = new Breeds(breedOpen);
        var vaccinations = new Vaccinations(vaccinationsOpen);
        var service = new DogDetailsService(new Repository(), breeds,
            NullLogger<DogDetailsService>.Instance, vaccinations);

        var result = await service.GetByIdAsync(1, default);

        Assert.NotNull(result);
        Assert.Equal("Bella", result.Name);
        Assert.Equal(1, result.Id);
        if (breedOpen) Assert.Null(result.BreedDetails);
        else Assert.Equal(breeds.Profile, result.BreedDetails);
        if (vaccinationsOpen) Assert.Null(result.Vaccinations);
        else Assert.NotNull(result.Vaccinations); // Empty history is available, not a failure.
    }

    private sealed class Repository : IDogDetailsRepository
    {
        public Task<Dog?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<Dog?>(new Dog { Id = id, Name = "Bella", Breed = "Golden Retriever",
                AgeInMonths = 14, Status = DogStatus.Available });
    }

    private sealed class Breeds(bool open) : IBreedsApiClient
    {
        public BreedProfileResponse Profile { get; } = new("golden-retriever", "Golden Retriever",
            "Sporting", "Friendly", "10–12 years", "25–34 kg", "Scotland");
        public Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default) =>
            open ? Task.FromException<BreedProfileResponse?>(new BrokenCircuitException())
                 : Task.FromResult<BreedProfileResponse?>(Profile);
    }

    private sealed class Vaccinations(bool open) : IVaccinationsApiClient
    {
        public Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int dogId, CancellationToken cancellationToken = default) =>
            open ? Task.FromException<IReadOnlyList<VaccinationRecordResponse>>(new BrokenCircuitException())
                 : Task.FromResult<IReadOnlyList<VaccinationRecordResponse>>([]);
    }
}
