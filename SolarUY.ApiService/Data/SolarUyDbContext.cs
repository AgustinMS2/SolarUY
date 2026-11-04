using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Data;

public class SolarUyDbContext : DbContext
{
    public SolarUyDbContext(DbContextOptions<SolarUyDbContext> options) : base(options)
    {
    }

    public DbSet<SolarPark> SolarParks => Set<SolarPark>();
    public DbSet<SolarNode> SolarNodes => Set<SolarNode>();

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

        modelBuilder.Entity<SolarNode>(entity =>
        {
            entity.ToTable("SolarNodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.SolarParkId, e.NormalizedName }).IsUnique();
            entity.HasOne(e => e.SolarPark).WithMany().HasForeignKey(e => e.SolarParkId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
