using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Data;

public class SolarUyDbContext : DbContext
{
    public SolarUyDbContext(DbContextOptions<SolarUyDbContext> options) : base(options)
    {
    }

    public DbSet<SolarPark> SolarParks => Set<SolarPark>();
    public DbSet<SolarPanel> SolarPanels => Set<SolarPanel>();

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

        modelBuilder.Entity<SolarPanel>(entity =>
        {
            entity.ToTable("SolarPanels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerieNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NormalizedSerieNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.NormalizedSerieNumber).IsUnique();
            entity.Property(e => e.PowerW).IsRequired();
            entity.Property(e => e.Efficiency).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasOne(e => e.SolarPark)
                .WithMany()
                .HasForeignKey(e => e.SolarParkId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
