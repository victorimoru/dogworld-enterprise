using DogWorld.Contracts;
namespace DogWorld.Desktop.Services;
public sealed record DogPage(IReadOnlyList<AvailableDog> Dogs, int TotalCount);
public interface IDogCatalogueClient
{
    Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
}
