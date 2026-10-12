using DogWorld.Vaccinations.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Vaccinations.Api.Data;

public sealed class VaccinationDbContext(DbContextOptions<VaccinationDbContext> options) : DbContext(options)
{
    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<Vaccination>();
        record.HasKey(v => v.Id);
        record.Property(v => v.VaccineName).HasMaxLength(200).IsRequired();
        record.HasIndex(v => new { v.DogId, v.AdministeredOn, v.Id }).IsDescending(false, true, false);
        record.ToTable("Vaccinations", table =>
        {
            table.HasCheckConstraint("CK_Vaccinations_DogId", "[DogId] > 0");
            table.HasCheckConstraint("CK_Vaccinations_Dates", "[NextDueOn] IS NULL OR [NextDueOn] >= [AdministeredOn]");
        });
    }
}
