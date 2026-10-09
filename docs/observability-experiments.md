# Observability experiments

Status: planned, not executed. No telemetry resources or instrumentation are configured.

## First-feature evidence

| Exercise | Evidence required |
| --- | --- |
| Successful request | API request and SQL dependency share a trace; record how to find them. |
| Database failure | Failed API request, failed SQL dependency, and associated diagnostic context can be correlated. |
| Baseline | Request count, failures, and API-duration p50/p95/p99 for the same endpoint and time window. |
| Tail latency | Repeat the workload with a controlled minority of slow requests; compare percentiles and average with baseline. |
| Delay attribution | Distinguish actual SQL dependency duration from deliberate delay elsewhere in the API. Do not label an application delay as a slow SQL query. |
| Recovery | Disable the fault and demonstrate metrics returning toward baseline. |

p95 is the duration at or below which approximately 95% of measured requests fall. API duration and browser-perceived duration are different measurements. Establish a baseline before selecting performance thresholds.

## Record for every experiment

- Application revision, environment, endpoint, and hypothesis.
- Seed data, warm-up, request count, concurrency, and workload duration.
- Fault type, location, proportion, and duration when applicable.
- UTC measurement window and telemetry sampling configuration.
- KQL/query filters, observed telemetry count, failures, and duration percentiles.
- Ingestion delay, missing telemetry, and sample-size limitations (particularly p99).
- Trace evidence, interpretation, recovery evidence, and cleanup.

## Later exercises

- Browser-to-API correlation and external HTTP dependencies.
- Containerized repeat of a known diagnostic exercise.
- Traffic, latency, and error dashboards; an alert firing and recovering.
- Availability checks after resolving local connectivity, and Grafana integration after cost review.

Before sending telemetry, review current Azure costs against the GBP 3 monthly target and define ingestion controls and a stop procedure. Alerts alone cannot guarantee that spending stays under the target. Keep fault controls local and disabled by default, and never collect real applicant details or secrets.
