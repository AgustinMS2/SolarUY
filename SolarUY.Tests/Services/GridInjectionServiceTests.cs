using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

/// <summary>
/// Reglas de negocio de la spec 033 (activar/desactivar inyección a la red).
/// </summary>
public class GridInjectionServiceTests
{
    private static readonly string ConfirmationErrorMessage =
        "La confirmación no coincide. Escribí exactamente 'CONFIRMAR' para proceder.";

    private static readonly string CutConflictMessage =
        "La inyección a la red ya se encuentra detenida.";

    private static SolarPark ActivePark() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Parque Central",
        NormalizedName = "parque central",
        MaxCapacityKw = 500,
        PowerAlertThresholdKw = 50,
        Status = SolarParkStatus.Active,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static SolarNode NodeForPark(Guid parkId, string name = "Nodo 1") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NormalizedName = name.ToLowerInvariant(),
        SolarParkId = parkId,
        Status = SolarNodeStatus.Active,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static Inverter InverterForNode(Guid nodeId, string serie = "INV-001") => new()
    {
        Id = Guid.NewGuid(),
        SerieNumber = serie,
        NormalizedSerieNumber = serie.ToLowerInvariant(),
        BrandAndModel = "Huawei SUN2000",
        NominalCapacityKw = 100,
        SolarNodeId = nodeId,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static GridInjectionService CreateService(
        FakeSolarParkRepository parkRepo,
        FakeSolarNodeRepository nodeRepo,
        FakeInverterRepository inverterRepo,
        FakeGridInjectionEventRepository eventRepo,
        FakeInverterCommandSender sender)
    {
        return new GridInjectionService(
            parkRepo,
            nodeRepo,
            inverterRepo,
            eventRepo,
            sender,
            NullLogger<GridInjectionService>.Instance);
    }

    // RF-3: la confirmación debe coincidir exactamente con "CONFIRMAR" (sin Trim ni mayúsculas forzadas).
    [Theory]
    [InlineData("confirmar")]
    [InlineData("CONFIRMAR ")]
    [InlineData(" CONFIRMAR")]
    [InlineData("Confirmar")]
    public void CutPark_ConfirmacionIncorrecta_DevuelveValidationYNoEnviaOrden(string texto)
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, sender);

        var result = service.CutParkInjection(park.Id, texto, "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(ConfirmationErrorMessage, result.ErrorMessage);
        Assert.True(park.GridInjectionEnabled);
        Assert.Empty(eventRepo.PersistedEvents);
        Assert.Empty(sender.SentInjectionCutCommands);
    }

