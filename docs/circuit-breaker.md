# Circuit breaker increment — acceptance criteria

Status: implemented; original Red tests preserved and passing.

## Policy decision

Use independent, application-lifetime circuit state for Breeds and Vaccinations.
A logical call includes its original HTTP attempt and up to two retries. Count its
final outcome once. Composition: circuit breaker -> existing total timeout
-> existing retry -> HTTP. This also lets the breaker count total timeout failures.

- Sampling window: 30 seconds; minimum throughput: four logical calls.
- Open when at least 50% of sampled logical calls fail, after minimum throughput.
- Open duration: 15 seconds. Open calls do not send HTTP or retry.
- Failures: HTTP 408/5xx, HttpRequestException, and total timeout.
- Normal 4xx responses and caller cancellation do not trigger the breaker.
- A retry sequence ending successfully counts as a successful logical call.
- After the break duration, the next call becomes the half-open probe. Success closes
  the circuit; handled failure opens it for another break duration. Other concurrent
  calls are rejected while the probe is running. A probe is one logical call and may retry.
- Cached breed results may still be returned without reaching the circuit.
- Dog details preserve their existing partial-response behaviour on BrokenCircuitException.
- Log service name and transitions (open, half-open, closed) for Application Insights.
  Rejected calls have no outgoing HTTP span; fallback logs explain the missing dependency.

## First Red coverage

- Both policies: four failed logical calls retain all retries, then reject the next call.
- Both policies: two successes plus two failures reach the 50% threshold.
- Both policies: recovered retries and 404 responses do not trip the circuit.
- Independent policy instances isolate downstream services.
- DogDetailsService handles one or both open circuits and preserves healthy enrichment.

## Validation

Added deterministic-clock tests for successful and failed half-open probes, concurrent
probe exclusion, sampling-window expiry, and structured transition logs. Added tests
for network failures, total timeouts, and caller cancellation. Real application-host
registration tests confirm that clients resolved in separate scopes share their own
circuit and that the other service remains callable. These tests use HTTP stubs and
send no telemetry to Azure.

## Local Application Insights exercise

1. Restart Dog API to reset its in-memory circuits. For breed experiments, enable
   `ResilienceLab__DisableBreedCache=true` before starting Dog API.
2. Start the chosen downstream with the Unavailable scenario from the resilience lab.
3. Open dog details four times within 30 seconds. Each failed logical call performs
   three HTTP attempts. The fourth completed failure opens the circuit.
4. Request details again immediately: the failed service receives no HTTP call.
   Dog API returns partial details and records the circuit exception in its fallback log.
5. Stop the simulated service and restart it normally. At least 15 seconds after the
   circuit opened, request details again. This call probes recovery and closes the
   circuit if successful. No background probe is sent merely because time passed.
6. Inspect the transaction and the logs below. If the probe still fails, the circuit
   opens for another 15 seconds. Restart normal services when the exercise is done.

```kusto
traces
| where timestamp > ago(30m)
| where cloud_RoleName == 'DogWorld.Api'
| where message startswith 'Downstream circuit'
| project timestamp, message, operation_Id, customDimensions
| order by timestamp asc
```

Transition fields are CircuitState, DownstreamService, and (on opening) BreakDurationMs.
Ensure Information logs are enabled for DogWorld.Api.Resilience to see Closed events.
The pipeline instances live for one Dog API process. Multiple deployed instances would
have independent circuits. Cache hits do not reach the circuit. This increment's Azure
portal verification should be performed against your current resource using the steps above.

Polly semantics: https://www.pollydocs.org/strategies/circuit-breaker.html
