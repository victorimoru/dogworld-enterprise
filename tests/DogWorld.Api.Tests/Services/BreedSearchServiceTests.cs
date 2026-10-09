using DogWorld.Api.Repositories;
using DogWorld.Api.Services;
using DogWorld.Contracts;

namespace DogWorld.Api.Tests.Services;

public class BreedSearchServiceTests
{
    [Theory]
    [InlineData("  beagle  ", "beagle")]
    [InlineData("bEaG", "bEaG")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public async Task NormalizesSearchAndForwardsPageAndCancellation(string? input, string? expected)
    {
        using var cancellation = new CancellationTokenSource();
        var repository = new Stub();
        var service = new DogPageService(repository);
        var result = await service.GetPageAsync(2, 12, input, cancellation.Token);
        Assert.Equal(expected, repository.Breed);
        Assert.Equal(2, repository.Page);
        Assert.Equal(12, repository.PageSize);
        Assert.Equal(cancellation.Token, repository.Token);
        Assert.Same(repository.Result, result);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task StorageFailureIsNotDisguisedAsNoMatches()
    {
        var failure = new InvalidOperationException("Storage unavailable");
        var service = new DogPageService(new Stub { Failure = failure });
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPageAsync(1, 12, "Beagle", default));
        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new DogPageService(new Stub());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetPageAsync(1, 12, "Beagle", cancellation.Token));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task InvalidPaginationDoesNotCallRepository(int page, int size)
    {
        var repository = new Stub();
        var service = new DogPageService(repository);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPageAsync(page, size, "Beagle", default));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class Stub : IDogPageRepository
    {
        public DogPage Result { get; } = new([new AvailableDog(3, "Bella", "Beagle", 12)], 13);
        public Exception? Failure { get; init; }
        public string? Breed { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public Task<DogPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Search must use the breed-aware repository overload.");
        public Task<DogPage> GetPageAsync(int page, int pageSize, string? breed, CancellationToken cancellationToken)
        {
            Calls++;
            Page = page; PageSize = pageSize; Breed = breed; Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null ? Task.FromResult(Result) : Task.FromException<DogPage>(Failure);
        }
    }
}
