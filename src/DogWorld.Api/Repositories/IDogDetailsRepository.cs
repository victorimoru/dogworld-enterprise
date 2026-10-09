using DogWorld.Api.Models;

namespace DogWorld.Api.Repositories;

public interface IDogDetailsRepository
{
    Task<Dog?> GetByIdAsync(int id, CancellationToken cancellationToken);
}