    // RF-3: la misma regla aplica al corte por inversor individual.
    [Fact]
    public void CutInverter_ConfirmacionIncorrecta_DevuelveValidationYNoEnviaOrden()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        var node = NodeForPark(park.Id);
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        nodeRepo.PersistedNodes.Add(node);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverterRepo.Inverters.Add(inverter);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);

        var result = service.CutInverterInjection(node.Id, "confirmar", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(ConfirmationErrorMessage, result.ErrorMessage);
        Assert.True(inverter.GridInjectionEnabled);
        Assert.Empty(eventRepo.PersistedEvents);
        Assert.Empty(sender.SentInjectionCutCommands);
    }

    // RF-2: corte confirmado del parque cambia el estado del parque y de todos sus inversores,
    // envía la orden a cada uno y registra el evento con OrdersSent = N.
    [Fact]
    public void CutPark_ConfirmacionCorrecta_CortaParqueEInversoresYRegistraEvento()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var nodeA = NodeForPark(park.Id, "Nodo A");
        var nodeB = NodeForPark(park.Id, "Nodo B");
        nodeRepo.PersistedNodes.Add(nodeA);
        nodeRepo.PersistedNodes.Add(nodeB);
        var inverterRepo = new FakeInverterRepository();
        var inverterA = InverterForNode(nodeA.Id, "INV-001");
        var inverterB = InverterForNode(nodeB.Id, "INV-002");
        inverterRepo.Inverters.Add(inverterA);
        inverterRepo.Inverters.Add(inverterB);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);
        var before = DateTime.UtcNow;

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.True(result.Success);
        Assert.False(park.GridInjectionEnabled);
        Assert.False(inverterA.GridInjectionEnabled);
        Assert.False(inverterB.GridInjectionEnabled);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(InjectionTarget.Park, injectionEvent.TargetType);
        Assert.Equal(park.Id, injectionEvent.TargetId);
        Assert.Equal("Parque Central", injectionEvent.TargetName);
        Assert.Equal(park.Id, injectionEvent.ParkId);
        Assert.Equal(InjectionAction.Cut, injectionEvent.Action);
        Assert.Equal("admin@solaruy.local", injectionEvent.OrderedBy);
        Assert.InRange(injectionEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.Equal(2, injectionEvent.OrdersSent);
        Assert.Equal(["INV-001", "INV-002"], sender.SentInjectionCutCommands);
    }

    // Caso límite: parque sin inversores se corta igual, con OrdersSent = 0.
    [Fact]
    public void CutPark_SinInversores_CortaParqueConOrdersSentCero()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        nodeRepo.PersistedNodes.Add(NodeForPark(park.Id));
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, new FakeInverterRepository(), eventRepo, sender);

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.True(result.Success);
        Assert.False(park.GridInjectionEnabled);
        Assert.Empty(sender.SentInjectionCutCommands);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(0, injectionEvent.OrdersSent);
    }

    // RF-5: reanudación confirmada restaura el parque y sus inversores y registra el evento.
    [Fact]
    public void ResumePark_ConfirmacionCorrecta_RestauraParqueEInversoresYRegistraEvento()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        park.GridInjectionEnabled = false;
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var node = NodeForPark(park.Id);
        nodeRepo.PersistedNodes.Add(node);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverter.GridInjectionEnabled = false;
        inverterRepo.Inverters.Add(inverter);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);
        var before = DateTime.UtcNow;

        var result = service.ResumeParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.True(result.Success);
        Assert.True(park.GridInjectionEnabled);
        Assert.True(inverter.GridInjectionEnabled);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(InjectionAction.Resume, injectionEvent.Action);
        Assert.Equal(InjectionTarget.Park, injectionEvent.TargetType);
        Assert.Equal(1, injectionEvent.OrdersSent);
        Assert.InRange(injectionEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.Equal([inverter.SerieNumber], sender.SentInjectionResumeCommands);
    }

    // Caso límite: corte repetido no reenvía órdenes y responde con conflicto.
    [Fact]
    public void CutPark_YaCortada_DevuelveConflictSinReenviarOrdenes()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        park.GridInjectionEnabled = false;
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, sender);

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(CutConflictMessage, result.ErrorMessage);
        Assert.False(park.GridInjectionEnabled);
        Assert.Empty(sender.SentInjectionCutCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite: reanudación repetida responde con conflicto sin reenviar órdenes.
    [Fact]
    public void ResumePark_YaActiva_DevuelveConflictSinReenviarOrdenes()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, sender);

        var result = service.ResumeParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.True(park.GridInjectionEnabled);
        Assert.Empty(sender.SentInjectionResumeCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    [Fact]
    public void CutPark_ParqueInexistente_DevuelveNotFound()
    {
        var eventRepo = new FakeGridInjectionEventRepository();
        var service = CreateService(new FakeSolarParkRepository(), new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, new FakeInverterCommandSender());

        var result = service.CutParkInjection(Guid.NewGuid(), "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    [Fact]
    public void ResumePark_ParqueInexistente_DevuelveNotFound()
    {
        var eventRepo = new FakeGridInjectionEventRepository();
        var service = CreateService(new FakeSolarParkRepository(), new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, new FakeInverterCommandSender());

        var result = service.ResumeParkInjection(Guid.NewGuid(), "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite defensivo: parque inactivo rechaza la orden.
    [Fact]
    public void CutPark_ParqueInactivo_DevuelveConflict()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        park.Status = SolarParkStatus.Inactive;
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, sender);

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.True(park.GridInjectionEnabled);
        Assert.Empty(sender.SentInjectionCutCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite "todo o nada": si un inversor no confirma la orden, no cambia ningún estado,
    // no se registra el evento y se reporta el error con la serie del inversor.
    [Fact]
    public void CutPark_FallaUnInversor_DevuelveUnavailableSinCambiosNiEvento()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var nodeA = NodeForPark(park.Id, "Nodo A");
        var nodeB = NodeForPark(park.Id, "Nodo B");
        nodeRepo.PersistedNodes.Add(nodeA);
        nodeRepo.PersistedNodes.Add(nodeB);
        var inverterRepo = new FakeInverterRepository();
        var inverterA = InverterForNode(nodeA.Id, "INV-001");
        var inverterB = InverterForNode(nodeB.Id, "INV-002");
        inverterRepo.Inverters.Add(inverterA);
        inverterRepo.Inverters.Add(inverterB);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender { Succeeds = false };
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal("Error de comunicación con el inversor INV-001. La orden no pudo confirmarse.", result.ErrorMessage);
        Assert.True(park.GridInjectionEnabled);
        Assert.True(inverterA.GridInjectionEnabled);
        Assert.True(inverterB.GridInjectionEnabled);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite: corte del inversor individual del nodo (sin cortar el parque entero).
    [Fact]
    public void CutInverter_ConfirmacionCorrecta_CortaSoloElInversorYRegistraEvento()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var node = NodeForPark(park.Id);
        nodeRepo.PersistedNodes.Add(node);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverterRepo.Inverters.Add(inverter);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);
        var before = DateTime.UtcNow;

        var result = service.CutInverterInjection(node.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", "Sobretensión en la red");

        Assert.True(result.Success);
        Assert.False(inverter.GridInjectionEnabled);
        Assert.True(park.GridInjectionEnabled);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(InjectionTarget.Inverter, injectionEvent.TargetType);
        Assert.Equal(inverter.Id, injectionEvent.TargetId);
        Assert.Equal("INV-001", injectionEvent.TargetName);
        Assert.Equal(park.Id, injectionEvent.ParkId);
        Assert.Equal(InjectionAction.Cut, injectionEvent.Action);
        Assert.Equal(1, injectionEvent.OrdersSent);
        Assert.InRange(injectionEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.Equal([inverter.SerieNumber], sender.SentInjectionCutCommands);
    }

    // Caso límite: reanudación del inversor individual.
    [Fact]
    public void ResumeInverter_ConfirmacionCorrecta_ReanudaInversorYRegistraEvento()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var node = NodeForPark(park.Id);
        nodeRepo.PersistedNodes.Add(node);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverter.GridInjectionEnabled = false;
        inverterRepo.Inverters.Add(inverter);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);
        var before = DateTime.UtcNow;

        var result = service.ResumeInverterInjection(node.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.True(result.Success);
        Assert.True(inverter.GridInjectionEnabled);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal(InjectionTarget.Inverter, injectionEvent.TargetType);
        Assert.Equal(InjectionAction.Resume, injectionEvent.Action);
        Assert.Equal(1, injectionEvent.OrdersSent);
        Assert.InRange(injectionEvent.OrderedAtUtc, before, DateTime.UtcNow);
        Assert.Equal([inverter.SerieNumber], sender.SentInjectionResumeCommands);
    }

    // Caso límite: corte repetido por inversor responde con conflicto sin reenviar órdenes.
    [Fact]
    public void CutInverter_YaCortado_DevuelveConflictSinReenviarOrdenes()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        var node = NodeForPark(park.Id);
        nodeRepo.PersistedNodes.Add(node);
        var inverterRepo = new FakeInverterRepository();
        var inverter = InverterForNode(node.Id);
        inverter.GridInjectionEnabled = false;
        inverterRepo.Inverters.Add(inverter);
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, inverterRepo, eventRepo, sender);

        var result = service.CutInverterInjection(node.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.False(inverter.GridInjectionEnabled);
        Assert.Empty(sender.SentInjectionCutCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // Caso límite: nodo sin inversor no tiene inyección que cortar.
    [Fact]
    public void CutInverter_NodoSinInversor_DevuelveNotFound()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var nodeRepo = new FakeSolarNodeRepository();
        nodeRepo.PersistedNodes.Add(NodeForPark(park.Id));
        var eventRepo = new FakeGridInjectionEventRepository();
        var sender = new FakeInverterCommandSender();
        var service = CreateService(parkRepo, nodeRepo, new FakeInverterRepository(), eventRepo, sender);

        var result = service.CutInverterInjection(Guid.NewGuid(), "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(sender.SentInjectionCutCommands);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    [Fact]
    public void CutInverter_NodoInexistente_DevuelveNotFound()
    {
        var eventRepo = new FakeGridInjectionEventRepository();
        var service = CreateService(new FakeSolarParkRepository(), new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, new FakeInverterCommandSender());

        var result = service.CutInverterInjection(Guid.NewGuid(), "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", null);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Empty(eventRepo.PersistedEvents);
    }

    // NFR: auditoría con usuario, IP y motivo del corte persistidos en la bitácora.
    [Fact]
    public void CutPark_MotivoEIpPersistidos()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, new FakeInverterCommandSender());

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", "  Curtailment solicitado por UTE  ");

        Assert.True(result.Success);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Equal("Curtailment solicitado por UTE", injectionEvent.Reason);
        Assert.Equal("10.0.0.5", injectionEvent.RemoteIp);
        Assert.Equal("admin@solaruy.local", injectionEvent.OrderedBy);
    }

    // El motivo es opcional: un corte sin motivo se registra igualmente.
    [Fact]
    public void CutPark_MotivoVacioPermitido()
    {
        var parkRepo = new FakeSolarParkRepository();
        var park = ActivePark();
        parkRepo.Parks.Add(park);
        var eventRepo = new FakeGridInjectionEventRepository();
        var service = CreateService(parkRepo, new FakeSolarNodeRepository(),
            new FakeInverterRepository(), eventRepo, new FakeInverterCommandSender());

        var result = service.CutParkInjection(park.Id, "CONFIRMAR", "admin@solaruy.local", "10.0.0.5", "   ");

        Assert.True(result.Success);
        var injectionEvent = Assert.Single(eventRepo.PersistedEvents);
        Assert.Null(injectionEvent.Reason);
    }
}
