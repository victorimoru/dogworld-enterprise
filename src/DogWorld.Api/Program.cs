using DogWorld.Api.Data;
using DogWorld.Api.Repositories;
using DogWorld.Api.Services;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;

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
        resource.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "DogWorld Api"));
}

builder.Services.AddDbContext<DogWorldDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DogWorld")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:DogWorld before using the API."));
});
builder.Services.AddScoped<IDogRepository, DogRepository>();
builder.Services.AddScoped<DogService>();
builder.Services.AddScoped<IDogPageRepository, DogRepository>();
builder.Services.AddScoped<DogPageService>();
builder.Services.AddScoped<IDogDetailsRepository, DogRepository>();
builder.Services.AddScoped<DogDetailsService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(DogWorld.Api.Resilience.BreedsApiResiliencePolicy.Create());
builder.Services.AddHttpClient<DogWorld.Api.Clients.BreedsApiClient>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["BreedsApi:BaseUrl"]
        ?? throw new InvalidOperationException("Configure BreedsApi:BaseUrl before using the breed client."));
    http.Timeout = Timeout.InfiniteTimeSpan; // Polly owns the total request timeout.
});
builder.Services.AddTransient<DogWorld.Api.Clients.IBreedsApiClient>(services =>
    new DogWorld.Api.Clients.CachedBreedsApiClient(
        services.GetRequiredService<DogWorld.Api.Clients.BreedsApiClient>(),
        services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
    {
        policy.WithOrigins(origins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .AllowAnyHeader()
              .WithExposedHeaders("Request-Context", "X-Total-Count");
    }
}));
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("WebClient");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Explicit local-lab setup; schema creation is never enabled in production.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:Initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<DogWorldDbContext>();
    await context.Database.MigrateAsync();
}

app.MapControllers();

app.Run();
