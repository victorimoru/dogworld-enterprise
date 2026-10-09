using DogWorld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWorld.Api.Data;

public class DogWorldDbContext(DbContextOptions<DogWorldDbContext> options) : DbContext(options)
{
    public DbSet<Dog> Dogs => Set<Dog>();
}
