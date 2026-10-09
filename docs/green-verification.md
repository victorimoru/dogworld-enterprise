# First Green increment

Implemented production code only, plus setup documentation. All files under tests/
remain identical to the Red commit.

- Dog and DogStatus model the agreed fields.
- DogService delegates to IDogRepository, preserving cancellation and exceptions.
- DogRepository uses SQL-translatable filtering and name/ID ordering with AsNoTracking.
- DogWorldDbContext exposes Dogs and uses convention-based integer identity keys.
- DogsController handles GET /api/dogs and uses the service through scoped dependency injection; controller routing replaces the original minimal endpoint.
- Exception middleware provides generic problem responses; no exception details are returned.
- Optional Development-only schema initialization uses EnsureCreated for the local lab.
- Development-only Swagger UI at /swagger and OpenAPI at /swagger/v1/swagger.json use Swashbuckle.AspNetCore 9.0.6. API launch profiles open Swagger.

## Verified

After Swagger was added, the Release build passed with zero warnings/errors and all
14 unchanged tests passed. HTTP smoke checks confirmed both Swagger UI and its
OpenAPI document return 200, with GET /api/dogs present. Release was used because
the running Debug API locked its executable; that user process was left running.

Solution build: zero warnings and errors. Eight unit tests and six integration tests
passed against the existing local SQL Server. SQL repository tests created and removed
their own GUID databases. The endpoint used a separate GUID database, removed after
the run. No Docker or Azure resources were used.

## Reproduce

From the repository root, configure your local SQL Server using an account with
create/drop permissions. Never point the API test connection at application data.

```powershell
$env:DOGWORLD_TEST_SQLSERVER = 'Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$testDatabase = 'DogWorldApiTests_' + [Guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DogWorld = $env:DOGWORLD_TEST_SQLSERVER + ';Database=' + $testDatabase
$env:Database__Initialize = 'true'
dotnet test DogWorld.sln
```

The repository fixture cleans its own databases. The endpoint test does not own a
database fixture: after the run, remove only the database named in $testDatabase
using your SQL administration tool. Stop the API/test host before cleanup. The
WebApplicationFactory host runs in Development, enabling explicit schema setup.

Existing tests do not yet verify API response fields, generic error responses,
browser behaviour, telemetry, or migrations. Those remain future work; a passing
suite does not mean the entire available-dog feature is complete.
