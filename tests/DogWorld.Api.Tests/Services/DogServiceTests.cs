using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;

namespace DogWorld.Api.Tests.Services;

public class DogServiceTests
{
    [Fact]
    public async Task GetAvailableDogsAsync_ReturnsRepositoryResults()
    {
        var dog = new Dog { Id = 1, Name = "Bella", Breed = "Beagle", AgeInMonths = 24, Status = DogStatus.Available };
        var repository = new StubDogRepository { Dogs = [dog] };
        var service = new DogService(repository);

        var result = await service.GetAvailableDogsAsync(CancellationToken.None);

        var actual = Assert.Single(result);
        Assert.Equal(dog.Id, actual.Id);
        Assert.Equal(dog.Name, actual.Name);
        Assert.Equal(dog.Breed, actual.Breed);
        Assert.Equal(dog.AgeInMonths, actual.AgeInMonths);
    }

    [Fact]
    public async Task GetAvailableDogsAsync_WhenRepositoryIsEmpty_ReturnsEmptyCollection()
    {
        var service = new DogService(new StubDogRepository());

        var result = await service.GetAvailableDogsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableDogsAsync_WhenRepositoryFails_DoesNotDisguiseFailureAsEmptyResults()
    {
        var failure = new InvalidOperationException("Simulated storage failure");
        var service = new DogService(new StubDogRepository { Failure = failure });

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetAvailableDogsAsync(CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task GetAvailableDogsAsync_ForwardsCancellationToRepository()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubDogRepository();
        var service = new DogService(repository);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetAvailableDogsAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, repository.ReceivedToken);
    }

    private sealed class StubDogRepository : IDogRepository
    {
        public IReadOnlyList<Dog> Dogs { get; init; } = [];
        public Exception? Failure { get; init; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<Dog>> GetAvailableDogsAsync(CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null
                ? Task.FromResult(Dogs)
                : Task.FromException<IReadOnlyList<Dog>>(Failure);
        }
    }
}
