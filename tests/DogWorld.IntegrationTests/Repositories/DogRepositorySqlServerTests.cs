using DogWorld.Api.Data;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.IntegrationTests.Repositories;

[Trait("Category", "SqlServer")]
public class DogRepositorySqlServerTests : IAsyncLifetime
{
    private readonly string _databaseName = $"DogWorldTests_{Guid.NewGuid():N}";
    private DbContextOptions<DogWorldDbContext> _options = null!;
    private bool _databaseCreated;

    public async Task InitializeAsync()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            throw new InvalidOperationException(
                "Set DOGWORLD_TEST_SQLSERVER to a local development SQL Server connection. " +
                "Tests create and delete their own uniquely named database.");
        }

        var connection = new SqlConnectionStringBuilder(configuredConnection);
        if (!string.IsNullOrWhiteSpace(connection.AttachDBFilename))
        {
            throw new InvalidOperationException("File-attached databases are not supported by this test fixture.");
        }

        // Never use the configured application's database or shared connection pool.
        connection.InitialCatalog = _databaseName;
        connection.Pooling = false;
        _options = new DbContextOptionsBuilder<DogWorldDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;

        await using var context = CreateContext();
        _databaseCreated = await context.Database.EnsureCreatedAsync();
        Assert.True(_databaseCreated, "Expected a newly created, isolated test database.");
    }

    public async Task DisposeAsync()
    {
        if (!_databaseCreated)
            return;

        await using var context = CreateContext();
        Assert.Equal(_databaseName, context.Database.GetDbConnection().Database);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task GetAvailableDogsAsync_ReadsOnlyAvailableDogsFromSqlServer()
    {
        var bella = CreateDog("Bella", DogStatus.Available);
        var max = CreateDog("Max", DogStatus.Adopted);
        var zoe = CreateDog("Zoe", DogStatus.Available);
        await SeedAsync(bella, max, zoe);

        // A new context forces a database read rather than reusing seeded entities.
        await using var context = CreateContext();
        var repository = new DogRepository(context);
        var result = await repository.GetAvailableDogsAsync(CancellationToken.None);

        Assert.Equal(new[] { bella.Id, zoe.Id }, result.Select(dog => dog.Id));
        Assert.All(result, dog => Assert.Equal(DogStatus.Available, dog.Status));
        Assert.Equal("Bella", result[0].Name);
        Assert.Equal("Beagle", result[0].Breed);
        Assert.Equal(24, result[0].AgeInMonths);
    }

    [Fact]
    public async Task GetAvailableDogsAsync_OrdersByNameThenGeneratedId()
    {
        var zoe = CreateDog("Zoe", DogStatus.Available);
        var bellaOne = CreateDog("Bella", DogStatus.Available);
        var bellaTwo = CreateDog("Bella", DogStatus.Available);
        await SeedAsync(zoe, bellaOne, bellaTwo);
        var expected = new[] { bellaOne.Id, bellaTwo.Id }.OrderBy(id => id).Append(zoe.Id);

        await using var context = CreateContext();
        var result = await new DogRepository(context).GetAvailableDogsAsync(CancellationToken.None);

        Assert.Equal(expected, result.Select(dog => dog.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAvailableDogsAsync_WhenNoneAvailable_ReturnsEmpty(bool seedAdoptedDog)
    {
        if (seedAdoptedDog)
            await SeedAsync(CreateDog("Max", DogStatus.Adopted));

        await using var context = CreateContext();
        var result = await new DogRepository(context).GetAvailableDogsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableDogsAsync_DoesNotChangeStoredDogsOrLeavePendingChanges()
    {
        await SeedAsync(CreateDog("Bella", DogStatus.Available), CreateDog("Max", DogStatus.Adopted));
        var before = await ReadSnapshotAsync();

        await using (var context = CreateContext())
        {
            await new DogRepository(context).GetAvailableDogsAsync(CancellationToken.None);
            Assert.False(context.ChangeTracker.HasChanges());
        }

        var after = await ReadSnapshotAsync();
        Assert.Equal(before, after);
    }

    private DogWorldDbContext CreateContext() => new(_options);

    private async Task SeedAsync(params Dog[] dogs)
    {
        await using var context = CreateContext();
        context.Dogs.AddRange(dogs);
        await context.SaveChangesAsync();
    }

    private async Task<DogSnapshot[]> ReadSnapshotAsync()
    {
        await using var context = CreateContext();
        var dogs = await context.Dogs.AsNoTracking().OrderBy(dog => dog.Id).ToListAsync();
        return dogs.Select(dog => new DogSnapshot(dog.Id, dog.Name, dog.Breed, dog.AgeInMonths, dog.Status)).ToArray();
    }

    private static Dog CreateDog(string name, DogStatus status) => new()
    {
        Name = name,
        Breed = "Beagle",
        AgeInMonths = 24,
        Status = status
    };

    private sealed record DogSnapshot(int Id, string Name, string Breed, int AgeInMonths, DogStatus Status);
}
