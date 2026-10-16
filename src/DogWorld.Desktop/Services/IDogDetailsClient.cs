using DogWorld.Contracts;
namespace DogWorld.Desktop.Services;
public interface IDogDetailsClient
{
    Task<DogDetails?> GetAsync(int id, CancellationToken token);
}
