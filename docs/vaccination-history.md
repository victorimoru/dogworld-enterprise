# Vaccination history: first increment

Stage: Define / scaffold / Red. No vaccination endpoint or persistence is implemented.

## Ownership and contract

DogWorld.Vaccinations.Api owns vaccination records in its own SQL Server database
in the next increment. It references dogs by ID, with no cross-database joins or
foreign keys and no lookup into the Dog API. DogWorld.Api remains responsible for
checking whether a dog exists when aggregating its details.

GET /api/dogs/{dogId}/vaccinations:
- Positive ID, records exist: 200 with an array of records.
- Positive ID, no stored records: 200 with []. This includes unknown dog IDs;
  the service does not claim that a dog exists or is unvaccinated.
- Zero or negative ID: 400 application/problem+json.
- Storage failure: generic 5xx; never substitute an empty array for failure.

Planned record fields: id, dogId, vaccineName, administeredOn (YYYY-MM-DD),
nextDueOn (nullable YYYY-MM-DD). Dates are recorded facts; no medical schedule
or vaccination-status calculation is inferred. Order by administeredOn descending,
then id ascending for ties. Reads never mutate records.

The first Red tests cover an empty history and invalid IDs only. They run against
the real empty API host. They do not prove SQL correctness or implement fake
replacement routes. The app scaffold exists solely to host these tests.

## Following increments

1. Define repository/service contracts and add record/filter/order/read-only tests.
2. Add SQL Server integration tests using isolated test databases, then implement
   storage, migrations and synthetic seed records in a separate database.
3. Add downstream client/resilience tests and parallel aggregation with breed data.
4. Add UI tests distinguishing records, no recorded history and service unavailable.
5. Validate traces and independent dependency failures locally.

Writes, authentication, deployment and clinical recommendations are outside this
read-only feature. Do not add production endpoints that always return [] merely
to pass the initial test.

Run the first tests from the repository root:

```powershell
 dotnet test tests/DogWorld.Vaccinations.Api.Tests/DogWorld.Vaccinations.Api.Tests.csproj -c Release
```
