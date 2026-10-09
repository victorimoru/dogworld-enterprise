namespace DogWorld.Contracts;

public sealed record BreedProfileResponse(
    string Id,
    string Name,
    string BreedGroup,
    string Temperament,
    string LifeExpectancy,
    string WeightRange,
    string Origin
);
