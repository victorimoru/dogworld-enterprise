# Vaccination history

The read-only API and SQL Server persistence are implemented. Vaccination records
belong to a separate DogWorldVaccinations database. Dog IDs are references only:
there are no cross-database joins, foreign keys, or Dog API existence lookups.

## Contract

GET /api/dogs/{dogId}/vaccinations

- Positive ID with records: 200 with records, ordered by administeredOn descending,
  then id ascending.
- Positive ID without records (including unknown dog IDs): 200 with [].
- Zero or negative ID: 400 application/problem+json.
- Storage failure: 500 Problem Details, never an empty success response.

Records contain id, dogId, vaccineName, administeredOn (YYYY-MM-DD), and nullable
nextDueOn. These are recorded facts, not a calculated clinical schedule. Reads use
no tracking and do not mutate data. Request cancellation is passed to SQL Server.

## Local setup

The Development connection string uses localhost SQL Server, Windows SSPI and the
separate DogWorldVaccinations database. From the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
dotnet ef database update --project src/DogWorld.Vaccinations.Api
dotnet run --project src/DogWorld.Vaccinations.Api --launch-profile http
```

Swagger is at http://localhost:5393/swagger. Migrations are explicit; application
startup does not create or modify the database. The initial migration creates an
empty table, an index for ordered dog-history queries, and constraints requiring
positive dog IDs and next-due dates no earlier than administration dates.

SeedDemoVaccinations loads 50 synthetic records from the embedded, immutable Data/SeedData/vaccinations.v1.json resource: two per dog ID 1–25. These demo dates are not clinical recommendations. Seed IDs 100001–100050 are reserved; conflicts fail rather than overwrite existing records. Rolling back this migration deletes those seed IDs, including any subsequent edits to those records. An empty history does not establish that a dog
exists or that it is unvaccinated.

## Verification

The original three endpoint cases are unchanged. Three SQL repository tests cover
filtering, ordering, nullable dates, read-only behavior, empty history and cancellation.
Repository tests create and delete their own GUID-named databases. The SQL account
must have database creation/deletion rights on a local development instance.

Use a dedicated empty endpoint test database (not an application database):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOGWORLD_TEST_SQLSERVER = 'Server=localhost;Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True'
$env:ConnectionStrings__Vaccinations = 'Server=localhost;Database=DogWorldVaccinationsEndpointTests;Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True'
dotnet ef database update --project src/DogWorld.Vaccinations.Api
dotnet test tests/DogWorld.Vaccinations.Api.Tests -c Release
Remove-Item Env:ConnectionStrings__Vaccinations
Remove-Item Env:DOGWORLD_TEST_SQLSERVER
```

The connection-string override must be removed before running the development API.

## Following increments

1. Seed migration is implemented; apply database update to load the demo records.
2. Test and implement a downstream client with resilience and dog-details aggregation.
3. Add UI states for records, no recorded history, and service unavailable.
4. Validate distributed traces and independent dependency failures locally.

Writes, authentication, deployment, and clinical recommendations remain outside
this increment.
