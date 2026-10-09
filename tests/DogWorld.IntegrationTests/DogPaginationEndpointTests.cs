using System.Net;
using System.Net.Http.Json;
using DogWorld.Api.Data;
using DogWorld.Api.Models;
using DogWorld.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.IntegrationTests;

[Trait("Category", "SqlServer")]
public class DogPaginationEndpointTests : IAsyncLifetime
{
    private readonly string databaseName = $"DogWorldPaginationTests_{Guid.NewGuid():N}";
    private string connectionString = null!;
    private bool created;
    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set DOGWORLD_TEST_SQLSERVER.");
        var connection = new SqlConnectionStringBuilder(configured);
        if (!string.IsNullOrEmpty(connection.AttachDBFilename)) throw new InvalidOperationException("File-attached databases are not supported.");
        connection.InitialCatalog = databaseName;
        connection.Pooling = false;
        connectionString = connection.ConnectionString;
        await using var context = CreateContext();
        created = await context.Database.EnsureCreatedAsync();
        Assert.True(created);
        context.Dogs.AddRange(Enumerable.Range(1, 30).Reverse().Select(id => new Dog
        { Name = $"Dog {id:D2}", Breed = "Beagle", AgeInMonths = 12, Status = DogStatus.Available }));
        context.Dogs.Add(new Dog { Name = "Aaron", Breed = "Poodle", Status = DogStatus.Adopted });
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(1, 12, "Dog 01")]
    [InlineData(2, 12, "Dog 13")]
    [InlineData(3, 6, "Dog 25")]
    [InlineData(4, 0, null)]
    public async Task ReturnsRequestedSliceAndAvailableTotal(int page, int count, string? first)
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync($"/api/dogs?page={page}&pageSize=12");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("30", Assert.Single(response.Headers.GetValues("X-Total-Count")));
        var dogs = await response.Content.ReadFromJsonAsync<List<AvailableDog>>();
        Assert.NotNull(dogs);
        Assert.Equal(count, dogs.Count);
        if (first is not null) Assert.Equal(first, dogs[0].Name);
        Assert.DoesNotContain(dogs, dog => dog.Name == "Aaron");
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task InvalidPaginationReturnsBadRequest(string query)
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync($"/api/dogs?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
