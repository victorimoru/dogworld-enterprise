# API challenge verification

Removed the unfinished GetDogsWithDiagnosticDelayAsync interface/service method and
diagnostics/simulate controller action. Diagnostic delays and SQL errors for these
tests are injected only through test-owned EF interceptors.

Four new API tests exercise the actual controller, service, repository, and SQL Server:

- SQL failure returns generic HTTP 500 ProblemDetails without internal diagnostics.
- JSON contains the expected fields, excludes adopted dogs, and orders by name/ID.
- Empty storage returns HTTP 200 with an empty JSON array.
- Client cancellation reaches SQL command execution; the test waits for EF's command
  cancellation callback rather than checking only the client task.

Each new test uses its own GUID database and removes it after execution. The test
host runs in Production, disables telemetry, and does not initialize migrations.

Application Insights registration now requires a configured connection string and
can be disabled with ApplicationInsights:Enabled=false. Existing local telemetry
configuration is retained. Startup migration initialization remains disabled as in
the current application configuration/code; prepare the demo database with dotnet ef.

Run all tests from the solution directory with the already migrated demo database:

```powershell
$env:DOGWORLD_TEST_SQLSERVER = 'Server=localhost;Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True'
$env:ConnectionStrings__DogWorld = $env:DOGWORLD_TEST_SQLSERVER + ';Database=DogWorldLabMigrations'
$env:ApplicationInsights__Enabled = 'false'
dotnet test DogWorld.sln -c Release
```

The original endpoint test reads this demo database; repository/challenge tests use
separate databases. No existing test assertions were changed. These tests validate
HTTP and SQL behaviour, not delivery of telemetry to Azure.
