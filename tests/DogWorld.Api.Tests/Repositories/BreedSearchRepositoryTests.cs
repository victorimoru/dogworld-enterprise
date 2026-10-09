using DogWorld.Api.Data;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Api.Tests.Repositories;

// Isolated query behaviour only. SQL Server integration tests verify translation and collation.
public class BreedSearchRepositoryTests
{
    [Theory]
    [InlineData("Beagle")]
    [InlineData("bEaG")]
    public async Task FiltersBeforePagingAndCountsOnlyAvailableMatches(string term)
    {
        await using var context = await SeedAsync();
        var result = await new DogRepository(context).GetPageAsync(2, 1, term, default);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(3, Assert.Single(result.Dogs).Id);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(4, await context.Dogs.CountAsync());
    }

    [Theory]
    [InlineData("Dalmatian")]
    [InlineData("Boxer")]
    public async Task NoAvailableMatchesReturnsEmptyPage(string term)
    {
        await using var context = await SeedAsync();
        var result = await new DogRepository(context).GetPageAsync(1, 12, term, default);
        Assert.Empty(result.Dogs);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task NoFilterReturnsAllAvailableDogsInStableOrder()
    {
        await using var context = await SeedAsync();
        var result = await new DogRepository(context).GetPageAsync(1, 12, null, default);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { 1, 2, 3 }, result.Dogs.Select(dog => dog.Id));
    }

    private static async Task<DogWorldDbContext> SeedAsync()
    {
        var context = new DogWorldDbContext(new DbContextOptionsBuilder<DogWorldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Dogs.AddRange(
            new Dog { Id = 3, Name = "Bella", Breed = "Beagle", Status = DogStatus.Available },
            new Dog { Id = 2, Name = "Bella", Breed = "Beagle", Status = DogStatus.Available },
            new Dog { Id = 1, Name = "Aaron", Breed = "Poodle", Status = DogStatus.Available },
            new Dog { Id = 4, Name = "Adopted", Breed = "Boxer", Status = DogStatus.Adopted });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return context;
    }
}
