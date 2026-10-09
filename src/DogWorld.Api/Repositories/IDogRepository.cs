using DogWorld.Api.Models;

namespace DogWorld.Api.Repositories;

public interface IDogRepository
{
    Task<IReadOnlyList<Dog>> GetAvailableDogsAsync(CancellationToken cancellationToken);
}
