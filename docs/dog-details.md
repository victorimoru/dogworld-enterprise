# View dog details

## First Red increment

GET `/api/dogs/{id}` returns the selected dog's ID, name, breed, age in months
and a readable status (`Available` or `Adopted`). An unknown ID returns 404.
Reading details must not change stored data. No schema change is needed.

The first two endpoint tests use a dedicated temporary SQL Server database and
the actual API host. The success case must fail with 404 until the route exists.
The missing-ID case already passes because an absent route also returns 404;
it is a guard for Green, not evidence that lookup exists.

Run with DOGWORLD_TEST_SQLSERVER configured for the local test SQL Server:

```powershell
dotnet test tests/DogWorld.IntegrationTests --filter FullyQualifiedName~DogDetailsEndpointTests -c Release
```

## Implementation

The endpoint and `/dogs/{id}` page are implemented. Click a dog's name in the
available-dog list. Adopted dogs remain accessible by direct link with an Adopted
status; the available-dog list still excludes them. Unknown IDs show a not-found
message. Transient failures offer Retry. Navigation cancels the previous request.

The lookup uses AsNoTracking and a details DTO with a string status. A separate
IDogDetailsRepository interface keeps the existing list contract and test doubles
unchanged; DogRepository implements both interfaces. No migrations are needed.

The original Red run returned 404 instead of 200 for the known dog, as expected.

## Further verification

- Validate the details request and SQL dependency in Application Insights.
- Inspect responsive layout in a browser.

Keep the current list endpoint unchanged. No adoption submission, external breed
lookup or new descriptive fields are part of this feature.
