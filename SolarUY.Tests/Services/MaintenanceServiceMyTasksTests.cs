using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceMyTasksTests
{
    private static readonly Guid ParkId = Guid.NewGuid();
    private static readonly Guid NodeId = Guid.NewGuid();
    private static readonly Guid PanelId = Guid.NewGuid();
    private static readonly Guid TechAId = Guid.NewGuid();
    private static readonly Guid TechBId = Guid.NewGuid();

    private static (MaintenanceService Service, FakeMaintenanceRepository MaintenanceRepo, FakeTechnicianRepository TechRepo) CreateService()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var node = new SolarNode
        {
            Id = NodeId,
            Name = "Inversor Norte - Nodo 01",
            NormalizedName = "inversor norte - nodo 01",
            SolarParkId = ParkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        var park = new SolarPark
        {
            Id = ParkId,
            Name = "Parque Salto Solar",
            NormalizedName = "parque salto solar",
            Latitude = -31.38,
            Longitude = -57.96,
            MaxCapacityKw = 500,
            PowerAlertThresholdKw = 400,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        var panel = new SolarPanel
        {
            Id = PanelId,
            SerieNumber = "PANEL-SLT-001",
            NormalizedSerieNumber = "panel-slt-001",
            SolarParkId = ParkId,
            SolarPark = park,
            SolarNodeId = NodeId,
            SolarNode = node,
            PowerW = 400,
            Efficiency = 20,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        panelRepo.Parks.Add(park);
        panelRepo.Panels.Add(panel);

        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = TechAId,
            FullName = "Carlos Gómez",
            DocumentId = "41234567",
            NormalizedDocumentId = "41234567",
            Email = "carlos.gomez@solaruy.com",
            NormalizedEmail = "carlos.gomez@solaruy.com",
            PhoneNumber = "099123456",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = TechBId,
            FullName = "Lucía Silvera",
            DocumentId = "47654321",
            NormalizedDocumentId = "47654321",
            Email = "lucia.silvera@solaruy.com",
            NormalizedEmail = "lucia.silvera@solaruy.com",
            PhoneNumber = "098654321",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        var maintenanceRepo = new FakeMaintenanceRepository();
        maintenanceRepo.Panels.Add(panel);

        var service = new MaintenanceService(
            maintenanceRepo,
            panelRepo,
            techRepo,
            NullLogger<MaintenanceService>.Instance);

        return (service, maintenanceRepo, techRepo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMyMaintenances_WithNullOrWhitespaceEmail_ReturnsValidationError(string? email)
    {
        var (service, _, _) = CreateService();

        var result = service.GetMyMaintenances(email!);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void GetMyMaintenances_WhenTechnicianNotFound_ReturnsNotFoundWithExpectedMessage()
    {
        var (service, _, _) = CreateService();

        var result = service.GetMyMaintenances("desconocido@solaruy.com");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("No se encontró un perfil de técnico asociado a tu usuario.", result.ErrorMessage);
    }

    [Fact]
    public void GetMyMaintenances_FiltersOnlyAssignedToAuthenticatedTechnician()
    {
        var (service, maintenanceRepo, _) = CreateService();

        var orderA1 = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(1),
            Description = "Limpieza de paneles",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        var orderA2 = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(2),
            Description = "Reemplazo de fusible",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        var orderB = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechBId,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(1),
            Description = "Revisión general",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        maintenanceRepo.Add(orderA1);
        maintenanceRepo.Add(orderA2);
        maintenanceRepo.Add(orderB);

        var result = service.GetMyMaintenances("carlos.gomez@solaruy.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, o => Assert.Equal(TechAId, o.TechnicianId));
    }

    [Fact]
    public void GetMyMaintenances_PrioritizesPendingOrdersAscendingThenOthersDescending()
    {
        var (service, maintenanceRepo, _) = CreateService();
        var baseDate = DateTime.UtcNow;

        var pendingLate = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = baseDate.AddDays(3),
            Description = "Pendiente tardío",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = baseDate
        };
        var pendingEarly = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = baseDate.AddDays(1),
            Description = "Pendiente temprano",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = baseDate
        };
        var completedRecent = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = baseDate.AddDays(-1),
            Description = "Completado reciente",
            Status = MaintenanceStatus.Completed,
            CreatedAtUtc = baseDate
        };
        var cancelledOlder = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = baseDate.AddDays(-5),
            Description = "Cancelado antiguo",
            Status = MaintenanceStatus.Cancelled,
            CreatedAtUtc = baseDate
        };

        // Agregar en orden desordenado
        maintenanceRepo.Add(cancelledOlder);
        maintenanceRepo.Add(pendingLate);
        maintenanceRepo.Add(completedRecent);
        maintenanceRepo.Add(pendingEarly);

        var result = service.GetMyMaintenances("carlos.gomez@solaruy.com");

        Assert.True(result.Success);
        var list = result.Value!;
        Assert.Equal(4, list.Count);

        // 1°: pendingEarly
        Assert.Equal(pendingEarly.Id, list[0].Id);
        // 2°: pendingLate
        Assert.Equal(pendingLate.Id, list[1].Id);
        // 3°: completedRecent
        Assert.Equal(completedRecent.Id, list[2].Id);
        // 4°: cancelledOlder
        Assert.Equal(cancelledOlder.Id, list[3].Id);
    }

    [Fact]
    public void GetMyMaintenances_MapsParkPanelAndNodeDataCorrectly()
    {
        var (service, maintenanceRepo, _) = CreateService();

        var order = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = TechAId,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(1),
            Description = "Chequeo de conexiones",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        maintenanceRepo.Add(order);

        var result = service.GetMyMaintenances("carlos.gomez@solaruy.com");

        Assert.True(result.Success);
        var item = Assert.Single(result.Value!);
        Assert.Equal("PANEL-SLT-001", item.SolarPanelSerie);
        Assert.Equal("Parque Salto Solar", item.SolarParkName);
        Assert.Equal("Inversor Norte - Nodo 01", item.SolarNodeName);
        Assert.Equal("Carlos Gómez", item.TechnicianName);
        Assert.Equal("Chequeo de conexiones", item.Description);
    }
}
