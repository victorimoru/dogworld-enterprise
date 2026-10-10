# Downstream breed client

Resolve IBreedsApiClient from dependency injection. The registered decorator uses
one application-wide IMemoryCache and delegates cache misses to the typed
BreedsApiClient. Successful profiles expire five minutes after insertion; missing
profiles and failures are not cached. Concurrent misses may make separate calls.

Development uses BreedsApi:BaseUrl = http://localhost:5292/. Start the Breeds API
with its http or https launch profile (both expose this HTTP port). Other
environments must supply BreedsApi__BaseUrl with the downstream service address.

The Polly v8 pipeline allows two retries for HttpRequestException, HTTP 408 and
HTTP 5xx. Delays start at 200 ms and double. An outer five-second timeout covers
all HTTP attempts, body buffering and retry delays. Caller cancellation is not
retried. Other HTTP failures are propagated; 404 returns null. Invalid JSON also
propagates rather than being cached as a missing profile.

HttpClient uses normal .NET W3C context propagation for outgoing requests. No
manual trace headers or replacement trace IDs are added. Azure end-to-end trace
correlation still needs a live check after the client is consumed by a route.

This increment provides the client, cache, pipeline and dependency registration.
It does not yet add breed information to the dog-details endpoint or web page.
Existing client/cache/resilience tests are unchanged by the implementation.
