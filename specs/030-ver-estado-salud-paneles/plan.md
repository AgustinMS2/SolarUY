# Plan Técnico: Spec 030 — Ver Estado de Salud de Paneles

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Dtos/PanelHealthState.cs`: enum `Normal`, `Degraded`, `Critical`, `NoSun`, `Inactive`.
  - `Dtos/ParkHealthDtos.cs`:
    - `PanelHealthReadingDto`: `PanelId`, `State`, `ActivePowerW` (nulo sin lecturas), `ExpectedPowerW`, `EstimatedEfficiencyPercent` (nulo si no aplica), `IsLowPerformance`, `StateReason` (motivo en paneles sin lecturas).
    - `ParkHealthSummaryDto`: `Total`, `Normal`, `Degraded`, `Critical`, `NoSun`, `Inactive`.
    - `ParkHealthUpdateDto`: `ParkId`, `TimestampUtc`, `Summary` y `Panels` (lista de `PanelHealthReadingDto`); es lo que viaja por SignalR, un evento por parque y ciclo.
    - `ParkHealthSnapshotDto`: `ParkId`, `ParkName`, `TimestampUtc`, `Summary` y `Nodes` (grupos con `NodeId?`, `NodeName` y sus celdas: `PanelId`, `SerieNumber`, `NominalPowerW`, `NominalEfficiencyPercent`, `LastMaintenanceUtc?` y la lectura inicial). Es la carga inicial de la pantalla.
  - `Services/IPanelHealthService.cs` y `PanelHealthService.cs`: reglas puras (clasificación, contadores y último mantenimiento).
  - `Services/ParkHealthSnapshotBuilder.cs` (con interfaz): orquestación sin reglas de negocio; lee parque, paneles y órdenes completadas, simula las lecturas y arma el snapshot y el update.
  - `Services/PanelReadingSimulator.cs`: simulación de la potencia de un panel (misma fórmula que `PanelTelemetryBackgroundService` de la spec 029: esperada × variación del ±3 %, con ~5 % de caídas fuertes). Se duplica a propósito para no tocar la 029; se unificará en la spec 036.
  - `Hubs/ParkHealthHub.cs`: hub SignalR en `/hubs/park-health`, protegido con `[Authorize(Roles = Roles.Any)]`, con `SubscribeToPark(Guid parkId)` y `UnsubscribeFromPark(Guid parkId)`; grupo `park-health-{parkId}`.
  - `Services/ParkHealthBackgroundService.cs`: `BackgroundService` que cada 3 s emite un `ParkHealthUpdateDto` por cada parque activo al grupo correspondiente.
  - `Controllers/SolarParksController.cs`: `GET /api/solar-parks/{id}/panel-health` (200 con el snapshot, 404 "El parque solicitado no existe."); solo lectura, hereda `[Authorize(Roles = Roles.Any)]`.
  - `Program.cs`: registro de servicios y mapeo del hub. La autenticación por `access_token` en query string ya cubre cualquier ruta bajo `/hubs`.
- `SolarUY.Web`:
  - `Services/SolarParkApiClient.cs`: método `GetPanelHealthAsync(Guid id)`.
  - `Components/Pages/ParkHealth.razor` (ruta `/parques/{id:guid}/salud`): mapa de salud con contadores, leyenda fija, tarjeta de detalle y ciclo de vida `IAsyncDisposable`.
  - `Components/Pages/SolarParkList.razor`: botón "Salud" en los parques activos.
- `SolarUY.Tests`:
  - `Services/PanelHealthServiceTests.cs`: tests unitarios de las reglas.

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Sin cambios de esquema ni migraciones**: se reutilizan `SolarPanel`, `SolarNode`, `SolarPark` y `MaintenanceOrder` (`CompletedAtUtc`).
- El último mantenimiento sale de `IMaintenanceRepository.GetAll` con filtro `Status = Completed` y `ParkId`; no se agregan métodos al repositorio.
- El historial de salud no se guarda: se cubrirá en la spec 034.

---

## Lógica de Dominio y Reglas de Negocio (Puras)

- `IPanelHealthService`:
  - `PanelHealthReadingDto ClassifyHealth(SolarPanel panel, bool hasActiveNode, double activePowerW, double localHour)`
  - `ParkHealthSummaryDto BuildSummary(IEnumerable<PanelHealthState> states)`
  - `IReadOnlyDictionary<Guid, DateTime> LatestCompletedByPanel(IEnumerable<MaintenanceOrder> orders)`
- **Clasificación** (usa `CalculateExpectedPowerW` de la spec 029, sin duplicar la curva):
  1. Panel Inactivo → `Inactive` (motivo "Panel dado de baja").
  2. Panel Activo sin nodo → `Inactive` (motivo "Sin nodo asignado"); con nodo inactivo → `Inactive` (motivo "Nodo inactivo").
  3. Potencia esperada = 0 → `NoSun`.
  4. `ratio = activa / esperada`: `ratio >= 0,80` → `Normal`; `ratio < 0,10` → `Critical`; en el resto → `Degraded`.
  5. `IsLowPerformance = ratio < 0,40` (solo con sol), igual que la spec 029.
  6. Eficiencia estimada = eficiencia de placa × ratio (nula sin sol o sin lecturas).
- **Contadores**: `Total` es la cantidad de paneles; el resto cuenta cada estado.
- **Último mantenimiento**: por panel, el `CompletedAtUtc` más reciente entre las órdenes Completadas.

---

## API y Comunicación en Tiempo Real

- **Carga inicial**: `GET /api/solar-parks/{id}/panel-health` devuelve la estructura (nodos y paneles), el último mantenimiento y una primera lectura de cada panel.
- **Hub `ParkHealthHub`** (`/hubs/park-health`): grupo `park-health-{parkId}`; evento `ReceiveParkHealth(ParkHealthUpdateDto)`.
- **`ParkHealthBackgroundService`**: cada 3 s, por parque activo, arma el update con una lectura simulada por panel y lo envía al grupo. Un solo mensaje por parque y ciclo.
- Limitación conocida: igual que las specs 027 a 029, el servicio calcula todos los parques activos aunque nadie esté suscrito; con parques de 2000 paneles conviene medirlo (spec 036 o posterior).

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **Componente**: `ParkHealth.razor` en `/parques/{id:guid}/salud`:
  - Encabezado con el nombre del parque, semáforo del enlace SignalR y botón "Volver a Parques".
  - Contadores: Total, Normal, Alerta, Falla, Sin sol e Inactivos.
  - Leyenda fija con los cinco colores.
  - Cuadrícula agrupada por nodo; cada celda es un elemento liviano con el número de serie y el color del estado; las celdas se actualizan con cada evento.
  - Tarjeta de detalle al hacer clic o pasar el cursor: serie, nodo, estado, potencia, eficiencia, último mantenimiento y "Ver detalle" (`/paneles/{id}/monitoreo`).
  - Mensajes: parque sin paneles, parque inactivo, parque inexistente y "Reconectando con el parque...".
  - `HubConnection` con reconexión automática (`SubscribeToPark` al conectar y al reconectar) e `IAsyncDisposable` para desuscribir al salir.
- **`SolarParkList.razor`**: botón "Salud" en los parques con estado Activo.
- No se verifica con 2000 paneles reales: se diseña con celdas livianas y un único evento por ciclo, y se prueba con el parque de datos de ejemplo.

---

## Estrategia de Testing

- **Unit tests en `PanelHealthServiceTests.cs` (TDD, rojo → verde)**:
  - Clasificación: Normal (≥ 80 %, incluido el 80 % exacto), Degradado (entre el 10 % y el 80 %, incluido el 10 % exacto), Falla Crítica (< 10 %), Sin sol de noche, Inactivo por baja, por falta de nodo y por nodo inactivo.
  - `IsLowPerformance` (< 40 %), eficiencia estimada y ausencia de división por cero.
  - Contadores: total y cantidad por estado, lista vacía.
  - Último mantenimiento: toma la orden Completada más reciente, ignora Pendientes y Canceladas, sin órdenes no aparece.
- **Tests de arquitectura**: `SecurityArchitectureTests` en verde (el endpoint y el hub exigen rol).
- **UI Blazor**: verificación manual en navegador (cuadrícula, tarjeta, contadores, actualización en vivo, reconexión, mensajes y vista móvil).
