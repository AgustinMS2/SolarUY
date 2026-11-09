using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceReservationTests
{
    private static readonly Guid PanelId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();

    private static DateTime FutureDateUtc() => DateTime.UtcNow.AddHours(24);

    private static CreateMaintenanceReservationRequest ValidRequest() => new()
    {
        SolarPanelId = PanelId,
        TechnicianId = TechnicianId,
        Type = MaintenanceType.Preventive,
        ScheduledDateUtc = FutureDateUtc(),
        Description = "Revisión preventiva del panel"
    };

    private static (MaintenanceService Service, FakeMaintenanceRepository MaintenanceRepo) CreateService(
        FakeSolarPanelRepository? panelRepo = null,
        FakeTechnicianRepository? techniciannRepo = null,
        FakeMaintenanceRepository? maintenanceRepo = null)
    {
        panelRepo ??= new FakeSolarPanelRepository();
        panelRepo.Panels.Add(new SolarPanel
        {
            Id = PanelId,
            SerieNumber = "SP-001",
            NormalizedSerieNumber = "sp-001",
            SolarParkId = Guid.NewGuid(),
            PowerW = 450,
            Efficiency = 21,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        techniciannRepo ??= new FakeTechnicianRepository();
        techniciannRepo.PersistedTechnicians.Add(new Technician
        {
            Id = TechnicianId,
            FullName = "Test, Técnico",
            DocumentId = "t-001",
            NormalizedDocumentId = "t001",
            Email = "tecnico.test@email.com",
            NormalizedEmail = "tecnico.test@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        maintenanceRepo ??= new FakeMaintenanceRepository();

        var service = new MaintenanceService(
            maintenanceRepo,
            panelRepo,
            techniciannRepo,
            NullLogger<MaintenanceService>.Instance);

        return (service, maintenanceRepo);
    }

    [Fact]
    public void CreateReservation_WithValidData_CreatesPendingOrderInUtc()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        var request = ValidRequest();
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.True(result.Success);
        var savedOrder = maintenanceRepo.PersistedOrders.Single(o => o.Id == result.Value);
        Assert.Equal(MaintenanceStatus.Pending, savedOrder.Status);
        Assert.Equal(request.ScheduledDateUtc, savedOrder.ScheduledDateUtc);
        Assert.Equal(MaintenanceType.Preventive, savedOrder.Type);
        Assert.Equal("Revisión preventiva del panel", savedOrder.Description);
        Assert.InRange(savedOrder.CreatedAtUtc, beforeCreation, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0.5)]
    public void CreateReservation_WithPastOrTooCloseDate_ReturnsValidation(double hoursFromNow)
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.ScheduledDateUtc = DateTime.UtcNow.AddHours(hoursFromNow);

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La fecha y hora del mantenimiento debe ser posterior al momento actual.",
            result.ErrorMessage);
    }

    [Fact]
    public void CreateReservation_WithNonExistentPanel_ReturnsNotFound()
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.SolarPanelId = Guid.NewGuid();

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public void CreateReservation_WithInactivePanel_ReturnsValidation()
    {
        // Arrange
        var panelRepo = new FakeSolarPanelRepository();
        panelRepo.Panels.Add(new SolarPanel
        {
            Id = PanelId,
            SerieNumber = "SP-001",
            NormalizedSerieNumber = "sp-001",
            SolarParkId = Guid.NewGuid(),
            PowerW = 450,
            Efficiency = 21,
            Status = SolarPanelStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow
        });
        var (service, _) = CreateService(panelRepo: panelRepo);
        var request = ValidRequest();

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Debés seleccionar un panel solar activo.", result.ErrorMessage);
    }

    [Fact]
    public void CreateReservation_WithNonExistentTechnician_ReturnsNotFound()
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.TechnicianId = Guid.NewGuid();

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public void CreateReservation_WithInactiveTechnician_ReturnsValidation()
    {
        // Arrange
        var techRepo = new FakeTechnicianRepository();
        techRepo.PersistedTechnicians.Add(new Technician
        {
            Id = TechnicianId,
            FullName = "Inactivo, Técnico",
            DocumentId = "i-002",
            NormalizedDocumentId = "i002",
            Email = "inactivo@email.com",
            NormalizedEmail = "inactivo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow
        });
        var (service, _) = CreateService(techniciannRepo: techRepo);
        var request = ValidRequest();

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Debés seleccionar un técnico activo.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")] // 3 caracteres, menor al mínimo de 5.
    public void CreateReservation_WithShortDescription_ReturnsValidation(string description)
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.Description = description;

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La descripción del trabajo debe contener al menos 5 caracteres.",
            result.ErrorMessage);
    }

    [Fact]
    public void CreateReservation_WithFiveCharDescription_Succeeds()
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.Description = "corto"; // 5 caracteres

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void CreateReservation_WithDescriptionOver500_ReturnsValidation()
    {
        // Arrange
        var (service, _) = CreateService();
        var request = ValidRequest();
        request.Description = new string('a', 501);

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La descripción del trabajo no puede superar los 500 caracteres.",
            result.ErrorMessage);
    }

    [Fact]
    public void CreateReservation_WithPendingOrderSameDay_RequiresConfirmation()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = Guid.NewGuid(),
            Type = MaintenanceType.Corrective,
            // Mismo día local uruguayo (UTC-3) que la fecha de la reserva.
            ScheduledDateUtc = FutureDateUtc().AddHours(-5),
            Description = "Mantenimiento previo del mismo día",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        var request = ValidRequest();
        request.ConfirmSecondIntervention = false;

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.Equal(
            "El panel ya tiene un mantenimiento asignado para esa fecha. ¿Deseás agendar una segunda intervención?",
            result.ErrorMessage);
        Assert.DoesNotContain(maintenanceRepo.PersistedOrders, o => o.Description == "Revisión preventiva del panel");
    }

    [Fact]
    public void CreateReservation_WithConfirmedSecondIntervention_Succeeds()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        maintenanceRepo.PersistedOrders.Add(new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = PanelId,
            TechnicianId = Guid.NewGuid(),
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = FutureDateUtc().AddHours(-5),
            Description = "Mantenimiento previo del mismo día",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        var request = ValidRequest();
        request.ConfirmSecondIntervention = true;

        // Act
        var result = service.CreateReservation(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, maintenanceRepo.PersistedOrders.Count);
    }
}
