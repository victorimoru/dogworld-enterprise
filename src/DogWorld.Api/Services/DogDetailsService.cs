using DogWorld.Api.Clients;
using DogWorld.Api.Repositories;
using DogWorld.Contracts;
using Polly.Timeout;
using System.Text.Json;

namespace DogWorld.Api.Services;

public sealed class DogDetailsService(IDogDetailsRepository repository,
                                      IBreedsApiClient breeds,
                                      ILogger<DogDetailsService> logger,
                                      IVaccinationsApiClient vaccinations)
{
    public async Task<DogDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var dog = new DogDetails(entity.Id, entity.Name, entity.Breed, entity.AgeInMonths, entity.Status.ToString());
        var breedId = string.Join("-", dog.Breed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

        var breedTask = GetBreedAsync(breedId, id, cancellationToken);
        var vaccinationTask = GetVaccinationsAsync(id, cancellationToken);
        await Task.WhenAll(breedTask, vaccinationTask);

        cancellationToken.ThrowIfCancellationRequested();
        return dog with { BreedDetails = await breedTask, Vaccinations = await vaccinationTask };
    }

    private async Task<BreedProfileResponse?> GetBreedAsync(string breedId, int dogId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(breedId)) return null;
        try { return await breeds.GetBreedByIdAsync(breedId, token); }
        catch (Exception exception) when (IsDownstreamFailure(exception))
        {
            token.ThrowIfCancellationRequested();
            logger.LogWarning(exception, "Breed information unavailable for dog {DogId}, breed {BreedId}", dogId, breedId);
            return null;
        }
    }

    private async Task<IReadOnlyList<VaccinationRecordResponse>?> GetVaccinationsAsync(int dogId, CancellationToken token)
    {
        try { return await vaccinations.GetByDogIdAsync(dogId, token); }
        catch (Exception exception) when (IsDownstreamFailure(exception))
        {
            token.ThrowIfCancellationRequested();
            logger.LogWarning(exception, "Vaccination history unavailable for dog {DogId}", dogId);
            return null;
        }
    }

    private static bool IsDownstreamFailure(Exception exception) =>
        exception is HttpRequestException or TimeoutRejectedException or JsonException;
}
