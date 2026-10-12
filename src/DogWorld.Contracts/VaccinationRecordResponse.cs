namespace DogWorld.Contracts;

public sealed record VaccinationRecordResponse(int Id, int DogId, string VaccineName, DateOnly AdministeredOn, DateOnly? NextDueOn);
