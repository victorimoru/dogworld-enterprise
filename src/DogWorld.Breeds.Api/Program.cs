using DogWorld.Breeds.Api.Services;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);
// Local telemetry settings are ignored by Git; environment variables retain precedence.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();

var telemetryConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (builder.Configuration.GetValue("ApplicationInsights:Enabled", true)
    && !string.IsNullOrWhiteSpace(telemetryConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = telemetryConnectionString;
        options.SamplingRatio = builder.Configuration.GetValue<float>("ApplicationInsights:SamplingRatio", 1.0f);
        options.EnableQuickPulseMetricStream = false;
    });

    // Application Insights 3.x uses the OpenTelemetry service name for the cloud role.
    builder.Services.AddOpenTelemetry().ConfigureResource(resource =>
        resource.AddService("DogWorld.Breeds.Api"));
}

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IBreedService, BreedService>();

var app = builder.Build();
app.UseMiddleware<DogWorld.ResilienceLab.DevelopmentFaultMiddleware>();

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
