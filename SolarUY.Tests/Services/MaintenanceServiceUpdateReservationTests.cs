using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceUpdateReservationTests
{
    private static readonly Guid TechnicianOriginalId = Guid.NewGuid();
    private static readonly Guid TechnicianNewId = Guid.NewGuid();

    private static DateTime FutureDateUtc() => DateTime.UtcNow.AddHours(24);

    private static MaintenanceOrder PendingOrder(Guid? overrideOrderId = null) => new()
    {
        Id = overrideOrderId ?? Guid.NewGuid(),
        SolarPanelId = Guid.NewGuid(),
        TechnicianId = TechnicianOriginalId,
        Type = MaintenanceType.Preventive,
        ScheduledDateUtc = FutureDateUtc().AddHours(10),
        Description = "Revisión preventiva del panel",
        Status = MaintenanceStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static UpdateMaintenanceReservationRequest ValidUpdateRequest() => new()
    {
        TechnicianId = TechnicianNewId,
        ScheduledDateUtc = FutureDateUtc(),
        Description = "Revisión preventiva con herramientas nuevas"
    };

    private static (MaintenanceService Service, FakeMaintenanceRepository MaintenanceRepo) CreateService(
        FakeTechnicianRepository? technicianRepo = null,
        FakeMaintenanceRepository? maintenanceRepo = null)
    {
        technicianRepo ??= new FakeTechnicianRepository();
        AddActiveTechnician(technicianRepo, TechnicianNewId);

        maintenanceRepo ??= new FakeMaintenanceRepository();

        var service = new MaintenanceService(
            maintenanceRepo,
            new FakeSolarPanelRepository(),
            technicianRepo,
            NullLogger<MaintenanceService>.Instance);

        return (service, maintenanceRepo);
    }

    private static void AddActiveTechnician(FakeTechnicianRepository technicianRepo, Guid id, string name = "Nuevo, Técnico")
    {
        technicianRepo.PersistedTechnicians.Add(new Technician
        {
            Id = id,
            FullName = name,
            DocumentId = "n-001",
            NormalizedDocumentId = "n001",
            Email = "nuevo@email.com",
            NormalizedEmail = "nuevo@email.com",
            PhoneNumber = "+5491112345678",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public void UpdateReservation_OnPendingOrder_ChangesDateTechnicianAndDescription()
    {
        // Arrange
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = service.UpdateReservation(order.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(request.ScheduledDateUtc, order.ScheduledDateUtc);
        Assert.Equal(TechnicianNewId, order.TechnicianId);
        Assert.Equal("Revisión preventiva con herramientas nuevas", order.Description);
        Assert.Equal(MaintenanceStatus.Pending, order.Status);
        Assert.InRange(order.UpdatedAtUtc!.Value, beforeUpdate, DateTime.UtcNow);
    }

    [Fact]
    public void UpdateReservation_KeepingSameDateWithNewTechnician_Succeeds()
    {
        // Arrange — caso límite: cambiar solo el técnico manteniendo la fecha es válido.
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = new UpdateMaintenanceReservationRequest
        {
            TechnicianId = TechnicianNewId,
            ScheduledDateUtc = order.ScheduledDateUtc,
            Description = order.Description
        };

        // Act
        var result = service.UpdateReservation(order.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TechnicianNewId, order.TechnicianId);
        Assert.Equal(order.ScheduledDateUtc, order.ScheduledDateUtc);
    }

    [Fact]
    public void UpdateReservation_OnCompletedOrder_ReturnsConflictAndDoesNotChangeOrder()
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        order.Status = MaintenanceStatus.Completed;
        maintenanceRepo.PersistedOrders.Add(order);

        var result = service.UpdateReservation(order.Id, ValidUpdateRequest());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "No se pueden modificar mantenimientos que ya han sido completados o cancelados.",
            result.ErrorMessage);
        Assert.Equal(TechnicianOriginalId, order.TechnicianId);
        Assert.Null(order.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateReservation_OnCancelledOrder_ReturnsConflict()
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        order.Status = MaintenanceStatus.Cancelled;
        maintenanceRepo.PersistedOrders.Add(order);

        var result = service.UpdateReservation(order.Id, ValidUpdateRequest());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "No se pueden modificar mantenimientos que ya han sido completados o cancelados.",
            result.ErrorMessage);
    }

    [Fact]
    public void UpdateReservation_OnNonExistentOrder_ReturnsNotFound()
    {
        var (service, _) = CreateService();

        var result = service.UpdateReservation(Guid.NewGuid(), ValidUpdateRequest());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El mantenimiento solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void UpdateReservation_WithPastDate_ReturnsValidation()
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();
        request.ScheduledDateUtc = DateTime.UtcNow.AddHours(-1);

        var result = service.UpdateReservation(order.Id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La nueva fecha programada debe ser posterior al momento actual.",
            result.ErrorMessage);
        // La orden no debe haberse modificado.
        Assert.NotEqual(request.ScheduledDateUtc, order.ScheduledDateUtc);
    }

    [Fact]
    public void UpdateReservation_WithNonExistentTechnician_ReturnsNotFound()
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();
        request.TechnicianId = Guid.NewGuid();

        var result = service.UpdateReservation(order.Id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal(TechnicianOriginalId, order.TechnicianId);
    }

    [Fact]
    public void UpdateReservation_WithInactiveTechnician_ReturnsValidation()
    {
        var technicianRepo = new FakeTechnicianRepository();
        AddActiveTechnician(technicianRepo, TechnicianNewId);
        technicianRepo.PersistedTechnicians.Single(t => t.Id == TechnicianNewId).Status = TechnicianStatus.Inactive;
        var (service, maintenanceRepo) = CreateService(technicianRepo: technicianRepo);
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();

        var result = service.UpdateReservation(order.Id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Debés seleccionar un técnico activo.", result.ErrorMessage);
        Assert.Equal(TechnicianOriginalId, order.TechnicianId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void UpdateReservation_WithShortDescription_ReturnsValidation(string description)
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();
        request.Description = description;

        var result = service.UpdateReservation(order.Id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La descripción del trabajo debe contener al menos 5 caracteres.",
            result.ErrorMessage);
    }

    [Fact]
    public void UpdateReservation_WithDescriptionOver500_ReturnsValidation()
    {
        var (service, maintenanceRepo) = CreateService();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidUpdateRequest();
        request.Description = new string('a', 501);

        var result = service.UpdateReservation(order.Id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "La descripción del trabajo no puede superar los 500 caracteres.",
            result.ErrorMessage);
    }
}
