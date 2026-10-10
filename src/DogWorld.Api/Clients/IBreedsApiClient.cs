using DogWorld.Contracts;

namespace DogWorld.Api.Clients;

public interface IBreedsApiClient
{
    Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default);
}
