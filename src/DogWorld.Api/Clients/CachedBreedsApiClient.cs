using DogWorld.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace DogWorld.Api.Clients;

public sealed class CachedBreedsApiClient(IBreedsApiClient inner, IMemoryCache cache) : IBreedsApiClient
{
    public async Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breedId);
        cancellationToken.ThrowIfCancellationRequested();

        var key = (typeof(CachedBreedsApiClient), breedId.ToUpperInvariant());
        if (cache.TryGetValue(key, out BreedProfileResponse? cached)) return cached;

        var profile = await inner.GetBreedByIdAsync(breedId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (profile is not null) cache.Set(key, profile, TimeSpan.FromMinutes(5));
        return profile;
    }
}
