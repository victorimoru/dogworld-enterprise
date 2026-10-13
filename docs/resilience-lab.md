# Resilience experiments in Application Insights

The lab is enabled only in Development through process configuration. No fault
headers from a browser can enable it. Normal startup defaults to Off. Only GET
requests under /api are affected; /health and Swagger remain unaffected.

## Run a scenario

Stop the existing instance of the downstream API you want to simulate first.
From a PowerShell 7 terminal at the repository root:

```powershell
./scripts/Start-ResilienceLab.ps1 -Service Vaccinations -Scenario Unavailable
# Or:
./scripts/Start-ResilienceLab.ps1 -Service Breeds -Scenario Recover
```

Run the Dog API in another terminal with the local breed cache bypass enabled:

```powershell
$env:ResilienceLab__DisableBreedCache = 'true'
dotnet run --project src/DogWorld.Api --launch-profile http
```

Start the other downstream API and Blazor normally. Open a dog's details page once.
For Recover, do not call the simulated downstream directly from Swagger beforehand:
the first two API requests after that service starts consume the failures. Use a
single caller; this scenario is a process-wide counter, not a per-user setting.

| Scenario | Downstream behavior | Expected Dog API behavior |
| --- | --- | --- |
| Unavailable | Every API request returns 503 before database access | Three HTTP attempts (original + two retries); 200 with that enrichment unavailable |
| Recover | First two API requests return 503; subsequent requests execute normally | Third attempt can succeed; 200 with enrichment present |
| Timeout | Each API request waits seven seconds, observing cancellation | Five-second total resilience timeout; 200 with that enrichment unavailable |
| Off | No injection | Normal behavior |

Timeout is an outer total budget, so it does not imply three seven-second waits.
The downstream may record cancellation differently from an injected 503. Inspect
failed dependency duration and DogDetailsService warning logs. Cold starts or other
real failures can affect the recovery scenario; warm the unsimulated Dog API first.

## Read the result

1. Open the Dog API request in browser Network and copy X-Trace-Id.
2. In Application Insights Logs, use this query (Application Insights scope):

```kusto
union requests, dependencies, traces, exceptions
| where timestamp > ago(30m)
| where operation_Id == 'PASTE_TRACE_ID'
| project timestamp, itemType, cloud_RoleName, name, message,
          resultCode, success, duration, operation_ParentId, id, customDimensions
| order by timestamp asc
```

Injected downstream requests have customDimensions['resilience.lab.scenario'] and
customDimensions['resilience.lab.attempt']. The attempt number is service-wide.
Warnings also name the scenario. A 503 scenario gives failed downstream requests AND
failed Dog API dependencies. Successful fallback keeps the outer Dog API request 200;
that does not mean the downstream was healthy. Recovery gives failed attempts followed
by a successful attempt within the same operation ID.

To find simulated requests:

```kusto
requests
| where timestamp > ago(30m)
| where isnotempty(tostring(customDimensions['resilience.lab.scenario']))
| project timestamp, cloud_RoleName, name, resultCode, success, duration,
          scenario=customDimensions['resilience.lab.scenario'], operation_Id
| order by timestamp desc
```

Do a few manual requests, not a load test. Allow ingestion time and select a time
range containing the exercise. A healthy final response is evidence of graceful
fallback or retry recovery, not a reason to ignore downstream failures.

## Restore normal behavior

Stop the lab process (Ctrl+C) and start that API normally. The launcher restores its
process environment on exit. After stopping Dog API, clear the cache bypass and
restart it:

```powershell
Remove-Item Env:ResilienceLab__DisableBreedCache -ErrorAction SilentlyContinue
```

No database records, committed configuration, or Azure resource settings are changed
by the launcher. Production ignores ResilienceLab:Scenario even if it is set.
