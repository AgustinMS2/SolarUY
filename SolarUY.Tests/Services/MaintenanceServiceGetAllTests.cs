using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceGetAllTests
{
    private static readonly Guid ParkAId = Guid.NewGuid();
    private static readonly Guid ParkBId = Guid.NewGuid();
    private static readonly Guid PanelParkAId = Guid.NewGuid();
    private static readonly Guid PanelParkBId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid OtherTechnicianId = Guid.NewGuid();

    private static MaintenanceOrder Order(
        Guid panelId,
        Guid technicianId,
        MaintenanceStatus status,
        DateTime scheduledDateUtc,
        MaintenanceType type = MaintenanceType.Preventive)
    {
        var order = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panelId,
            TechnicianId = technicianId,
            Type = type,
            ScheduledDateUtc = scheduledDateUtc,
            Description = "Trabajo de prueba",
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
        return order;
    }

    private static FakeSolarPanelRepository CreatePanelRepository()
    {
        var panelRepo = new FakeSolarPanelRepository();
        panelRepo.Panels.Add(new SolarPanel
        {
            Id = PanelParkAId,
            SerieNumber = "SP-A",
            NormalizedSerieNumber = "sp-a",
            SolarParkId = ParkAId,
            PowerW = 450,
            Efficiency = 21,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        panelRepo.Panels.Add(new SolarPanel
        {
            Id = PanelParkBId,
            SerieNumber = "SP-B",
            NormalizedSerieNumber = "sp-b",
            SolarParkId = ParkBId,
            PowerW = 450,
            Efficiency = 21,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        panelRepo.Parks.Add(new SolarPark
        {
            Id = ParkAId,
            Name = "Parque Norte",
            NormalizedName = "parque norte",
            Latitude = -34.9,
            Longitude = -56.1,
            MaxCapacityKw = 100,
            PowerAlertThresholdKw = 80,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        panelRepo.Parks.Add(new SolarPark
        {
            Id = ParkBId,
            Name = "Parque Sur",
            NormalizedName = "parque sur",
            Latitude = -34.8,
            Longitude = -56.2,
            MaxCapacityKw = 100,
            PowerAlertThresholdKw = 80,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        return panelRepo;
    }

    private static (MaintenanceService Service, FakeMaintenanceRepository MaintenanceRepo) CreateService()
    {
        var maintenanceRepo = new FakeMaintenanceRepository();
        maintenanceRepo.Panels.AddRange(new[]
        {
            new SolarPanel
            {
                Id = PanelParkAId, SerieNumber = "SP-A", NormalizedSerieNumber = "sp-a", SolarParkId = ParkAId,
                PowerW = 450, Efficiency = 21, Status = SolarPanelStatus.Active, CreatedAtUtc = DateTime.UtcNow
            },
            new SolarPanel
            {
                Id = PanelParkBId, SerieNumber = "SP-B", NormalizedSerieNumber = "sp-b", SolarParkId = ParkBId,
                PowerW = 450, Efficiency = 21, Status = SolarPanelStatus.Active, CreatedAtUtc = DateTime.UtcNow
            }
        });

        var technicianRepo = new FakeTechnicianRepository();
        technicianRepo.PersistedTechnicians.Add(new Technician
        {
            Id = TechnicianId,
            FullName = "Perez, Juan",
            DocumentId = "p-001",
            NormalizedDocumentId = "p001",
            Email = "perez@email.com",
            NormalizedEmail = "perez@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        technicianRepo.PersistedTechnicians.Add(new Technician
        {
            Id = OtherTechnicianId,
            FullName = "Diaz, Maria",
            DocumentId = "d-002",
            NormalizedDocumentId = "d002",
            Email = "diaz@email.com",
            NormalizedEmail = "diaz@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        var service = new MaintenanceService(
            maintenanceRepo,
            CreatePanelRepository(),
            technicianRepo,
            NullLogger<MaintenanceService>.Instance);

        return (service, maintenanceRepo);
    }

    [Fact]
    public void GetAll_WithNoFilters_ReturnsAllOrderedByScheduledDateDescending()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        var newest = Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(5));
        var middle = Order(PanelParkAId, TechnicianId, MaintenanceStatus.Completed, DateTime.UtcNow.AddDays(3));
        var oldest = Order(PanelParkBId, OtherTechnicianId, MaintenanceStatus.Cancelled, DateTime.UtcNow.AddDays(1));
        maintenanceRepo.PersistedOrders.Add(newest);
        maintenanceRepo.PersistedOrders.Add(middle);
        maintenanceRepo.PersistedOrders.Add(oldest);

        // Act
        var orders = service.GetAll(new MaintenanceFilter());

        // Assert
        var result = orders.AssertSuccess();
        Assert.Equal(3, result.Count);
        // RF-2: orden cronolÃ³gico descendente por fecha programada.
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(middle.Id, result[1].Id);
        Assert.Equal(oldest.Id, result[2].Id);
    }

    [Fact]
    public void GetAll_WithStatusFilter_ReturnsOnlyThatStatus()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Completed, DateTime.UtcNow.AddDays(4)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkBId, OtherTechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(6)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter { Status = MaintenanceStatus.Pending });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(MaintenanceStatus.Pending, o.Status));
    }

    [Fact]
    public void GetAll_WithParkFilter_ReturnsOnlyOrdersFromThatPark()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkBId, OtherTechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(4)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter { ParkId = ParkBId });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Single(result);
        Assert.Equal(PanelParkBId, result[0].SolarPanelId);
    }

    [Fact]
    public void GetAll_WithTechnicianFilter_ReturnsOnlyOrdersOfThatTechnician()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkBId, OtherTechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(4)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter { TechnicianId = TechnicianId });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Single(result);
        Assert.Equal(TechnicianId, result[0].TechnicianId);
    }

    [Fact]
    public void GetAll_WithDateRangeFilter_ReturnsOnlyOrdersInsideRange()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        var tooOld = Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(-2));
        var inside = Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2));
        var laterInside = Order(PanelParkAId, TechnicianId, MaintenanceStatus.Completed, DateTime.UtcNow.AddDays(5));
        var tooFar = Order(PanelParkBId, OtherTechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(30));
        maintenanceRepo.PersistedOrders.Add(tooOld);
        maintenanceRepo.PersistedOrders.Add(inside);
        maintenanceRepo.PersistedOrders.Add(laterInside);
        maintenanceRepo.PersistedOrders.Add(tooFar);

        // Act
        var orders = service.GetAll(new MaintenanceFilter
        {
            FromUtc = DateTime.UtcNow.AddHours(-1),
            ToUtc = DateTime.UtcNow.AddDays(7)
        });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, o => o.Id == tooOld.Id || o.Id == tooFar.Id);
        Assert.Contains(result, o => o.Id == inside.Id);
        Assert.Contains(result, o => o.Id == laterInside.Id);
    }

    [Fact]
    public void GetAll_WithCombinedFilters_AppliesAllConditions()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Completed, DateTime.UtcNow.AddDays(3)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkBId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(4)));
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(20)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter
        {
            Status = MaintenanceStatus.Pending,
            ParkId = ParkAId,
            TechnicianId = TechnicianId,
            ToUtc = DateTime.UtcNow.AddDays(10)
        });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Single(result);
        Assert.Equal(PanelParkAId, result[0].SolarPanelId);
        Assert.Equal(TechnicianId, result[0].TechnicianId);
    }

    [Fact]
    public void GetAll_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Completed, DateTime.UtcNow.AddDays(2)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter { Status = MaintenanceStatus.Pending });

        // Assert
        var result = orders.AssertSuccess();
        Assert.Empty(result); // RF-4: "No se encontraron mantenimientos para los criterios seleccionados."
    }

    [Fact]
    public void GetAll_MapsNamesIntoDtos()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(Order(PanelParkAId, TechnicianId, MaintenanceStatus.Pending, DateTime.UtcNow.AddDays(2)));

        // Act
        var orders = service.GetAll(new MaintenanceFilter());

        // Assert
        var result = orders.AssertSuccess();
        // RF-1: la grilla necesita parque, panel y tÃ©cnico con nombre.
        Assert.Matches("SP-A", result[0].SolarPanelSerie);
        Assert.Matches("Parque Norte", result[0].SolarParkName);
        Assert.Matches("Perez, Juan", result[0].TechnicianName);
        Assert.Equal(MaintenanceStatus.Pending, result[0].Status);
        Assert.Equal(MaintenanceType.Preventive, result[0].Type);
    }
}

internal static class MaintenanceServiceGetAllTestExtensions
{
    /// <summary>Verifica que resulte Ã©xito y devuelve el valor para aserciones compactas.</summary>
    public static T AssertSuccess<T>(this ServiceResult<T> result)
    {
        Assert.True(result.Success, result.ErrorMessage ?? "Se esperaba Ã©xito.");
        return result.Value!;
    }
}
