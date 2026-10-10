var builder = WebApplication.CreateBuilder(args);

var telemetryConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (builder.Configuration.GetValue("ApplicationInsights:Enabled", true)
    && !string.IsNullOrWhiteSpace(telemetryConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = telemetryConnectionString);

    // Application Insights 3.x uses the OpenTelemetry service name for the cloud role.
    builder.Services.AddOpenTelemetry().ConfigureResource(resource =>
        resource.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "DogWorld Breeds Api"));
}

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IBreedService, BreedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("../openapi/v1.json", "DogWorld Breeds API v1"));
}

//app.UseHttpsRedirection();

app.MapGet("/api/breeds", async (IBreedService breedService, CancellationToken ct) =>
{
    var breeds = await breedService.GetAllBreedsAsync(ct);
    return Results.Ok(breeds);
})
.WithName("GetAllBreeds");

app.MapGet("/api/breeds/{id}", async (string id, IBreedService breedService, CancellationToken ct) =>
{
    var breed = await breedService.GetBreedByIdAsync(id, ct);
    return breed is not null ? Results.Ok(breed) : Results.NotFound();
})
.WithName("GetBreedById");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "DogWorld.Breeds.Api" }))
.WithName("HealthCheck");

app.Run();
