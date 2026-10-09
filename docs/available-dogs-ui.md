# Available dogs UI

The home page fetches api/dogs and displays name, breed, and age in months in API
order. Loading and empty states use status messages; failures show a generic alert
and Retry button. Retry fetches again without navigating or reloading the page.
Disposing the component cancels its request.

Five bUnit component tests were run against the placeholder first: all five failed
for missing UI behaviour. The same tests then passed against the implementation.
Existing backend tests were not changed. Component tests cover rendering and HTTP
handling; they do not replace a browser check of certificate trust and layout.

Run the API and Web projects with their HTTPS profiles, in separate terminals:

```powershell
dotnet run --project src/DogWorld.Api --launch-profile https
dotnet run --project src/DogWorld.Web --launch-profile https
```

Open https://localhost:7188. The Development frontend configuration points to
https://localhost:7192/. Trust your ASP.NET development certificate locally if your
browser reports a certificate error. SQL Server and the migrated demo database must
be available. In Visual Studio, select both projects as startup projects and choose
their HTTPS launch profiles.

For HTTP, launch both projects with `--launch-profile http` and open
http://localhost:5248. The web app then calls http://localhost:5157/.

ApiBaseUrl and ApiBaseUrlHttps live in Web/wwwroot/appsettings.Development.json.
The web app selects ApiBaseUrlHttps when opened over HTTPS, avoiding mixed-content
requests. The API's Development
Cors:AllowedOrigins lists the two local Web origins. Change these together when
changing ports. For deployment, override ApiBaseUrl (and ApiBaseUrlHttps when used)
with your API address and allow the actual frontend origin on the API.

Browser telemetry enables cross-origin correlation only for the configured API
host. The API allows tracing headers through CORS. Home uses ILogger for errors;
it does not wait for telemetry uploads before displaying dogs or the Retry button.

Run UI tests independently without SQL Server:

```powershell
dotnet test tests/DogWorld.Web.Tests/DogWorld.Web.Tests.csproj -c Release
```
