# DogWorld

A dog-adoption application for learning Application Insights and observability alongside [Azure Application Insights: APM from Zero to Production](https://www.udemy.com/course/azure-application-insights-apm-from-zero-to-production/).

## Current features

- Blazor WebAssembly dog catalogue, pagination, and dog details.
- Dog API backed by SQL Server, with breed and vaccination enrichment from downstream APIs.
- Downstream retries, timeouts, breed caching, and partial details when enrichment is unavailable.
- Correlated browser, API, HTTP dependency, and SQL telemetry in Application Insights.
- Development-only failure scenarios for retry recovery, exhausted retries, and timeouts.

## Run locally

Use .NET SDK 9.0.318 (see global.json), Visual Studio 2022 17.14 with the ASP.NET workload if using Visual Studio, and local SQL Server. Open DogWorld.sln. Docker is optional.

From the repository root:

```powershell
dotnet restore DogWorld.sln
dotnet build DogWorld.sln --no-restore
```

Configure the local SQL databases and apply migrations before starting. See [migration and seeding instructions](docs/migrations-and-demo-data.md). Local connections use Windows authentication (SSPI).

Run each application in a separate terminal:

```powershell
dotnet run --project src/DogWorld.Breeds.Api --launch-profile http
dotnet run --project src/DogWorld.Vaccinations.Api --launch-profile http
dotnet run --project src/DogWorld.Api --launch-profile http
dotnet run --project src/DogWorld.Web --launch-profile http
```

Open http://localhost:5248. The Dog API exposes Swagger at http://localhost:5157/swagger in Development.

## Desktop app

The Avalonia desktop catalogue uses the same Dog API, with 12 dogs per page,
local default illustrations, and loading, empty, error, and retry states.
Start DogWorld.Api, then run `dotnet run --project src/DogWorld.Desktop`.
See [desktop setup and acceptance criteria](docs/desktop-catalogue.md).

## Observability

Follow [distributed tracing setup](docs/distributed-tracing.md) to configure your own Application Insights resource and inspect correlated transactions. Resource settings are stored in Git-ignored local files; telemetry is disabled by default in tracked configuration.

Use [the resilience lab](docs/resilience-lab.md) to simulate downstream failures and inspect retry attempts and graceful fallback. Stop the simulated service and restart normally when finished.

## Structure

| Project | Responsibility |
| --- | --- |
| src/DogWorld.Web | Blazor WebAssembly frontend |
| src/DogWorld.Api | Dog API, SQL persistence, downstream aggregation and resilience |
| src/DogWorld.Breeds.Api | Breed information API |
| src/DogWorld.Vaccinations.Api | Vaccination history API and SQL persistence |
| src/DogWorld.Contracts | Shared request and response contracts |
| src/Shared | Development fault middleware compiled into downstream APIs |
| tests/ | Unit, endpoint, UI, correlation, and SQL integration tests |

## Working agreements

Define → compare structural options → scaffold → Red → compare feature approaches → Green → Challenge → Refactor → Verify independently → Own the result.

See [project definition](docs/project-definition.md), [first-feature acceptance criteria](docs/acceptance-criteria.md), and [observability experiments](docs/observability-experiments.md). Earlier verification documents describe their original increments. SQL integration tests require their documented database setup. Never commit credentials or real applicant data.
