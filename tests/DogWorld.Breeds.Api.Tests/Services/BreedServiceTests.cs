using DogWorld.Contracts;
using DogWorld.Breeds.Api.Services;

namespace DogWorld.Breeds.Api.Tests.Services;

public class BreedServiceTests
{
    [Fact]
    public async Task GetBreedByIdAsync_WhenBreedExists_ReturnsBreedProfile()
    {
        // Arrange
        var service = new BreedService();
        var breedId = "golden-retriever";

        // Act
        var result = await service.GetBreedByIdAsync(breedId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("golden-retriever", result.Id);
        Assert.Equal("Golden Retriever", result.Name);
        Assert.Equal("Sporting", result.BreedGroup);
        Assert.Equal("Friendly, Intelligent, Devoted", result.Temperament);
    }

    [Fact]
    public async Task GetAllBreedsAsync_ReturnsAllSeededBreeds()
    {
        // Arrange
        var service = new BreedService();

        // Act
        var result = await service.GetAllBreedsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
