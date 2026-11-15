# Plan Técnico: Spec 029 — Ver Generación en Tiempo Real del Panel

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Dtos/PanelTelemetryDto.cs`: DTO con `PanelId`, `SerieNumber`, `ParkName`, `NodeName`, `NominalPowerW`, `NominalEfficiencyPercent`, `ActivePowerW`, `ExpectedPowerW`, `PercentOfNominal`, `EstimatedEfficiencyPercent` (nulo cuando la potencia esperada es 0), `IsLowPerformance`, `TimestampUtc`.
  - `Services/IPanelTelemetryService.cs` y `PanelTelemetryService.cs`: servicio de dominio puro con la curva solar esperada, el cálculo de porcentajes y eficiencia y la regla de rendimiento bajo.
  - `Hubs/PanelTelemetryHub.cs`: hub SignalR en `/hubs/panel-telemetry`, protegido con `[Authorize(Roles = Roles.Any)]`, con `SubscribeToPanel(Guid panelId)` y `UnsubscribeFromPanel(Guid panelId)`.
  - `Services/PanelTelemetryBackgroundService.cs`: `BackgroundService` que cada 3-5 s genera lecturas simuladas y las emite al grupo `panel-{panelId}` de los paneles activos asignados a un nodo activo.
  - `Program.cs`: registro del servicio de telemetría, del servicio en segundo plano y mapeo del hub. La autenticación por `access_token` en query string ya cubre cualquier ruta bajo `/hubs`.
- `SolarUY.Web`:
  - `Components/Pages/PanelMonitoring.razor` (ruta `/paneles/{id:guid}/monitoreo`): vista interactiva con datos de placa, potencia en vivo, indicadores y gestión del ciclo de vida `IAsyncDisposable`.
  - `Components/Pages/PanelList.razor`: botón "Monitorear" en los paneles activos.
- `SolarUY.Tests`:
  - `Services/PanelTelemetryServiceTests.cs`: tests unitarios de la curva esperada y de las reglas de cálculo y alerta.

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Sin cambios de esquema ni migraciones**: se reutiliza `SolarPanel` (`PowerW`, `Efficiency`, `Status`, `SolarNodeId`, `SolarParkId`) y su relación con `SolarNode` y `SolarPark`.
- El almacenamiento histórico se cubrirá en la spec 034.

---

## Lógica de Dominio y Reglas de Negocio (Puras)

- `IPanelTelemetryService`:
  - `double CalculateExpectedPowerW(int nominalPowerW, double localHour)`
  - `PanelTelemetryDto CalculateMetrics(SolarPanel panel, string? parkName, string? nodeName, double activePowerW, double localHour, DateTime timestampUtc)`
- **Potencia esperada** (misma curva solar que las simulaciones de las specs 027 y 028):
  - Entre las 6:00 y las 20:00 (hora Uruguay): `esperada = PowerW × sen(π × (hora − 6) / 14)`.
  - Fuera de ese horario: `esperada = 0`.
- **Porcentaje de la placa**: `activa / PowerW × 100`.
- **Eficiencia instantánea estimada**: `Efficiency × (activa / esperada)`; `null` si `esperada = 0`.
- **Rendimiento bajo (RF-3)**: `IsLowPerformance = esperada > 0 && activa < 0.40 × esperada` (con el 40 % exacto no hay alerta; de noche no se evalúa).

---

## API y Comunicación en Tiempo Real

### Hub SignalR (`PanelTelemetryHub`)
- **Ruta**: `/hubs/panel-telemetry`.
- **Seguridad**: `[Authorize(Roles = Roles.Any)]` a nivel de clase.
- **Métodos**: `SubscribeToPanel(Guid panelId)` agrega la conexión al grupo `panel-{panelId}`; `UnsubscribeFromPanel(Guid panelId)` la quita.
- **Evento emitido**: `ReceivePanelTelemetry(PanelTelemetryDto telemetry)` hacia `Clients.Group($"panel-{panelId}")`.

### Servicio en segundo plano (`PanelTelemetryBackgroundService`)
- Mismo patrón que `NodeTelemetryBackgroundService`: ciclo de 3-5 s con `IServiceScopeFactory`, consulta los paneles activos con nodo activo y emite una lectura por panel.
- **Lectura simulada**: `esperada × jitter (0,97–1,03)`; en ~5 % de las lecturas se aplica un factor de 0,20–0,35 para ejercitar la alerta de rendimiento bajo.
- No emite para paneles inactivos, sin nodo o con nodo inactivo.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **Componente**: `PanelMonitoring.razor` en `/paneles/{id:guid}/monitoreo`:
  - Encabezado con serie del panel, parque y nodo, semáforo del enlace SignalR y botón "Volver a Paneles".
  - Datos de placa: potencia (W), eficiencia (%).
  - Visor de potencia actual (W) con barra de porcentaje de la placa, eficiencia instantánea estimada y potencia esperada a esa hora.
  - Indicador "Rendimiento bajo / Posible sombreado o suciedad" cuando `IsLowPerformance`.
  - Mensajes: "Panel no conectado a la red de un nodo." (RF-4), "El panel se encuentra inactivo.", "El nodo del panel se encuentra inactivo.", "El panel solicitado no existe." y "Reconectando con el panel..." (RF-5).
  - `HubConnection` con reconexión automática; `IAsyncDisposable` para desuscribir y liberar recursos al salir (RF-6).
- **`PanelList.razor`**: botón "Monitorear" solo en paneles con estado Activo.

---

## Estrategia de Testing

- **Unit tests en `PanelTelemetryServiceTests.cs` (TDD, rojo → verde)**:
  - Curva esperada: 0 W a las 6:00, a las 20:00 y de noche; la potencia de placa a las 13:00; simétrica respecto de las 13:00.
  - Porcentaje de la placa y eficiencia estimada; eficiencia nula cuando la esperada es 0 (sin división por cero).
  - Rendimiento bajo: alerta por debajo del 40 %, sin alerta con el 40 % exacto o por encima, sin alerta de noche.
- **Tests de arquitectura**: `SecurityArchitectureTests` debe seguir en verde (el hub exige rol).
- **UI Blazor**: verificación manual en navegador (actualización en vivo, reconexión, mensajes de panel sin nodo / inactivo, vista móvil).
