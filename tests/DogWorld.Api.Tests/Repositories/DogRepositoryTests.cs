using DogWorld.Api.Data;
using DogWorld.Api.Models;
using DogWorld.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Api.Tests.Repositories;

// These isolated tests check repository behaviour, not SQL Server translation.
// SQL Server integration tests remain necessary before claiming persistence correctness.
public class DogRepositoryTests
{
    [Fact]
    public async Task GetAvailableDogsAsync_ExcludesAdoptedDogs()
    {
        using var context = CreateContext();
        context.Dogs.AddRange(
            CreateDog(1, "Bella", DogStatus.Available),
            CreateDog(2, "Max", DogStatus.Adopted),
            CreateDog(3, "Zoe", DogStatus.Available));
        await context.SaveChangesAsync();
        var repository = new DogRepository(context);

        var result = await repository.GetAvailableDogsAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 3 }, result.Select(dog => dog.Id));
        Assert.All(result, dog => Assert.Equal(DogStatus.Available, dog.Status));
    }

    [Fact]
    public async Task GetAvailableDogsAsync_OrdersByNameThenId()
    {
        using var context = CreateContext();
        context.Dogs.AddRange(
            CreateDog(3, "Zoe", DogStatus.Available),
            CreateDog(2, "Bella", DogStatus.Available),
            CreateDog(1, "Bella", DogStatus.Available));
        await context.SaveChangesAsync();
        var repository = new DogRepository(context);

        var result = await repository.GetAvailableDogsAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(dog => dog.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAvailableDogsAsync_WhenNoDogsAreAvailable_ReturnsEmpty(bool hasAdoptedDog)
    {
        using var context = CreateContext();
        if (hasAdoptedDog)
        {
            context.Dogs.Add(CreateDog(1, "Bella", DogStatus.Adopted));
            await context.SaveChangesAsync();
        }
        var repository = new DogRepository(context);

        var result = await repository.GetAvailableDogsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private static DogWorldDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DogWorldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DogWorldDbContext(options);
    }

    private static Dog CreateDog(int id, string name, DogStatus status) => new()
    {
        Id = id,
        Name = name,
        Breed = "Beagle",
        AgeInMonths = 24,
        Status = status
    };
}
