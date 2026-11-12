using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Data;

public static class DbInitializer
{
    public static void Seed(SolarUyDbContext context)
    {
        // Si ya existen parques cargados, no realizamos la precarga para no duplicar datos
        if (context.SolarParks.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. Parques Solares
        var saltoGrande = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Solar Salto Grande",
            NormalizedName = "parque solar salto grande",
            Latitude = -31.2333,
            Longitude = -57.9000,
            MaxCapacityKw = 500.0,
            PowerAlertThresholdKw = 75.0,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = now.AddMonths(-6)
        };

        var cerroDelMarco = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Solar Cerro del Marco",
            NormalizedName = "parque solar cerro del marco",
            Latitude = -30.9025,
            Longitude = -55.5508,
            MaxCapacityKw = 350.0,
            PowerAlertThresholdKw = 50.0,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var pasoDeLosToros = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Solar Paso de los Toros",
            NormalizedName = "parque solar paso de los toros",
            Latitude = -32.8167,
            Longitude = -56.5167,
            MaxCapacityKw = 250.0,
            PowerAlertThresholdKw = 40.0,
            Status = SolarParkStatus.Inactive,
            CreatedAtUtc = now.AddMonths(-2)
        };

        context.SolarParks.AddRange(saltoGrande, cerroDelMarco, pasoDeLosToros);
        context.SaveChanges();

