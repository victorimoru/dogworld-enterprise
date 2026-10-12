using DogWorld.Vaccinations.Api.Data;
using DogWorld.Vaccinations.Api.Repositories;
using DogWorld.Vaccinations.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Vaccinations.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<VaccinationDbContext>(options => options.UseSqlServer(
            builder.Configuration.GetConnectionString("Vaccinations")
                ?? throw new InvalidOperationException("Configure ConnectionStrings:Vaccinations.")));
        builder.Services.AddScoped<IVaccinationRepository, VaccinationRepository>();
        builder.Services.AddScoped<VaccinationService>();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.MapGet("/api/dogs/{dogId:int}/vaccinations", async (int dogId, VaccinationService service, 
                                                                CancellationToken cancellationToken) =>
        {
            if (dogId <= 0) return Results.Problem(statusCode: 400, title: "Dog ID must be positive.");
            return Results.Ok(await service.GetByDogIdAsync(dogId, cancellationToken));

        }).WithName("GetVaccinationHistory")
          .Produces<IReadOnlyList<Contracts.VaccinationRecordResponse>>()
          .ProducesProblem(400).ProducesProblem(500);
        app.Run();
    }
}
