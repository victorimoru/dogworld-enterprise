using DogWorld.Api.Repositories;
using DogWorld.Contracts;

namespace DogWorld.Api.Services;

public class DogDetailsService(IDogDetailsRepository repository)
{
    public async Task<DogDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var dog = await repository.GetByIdAsync(id, cancellationToken);
        return dog is null ? null : new DogDetails(dog.Id, dog.Name, dog.Breed, dog.AgeInMonths, dog.Status.ToString());
    }
}
