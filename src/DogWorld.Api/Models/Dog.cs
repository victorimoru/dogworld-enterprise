namespace DogWorld.Api.Models;

public class Dog
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public int AgeInMonths { get; set; }
    public DogStatus Status { get; set; }
}
