# Project definition

## Purpose

Build enough application behaviour to learn correlated traces, latency percentiles, failure diagnosis, KQL, dashboards, and alerts. Keep business scope small.

## Agreed scope

- Blazor WebAssembly frontend, separate ASP.NET Core API, SQL Server, and a later external breed-information API.
- Browse available dogs, view a dog, search by breed, record adoption interest, and retrieve breed information.
- Dog status is Available or Adopted. Only Available dogs appear in the available-dog list.
- Adoption requests record interest only: multiple requests per dog are allowed and requests never change dog status.
- No identity, reservation, or approval workflow. Use synthetic applicant data.
- Run locally with telemetry sent to Azure. Introduce containerization incrementally; Docker is installed and SQL Server is available.
- The learner creates the Application Insights resource. No Azure resources are provisioned by the scaffold.
- Maximum Azure spending target: GBP 3 per month. Budget alerts do not enforce a hard cap. Verify pricing and plan ingestion limits, exercise duration, and shutdown steps before chargeable exercises.
- Study time: three hours daily (30 minutes course, 90 minutes implementation, 45 minutes telemetry investigation, 15 minutes notes), adjustable as needed.

## Invariants

- Reads do not mutate data.
- Controlled delays and failures are disabled by default and restricted to local exercises.
- Responses and telemetry exclude credentials, connection strings, and applicant details.
- Diagnostic exercises use repeatable workloads and recorded measurement settings.

## Milestones

1. Scaffold and verify the empty solution.
2. Implement the available-dog feature through Red/Green cycles.
3. Instrument and validate API/SQL telemetry and correlation.
4. Measure baseline and tail latency; investigate controlled failures.
5. Add external dependency and browser tracing exercises as the application grows.
6. Repeat diagnostics in containers and build KQL queries, dashboards, and alerts.
7. Plan availability testing and Grafana exercises, including connectivity and cost review. Azure availability probes require an intentional way to reach a locally hosted application.

## Initial technical decisions

- Use the installed .NET 9 SDK, pinned by global.json, for compatibility with Visual Studio 2022 17.14. The original .NET 10 scaffold required a newer IDE; revisit the framework when upgrading Visual Studio.
- Keep persistence within the API initially rather than adding Domain/Application/Infrastructure projects.
- Use separate xUnit unit and integration test projects. Add integration hosting/database tooling when actual tests require it.
- Defer instrumentation package choice, external API selection, Docker configuration, and database configuration until their milestones.
