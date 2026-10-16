using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DogWorld.Contracts;
namespace DogWorld.Desktop.Services;
public sealed class DogDetailsClient(HttpClient http) : IDogDetailsClient
{
    public async Task<DogDetails?> GetAsync(int id, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        using var response = await http.GetAsync($"api/dogs/{id}",token);
        if(response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var dog = await response.Content.ReadFromJsonAsync<DogDetails>(token);
        if(dog is null || dog.Id != id || string.IsNullOrWhiteSpace(dog.Name))
            throw new JsonException("Expected details for the selected dog.");
        return dog;
    }
}
