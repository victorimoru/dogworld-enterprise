using DogWorld.Contracts;
using DogWorld.Vaccinations.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Vaccinations.Api.Repositories;

public sealed class VaccinationRepository(VaccinationDbContext context) : IVaccinationRepository
{
    public async Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(int dogId, CancellationToken cancellationToken) =>
        await context.Vaccinations.AsNoTracking().Where(v => v.DogId == dogId)
            .OrderByDescending(v => v.AdministeredOn).ThenBy(v => v.Id)
            .Select(v => new VaccinationRecordResponse(v.Id, v.DogId, v.VaccineName, v.AdministeredOn, v.NextDueOn))
            .ToListAsync(cancellationToken);
}
