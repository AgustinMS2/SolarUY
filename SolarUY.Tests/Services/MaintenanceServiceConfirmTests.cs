using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class MaintenanceServiceConfirmTests
{
    private const string NotesRequiredMessage = "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres.";
    private const string NotesTooLongMessage = "Las observaciones técnicas no pueden superar los 1000 caracteres.";
    private const string PartsTooLongMessage = "Los componentes reemplazados no pueden superar los 300 caracteres.";
    private const string OutcomeRequiredMessage = "Seleccioná el estado del panel tras la tarea.";
    private const string AlreadyClosedMessage = "Este mantenimiento ya fue completado previamente.";

    private readonly FakeMaintenanceRepository _repository = new();
    private readonly MaintenanceService _service;

    public MaintenanceServiceConfirmTests()
    {
        _service = new MaintenanceService(
            _repository,
            new FakeSolarPanelRepository(),
            new FakeTechnicianRepository(),
            NullLogger<MaintenanceService>.Instance);
    }

    private MaintenanceOrder AddOrder(MaintenanceStatus status = MaintenanceStatus.Pending)
    {
        var order = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = Guid.NewGuid(),
            TechnicianId = Guid.NewGuid(),
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = DateTime.UtcNow.AddHours(24),
            Description = "Revisión del string",
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        _repository.PersistedOrders.Add(order);
        return order;
    }

    private static ConfirmMaintenanceRequest Request(
        string? notes = "Se reemplazó el fusible del string y se verificó la continuidad.",
        string? parts = "Fusible de string 15A",
        string? outcome = "Operational")
    {
        return new ConfirmMaintenanceRequest { Notes = notes, ReplacedParts = parts, PanelOutcome = outcome };
    }

    // ---- RF-1

    [Fact]
    public void ConfirmMaintenance_OnPendingOrder_CompletesItAndStoresTheReport()
    {
        var order = AddOrder();
        var before = DateTime.UtcNow;

        var result = _service.ConfirmMaintenance(order.Id, Request());

        Assert.True(result.Success);
        Assert.Equal(MaintenanceStatus.Completed, order.Status);
        Assert.Equal("Se reemplazó el fusible del string y se verificó la continuidad.", order.ResolutionNotes);
        Assert.Equal("Fusible de string 15A", order.ReplacedParts);
        Assert.Equal(MaintenancePanelOutcome.Operational, order.PanelOutcome);
        Assert.InRange(order.CompletedAtUtc!.Value, before, DateTime.UtcNow);
        Assert.InRange(order.UpdatedAtUtc!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void ConfirmMaintenance_WithFollowUpOutcome_StoresIt()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(outcome: "NeedsFollowUp"));

        Assert.True(result.Success);
        Assert.Equal(MaintenancePanelOutcome.NeedsFollowUp, order.PanelOutcome);
    }

    [Fact]
    public void ConfirmMaintenance_TrimsNotesAndReplacedParts()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: "   Limpieza y ajuste de conectores   ", parts: "  Conector MC4  "));

        Assert.True(result.Success);
        Assert.Equal("Limpieza y ajuste de conectores", order.ResolutionNotes);
        Assert.Equal("Conector MC4", order.ReplacedParts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfirmMaintenance_WithoutReplacedParts_IsValidAndStoresNull(string? parts)
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(parts: parts));

        Assert.True(result.Success);
        Assert.Null(order.ReplacedParts);
    }

    // ---- RF-2

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    [InlineData("123456789")]
    [InlineData("   123456789   ")]
    public void ConfirmMaintenance_WithMissingOrShortNotes_ReturnsValidationAndChangesNothing(string? notes)
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: notes));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(NotesRequiredMessage, result.ErrorMessage);
        AssertUntouched(order);
    }

    [Fact]
    public void ConfirmMaintenance_WithNotesOfExactly10Characters_IsAccepted()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: new string('a', 10)));

        Assert.True(result.Success);
    }

    [Fact]
    public void ConfirmMaintenance_WithNotesOfExactly1000Characters_IsAccepted()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: new string('a', 1000)));

        Assert.True(result.Success);
        Assert.Equal(1000, order.ResolutionNotes!.Length);
    }

    [Fact]
    public void ConfirmMaintenance_WithNotesLongerThan1000_ReturnsValidationAndChangesNothing()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: new string('a', 1001)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(NotesTooLongMessage, result.ErrorMessage);
        AssertUntouched(order);
    }

    [Fact]
    public void ConfirmMaintenance_WithReplacedPartsOfExactly300Characters_IsAccepted()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(parts: new string('p', 300)));

        Assert.True(result.Success);
    }

    [Fact]
    public void ConfirmMaintenance_WithReplacedPartsLongerThan300_ReturnsValidationAndChangesNothing()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(parts: new string('p', 301)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(PartsTooLongMessage, result.ErrorMessage);
        AssertUntouched(order);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Otro")]
    [InlineData("operational")]
    [InlineData("1")]
    public void ConfirmMaintenance_WithMissingOrInvalidPanelOutcome_ReturnsValidationAndChangesNothing(string? outcome)
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(outcome: outcome));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(OutcomeRequiredMessage, result.ErrorMessage);
        AssertUntouched(order);
    }

    [Fact]
    public void ConfirmMaintenance_ReportsNotesBeforePartsAndOutcome()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: "corto", parts: new string('p', 301), outcome: null));

        Assert.Equal(NotesRequiredMessage, result.ErrorMessage);
    }

    [Fact]
    public void ConfirmMaintenance_ReportsPartsBeforeOutcome()
    {
        var order = AddOrder();

        var result = _service.ConfirmMaintenance(order.Id, Request(parts: new string('p', 301), outcome: null));

        Assert.Equal(PartsTooLongMessage, result.ErrorMessage);
    }

    // ---- RF-4

    [Theory]
    [InlineData(MaintenanceStatus.Completed)]
    [InlineData(MaintenanceStatus.Cancelled)]
    public void ConfirmMaintenance_OnClosedOrder_ReturnsConflictAndKeepsTheOrderAsIs(MaintenanceStatus status)
    {
        var order = AddOrder(status);
        order.ResolutionNotes = "Notas originales";

        var result = _service.ConfirmMaintenance(order.Id, Request());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(AlreadyClosedMessage, result.ErrorMessage);
        Assert.Equal(status, order.Status);
        Assert.Equal("Notas originales", order.ResolutionNotes);
        Assert.Null(order.PanelOutcome);
    }

    [Fact]
    public void ConfirmMaintenance_OnClosedOrder_ReportsTheConflictBeforeInvalidFields()
    {
        var order = AddOrder(MaintenanceStatus.Completed);

        var result = _service.ConfirmMaintenance(order.Id, Request(notes: "", outcome: null));

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public void ConfirmMaintenance_CannotBeConfirmedTwice()
    {
        var order = AddOrder();
        Assert.True(_service.ConfirmMaintenance(order.Id, Request()).Success);

        var second = _service.ConfirmMaintenance(order.Id, Request(notes: "Segundo intento de confirmación"));

        Assert.Equal(ServiceErrorType.Conflict, second.ErrorType);
        Assert.Equal("Se reemplazó el fusible del string y se verificó la continuidad.", order.ResolutionNotes);
    }

    // ---- RF-6

    [Fact]
    public void ConfirmMaintenance_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var result = _service.ConfirmMaintenance(Guid.NewGuid(), Request(notes: "", outcome: null));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El mantenimiento solicitado no existe.", result.ErrorMessage);
    }

    private static void AssertUntouched(MaintenanceOrder order)
    {
        Assert.Equal(MaintenanceStatus.Pending, order.Status);
        Assert.Null(order.ResolutionNotes);
        Assert.Null(order.ReplacedParts);
        Assert.Null(order.PanelOutcome);
        Assert.Null(order.CompletedAtUtc);
    }
}
