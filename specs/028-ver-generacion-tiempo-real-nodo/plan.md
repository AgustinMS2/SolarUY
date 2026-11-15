# Plan Técnico: Spec 028 — Ver Generación en Tiempo Real del Nodo

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Dtos/NodeTelemetryDto.cs`: DTO con métricas de telemetría del nodo e inversor (`NodeId`, `NodeName`, `InverterSerieNumber`, `NominalCapacityKw`, `ActivePowerKw`, `DcVoltage`, `DcCurrent`, `AcVoltage`, `GridFrequencyHz`, `InverterTemperatureC`, `IsOverheating`, `IsGridFrequencyAnomaly`, `TimestampUtc`; la spec 037 agrega `Fault` y `FaultLabel`).
  - `Services/INodeTelemetryService.cs` y `NodeTelemetryService.cs`: servicio de dominio puro con lógica de cálculo, normalización y evaluación de umbrales (sobretemperatura $> 75\,^\circ\text{C}$ y anomalía de frecuencia fuera de $[49.5, 50.5]\,\text{Hz}$).
  - `Hubs/NodeTelemetryHub.cs`: hub SignalR en `/hubs/node-telemetry`, protegido con `[Authorize(Roles = Roles.Any)]`, con soporte para suscripción y desuscripción de grupos (`SubscribeToNode(Guid nodeId)` y `UnsubscribeFromNode(Guid nodeId)`).
  - `Services/NodeTelemetryBackgroundService.cs`: Servicio en segundo plano (hijo de `SimulationEmitterBackgroundService`, spec 036) que en cada ciclo (cada `Simulation:IntervalSeconds`, 5 s por defecto) toma la lectura del inversor del motor de simulación (`ISolarSimulationEngine.ReadInverter`, con las fallas de la spec 037) y la emite al hub SignalR para los nodos activos que cuenten con inversor asignado.
  - `Program.cs`: mapeo del hub en `/hubs/node-telemetry`.
- `SolarUY.Web`:
  - `Components/Pages/NodeMonitoring.razor` (ruta `/nodos/{id:guid}/monitoreo`): vista interactiva (rótulo "En vivo") con métricas eléctricas y térmicas en vivo, mini-gráfica SVG reactiva de tendencia de potencia, alertas de sobretemperatura/frecuencia, banner "En Falla" con la falla simulada del inversor (spec 037) y gestión del ciclo de vida `IAsyncDisposable`.
  - `Components/Pages/SolarNodeList.razor`: adición del botón de acción "Monitorear" en cada nodo del listado.
- `SolarUY.Tests`:
  - `Services/NodeTelemetryServiceTests.cs`: tests unitarios sobre las reglas de negocio de cálculo, detección de anomalías eléctricas y térmicas, y comportamiento ante nodos inactivos o sin inversor.
  - Verificación global con `dotnet test` (mantener los tests de arquitectura en verde).

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Sin cambios de esquema ni migraciones**: Se reutilizan las entidades existentes `SolarNode` e `Inverter` (con su `NominalCapacityKw`, `SerieNumber` y relación 1:1) para contextualizar la telemetría.
- El almacenamiento histórico a largo plazo se cubrirá en la Spec 034.

---

## Lógica de Dominio y Reglas de Negocio (Puras)

- `INodeTelemetryService`:
  - `NodeTelemetryDto CalculateMetrics(SolarNode node, Inverter? inverter, double activePowerKw, double dcVoltage, double dcCurrent, double acVoltage, double frequencyHz, double temperatureC, DateTime timestampUtc, SimulatedFaultType? fault = null)`
- **Reglas de evaluación (funciones puras)**:
  - **Sobretemperatura (RF-3)**:
    $$\text{IsOverheating} = \text{InverterTemperatureC} > 75.0\,^\circ\text{C}$$
  - **Anomalía de Frecuencia de Red (RF-4)**:
    $$\text{IsGridFrequencyAnomaly} = \text{GridFrequencyHz} < 49.5\,\text{Hz} \lor \text{GridFrequencyHz} > 50.5\,\text{Hz}$$
  - **Nodo inactivo o sin inversor (RF-5 / Casos límite)**:
    - Si el nodo está inactivo, `inverter == null`, la inyección a la red está detenida (spec 033) o el inversor tiene la falla de aislamiento (spec 037), los valores eléctricos se reportan en 0.0 y no se evalúan alarmas espurias.
  - **Falla simulada (RF-8, spec 037)**: el tipo de falla vigente viaja en `Fault` y su nombre en `FaultLabel`.

---

## API y Comunicación en Tiempo Real

### Hub SignalR (`NodeTelemetryHub`)
- **Ruta**: `/hubs/node-telemetry`
- **Seguridad**: `[Authorize(Roles = Roles.Any)]` a nivel de clase.
- **Métodos cliente**:
  - `SubscribeToNode(Guid nodeId)`: agrega la conexión al grupo `node-{nodeId}`.
  - `UnsubscribeFromNode(Guid nodeId)`: remueve la conexión del grupo.
- **Evento emitido**:
  - `ReceiveNodeTelemetry(NodeTelemetryDto telemetry)` hacia `Clients.Group($"node-{nodeId}")`.

### Servicio en Segundo Plano (`NodeTelemetryBackgroundService`)
- Corre cada `Simulation:IntervalSeconds` (5 segundos por defecto) consultando nodos activos con inversor asignado.
- Toma las lecturas del motor de simulación de la spec 036 (las mismas que usan las demás pantallas en vivo) y las emite a los grupos suscritos a través de `IHubContext<NodeTelemetryHub>`.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **Componente**: `NodeMonitoring.razor` en `/nodos/{id:guid}/monitoreo`:
  - **Encabezado y Navegación**:
    - Nombre del nodo, parque perteneciente y serie del inversor.
    - Semáforo de estado de conexión SignalR (Conectado / Reconectando / Desconectado).
    - Botón "Volver a Nodos".
  - **Mini-Gráfica SVG Reactiva (RF-2)**:
    - Sparkline/área SVG con la evolución de las últimas lecturas de potencia activa (búfer rodante de 15-20 puntos), con animación fluida sin parpadeo.
  - **Cuadrícula de Métricas Eléctricas**:
    1. *Potencia Activa* (kW) y % de la capacidad nominal del inversor.
    2. *Voltaje DC de Entrada* (V).
    3. *Corriente DC* (A).
    4. *Voltaje AC de Salida* (V).
    5. *Frecuencia de Red* (Hz) — resaltada en rojo si `IsGridFrequencyAnomaly` es true.
    6. *Temperatura Interna* (°C) — resaltada en ámbar/rojo si `IsOverheating` es true.
  - **Banners de Alerta Reactivos**:
    - Alerta si `IsOverheating`: *"Sobretemperatura en inversor (<temperatura> °C)"*.
    - Alerta si `IsGridFrequencyAnomaly`: *"Anomalía en sincronismo de red eléctrica (<frecuencia> Hz)"*.
  - **Caso Nodo sin Inversor (RF-5)**:
    - Banner informativo *"Este nodo no posee un inversor configurado."* y desconexión de telemetría.
  - **Ciclo de Vida y Desconexión (RF-6, RF-7)**:
    - `HubConnection` con reconexión automática e `IAsyncDisposable` para desuscribir y liberar recursos al salir.

---

## Estrategia de Testing

- **Unit tests en `NodeTelemetryServiceTests.cs` (TDD, RED → GREEN)**:
  - Detección de sobretemperatura cuando $T > 75\,^\circ\text{C}$.
  - Detección de frecuencia anómala cuando $f < 49.5\,\text{Hz}$ o $f > 50.5\,\text{Hz}$.
  - Frecuencia normal en $50.0\,\text{Hz}$ sin anomalía.
  - Manejo de nodo inactivo o sin inversor.
  - Inyección detenida (spec 033) y falla de aislamiento (spec 037): valores en cero y sin alarmas; falla de sobretemperatura: se conservan las lecturas y se levanta la alarma.
- **Tests de arquitectura**:
  - `SecurityArchitectureTests` manteniéndose 100% en verde.

