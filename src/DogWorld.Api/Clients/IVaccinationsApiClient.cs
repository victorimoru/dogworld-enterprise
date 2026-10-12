using DogWorld.Contracts;

namespace DogWorld.Api.Clients;

public interface IVaccinationsApiClient
{
    Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(
        int dogId,
        CancellationToken cancellationToken = default);
}
