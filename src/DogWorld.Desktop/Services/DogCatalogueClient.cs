using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DogWorld.Contracts;
namespace DogWorld.Desktop.Services;
public sealed class DogCatalogueClient(HttpClient http) : IDogCatalogueClient
{
    public async Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        using var response = await http.GetAsync($"api/dogs?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var dogs = await response.Content.ReadFromJsonAsync<AvailableDog[]>(cancellationToken)
            ?? throw new JsonException("Expected a dog array.");

        if (!response.Headers.TryGetValues("X-Total-Count", out var values)
            || !int.TryParse(values.SingleOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            || count < dogs.Length || dogs.Length > pageSize || dogs.Any(dog => dog is null))
            throw new JsonException("Invalid catalogue pagination response.");

        return new DogPage(dogs, count);
    }
}
