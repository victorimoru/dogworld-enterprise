using DogWorld.Contracts;

namespace DogWorld.Breeds.Api.Services;

public interface IBreedService
{
    Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreedProfileResponse>> GetAllBreedsAsync(CancellationToken cancellationToken = default);
}
