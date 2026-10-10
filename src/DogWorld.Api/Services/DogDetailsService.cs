using DogWorld.Api.Clients;
using DogWorld.Api.Repositories;
using DogWorld.Contracts;
using Polly.Timeout;
using System.Text.Json;

namespace DogWorld.Api.Services;

public sealed class DogDetailsService(IDogDetailsRepository repository, IBreedsApiClient breeds, ILogger<DogDetailsService> logger)
{
    public async Task<DogDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var dog = new DogDetails(entity.Id, entity.Name, entity.Breed, entity.AgeInMonths, entity.Status.ToString());
        var breedId = string.Join("-", dog.Breed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

        if (string.IsNullOrEmpty(breedId)) return dog;
        try
        {
            var profile = await breeds.GetBreedByIdAsync(breedId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return dog with { BreedDetails = profile };
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException or JsonException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogWarning(exception, "Breed information unavailable for dog {DogId}, breed {BreedId}", id, breedId);
            return dog;
        }
    }
}
