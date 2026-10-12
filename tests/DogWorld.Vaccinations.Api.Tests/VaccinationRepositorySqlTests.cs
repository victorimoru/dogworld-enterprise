using DogWorld.Vaccinations.Api.Data;
using DogWorld.Vaccinations.Api.Models;
using DogWorld.Vaccinations.Api.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Vaccinations.Api.Tests;

public class VaccinationRepositorySqlTests : IAsyncLifetime
{
    private readonly string databaseName = $"DogWorldVaccinationTests_{Guid.NewGuid():N}";
    private string connectionString = null!;
    private bool created;

    public async Task InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set DOGWORLD_TEST_SQLSERVER for SQL tests."));
        if (!string.IsNullOrEmpty(connection.AttachDBFilename)) throw new InvalidOperationException("File-attached databases are not supported.");
        connection.InitialCatalog = databaseName;
        connection.Pooling = false;
        connectionString = connection.ConnectionString;
        await using var context = Context();
        created = await context.Database.EnsureCreatedAsync();
        Assert.True(created);
    }

    [Fact]
    public async Task FiltersOrdersAndDoesNotTrackOrModifyRecords()
    {
        await using var context = Context();
        var first = Record(7, "Rabies", new DateOnly(2025, 3, 1));
        var second = Record(7, "Booster", new DateOnly(2025, 3, 1));
        var older = Record(7, "Initial", new DateOnly(2024, 1, 1));
        first.NextDueOn = new DateOnly(2026, 3, 1);
        context.Vaccinations.AddRange(older, Record(8, "Other dog", new DateOnly(2026, 1, 1)), first, second);
        await context.SaveChangesAsync();
        var expected = new[] { first, second }.OrderBy(v => v.Id).Append(older).Select(v => v.Id).ToArray();
        context.ChangeTracker.Clear();

        var result = await new VaccinationRepository(context).GetByDogIdAsync(7, default);

        Assert.Equal(expected, result.Select(v => v.Id));
        Assert.All(result, v => Assert.Equal(7, v.DogId));
        var rabies = Assert.Single(result, v => v.VaccineName == "Rabies");
        Assert.Equal(new DateOnly(2025, 3, 1), rabies.AdministeredOn);
        Assert.Equal(new DateOnly(2026, 3, 1), rabies.NextDueOn);
        Assert.Null(Assert.Single(result, v => v.VaccineName == "Initial").NextDueOn);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(4, await context.Vaccinations.CountAsync());
    }

    [Fact]
    public async Task NoRecordsReturnsEmptyHistory()
    {
        await using var context = Context();
        Assert.Empty(await new VaccinationRepository(context).GetByDogIdAsync(42, default));
    }

    [Fact]
    public async Task CancelledRequestPropagatesCancellation()
    {
        await using var context = Context();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new VaccinationRepository(context).GetByDogIdAsync(42, cancellation.Token));
    }

    private static Vaccination Record(int dogId, string name, DateOnly date) => new()
    { DogId = dogId, VaccineName = name, AdministeredOn = date };
    private VaccinationDbContext Context() => new(new DbContextOptionsBuilder<VaccinationDbContext>().UseSqlServer(connectionString).Options);
    public async Task DisposeAsync()
    {
        if (!created) return;
        await using var context = Context();
        Assert.Equal(databaseName, context.Database.GetDbConnection().Database);
        await context.Database.EnsureDeletedAsync();
    }
}
