using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceCancelTests
{
    private static MaintenanceOrder PendingOrder(Guid? overrideOrderId = null) => new()
    {
        Id = overrideOrderId ?? Guid.NewGuid(),
        SolarPanelId = Guid.NewGuid(),
        TechnicianId = Guid.NewGuid(),
        Type = MaintenanceType.Preventive,
        ScheduledDateUtc = DateTime.UtcNow.AddHours(24),
        Description = "Revisión preventiva del panel",
        Status = MaintenanceStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static CancelMaintenanceRequest ValidCancelRequest() => new()
    {
        Reason = "El panel se reemplazó por una falla de hardware."
    };

    private static MaintenanceService CreateService(
        FakeMaintenanceRepository maintenanceRepo,
        FakeSolarPanelRepository? panelRepo = null,
        FakeTechnicianRepository? technicianRepo = null)
    {
        return new MaintenanceService(
            maintenanceRepo,
            panelRepo ?? new FakeSolarPanelRepository(),
            technicianRepo ?? new FakeTechnicianRepository(),
            NullLogger<MaintenanceService>.Instance);
    }

    [Fact]
    public void CancelReservation_OnPendingOrder_SetsCancelledWithReasonAndUtcDate()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = ValidCancelRequest();
        var beforeCancel = DateTime.UtcNow;
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MaintenanceStatus.Cancelled, order.Status);
        Assert.Equal(request.Reason, order.CancellationReason);
        Assert.InRange(order.UpdatedAtUtc!.Value, beforeCancel, DateTime.UtcNow);
        // Sigue referenciando técnico y panel; se liberan directamente para nuevas asignaciones.
        Assert.NotEqual(Guid.Empty, order.TechnicianId);
        Assert.NotEqual(Guid.Empty, order.SolarPanelId);
    }

    [Fact]
    public void CancelReservation_TrimsReason()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = new CancelMaintenanceRequest { Reason = "   Se agendó por error, ya no es necesario   " };
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Se agendó por error, ya no es necesario", order.CancellationReason);
    }

    [Theory]
    [InlineData(9)]  // 9 caracteres: debajo del mínimo de 10.
    [InlineData(1)]
    [InlineData(0)]
    public void CancelReservation_WithReasonShorterThan10_ReturnsValidation(int reasonLength)
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = new CancelMaintenanceRequest { Reason = reasonLength == 0 ? "" : new string('a', reasonLength) };
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.",
            result.ErrorMessage);
        // La orden no debe haberse cancelado.
        Assert.Equal(MaintenanceStatus.Pending, order.Status);
        Assert.Null(order.UpdatedAtUtc);
    }

    [Fact]
    public void CancelReservation_WithReasonOfExactly10Chars_Succeeds()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = new CancelMaintenanceRequest { Reason = new string('a', 10) };
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MaintenanceStatus.Cancelled, order.Status);
    }

    [Fact]
    public void CancelReservation_WithReasonOver500_ReturnsValidation()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        maintenanceRepo.PersistedOrders.Add(order);
        var request = new CancelMaintenanceRequest { Reason = new string('a', 501) };
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "El motivo de cancelación no puede superar los 500 caracteres.",
            result.ErrorMessage);
        Assert.Equal(MaintenanceStatus.Pending, order.Status);
    }

    [Fact]
    public void CancelReservation_OnCompletedOrder_ReturnsConflictAndDoesNotCancel()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        order.Status = MaintenanceStatus.Completed;
        maintenanceRepo.PersistedOrders.Add(order);
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, ValidCancelRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "El mantenimiento ya se encuentra finalizado o cancelado.",
            result.ErrorMessage);
        Assert.Equal(MaintenanceStatus.Completed, order.Status);
        Assert.Null(order.CancellationReason);
    }

    [Fact]
    public void CancelReservation_OnAlreadyCancelledOrder_ReturnsConflict()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var order = PendingOrder();
        order.Status = MaintenanceStatus.Cancelled;
        maintenanceRepo.PersistedOrders.Add(order);
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(order.Id, ValidCancelRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "El mantenimiento ya se encuentra finalizado o cancelado.",
            result.ErrorMessage);
    }

    [Fact]
    public void CancelReservation_OnNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var maintenanceRepo = new FakeMaintenanceRepository();
        var service = CreateService(maintenanceRepo);

        // Act
        var result = service.CancelReservation(Guid.NewGuid(), ValidCancelRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El mantenimiento solicitado no existe.", result.ErrorMessage);
    }
}
