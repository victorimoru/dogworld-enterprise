namespace DogWorld.Vaccinations.Api.Models;

public sealed class Vaccination
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateOnly AdministeredOn { get; set; }
    public DateOnly? NextDueOn { get; set; }
}
