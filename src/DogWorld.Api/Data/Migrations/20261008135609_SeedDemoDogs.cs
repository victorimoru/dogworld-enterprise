using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogWorld.Api.Data.Migrations;

public partial class SeedDemoDogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep this versioned resource immutable after the migration is applied.
        using var stream = typeof(SeedDemoDogs).Assembly.GetManifestResourceStream(
            "DogWorld.Api.Data.SeedData.dog-names.legacy.json")
            ?? throw new InvalidOperationException("The v1 dog names resource is missing.");
        var names = JsonSerializer.Deserialize<string[]>(stream)
            ?? throw new InvalidOperationException("The dog names JSON must contain an array.");
        if (names.Length != 50 || names.Any(string.IsNullOrWhiteSpace)
            || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            throw new InvalidOperationException("Expected 50 distinct, non-empty dog names.");

        string[] breeds = ["Beagle", "Labrador", "Poodle", "German Shepherd", "Bulldog"];
        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index].Replace("'", "''");
            var breed = breeds[index % breeds.Length].Replace("'", "''");
            var age = 6 + (index * 3 % 120);
            var status = (index + 1) % 5 == 0 ? 1 : 0;
            // Existing rows from the earlier runtime seeder retain their IDs and data.
            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [Dogs] WHERE [Name] = N'{name}')
                BEGIN
                    INSERT INTO [Dogs] ([Name], [Breed], [AgeInMonths], [Status])
                    VALUES (N'{name}', N'{breed}', {age}, {status});
                END;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Name matches can predate this migration, and dogs may have been edited.
        // Automatic deletion cannot safely identify rows this migration still owns.
        throw new NotSupportedException(
            "SeedDemoDogs cannot be automatically rolled back. Use a reviewed corrective migration to preserve existing dog data.");
    }
}
