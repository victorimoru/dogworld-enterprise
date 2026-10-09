using System.Net;
using System.Text.Json;
using DogWorld.Api.Data;
using DogWorld.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.IntegrationTests;

[Trait("Category", "SqlServer")]
public class DogDetailsEndpointTests : IAsyncLifetime
{
    private readonly string databaseName = $"DogWorldDetailsTests_{Guid.NewGuid():N}";
    private string connectionString = null!;
    private bool created;
    private Dog selected = null!;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set DOGWORLD_TEST_SQLSERVER to a local development SQL Server connection.");
        var connection = new SqlConnectionStringBuilder(configured);
        if (!string.IsNullOrEmpty(connection.AttachDBFilename))
            throw new InvalidOperationException("File-attached databases are not supported.");
        connection.InitialCatalog = databaseName;
        connection.Pooling = false;
        connectionString = connection.ConnectionString;
        await using var context = CreateContext();
        created = await context.Database.EnsureCreatedAsync();
        Assert.True(created);
        selected = new Dog { Name = "Bella", Breed = "Poodle", AgeInMonths = 14, Status = DogStatus.Available };
        context.Dogs.AddRange(
            new Dog { Name = "Max", Breed = "Beagle", AgeInMonths = 36, Status = DogStatus.Available },
            selected);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ExistingId_ReturnsSelectedDogDetails_WithoutChangingStoredData()
    {
        using var host = CreateHost();
        using var client = host.CreateClient();

        using var response = await client.GetAsync($"/api/dogs/{selected.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var dog = json.RootElement;
        Assert.Equal(selected.Id, dog.GetProperty("id").GetInt32());
        Assert.Equal("Bella", dog.GetProperty("name").GetString());
        Assert.Equal("Poodle", dog.GetProperty("breed").GetString());
        Assert.Equal(14, dog.GetProperty("ageInMonths").GetInt32());
        Assert.Equal("Available", dog.GetProperty("status").GetString());

        await using var context = CreateContext();
        Assert.Equal(2, await context.Dogs.CountAsync());
        var stored = await context.Dogs.SingleAsync(d => d.Id == selected.Id);
        Assert.Equal(selected.Name, stored.Name);
        Assert.Equal(selected.Breed, stored.Breed);
        Assert.Equal(selected.AgeInMonths, stored.AgeInMonths);
        Assert.Equal(selected.Status, stored.Status);
    }

    [Fact]
    public async Task MissingId_ReturnsNotFound()
    {
        using var host = CreateHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/api/dogs/2147483647");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private DogWorldDbContext CreateContext() => new(new DbContextOptionsBuilder<DogWorldDbContext>()
        .UseSqlServer(connectionString).Options);

    private AvailableDogsChallengeTests.IChallengeHost CreateHost()
    {
        var entryPoint = typeof(DogWorldDbContext).Assembly.EntryPoint!.DeclaringType!;
        var hostType = typeof(AvailableDogsChallengeTests.ChallengeHost<>).MakeGenericType(entryPoint);
        return (AvailableDogsChallengeTests.IChallengeHost)Activator.CreateInstance(hostType, connectionString, null)!;
    }

    public async Task DisposeAsync()
    {
        if (!created) return;
        await using var context = CreateContext();
        Assert.Equal(databaseName, context.Database.GetDbConnection().Database);
        await context.Database.EnsureDeletedAsync();
    }
}
