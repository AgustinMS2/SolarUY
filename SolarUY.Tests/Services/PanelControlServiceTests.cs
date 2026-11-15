using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

/// <summary>
/// Reglas de negocio de la spec 032 (activar/desactivar panel).
/// </summary>
public class PanelControlServiceTests
{
    private static readonly string MaintenanceActiveMessage =
        "No se puede reactivar el panel mientras haya personal trabajando en un mantenimiento activo.";

    private static readonly string CommunicationErrorMessage =
        "Error de comunicación con el inversor. La orden no pudo confirmarse.";

    private static SolarPanel ActivePanel() => new()
    {
        Id = Guid.NewGuid(),
        SerieNumber = "PANEL-001",
        NormalizedSerieNumber = "panel-001",
        PowerW = 550,
        Efficiency = 0.21,
        SolarParkId = Guid.NewGuid(),
        Status = SolarPanelStatus.Active,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static Inverter InverterForNode(Guid nodeId) => new()
    {
        Id = Guid.NewGuid(),
        SerieNumber = "INV-001",
        NormalizedSerieNumber = "inv-001",
        BrandAndModel = "Huawei SUN2000",
        NominalCapacityKw = 100,
        Endpoint = "https://inverter-sg-01.local/api",
        SolarNodeId = nodeId,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static PanelControlService CreateService(
        FakeSolarPanelRepository panelRepo,
        FakeMaintenanceRepository maintenanceRepo,
        FakePanelControlEventRepository eventRepo,
        FakeInverterRepository inverterRepo,
        FakeInverterCommandSender sender)
    {
        return new PanelControlService(
            panelRepo,
            maintenanceRepo,
            eventRepo,
            inverterRepo,
            sender,
            NullLogger<PanelControlService>.Instance);
    }

    private static (PanelControlService Service, FakePanelControlEventRepository EventRepo,
        FakeInverterCommandSender Sender) CreateDefault()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var maintenanceRepo = new FakeMaintenanceRepository();
        var eventRepo = new FakePanelControlEventRepository();
        var inverterRepo = new FakeInverterRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(panelRepo, maintenanceRepo, eventRepo, inverterRepo, sender);
        return (service, eventRepo, sender);
    }

    // RF-2: desactivar un panel activo cambia el estado, envía la orden y registra la bitácora.
    [Fact]
    public void Deactivate_PanelActivo_CambiaEstadoYRegistraBitacora()
    {
        // Arrange
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        var node = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo 1", NormalizedName = "nodo 1", SolarParkId = panel.SolarParkId };
        panel.SolarNodeId = node.Id;
        panelRepo.Panels.Add(panel);
        panelRepo.Parks.Add(new SolarPark { Id = panel.SolarParkId, Name = "Parque", NormalizedName = "parque" });

        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverterRepo.Inverters.Add(inverter);

        var maintenanceRepo = new FakeMaintenanceRepository();
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(panelRepo, maintenanceRepo, eventRepo, inverterRepo, sender);
        var before = DateTime.UtcNow;

        // Act
        var result = service.Deactivate(panel.Id, "admin@solaruy.local");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SolarPanelStatus.Deactivated, panel.Status);
        var controlEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(PanelControlAction.Deactivate, controlEvent.Action);
        Assert.Equal(panel.Id, controlEvent.SolarPanelId);
        Assert.Equal("admin@solaruy.local", controlEvent.OrderedBy);
        Assert.InRange(controlEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.True(controlEvent.OrderSentToInverter);
        Assert.Equal(inverter.SerieNumber, controlEvent.InverterSerieNumber);
    }

    [Fact]
    public void Deactivate_PanelInexistente_DevuelveNotFound()
    {
        var (service, eventRepo, _) = CreateDefault();

        var result = service.Deactivate(Guid.NewGuid(), "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    [Fact]
    public void Deactivate_PanelYaDesactivado_DevuelveConflict()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panel.Status = SolarPanelStatus.Deactivated;
        panelRepo.Panels.Add(panel);
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(),
            new FakePanelControlEventRepository(), new FakeInverterRepository(), new FakeInverterCommandSender());

        var result = service.Deactivate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(SolarPanelStatus.Deactivated, panel.Status);
    }

    [Fact]
    public void Deactivate_PanelDadoDeBaja_DevuelveConflict()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panel.Status = SolarPanelStatus.Inactive;
        panelRepo.Panels.Add(panel);
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(),
            new FakePanelControlEventRepository(), new FakeInverterRepository(), new FakeInverterCommandSender());

