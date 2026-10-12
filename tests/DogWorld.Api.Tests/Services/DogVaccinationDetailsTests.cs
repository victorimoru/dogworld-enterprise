using DogWorld.Api.Clients;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;
using DogWorld.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace DogWorld.Api.Tests.Services;

public class DogVaccinationDetailsTests
{
    [Fact]
    public async Task BothDependenciesStartBeforeEitherCompletes()
    {
        var breedSource = new TaskCompletionSource<BreedProfileResponse?>();
        var vaccineSource = new TaskCompletionSource<IReadOnlyList<VaccinationRecordResponse>>();
        var vaccines = new Vaccines { Result = vaccineSource.Task };
        var breeds = new Breeds { Result = breedSource.Task };
        using var cancellation = new CancellationTokenSource();
        var pending = Create(vaccines, breeds).GetByIdAsync(7, cancellation.Token);
        Assert.True(breeds.Called);
        Assert.Equal(7, vaccines.Id);
        Assert.Equal(cancellation.Token, vaccines.Token);
        vaccineSource.SetResult([new(1, 7, "Rabies", new(2026, 9, 15), null)]);
        breedSource.SetResult(null);
        var result = await pending;
        Assert.Single(result!.Vaccinations!);
    }

    [Fact]
    public async Task EmptyHistoryIsDifferentFromUnavailable()
    {
        var empty = await Create(new Vaccines()).GetByIdAsync(7, default);
        Assert.Empty(empty!.Vaccinations!);
        var failed = await Create(new Vaccines { Result = Task.FromException<IReadOnlyList<VaccinationRecordResponse>>(new HttpRequestException()) }).GetByIdAsync(7, default);
        Assert.Null(failed!.Vaccinations);
        Assert.NotNull(failed.BreedDetails);
    }

    [Fact]
    public async Task MissingDogSkipsBothDependencies()
    {
        var vaccines = new Vaccines();
        var breeds = new Breeds();
        var result = await Create(vaccines, breeds, true).GetByIdAsync(7, default);
        Assert.Null(result);
        Assert.Null(vaccines.Id);
        Assert.False(breeds.Called);
    }

    [Fact]
    public async Task CancellationDuringEnrichmentPropagates()
    {
        using var source = new CancellationTokenSource();
        var vaccines = new Vaccines { Result = Task.FromCanceled<IReadOnlyList<VaccinationRecordResponse>>(new CancellationToken(true)) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(vaccines).GetByIdAsync(7, source.Token));
    }

    private static DogDetailsService Create(Vaccines vaccines, Breeds? breeds = null, bool missing = false) =>
        new(new Repository(missing), breeds ?? new Breeds(), NullLogger<DogDetailsService>.Instance, vaccines);
    private sealed class Repository(bool missing) : IDogDetailsRepository
    {
        public Task<Dog?> GetByIdAsync(int id, CancellationToken token) => Task.FromResult(missing ? null : new Dog { Id = id, Name = "Bella", Breed = "Beagle" });
    }
    private sealed class Breeds : IBreedsApiClient
    {
        public bool Called { get; private set; }
        public Task<BreedProfileResponse?> Result { get; init; } = Task.FromResult<BreedProfileResponse?>(new("beagle", "Beagle", "Hound", "Friendly", "12 years", "10 kg", "England"));
        public Task<BreedProfileResponse?> GetBreedByIdAsync(string id, CancellationToken token = default) { Called = true; return Result; }
    }
    private sealed class Vaccines : IVaccinationsApiClient
    {
        public int? Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<IReadOnlyList<VaccinationRecordResponse>> Result { get; init; } = Task.FromResult<IReadOnlyList<VaccinationRecordResponse>>([]);
        public Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int id, CancellationToken token = default) { Id = id; Token = token; return Result; }
    }
}
