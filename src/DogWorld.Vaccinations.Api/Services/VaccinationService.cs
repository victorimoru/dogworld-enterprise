using DogWorld.Contracts;
using DogWorld.Vaccinations.Api.Repositories;

namespace DogWorld.Vaccinations.Api.Services;

public sealed class VaccinationService(IVaccinationRepository repository)
{
    public Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int dogId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dogId);
        cancellationToken.ThrowIfCancellationRequested();
        return repository.GetByDogIdAsync(dogId, cancellationToken);
    }
}