        var result = service.Deactivate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(SolarPanelStatus.Inactive, panel.Status);
    }

    // Caso límite aclarado: panel sin nodo/inversor se desactiva sin enviar orden.
    [Fact]
    public void Deactivate_SinInversor_CambiaEstadoSinOrden()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel(); // SolarNodeId = null
        panelRepo.Panels.Add(panel);
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(),
            eventRepo, new FakeInverterRepository(), sender);

        var result = service.Deactivate(panel.Id, "admin@solaruy.local");

        Assert.True(result.Success);
        Assert.Equal(SolarPanelStatus.Deactivated, panel.Status);
        Assert.Empty(sender.SentDeactivateCommands);
        var controlEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.False(controlEvent.OrderSentToInverter);
        Assert.Null(controlEvent.InverterSerieNumber);
    }

    // Caso límite: falla la comunicación con el inversor, no cambia el estado ni registra bitácora.
    [Fact]
    public void Deactivate_FallaComunicacion_DevuelveUnavailableYNoCambiaEstado()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        var node = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo 1", NormalizedName = "nodo 1", SolarParkId = panel.SolarParkId };
        panel.SolarNodeId = node.Id;
        panelRepo.Panels.Add(panel);
        var inverterRepo = new FakeInverterRepository();
        inverterRepo.Inverters.Add(InverterForNode(node.Id));
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender { Succeeds = false };
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(), eventRepo, inverterRepo, sender);

        var result = service.Deactivate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal(CommunicationErrorMessage, result.ErrorMessage);
        Assert.Equal(SolarPanelStatus.Active, panel.Status);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // RF-4: reactivar un panel desactivado restablece el estado Operativo con bitácora.
    [Fact]
    public void Activate_PanelDesactivado_CambiaEstadoYRegistraBitacora()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panel.Status = SolarPanelStatus.Deactivated;
        var node = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo 1", NormalizedName = "nodo 1", SolarParkId = panel.SolarParkId };
        panel.SolarNodeId = node.Id;
        panelRepo.Panels.Add(panel);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverterRepo.Inverters.Add(inverter);
        var maintenanceRepo = new FakeMaintenanceRepository();
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(panelRepo, maintenanceRepo, eventRepo, inverterRepo, sender);
        var before = DateTime.UtcNow;

        var result = service.Activate(panel.Id, "admin@solaruy.local");

        Assert.True(result.Success);
        Assert.Equal(SolarPanelStatus.Active, panel.Status);
        var controlEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(PanelControlAction.Activate, controlEvent.Action);
        Assert.Equal(panel.Id, controlEvent.SolarPanelId);
        Assert.Equal("admin@solaruy.local", controlEvent.OrderedBy);
        Assert.InRange(controlEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.True(controlEvent.OrderSentToInverter);
        Assert.Equal(inverter.SerieNumber, controlEvent.InverterSerieNumber);
    }

    [Fact]
    public void Activate_PanelInexistente_DevuelveNotFound()
    {
        var (service, eventRepo, _) = CreateDefault();

        var result = service.Activate(Guid.NewGuid(), "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    [Fact]
    public void Activate_PanelActivo_DevuelveConflict()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panelRepo.Panels.Add(panel);
        var eventRepo = new FakePanelControlEventRepository();
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(),
            eventRepo, new FakeInverterRepository(), new FakeInverterCommandSender());

        var result = service.Activate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(SolarPanelStatus.Active, panel.Status);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // RF-5: mantenimiento Pendiente del panel impide la reactivación remota.
    [Fact]
    public void Activate_ConMantenimientoPendiente_BloqueaConMensajeRF5()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panel.Status = SolarPanelStatus.Deactivated;
        panelRepo.Panels.Add(panel);
        var maintenanceRepo = new FakeMaintenanceRepository();
        maintenanceRepo.PersistedOrders.Add(new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panel.Id,
            TechnicianId = Guid.NewGuid(),
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = DateTime.UtcNow.AddHours(-2),
            Description = "Reemplazo de celdas dañadas",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(panelRepo, maintenanceRepo, eventRepo,
            new FakeInverterRepository(), sender);

        var result = service.Activate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(MaintenanceActiveMessage, result.ErrorMessage);
        Assert.Equal(SolarPanelStatus.Deactivated, panel.Status);
        Assert.Empty(sender.SentActivateCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite: falla la comunicación al reactivar; el panel sigue desactivado.
    [Fact]
    public void Activate_FallaComunicacion_DevuelveUnavailable()
    {
        var panelRepo = new FakeSolarPanelRepository();
        var panel = ActivePanel();
        panel.Status = SolarPanelStatus.Deactivated;
        var node = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo 1", NormalizedName = "nodo 1", SolarParkId = panel.SolarParkId };
        panel.SolarNodeId = node.Id;
        panelRepo.Panels.Add(panel);
        var inverterRepo = new FakeInverterRepository();
        inverterRepo.Inverters.Add(InverterForNode(node.Id));
        var eventRepo = new FakePanelControlEventRepository();
        var sender = new FakeInverterCommandSender { Succeeds = false };
        var service = CreateService(panelRepo, new FakeMaintenanceRepository(), eventRepo, inverterRepo, sender);

        var result = service.Activate(panel.Id, "admin@solaruy.local");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal(CommunicationErrorMessage, result.ErrorMessage);
        Assert.Equal(SolarPanelStatus.Deactivated, panel.Status);
        Assert.Empty(eventRepo.PersistedEvents);
    }
}
