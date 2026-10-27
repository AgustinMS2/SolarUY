using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Data;

public class SolarUyDbContext : DbContext
{
    public SolarUyDbContext(DbContextOptions<SolarUyDbContext> options) : base(options)
    {
    }

    public DbSet<SolarPark> SolarParks => Set<SolarPark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SolarPark>(entity =>
        {
            entity.ToTable("SolarParks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.NormalizedName).IsUnique();
        });
    }
}
