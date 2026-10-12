using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogWorld.Vaccinations.Api.Data.Migrations;

public partial class SeedDemoVaccinations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var record in ReadRecords())
        {
            migrationBuilder.InsertData(
                table: "Vaccinations",
                columns: ["Id", "DogId", "VaccineName", "AdministeredOn", "NextDueOn"],
                values: [record.Id, record.DogId, record.VaccineName, record.AdministeredOn, record.NextDueOn]);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var record in ReadRecords())
            migrationBuilder.DeleteData("Vaccinations", "Id", record.Id);
    }

    private static SeedRecord[] ReadRecords()
    {
        // Immutable migration input: corrections belong in a new migration/resource.
        using var stream = typeof(SeedDemoVaccinations).Assembly.GetManifestResourceStream(
            "DogWorld.Vaccinations.Api.Data.SeedData.vaccinations.v1.json")
            ?? throw new InvalidOperationException("Vaccination seed resource is missing.");
        return JsonSerializer.Deserialize<SeedRecord[]>(stream)
            ?? throw new InvalidOperationException("Vaccination seed resource must contain an array.");
    }

    private sealed record SeedRecord(int Id, int DogId, string VaccineName,
        DateOnly AdministeredOn, DateOnly? NextDueOn);
}
