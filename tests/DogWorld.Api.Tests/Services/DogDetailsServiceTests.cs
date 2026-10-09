using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;

namespace DogWorld.Api.Tests.Services;

public class DogDetailsServiceTests
{
    [Fact]
    public async Task MapsAdoptedDogAndForwardsIdAndToken()
    {
        using var cancellation = new CancellationTokenSource();
        var repository = new Stub();
        var result = await new DogDetailsService(repository).GetByIdAsync(7, cancellation.Token);
        Assert.NotNull(result);
        Assert.Equal(7, repository.Id);
        Assert.Equal(cancellation.Token, repository.Token);
        Assert.Equal("Adopted", result.Status);
        Assert.Equal("Bella", result.Name);
    }

    [Fact]
    public async Task MissingDogReturnsNull()
    {
        Assert.Null(await new DogDetailsService(new Stub { Missing = true }).GetByIdAsync(5, default));
    }

    private sealed class Stub : IDogDetailsRepository
    {
        public bool Missing { get; init; }
        public int Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<Dog?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            Id = id; Token = cancellationToken;
            return Task.FromResult(Missing ? null : new Dog { Id = id, Name = "Bella", Breed = "Poodle", AgeInMonths = 14, Status = DogStatus.Adopted });
        }
    }
}
