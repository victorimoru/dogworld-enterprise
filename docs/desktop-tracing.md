# Desktop tracing

Implementation scope: local Avalonia client, optional Azure Monitor trace export.
No Azure resources or deployments are created by this increment.

Acceptance criteria:
- Telemetry is disabled unless explicitly enabled and a connection string is present.
- Resource service name is DogWorld.Desktop.
- Catalogue and details loads create operation spans with correlated HTTP children.
- traceparent reaches Dog API and remains correlated across downstream HTTP calls.
- Success, failure, and user cancellation are distinct; cancellation is not an operation error.
- Do not record dog names, response bodies, or exception messages in operation tags.
- Keep local configuration ignored by Git; dispose the provider at desktop shutdown.
- Verify locally without sending telemetry to Azure.

## Enable when ready
Telemetry is off by default. From the repository root, configure your current
Application Insights resource (the helper writes ignored local files for all five apps):

    ./scripts/Set-AppInsightsLocal.ps1 -ConnectionString "<current connection string>"

Rebuild/restart the desktop and APIs. Desktop reads appsettings.Local.json next to
its executable; the project copies the source local file during build, but never
publishes it. Environment overrides are ApplicationInsights__Enabled and
APPLICATIONINSIGHTS_CONNECTION_STRING. An explicit false disables export even
when a connection string exists. Local JSON uses the ApplicationInsights section
with Enabled and ConnectionString properties.

Desktop exports traces only, using role DogWorld.Desktop. It does not configure
metrics or Live Metrics. Offline exporter storage is disabled. This does not
change the server SDK's telemetry or sampling settings.

## Manual verification
1. Start DogWorld.Api, DogWorld.Breeds.Api and DogWorld.Vaccinations.Api.
2. Start the desktop; load the catalogue and select a dog.
3. In Application Insights transaction search, find DogWorld.Desktop operations
   dogs.catalogue.load or dog.details.load and inspect the related API requests
   and downstream dependencies.
4. Repeat the existing downstream failure simulation. The API can return partial
   details successfully while downstream dependencies fail: the desktop operation
   then correctly remains successful.
5. Close or change the selected dog during a pending load. The operation outcome
   is cancelled, not failure. The automatic HTTP span may still record a cancelled
   transport as an error; use dogworld.outcome on the parent operation to distinguish it.
6. Disable telemetry using ApplicationInsights__Enabled=false or the helper's
   -Disable switch and restart the applications.

Custom operation tags exclude names, response bodies and exception messages.
Standard HTTP instrumentation still records URL metadata. Each operation restores
the caller's activity context, and the provider is disposed on desktop exit.

## Local verification
Run dotnet test tests/DogWorld.Desktop.Tests. The trace propagation test uses local
HTTP endpoints and an in-memory exporter, including an API-to-downstream call.
It verifies trace IDs, HTTP parent relationships and independent details loads.
No Azure connection or ingestion is needed. Azure ingestion must be checked
separately after enabling a valid resource.
