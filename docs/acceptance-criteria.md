# First feature: list available dogs

Status: first Green increment verified: 8 unit cases and 6 integration cases pass with production code implemented and test files unchanged. UI and observability work remains. See [Green verification](green-verification.md). The Red notes below preserve the original failure evidence.

## First Red increment

The available-dog list uses `GET /api/dogs`. The first API contract test expects HTTP 200. The current API has no routes, so the expected failure is HTTP 404 instead of 200. This test hosts the actual API entry point using WebApplicationFactory; it does not implement a replacement endpoint.

This is only the first contract increment. It does not yet prove the response body, Available-only filtering, ordering, or SQL behaviour. Those require subsequent tests and fixtures. No database or Azure resource is required to reproduce this initial failure.

Run from the repository root:

```powershell
dotnet test tests/DogWorld.IntegrationTests/DogWorld.IntegrationTests.csproj --filter FullyQualifiedName~AvailableDogsEndpointTests
```

## Functional acceptance criteria

| Scenario | Expected result |
| --- | --- |
| Mixed Available and Adopted seed data | Show only Available dogs with name, breed, and age, in a stable order. |
| No Available dogs | API returns a successful empty result; page displays "No dogs available." |
| Request in progress | Page displays a loading state until completion. |
| Database operation fails | API returns a generic failure; page displays an error and retry action. Internal details are excluded from the response. |
| Retry after recovery | Available dogs load successfully. |

Listing never mutates stored data. Use a small fixed synthetic dataset. Pagination, external breed retrieval, and adoption submissions are outside this feature.

## Details to settle in the Red stage

- Service/repository tests now specify name then unique ID ordering and age in whole months as proposed contracts. See [unit test notes](../tests/DogWorld.Api.Tests/README.md). Compilation was verified to fail on absent production types; these tests have not executed assertions.
- Name collation and age validation remain to be defined.
- Response contract and generic error contract (route selected: `GET /api/dogs`).

These are not implemented decisions. Make them explicit before writing assertions that depend on them.

## Test plan

- Unit tests for filtering and ordering only where meaningful application logic owns those decisions.
- Integration tests against SQL Server for actual query filtering, ordering, empty results, and read-only behaviour; do not substitute an in-memory database to claim SQL correctness.
- API integration tests for successful and database-failure responses without leaking internal details.
- Verify loading, empty, error, and retry states in the UI when it exists; select automation based on the UI implementation.

Confirm each new test fails for the intended missing behaviour, then implement the minimum needed. Avoid placeholder passing tests or tests that merely mirror implementation. Azure telemetry delivery requires separate observed evidence.
