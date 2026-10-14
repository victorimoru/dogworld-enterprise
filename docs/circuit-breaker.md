# Circuit breaker increment — acceptance criteria

Status: first Red increment. No production implementation yet.

## Policy decision

Use independent, application-lifetime circuit state for Breeds and Vaccinations.
A logical call includes its original HTTP attempt and up to two retries. Count its
final outcome once. Proposed composition: circuit breaker -> existing total timeout
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

## Remaining test increments before completion

- Verify shared state across separate client instances resolved from actual DI registrations.
- Deterministic clock-based half-open recovery, failed probe reopening, concurrent probe exclusion,
  and expiry of the sampling window; avoid real-time sleeps in tests.
- Network failure, total timeout, cancellation, and transition-log coverage.
- Verify endpoint 200 partial responses and correlated transition telemetry in the local lab.

Polly semantics: https://www.pollydocs.org/strategies/circuit-breaker.html
