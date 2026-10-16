using DogWorld.Contracts;
using DogWorld.Desktop.Services;
namespace DogWorld.Desktop.Telemetry;
public sealed class TracedDogCatalogueClient(IDogCatalogueClient inner) : IDogCatalogueClient
{
    public Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        DesktopOperations.RunAsync("dogs.catalogue.load",
            () => inner.GetPageAsync(page, pageSize, cancellationToken), cancellationToken);
}
public sealed class TracedDogDetailsClient(IDogDetailsClient inner) : IDogDetailsClient
{
    public Task<DogDetails?> GetAsync(int id, CancellationToken cancellationToken) =>
        DesktopOperations.RunAsync("dog.details.load",
            () => inner.GetAsync(id, cancellationToken), cancellationToken);
}
