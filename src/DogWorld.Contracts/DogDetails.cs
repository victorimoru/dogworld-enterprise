namespace DogWorld.Contracts;

public sealed record DogDetails(int Id, string Name, string Breed, int AgeInMonths, string Status)
{
    // null means unavailable; an empty list means no recorded history.
    public IReadOnlyList<VaccinationRecordResponse>? Vaccinations { get; init; }

    public BreedProfileResponse? BreedDetails { get; init; }
}
