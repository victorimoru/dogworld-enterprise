# Desktop catalogue increment

Avalonia with CommunityToolkit MVVM; consumes existing DogWorld.Contracts and HTTP API.

Acceptance criteria:
- Load page 1 automatically and request exactly 12 available dogs per page.
- Use X-Total-Count for page count and disable navigation at page boundaries.
- Show loading, empty, error, and retry states. Avoid overlapping requests.
- Return to the last available page if adoption/deletion removes the requested page.
- Cancel pending work when the window closes. Cancellation is not an API error.
- Show local default illustrations with an explicit placeholder label.
- Keep HTTP access out of the view; test view-model behaviour with a stub client.

Scope: catalogue only. Details, adoption actions, filters, and desktop telemetry are later increments.

Run DogWorld.Api normally, then:

```powershell
dotnet run --project src/DogWorld.Desktop
```

Default endpoint: http://localhost:5157/. Override before launching if necessary:

```powershell
$env:DOGWORLD_API_BASE_URL = 'http://localhost:5157/'
```

No Azure instrumentation is added to the desktop app. Existing API telemetry settings
still apply independently. Windows is the locally validated target; Linux and macOS
require their own runtime validation.

## Verification

The first five view-model tests were run against a throwing scaffold and failed,
then passed after implementation. HTTP contract tests cover paging and malformed
count headers. An Avalonia headless test renders the actual XAML with Skia and checks
catalogue content and pagination command boundaries using sample data.

```powershell
dotnet test tests/DogWorld.Desktop.Tests -c Release
```

To save a rendered preview from the UI test, set DOGWORLD_DESKTOP_PREVIEW to an
absolute PNG path before running it. The preview uses test data, not the SQL database.
Live API use requires the configured DogWorld database and its migrations.
The desktop test project uses xUnit 3 because Avalonia 12 headless support requires it;
existing test projects remain unchanged.
