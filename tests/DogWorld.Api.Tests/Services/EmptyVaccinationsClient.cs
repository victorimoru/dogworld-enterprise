using DogWorld.Api.Clients;
using DogWorld.Contracts;

namespace DogWorld.Api.Tests.Services;

internal sealed class EmptyVaccinationsClient : IVaccinationsApiClient
{
    public Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int dogId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VaccinationRecordResponse>>([]);
}
