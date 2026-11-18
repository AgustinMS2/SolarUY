# Plan Técnico: Spec 039 — Corrección de Integración Transversal y Hallazgos de Auditoría

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Dtos/TechnicianDto.cs` y `Dtos/ConfirmMaintenanceRequest.cs`: Adición de `PendingMaintenancesCount` en `TechnicianDto` y parámetros opcionales de usuario/rol en confirmación de mantenimiento.
  - `Repositories/MaintenanceRepository.cs`: Corrección de `CountPendingByTechnician` para contar todas las órdenes con `Status == MaintenanceStatus.Pending` del técnico sin excluir las de hora pasada del día actual.
  - `Services/MaintenanceService.cs` y `Controllers/MaintenancesController.cs`:
    - En `ConfirmMaintenance`: validar pertenencia de la orden cuando quien confirma no es Administrador (RF-2), y si `PanelOutcome == Operational`, invocar `ISimulatedFaultRepository.EndByTarget(SimulatedFaultTarget.Panel, order.SolarPanelId, now)` (RF-1).
    - En `CreateReservation` y `UpdateReservation`: aceptar paneles `Active` o `Deactivated` (rechazando solo `Inactive`), verificar que el parque del panel esté `Active`, y revalidar colisión diaria en `UpdateReservation` (RF-3, RF-4).
  - `Services/TechnicianService.cs`, `Services/UserRoleService.cs`:
    - `TechnicianService.Create`: guardar `DocumentId = trimmedDocument` y `NormalizedDocumentId = normalizedDocument`, validando `!string.IsNullOrEmpty(normalizedDocument)` (RF-12).
    - `TechnicianService.GetAll`: calcular `PendingMaintenancesCount = _maintenanceRepository.CountPendingByTechnician(t.Id)` (RF-10).
    - `UserRoleService.ChangeRole`: sincronizar el cambio de rol con `ITechnicianRepository` y bloquear el paso de `Técnico` a `Administrador` si posee mantenimientos pendientes (RF-13).
  - `Services/SolarPanelService.cs`:
    - En `Update`: si cambia `SolarParkId` respecto al actual, limpiar `SolarNodeId = null` (RF-7).
    - En `DeletePanel`: devolver `ServiceErrorType.Conflict` si ya está inactivo, desasignar del nodo y cerrar fallas simuladas y alertas activas del panel (RF-6).
  - `Services/GridInjectionService.cs`, `Services/ParkTelemetryBackgroundService.cs`, `Services/NodeTelemetryService.cs`, `Services/PanelHealthService.cs`, `Services/AlertDetectionRules.cs`, `Services/SimulationFaultService.cs`:
    - Validar nodo y parque activos en `CutInverterInjection` / `ResumeInverterInjection` (RF-16).
    - Filtrar `Where(i => i.GridInjectionEnabled)` en `ParkTelemetryBackgroundService` (RF-15).
    - Conservar `DcVoltage` residual e `InverterTemperatureC` ambiente en `NodeTelemetryService` cuando `fault == InsulationFault` y el nodo/inversor están activos con inyección habilitada (RF-17).
    - Suprimir alertas de nodo en `AlertDetectionRules` cuando `!telemetry.GridInjectionEnabled` (RF-15).
    - Retornar `"Panel desactivado manualmente"` en `PanelHealthService` cuando `panel.Status == SolarPanelStatus.Deactivated` e incluir paneles `Deactivated` en `SimulationFaultService.GetOverview()` (RF-18).
  - `Services/SolarUyDomainContextRetriever.cs`, `Services/GeminiLlmService.cs`, `Services/SolarUyKnowledgeAssistant.cs`:
    - Ordenar ascendentemente por `ScheduledDateUtc` las órdenes pendientes antes de `.Take(5)`, evitar duplicar `userMessage` en `GeminiLlmService`, y detectar nombres de parques inexistentes en `SolarUyKnowledgeAssistant` (RF-20).
- `SolarUY.Web`:
  - `Components/Pages/PanelList.razor` y `Components/Pages/PanelDelete.razor`: Botón `"Dar de baja"` para Administradores, renderizado de `@_errorMessage` y redirección con `?mensaje=` al completar la baja (RF-6, RF-9).
  - `Components/Pages/SolarNodeCreate.razor` y `Components/Pages/SolarNodeList.razor`: Filtro de parques activos, eliminación del `forceLoad: true` tras crear nodo, y mensaje de error RF-6 en listado de nodos (RF-8, RF-9).
  - `Components/Pages/TechnicianList.razor`, `Components/Pages/TechnicianCreate.razor` y `Components/Layout/NavMenu.razor`: Columna `"Mantenimientos asignados en curso"`, navegación en botón `"Cancelar"`, retiro de `required` HTML nativo, y enlace a `/tecnicos` visible para `Técnico` (RF-10).
  - `Components/Pages/MaintenanceCreate.razor` y `Components/Pages/MyMaintenances.razor`: Lectura de query params en `OnParametersSet` con tipo `"Corrective"`, inclusión de paneles `"Desactivado"` de parques activos, navegación en `"Cancelar"` y selección automática del primer técnico para el Administrador en `/mis-mantenimientos` (RF-3, RF-5, RF-14).
  - `Components/Pages/ParkMonitoring.razor` y `Components/Pages/ChatAssistant.razor`: Interpolación de `(<umbral> kW)`, aviso cuando el parque no tiene inversores, y no duplicar el último mensaje en el historial de chat (RF-19, RF-20).
- `SolarUY.Tests`:
  - Nuevos tests unitarios en `MaintenanceServiceConfirmTests`, `MaintenanceServiceReservationTests`, `TechnicianServiceCreateTests`, `TechnicianServiceGetAllTests`, `UserRoleServiceTests`, `SolarPanelServiceTests`, `GridInjectionServiceTests`, `NodeTelemetryServiceTests`, `PanelHealthServiceTests` y `SolarUyKnowledgeAssistantTests`.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)
- No requiere migraciones de EF Core: todas las entidades y columnas necesarias ya existen en el esquema actual.

## Estrategia de Tests
- **Tests de arquitectura**: Todos los tests de arquitectura en `SolarUY.Tests/Architecture/` deben permanecer en verde.
- **Tests unitarios (TDD)**: Se escriben primero en rojo y luego se implementan en verde para cada bloque de servicios (T1, T4 y T7).
