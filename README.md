# DogWorld

A small dog-adoption application for learning Application Insights and observability alongside [Azure Application Insights: APM from Zero to Production](https://www.udemy.com/course/azure-application-insights-apm-from-zero-to-production/).

## Current state

The first Green increment implements `GET /api/dogs` through DogService and an EF Core SQL Server repository. It returns Available dogs ordered by name then ID, without tracking or changing them. All 14 existing tests pass (8 unit, 1 endpoint, 5 SQL Server cases); test files were not modified. Test-directory README files describe the historical Red stage.

The frontend displays available dogs with loading, empty, error, and retry states. See [UI setup](docs/available-dogs-ui.md). Migrations and 50 demo dogs are available; telemetry, containers, and remaining business features are not implemented. See [migration and seeding instructions](docs/migrations-and-demo-data.md).

## Prerequisites and local commands

- Visual Studio 2022 17.14 with the ASP.NET and web development workload, if using Visual Studio. Open DogWorld.sln.
- .NET SDK 9.0.318 (or a later patch in the same feature band; see global.json).
- SQL Server is required to run the API and integration tests. Docker is optional if SQL Server is already installed.

Run from the repository root:

```powershell
dotnet restore DogWorld.sln
dotnet build DogWorld.sln --no-restore
$env:ConnectionStrings__DogWorld = 'Server=localhost;Database=DogWorldLabMigrations;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:Database__Initialize = 'true'
dotnet run --project src/DogWorld.Api --launch-profile http
```

In another terminal:

```powershell
dotnet run --project src/DogWorld.Web --launch-profile http
```

For the connected UI, run both projects with --launch-profile https; the frontend calls the API at https://localhost:7192/. See docs/available-dogs-ui.md.

In Development, open `/swagger` on the API URL to explore and call `GET /api/dogs`.
The generated OpenAPI document is at `/swagger/v1/swagger.json`. Swagger is disabled
outside Development. The API launch profiles open Swagger automatically.

Use a new dedicated local database. `Database__Initialize=true` applies migrations only in Development. The seed migrations read embedded JSON; `UpdateDemoDogsFromJson` applies full dog records directly by ID through EF Core UpdateData. Disable it after setup. Without configuration the API does not fall back to fake data. TrustServerCertificate is for the local lab only. See [Green verification](docs/green-verification.md) for test configuration.

## Structure

| Project | Responsibility |
| --- | --- |
| src/DogWorld.Web | Standalone Blazor WebAssembly frontend |
| src/DogWorld.Api | ASP.NET Core API, service, repository, and SQL Server context |
| src/DogWorld.Contracts | Shared request and response types; currently empty |
| tests/DogWorld.Api.Tests | 8 service and repository unit cases |
| tests/DogWorld.IntegrationTests | 1 endpoint and 5 SQL Server cases |

Web and API reference Contracts. Both test projects reference API. No additional architecture layers are introduced at this stage.

## Working agreements

Read [project definition](docs/project-definition.md), [first-feature acceptance criteria](docs/acceptance-criteria.md), and [observability experiments](docs/observability-experiments.md).

Define → compare structural options → scaffold → Red → compare feature approaches → Green → Challenge → Refactor → Verify independently → Own the result.

The existing tests now pass. Remaining UI and observability acceptance criteria still require their own Red/Green cycles. Never commit credentials or real applicant data.
