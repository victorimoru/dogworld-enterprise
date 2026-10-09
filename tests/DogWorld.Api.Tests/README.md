# Service and repository Red tests

Production types are deliberately absent. This project currently fails to compile;
these are test-first specifications, not executed assertion failures.

Proposed contracts expressed by the tests:

- `DogWorld.Api.Models.Dog`: integer Id, string Name/Breed, integer AgeInMonths, DogStatus Status.
- `DogStatus`: Available and Adopted.
- `IDogRepository.GetAvailableDogsAsync(CancellationToken)`: Task<IReadOnlyList<Dog>>.
- `DogService(IDogRepository)`: exposes GetAvailableDogsAsync(CancellationToken), preserves result fields, forwards cancellation, and lets storage failures reach the API error boundary.
- `DogWorldDbContext(DbContextOptions<DogWorldDbContext>)`: EF Core context exposing Dogs.
- `DogRepository(DogWorldDbContext)`: implements IDogRepository, filters Available dogs, orders by Name then Id, and returns an empty collection when none match.

These choices are proposed by the tests, not implemented production contracts.
Name collation and age validation need further specification. The ordering examples
use simple names and an explicit ID tie-breaker; they do not settle SQL collation.

Service tests use a stub repository. Repository tests use a unique EF Core InMemory
store per case. They do not verify SQL translation, constraints, transactions,
database failure behaviour, or read-only persistence; add real SQL Server integration
tests for those requirements. Do not copy the stub into production.

Run: `dotnet test tests/DogWorld.Api.Tests/DogWorld.Api.Tests.csproj`

Expected current result: compiler errors for missing Models, Services, Repositories,
and Data types. Once contracts exist, confirm behavioural assertion failures before
implementing Green. The API endpoint test previously verified expected 200 versus
actual 404. The new SQL Server test specifications now prevent the integration
project from compiling until its proposed production types exist as well.
