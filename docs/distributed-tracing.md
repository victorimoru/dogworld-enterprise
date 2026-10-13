# First distributed tracing lesson

## Configure local telemetry

From the repository root in PowerShell 7:

```powershell
./scripts/Set-AppInsightsLocal.ps1 -ConnectionString 'PASTE THE NEW APPLICATION INSIGHTS CONNECTION STRING'
```

This updates ignored local settings for all four applications. Restart/rebuild all
four applications after configuring it. Browser settings are downloaded by visitors;
Application Insights connection strings identify the ingestion destination and are
not credentials for reading telemetry. Never add Azure credentials to wwwroot.

Server environment variable APPLICATIONINSIGHTS_CONNECTION_STRING overrides local
configuration. Remove stale overrides before using the script. Blazor WebAssembly
cannot read your machine's environment variables: it uses wwwroot/telemetry.local.json.
Do not publish the local settings with a production build.

Until a new resource is configured, telemetry is disabled. The old resource identities
have been removed from tracked settings. To switch off a configured lab, run the same
script with -Disable. The server also accepts ApplicationInsights__Enabled=false.

## Run locally

Run each command in a separate terminal (or configure multiple startup projects in VS):

```powershell
dotnet run --project src/DogWorld.Breeds.Api --launch-profile http
dotnet run --project src/DogWorld.Vaccinations.Api --launch-profile http
dotnet run --project src/DogWorld.Api --launch-profile http
dotnet run --project src/DogWorld.Web --launch-profile http
```

Open http://localhost:5248, then click a dog's image. Use the same HTTP profiles for
this exercise; changing to HTTPS requires trusted local development certificates.
Both SQL databases must already have their migrations applied.

Expected trace:

```text
DogWorld.Web      page view -> fetch GET /api/dogs/1
DogWorld.Api                  request -> SQL dog lookup
                                      -> HTTP Breeds dependency
DogWorld.Breeds.Api                         request
                                      -> HTTP Vaccinations dependency
DogWorld.Vaccinations.Api                   request -> SQL history lookup
```

The two downstream HTTP calls run in parallel. They share a trace ID but have distinct
span IDs. The downstream request's parent ID matches the corresponding HTTP dependency
ID. Cached breed profiles intentionally omit the Breeds HTTP request: restart Dog API
or select a previously uncached breed to demonstrate the full map.

## Find your first transaction in Azure

1. Open the new Application Insights resource.
2. Open Application Map and select a recent time range. Roles use the four names above.
3. Open Transaction search (Search), find GET api/dogs/{id:int}, and open the end-to-end
   transaction details. Telemetry ingestion can take a few minutes.
4. In browser developer tools, select the api/dogs request in Network. Its request
   traceparent contains the 32-character trace ID. The API also returns X-Trace-Id,
   exposed through CORS, for direct lookup.
5. Open Logs scoped to the Application Insights resource and run:

```kusto
union requests, dependencies, pageViews, traces, exceptions
| where timestamp > ago(30m)
| where operation_Id == 'PASTE_X_TRACE_ID_HERE'
| project timestamp, itemType, cloud_RoleName, name, message,
          operation_Id, operation_ParentId, id, duration, success, resultCode
| order by timestamp asc
```

To find recent dog-detail requests:

```kusto
requests
| where timestamp > ago(30m)
| where cloud_RoleName == 'DogWorld.Api'
| where url contains '/api/dogs/'
| project timestamp, name, duration, success, resultCode, operation_Id
| order by timestamp desc
```

To compare API response-time percentiles (milliseconds):

```kusto
requests
| where timestamp > ago(30m)
| where cloud_RoleName == 'DogWorld.Api'
| summarize requests=count(), p50=percentile(duration, 50),
            p95=percentile(duration, 95), p99=percentile(duration, 99) by name
```

These queries use the Application Insights table aliases. A workspace-scoped Logs
view may use AppRequests/AppDependencies and PascalCase columns instead. A handful
of manual requests proves wiring, not statistically meaningful p95/p99 performance.

## First failure exercise

After confirming a healthy request, stop Vaccinations API and reload the dog's page.
The page should still show the dog and breed, with vaccination history unavailable.
In the transaction, the Dog API request can be successful while the vaccination HTTP
dependencies fail. Retries create additional child dependencies. Restart the service.
Do not lengthen timeouts to hide a cold-start delay; distinguish startup behavior from
normal response times using the recorded dependency durations.

## Volume and budget

The first lesson retains 100% of browser and server traces so sample transactions are
complete. Live Metrics is disabled and browser logs start at Warning. Use a few manual
requests; stop the apps or disable telemetry when finished. Sampling is not a monetary
cap. A £3 Azure budget alert does not stop spending, and a daily ingestion cap is not
an exact currency ceiling. Review the resource/workspace ingestion settings and Azure
Cost Management before running load tests. No budget or resource settings were changed.

## Troubleshooting

- No browser data: check the SDK CDN and ingestion POSTs in Network, local JSON serving,
  content blockers and the connection string. The app continues after a bounded wait
  if browser instrumentation is blocked; that fallback request may lack browser tracing.
- Broken correlation: inspect traceparent and the API CORS preflight; headers must reach
  the API. Do not create a replacement trace ID in a custom HttpClient handler.
- Missing SQL spans: inspect dependency types before assuming a database query was traced.
  The installed server SDK provides SQL client instrumentation; local verification captured both Dogs and Vaccinations SQL spans; not every SQL command is guaranteed to appear.
- Resource not found: verify the Application ID belongs to the current, existing resource.

References:
- https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/NETCORE/Readme.md
- https://github.com/IvanJosipovic/BlazorApplicationInsights
- https://learn.microsoft.com/azure/azure-monitor/app/distributed-trace-data

## Validation

Browser, API, downstream HTTP, and SQL correlation were verified in Application
Insights. Retry recovery, retry exhaustion, and timeout fallback were also verified.
See [the resilience lab](resilience-lab.md) to reproduce these experiments against
your own resource. Trace IDs are specific to each run and resource.
