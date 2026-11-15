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
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<PanelControlEvent> PanelControlEvents => Set<PanelControlEvent>();
    public DbSet<ParkHourlyGeneration> ParkHourlyGenerations => Set<ParkHourlyGeneration>();
    public DbSet<GridInjectionEvent> GridInjectionEvents => Set<GridInjectionEvent>();

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
            // Spec 033: estado administrativo de inyección con default true en la BD.
            entity.Property(e => e.GridInjectionEnabled).IsRequired().HasDefaultValue(true);
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
            // Spec 033: estado administrativo de inyección con default true en la BD.
            entity.Property(e => e.GridInjectionEnabled).IsRequired().HasDefaultValue(true);
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
            entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
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

        // Spec 031: alertas de falla.
        modelBuilder.Entity<Alert>(entity =>
        {            entity.ToTable("Alerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParkName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EquipmentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.OccurrenceCount).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.LastOccurrenceAtUtc).IsRequired();
            // Contador de la campana (alertas Pendientes).
            entity.HasIndex(e => e.Status);
            // Deduplicado (RF-2) y consolidación (RF-6): alertas recientes de la misma severidad y equipo.
            entity.HasIndex(e => new { e.Severity, e.EquipmentType, e.EquipmentId, e.LastOccurrenceAtUtc });
            entity.HasIndex(e => e.SolarParkId);
            entity.HasOne(e => e.SolarPark)
                .WithMany()
                .HasForeignKey(e => e.SolarParkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Spec 032: bitácora de órdenes de control sobre paneles.
        modelBuilder.Entity<PanelControlEvent>(entity =>
        {
            entity.ToTable("PanelControlEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.OrderedBy).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OrderedAtUtc).IsRequired();
            entity.Property(e => e.OrderSentToInverter).IsRequired();
            entity.Property(e => e.InverterSerieNumber).HasMaxLength(100);
            // Historial de eventos por panel.
            entity.HasIndex(e => e.SolarPanelId);
            entity.HasOne(e => e.SolarPanel)
                .WithMany()
                .HasForeignKey(e => e.SolarPanelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Spec 033: bitácora inmutable de órdenes de inyección a la red.
        modelBuilder.Entity<GridInjectionEvent>(entity =>
        {
            entity.ToTable("GridInjectionEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetType).IsRequired();
            entity.Property(e => e.TargetName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.OrderedBy).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OrderedAtUtc).IsRequired();
            entity.Property(e => e.RemoteIp).HasMaxLength(45);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.OrdersSent).IsRequired();
            // Historial de eventos por parque, en orden cronológico.
            entity.HasIndex(e => new { e.ParkId, e.OrderedAtUtc });
            entity.HasOne(e => e.Park)
                .WithMany()
                .HasForeignKey(e => e.ParkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Spec 034: registros históricos de generación horaria por parque.
        modelBuilder.Entity<ParkHourlyGeneration>(entity =>
        {
            entity.ToTable("ParkHourlyGenerations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TimestampUtc).IsRequired();
            entity.Property(e => e.DateLocal).IsRequired();
            entity.Property(e => e.HourLocal).IsRequired();
            entity.Property(e => e.EnergyGeneratedKwh).IsRequired();
            entity.Property(e => e.PeakPowerKw).IsRequired();

            // Clave única compuesta por parque, fecha local y hora local
            entity.HasIndex(e => new { e.SolarParkId, e.DateLocal, e.HourLocal }).IsUnique();

            // Índice para acelerar filtros por rango de fechas
            entity.HasIndex(e => new { e.SolarParkId, e.DateLocal });

            entity.HasOne(e => e.SolarPark)
                .WithMany()
                .HasForeignKey(e => e.SolarParkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
