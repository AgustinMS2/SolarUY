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
    public DbSet<SolarNode> SolarNodes => Set<SolarNode>();
    public DbSet<Inverter> Inverters => Set<Inverter>();
    public DbSet<Technician> Technicians => Set<Technician>();

    public DbSet<MaintenanceOrder> MaintenanceOrders => Set<MaintenanceOrder>();

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
            // A panel is optionally connected to a node (null = "Sin asignar").
            entity.HasOne(e => e.SolarNode)
                .WithMany()
                .HasForeignKey(e => e.SolarNodeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.SolarNodeId);
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

        modelBuilder.Entity<Inverter>(entity =>
        {
            entity.ToTable("Inverters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerieNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NormalizedSerieNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BrandAndModel).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NominalCapacityKw).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => e.NormalizedSerieNumber).IsUnique();
            // At most one inverter per node.
            entity.HasIndex(e => e.SolarNodeId).IsUnique();
            entity.HasOne(e => e.SolarNode).WithMany().HasForeignKey(e => e.SolarNodeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Technician>(entity =>
        {
            entity.ToTable("Technicians");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NormalizedDocumentId).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedEmail).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Specialty).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => e.NormalizedDocumentId).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<MaintenanceOrder>(entity =>
        {
            entity.ToTable("MaintenanceOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.ScheduledDateUtc).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(500);
            entity.Property(e => e.ReplacedParts).HasMaxLength(500);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            // Spec 021 (RF-3): consulta de "un mantenimiento pendiente por día para un panel".
            entity.HasIndex(e => new { e.SolarPanelId, e.Status });
            entity.HasIndex(e => e.TechnicianId);
            entity.HasOne(e => e.SolarPanel)
                .WithMany()
                .HasForeignKey(e => e.SolarPanelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Technician)
                .WithMany()
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    }
}
