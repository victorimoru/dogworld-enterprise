using DogWorld.Api.Clients;
using DogWorld.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace DogWorld.Api.Tests.Clients;

public class CachedBreedsApiClientTests
{
    [Fact]
    public async Task SuccessIsCachedForFiveMinutesAndThenFetchedAgain()
    {
        var clock = new Clock();
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var inner = new Stub();
        var client = new CachedBreedsApiClient(inner, cache);
        var first = await client.GetBreedByIdAsync("beagle", default);
        clock.UtcNow += TimeSpan.FromMinutes(4);
        Assert.Equal(first, await client.GetBreedByIdAsync("beagle", default));
        Assert.Equal(1, inner.Calls);
        clock.UtcNow += TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1);
        await client.GetBreedByIdAsync("beagle", default);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task DifferentBreedIdsHaveSeparateEntries()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new Stub();
        var client = new CachedBreedsApiClient(inner, cache);
        Assert.Equal("beagle", (await client.GetBreedByIdAsync("beagle", default))!.Id);
        Assert.Equal("poodle", (await client.GetBreedByIdAsync("poodle", default))!.Id);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task MissingBreedIsNotCached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new Stub { Missing = true };
        var client = new CachedBreedsApiClient(inner, cache);
        Assert.Null(await client.GetBreedByIdAsync("beagle", default));
        inner.Missing = false;
        Assert.NotNull(await client.GetBreedByIdAsync("beagle", default));
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task FailureIsNotCachedAndRecoveryCanSucceed()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new Stub { Failure = new HttpRequestException("Unavailable") };
        var client = new CachedBreedsApiClient(inner, cache);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetBreedByIdAsync("beagle", default));
        inner.Failure = null;
        Assert.NotNull(await client.GetBreedByIdAsync("beagle", default));
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task CacheMissForwardsCancellation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new CachedBreedsApiClient(new Stub(), cache);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetBreedByIdAsync("beagle", cancellation.Token));
    }

    private sealed class Clock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
    }
    private sealed class Stub : IBreedsApiClient
    {
        public int Calls { get; private set; }
        public bool Missing { get; set; }
        public Exception? Failure { get; set; }
        public Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) return Task.FromException<BreedProfileResponse?>(Failure);
            return Task.FromResult(Missing ? null : new BreedProfileResponse(breedId, breedId, "Group", "Friendly", "12 years", "10 kg", "England"));
        }
    }
}
