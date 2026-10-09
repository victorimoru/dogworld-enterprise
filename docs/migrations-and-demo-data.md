# Migrations and JSON demo data

Development uses local SQL Server DogWorldLabMigrations with SSPI Windows authentication.

Migration history:
1. InitialCreate creates Dogs.
2. SeedDemoDogs preserves the original data using embedded dog-names.legacy.json.
3. UpdateDemoDogsFromJson applies the full records in embedded dog-names.v1.json.

The current JSON contains 50 objects with Id, Name, Breed, AgeInMonths, and Status.
All values come from the file; Status accepts Available or Adopted and maps to the
stored enum values 0 or 1. IDs must be unique, 1 through 50; names and breeds must
be non-empty and ages non-negative. The dataset contains 30 Available and 20 Adopted.

The follow-up migration uses EF Core UpdateData to assign the JSON fields directly by ID. It relies on SeedDemoDogs having created those IDs; it no longer compares legacy values or protects later edits to those seed rows. EF handles SQL literal escaping, and JSON status strings deserialize to a migration-local enum. Fresh databases receive the original seed then the JSON update. Editing this already-applied migration does not rerun it on existing databases.

Run from the solution directory:

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__Initialize = 'false'
dotnet ef database update --project src/DogWorld.Api --startup-project src/DogWorld.Api --configuration Release
```

ConnectionStrings__DogWorld or user secrets override appsettings.Development.json.
Database:Initialize optionally applies migrations on Development startup and defaults
to false. There are no runtime seeding hooks or SeedDemoData flag. Data migrations
run in any environment where they are applied, not only Development.

Both data migrations are forward-only: use a reviewed corrective migration to undo
data changes. Do not change applied seed resources again; create a new versioned JSON
and migration for future data updates. Resources are embedded so no working-directory
file dependency is needed. Do not apply InitialCreate over an EnsureCreated schema.

The existing test files remain unchanged. Their SQL repository fixtures use independent
EnsureCreated databases; the API test exercises the complete migration chain.
