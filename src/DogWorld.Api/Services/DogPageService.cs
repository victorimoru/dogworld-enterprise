using DogWorld.Api.Repositories;

namespace DogWorld.Api.Services;

public class DogPageService(IDogPageRepository repository)
{
    public Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive, pageSize must be 1–100, and the offset must fit in an integer.");
        return repository.GetPageAsync(page, pageSize, cancellationToken);
    }
}
