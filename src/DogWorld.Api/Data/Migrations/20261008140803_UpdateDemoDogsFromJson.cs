using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogWorld.Api.Data.Migrations;

public partial class UpdateDemoDogsFromJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var dogs = ReadResource<SeedDog[]>("dog-names.v1.json");
        if (dogs.Length != 50 || dogs.Select(dog => dog.Id).Distinct().Count() != 50
            || dogs.Any(dog => dog.Id < 1 || dog.Id > 50
                || string.IsNullOrWhiteSpace(dog.Name) || string.IsNullOrWhiteSpace(dog.Breed)
                || dog.AgeInMonths < 0))
            throw new InvalidOperationException("Expected 50 valid dog records with unique IDs 1 through 50.");

        foreach (var dog in dogs)
        {
            migrationBuilder.UpdateData(
                table: "Dogs", keyColumn: "Id", keyValue: dog.Id,
                columns: ["Name", "Breed", "AgeInMonths", "Status"],
                values: [dog.Name, dog.Breed, dog.AgeInMonths, (int)dog.Status]);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Use a reviewed corrective migration to roll back demo data without overwriting later changes.");

    private static T ReadResource<T>(string filename)
    {
        using var stream = typeof(UpdateDemoDogsFromJson).Assembly.GetManifestResourceStream(
            $"DogWorld.Api.Data.SeedData.{filename}")
            ?? throw new InvalidOperationException($"Missing seed resource: {filename}");
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter<SeedStatus>(allowIntegerValues: false));
        return JsonSerializer.Deserialize<T>(stream, options)
            ?? throw new InvalidOperationException($"Invalid seed resource: {filename}");
    }

    private enum SeedStatus { Available = 0, Adopted = 1 }

    private sealed record SeedDog
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string Breed { get; init; }
        public required int AgeInMonths { get; init; }
        public required SeedStatus Status { get; init; }
    }
}
