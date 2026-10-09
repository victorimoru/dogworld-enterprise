using DogWorld.Api.Data;
using DogWorld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Api.Repositories;

public class DogRepository(DogWorldDbContext context) : IDogRepository, IDogDetailsRepository, IDogPageRepository
{
    public async Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Dogs.AsNoTracking().Where(dog => dog.Status == DogStatus.Available);
        var count = await query.CountAsync(cancellationToken);
        var dogs = await query.OrderBy(dog => dog.Name).ThenBy(dog => dog.Id)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize)
            .Select(dog => new DogWorld.Contracts.AvailableDog(dog.Id, dog.Name, dog.Breed, dog.AgeInMonths))
            .ToListAsync(cancellationToken);
        return new DogPage(dogs, count);
    }

    public Task<Dog?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Dogs.AsNoTracking().SingleOrDefaultAsync(dog => dog.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Dog>> GetAvailableDogsAsync(CancellationToken cancellationToken) =>
        await context.Dogs
            .AsNoTracking()
            .Where(dog => dog.Status == DogStatus.Available)
            .OrderBy(dog => dog.Name)
            .ThenBy(dog => dog.Id)
            .ToListAsync(cancellationToken);
}
