# SQL Server integration tests (Red stage)

The SQL tests exercise the proposed real DogRepository and DogWorldDbContext using
the SQL Server EF Core provider. No repository, model, context, schema, or endpoint
implementation is included here. The project currently fails compilation because
those production types are absent. Assertions have not executed against SQL Server.
Even a test filter for the endpoint cannot bypass this project's compilation errors.

## Test cases

- Persist mixed statuses and retrieve only Available dogs, including their display fields.
- Order by Name, then database-generated integer Id for duplicate names.
- Return an empty collection from an empty database.
- Return an empty collection when only Adopted dogs exist.
- Leave persisted dog values and row count unchanged, with no pending tracked changes.

Each case gets a new GUID-named database. Seed, query, and verification use separate
contexts, so the tests actually cross the SQL persistence boundary. Name collation
edge cases remain outside the current simple-name examples.

## Running once production contracts exist

Use a local development SQL Server instance (installed or running in Docker) and
an account allowed to create and drop test databases. No Docker image is started
automatically. Configure the connection only in the current terminal environment;
never commit credentials. Example using Windows authentication:

```powershell
$env:DOGWORLD_TEST_SQLSERVER = 'Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/DogWorld.IntegrationTests/DogWorld.IntegrationTests.csproj --filter Category=SqlServer
```

Replace localhost with your local instance name if needed. TrustServerCertificate
is for the local lab only. The fixture replaces Initial Catalog with its generated
DogWorldTests_<GUID> name; it never uses a configured application's database.
File attachment connections are rejected. Missing configuration fails explicitly.

The fixture creates the schema with EnsureCreated and deletes its own database
after each case. This validates model-based SQL behaviour, not migrations. Abrupt
process termination or failed setup/cleanup may leave a DogWorldTests_<GUID>
database; inspect and remove only a database verified to belong to this test run.

After types compile, first verify genuine behavioural failures before Green.
An unavailable SQL instance, authentication error, or schema setup failure is an
environment/setup failure, not proof of the expected repository Red behaviour.
