using System.Reflection;
using System.Text.Json;
using DogWorld.Contracts;

namespace DogWorld.Breeds.Api.Services;

public class BreedService : IBreedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyList<BreedProfileResponse> _breeds;

    public BreedService()
    {
        _breeds = LoadBreedsFromSeed();
    }

    public Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(breedId))
        {
            return Task.FromResult<BreedProfileResponse?>(null);
        }

        var match = _breeds.FirstOrDefault(b => b.Id.Equals(breedId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<BreedProfileResponse>> GetAllBreedsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_breeds);
    }

    private static IReadOnlyList<BreedProfileResponse> LoadBreedsFromSeed()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DogWorld.Breeds.Api.Data.breeds-seed.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            var breedsFromResource = JsonSerializer.Deserialize<List<BreedProfileResponse>>(stream, JsonOptions);
            if (breedsFromResource != null)
            {
                return breedsFromResource.AsReadOnly();
            }
        }

        var fallbackPath = Path.Combine(AppContext.BaseDirectory, "Data", "breeds-seed.json");
        if (File.Exists(fallbackPath))
        {
            var jsonText = File.ReadAllText(fallbackPath);
            var breedsFromFile = JsonSerializer.Deserialize<List<BreedProfileResponse>>(jsonText, JsonOptions);
            if (breedsFromFile != null)
            {
                return breedsFromFile.AsReadOnly();
            }
        }

        return [];
    }
}