        // 2. Nodos Solares
        var nodeSgNorte = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo SG-Norte",
            NormalizedName = "nodo sg-norte",
            SolarParkId = saltoGrande.Id,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var nodeSgSur = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo SG-Sur",
            NormalizedName = "nodo sg-sur",
            SolarParkId = saltoGrande.Id,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var nodeSgReserva = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo SG-Reserva",
            NormalizedName = "nodo sg-reserva",
            SolarParkId = saltoGrande.Id,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = now.AddMonths(-3)
        };

        var nodeCmCentral = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo CM-Central",
            NormalizedName = "nodo cm-central",
            SolarParkId = cerroDelMarco.Id,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var nodeCmEste = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo CM-Este",
            NormalizedName = "nodo cm-este",
            SolarParkId = cerroDelMarco.Id,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var nodePt01 = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo PT-01",
            NormalizedName = "nodo pt-01",
            SolarParkId = pasoDeLosToros.Id,
            Status = SolarNodeStatus.Inactive,
            CreatedAtUtc = now.AddMonths(-2)
        };

        context.SolarNodes.AddRange(nodeSgNorte, nodeSgSur, nodeSgReserva, nodeCmCentral, nodeCmEste, nodePt01);
        context.SaveChanges();

        // 3. Inversores (Asignados a nodos)
        var invSg01 = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-SG-001",
            NormalizedSerieNumber = "inv-sg-001",
            BrandAndModel = "SMA Sunny Tripower CORE2 110kW",
            NominalCapacityKw = 110.0,
            Endpoint = "https://inverter-sg-01.local/api",
            SolarNodeId = nodeSgNorte.Id,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var invSg02 = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-SG-002",
            NormalizedSerieNumber = "inv-sg-002",
            BrandAndModel = "Huawei SUN2000-100KTL-M2",
            NominalCapacityKw = 100.0,
            Endpoint = "https://inverter-sg-02.local/api",
            SolarNodeId = nodeSgSur.Id,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var invCm01 = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-CM-001",
            NormalizedSerieNumber = "inv-cm-001",
            BrandAndModel = "Fronius Tauro ECO 50kW",
            NominalCapacityKw = 50.0,
            Endpoint = "https://inverter-cm-01.local/api",
            SolarNodeId = nodeCmCentral.Id,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var invCm02 = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-CM-002",
            NormalizedSerieNumber = "inv-cm-002",
            BrandAndModel = "Fronius Tauro ECO 50kW",
            NominalCapacityKw = 50.0,
            Endpoint = "https://inverter-cm-02.local/api",
            SolarNodeId = nodeCmEste.Id,
            CreatedAtUtc = now.AddMonths(-4)
        };

        context.Inverters.AddRange(invSg01, invSg02, invCm01, invCm02);
        context.SaveChanges();

        // 4. Paneles Solares
        var panelSg01 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-SG-001",
            NormalizedSerieNumber = "panel-sg-001",
            PowerW = 400,
            Efficiency = 20.5,
            SolarParkId = saltoGrande.Id,
            SolarNodeId = nodeSgNorte.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var panelSg02 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-SG-002",
            NormalizedSerieNumber = "panel-sg-002",
            PowerW = 400,
            Efficiency = 20.5,
            SolarParkId = saltoGrande.Id,
            SolarNodeId = nodeSgNorte.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var panelSg03 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-SG-003",
            NormalizedSerieNumber = "panel-sg-003",
            PowerW = 450,
            Efficiency = 21.0,
            SolarParkId = saltoGrande.Id,
            SolarNodeId = nodeSgSur.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var panelSg04 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-SG-004",
            NormalizedSerieNumber = "panel-sg-004",
            PowerW = 450,
            Efficiency = 21.0,
            SolarParkId = saltoGrande.Id,
            SolarNodeId = nodeSgSur.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var panelSg05 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-SG-005",
            NormalizedSerieNumber = "panel-sg-005",
            PowerW = 500,
            Efficiency = 21.5,
            SolarParkId = saltoGrande.Id,
            SolarNodeId = null, // Sin asignar a nodo
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-3)
        };

        var panelCm01 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-CM-001",
            NormalizedSerieNumber = "panel-cm-001",
            PowerW = 550,
            Efficiency = 22.0,
            SolarParkId = cerroDelMarco.Id,
            SolarNodeId = nodeCmCentral.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var panelCm02 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-CM-002",
            NormalizedSerieNumber = "panel-cm-002",
            PowerW = 550,
            Efficiency = 22.0,
            SolarParkId = cerroDelMarco.Id,
            SolarNodeId = nodeCmCentral.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var panelCm03 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-CM-003",
            NormalizedSerieNumber = "panel-cm-003",
            PowerW = 550,
            Efficiency = 22.0,
            SolarParkId = cerroDelMarco.Id,
            SolarNodeId = nodeCmEste.Id,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = now.AddMonths(-4)
        };

        var panelPt01 = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PANEL-PT-001",
            NormalizedSerieNumber = "panel-pt-001",
            PowerW = 350,
            Efficiency = 19.5,
            SolarParkId = pasoDeLosToros.Id,
            SolarNodeId = null,
            Status = SolarPanelStatus.Inactive,
            CreatedAtUtc = now.AddMonths(-2)
        };

        context.SolarPanels.AddRange(
            panelSg01, panelSg02, panelSg03, panelSg04, panelSg05,
            panelCm01, panelCm02, panelCm03, panelPt01
        );
        context.SaveChanges();

        // 5. Técnicos (incluye a tecnico@solaruy.local de Keycloak)
        var techCarlos = new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Carlos Méndez",
            DocumentId = "4.567.890-1",
            NormalizedDocumentId = "4.567.890-1",
            Email = "tecnico@solaruy.local",
            NormalizedEmail = "tecnico@solaruy.local",
            PhoneNumber = "099123456",
            Specialty = "Inversores y Media Tensión",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = now.AddMonths(-6)
        };

        var techMariana = new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Mariana Rodríguez",
            DocumentId = "3.890.123-4",
            NormalizedDocumentId = "3.890.123-4",
            Email = "mrodriguez@solaruy.local",
            NormalizedEmail = "mrodriguez@solaruy.local",
            PhoneNumber = "098654321",
            Specialty = "Termografía y Paneles",
            Status = TechnicianStatus.Active,
            CreatedAtUtc = now.AddMonths(-5)
        };

        var techGonzalo = new Technician
        {
            Id = Guid.NewGuid(),
            FullName = "Gonzalo Varela",
            DocumentId = "2.345.678-9",
            NormalizedDocumentId = "2.345.678-9",
            Email = "gvarela@solaruy.local",
            NormalizedEmail = "gvarela@solaruy.local",
            PhoneNumber = "091112233",
            Specialty = "Redes y Monitoreo SCADA",
            Status = TechnicianStatus.Inactive,
            CreatedAtUtc = now.AddMonths(-3)
        };

        context.Technicians.AddRange(techCarlos, techMariana, techGonzalo);
        context.SaveChanges();

        // 6. Órdenes de Mantenimiento
        var orderToday = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panelSg01.Id,
            TechnicianId = techCarlos.Id,
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = now, // Tarea de hoy para Carlos Méndez
            Description = "Inspección urgente de punto caliente detectado en rama 2 del inversor.",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = now.AddDays(-1)
        };

        var orderFuture = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panelSg02.Id,
            TechnicianId = techCarlos.Id,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = now.AddDays(3),
            Description = "Limpieza programada de módulos y reapriete de terminales MC4.",
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = now.AddDays(-2)
        };

        var orderCompleted = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panelCm01.Id,
            TechnicianId = techMariana.Id,
            Type = MaintenanceType.Preventive,
            ScheduledDateUtc = now.AddDays(-5),
            Description = "Revisión semestral de degradación y medición de curva I-V.",
            Status = MaintenanceStatus.Completed,
            PanelOutcome = MaintenancePanelOutcome.Operational,
            ResolutionNotes = "Curva I-V dentro de tolerancia nominal (+-2%). Módulo en óptimo estado.",
            CompletedAtUtc = now.AddDays(-5).AddHours(2),
            CreatedAtUtc = now.AddDays(-10)
        };

        var orderCancelled = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panelSg03.Id,
            TechnicianId = techCarlos.Id,
            Type = MaintenanceType.Corrective,
            ScheduledDateUtc = now.AddDays(-2),
            Description = "Revisión por alerta de caída momentánea de tensión en string.",
            Status = MaintenanceStatus.Cancelled,
            CancellationReason = "Sombreado transitorio por nubosidad densa. No requiere intervención técnica.",
            CreatedAtUtc = now.AddDays(-4),
            UpdatedAtUtc = now.AddDays(-2)
        };

        context.MaintenanceOrders.AddRange(orderToday, orderFuture, orderCompleted, orderCancelled);
        context.SaveChanges();
    }
}

