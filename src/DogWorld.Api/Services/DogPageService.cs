using DogWorld.Api.Repositories;

namespace DogWorld.Api.Services;

public class DogPageService(IDogPageRepository repository)
{
    public Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        GetPageAsync(page, pageSize, null, cancellationToken);

    public async Task<DogPage> GetPageAsync(int page, int pageSize, string? breed, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive, pageSize must be 1–100");

        var normalizedBreed = string.IsNullOrWhiteSpace(breed) ? null : breed.Trim();
        return await repository.GetPageAsync(page, pageSize, normalizedBreed, cancellationToken);
    }
}
