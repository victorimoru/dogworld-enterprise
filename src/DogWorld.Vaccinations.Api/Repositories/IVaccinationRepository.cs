using DogWorld.Contracts;

namespace DogWorld.Vaccinations.Api.Repositories;

public interface IVaccinationRepository
{
    Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int dogId, CancellationToken cancellationToken);
}
