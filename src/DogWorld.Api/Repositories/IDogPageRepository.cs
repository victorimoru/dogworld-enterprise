using DogWorld.Contracts;

namespace DogWorld.Api.Repositories;

public sealed record DogPage(IReadOnlyList<AvailableDog> Dogs, int TotalCount);

public interface IDogPageRepository
{
    Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<DogPage> GetPageAsync(int page, int pageSize, string? breed, CancellationToken cancellationToken);
}
