using DogWorld.Contracts;

namespace DogWorld.Breeds.Api.Services;

public class BreedService : IBreedService
{
    public Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<BreedProfileResponse>> GetAllBreedsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
