using System.Net;
using System.Net.Http.Json;
using DogWorld.Api.Data;
using DogWorld.Api.Models;
using DogWorld.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.IntegrationTests;

[Trait("Category", "SqlServer")]
public class BreedSearchEndpointTests : IAsyncLifetime
{
    private readonly string databaseName = $"DogWorldBreedSearchTests_{Guid.NewGuid():N}";
    private string connectionString = null!;
    private bool created;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set DOGWORLD_TEST_SQLSERVER.");
        var connection = new SqlConnectionStringBuilder(configured);
        if (!string.IsNullOrEmpty(connection.AttachDBFilename))
            throw new InvalidOperationException("File-attached databases are not supported.");
        connection.InitialCatalog = databaseName;
        connection.Pooling = false;
        connectionString = connection.ConnectionString;
        await using var context = CreateContext();
        created = await context.Database.EnsureCreatedAsync();
        Assert.True(created);
        context.Dogs.AddRange(Enumerable.Range(1, 14).Reverse().Select(id => new Dog
        {
            Name = $"Beagle {id:D2}", Breed = "Beagle", AgeInMonths = 12, Status = DogStatus.Available
        }));
        context.Dogs.AddRange(
            new Dog { Name = "Aaron", Breed = "Poodle", AgeInMonths = 18, Status = DogStatus.Available },
            new Dog { Name = "Adopted Beagle", Breed = "Beagle", AgeInMonths = 24, Status = DogStatus.Adopted },
            new Dog { Name = "Only adopted", Breed = "Boxer", AgeInMonths = 30, Status = DogStatus.Adopted });
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData("Beagle")]
    [InlineData("bEaG")]
    [InlineData("  beagle  ")]
    public async Task MatchesBreedIgnoringCaseAndSurroundingWhitespace(string term)
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync($"/api/dogs?page=1&pageSize=12&breed={Uri.EscapeDataString(term)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dogs = await response.Content.ReadFromJsonAsync<List<AvailableDog>>();
        Assert.NotNull(dogs);
        Assert.Equal(12, dogs.Count);
        Assert.All(dogs, dog => Assert.Equal("Beagle", dog.Breed));
        Assert.Equal(Enumerable.Range(1, 12).Select(id => $"Beagle {id:D2}"), dogs.Select(dog => dog.Name));
        Assert.Equal("14", Assert.Single(response.Headers.GetValues("X-Total-Count")));
    }

    [Theory]
    [InlineData("Dalmatian")]
    [InlineData("Boxer")]
    public async Task NoAvailableMatchesReturnsEmptyArrayAndZeroTotal(string term)
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync($"/api/dogs?page=1&pageSize=12&breed={term}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dogs = await response.Content.ReadFromJsonAsync<List<AvailableDog>>();
        Assert.NotNull(dogs);
        Assert.Empty(dogs);
        Assert.Equal("0", Assert.Single(response.Headers.GetValues("X-Total-Count")));
    }

    [Fact]
    public async Task FiltersBeforePaginationAndCountsOnlyAvailableMatches()
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/api/dogs?page=2&pageSize=12&breed=Beagle");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dogs = await response.Content.ReadFromJsonAsync<List<AvailableDog>>();
        Assert.NotNull(dogs);
        Assert.Equal(["Beagle 13", "Beagle 14"], dogs.Select(dog => dog.Name));
        Assert.Equal("14", Assert.Single(response.Headers.GetValues("X-Total-Count")));
    }

    private DogWorldDbContext CreateContext() => new(new DbContextOptionsBuilder<DogWorldDbContext>().UseSqlServer(connectionString).Options);
    private AvailableDogsChallengeTests.IChallengeHost CreateHost()
    {
        var type = typeof(AvailableDogsChallengeTests.ChallengeHost<>).MakeGenericType(typeof(DogWorldDbContext).Assembly.EntryPoint!.DeclaringType!);
        return (AvailableDogsChallengeTests.IChallengeHost)Activator.CreateInstance(type, connectionString, null)!;
    }
    public async Task DisposeAsync()
    {
        if (!created) return;
        await using var context = CreateContext();
        Assert.Equal(databaseName, context.Database.GetDbConnection().Database);
        await context.Database.EnsureDeletedAsync();
    }
}
