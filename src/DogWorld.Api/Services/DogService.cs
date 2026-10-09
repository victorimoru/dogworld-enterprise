using DogWorld.Api.Models;
using DogWorld.Api.Repositories;

namespace DogWorld.Api.Services;

public class DogService(IDogRepository repository)
{
    public async Task<IReadOnlyList<Dog>> GetAvailableDogsAsync(CancellationToken cancellationToken)
    {
        var dogs = await repository.GetAvailableDogsAsync(cancellationToken);
        if (!dogs.Any()) 
        {
            return [];
        }
        return dogs;
    }
}
