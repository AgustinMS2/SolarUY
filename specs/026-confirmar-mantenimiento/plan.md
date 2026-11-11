# Plan Técnico: Spec 026 — Confirmar Mantenimiento

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/MaintenanceOrder.cs`: nueva propiedad `MaintenancePanelOutcome? PanelOutcome` y enum `MaintenancePanelOutcome { Operational = 1, NeedsFollowUp = 2 }`. La entidad ya tiene `ResolutionNotes`, `ReplacedParts` y `CompletedAtUtc`.
  - `Data/SolarUyDbContext.cs`: configuración de `PanelOutcome` y ampliación del límite de `ResolutionNotes` de 500 a 1000 caracteres (la spec permite hasta 1000). `ReplacedParts` ya admite 500; el límite de 300 lo aplica el servicio.
  - Migración: `AddMaintenancePanelOutcome`.
  - `Repositories/IMaintenanceRepository.cs`: ya expone `GetById` y `Update`; no hace falta código nuevo.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `ServiceResult ConfirmMaintenance(Guid id, ConfirmMaintenanceRequest request)`.
  - `Dtos/ConfirmMaintenanceRequest.cs`: `(string? Notes, string? ReplacedParts, string? PanelOutcome)`. `PanelOutcome` acepta `"Operational"` o `"NeedsFollowUp"`.
  - `Controllers/MaintenancesController.cs`: `POST /api/maintenances/{id}/confirm` con `[Authorize(Roles = Roles.Any)]` (Técnico y Administrador).
  - **RF-3 (alertas) queda fuera de este plan**: el repositorio `IAlertRepository` del plan original no existe porque las alertas pertenecen a la spec 031.
- `SolarUY.Web`:
  - Cliente HTTP de mantenimientos: método `ConfirmAsync(Guid id, ConfirmMaintenanceRequest request)`.
  - `Components/Pages/MaintenanceConfirm.razor` (ruta `/mantenimientos/{id:guid}/confirmar`, `[Authorize(Roles = AppRoles.Any)]`): formulario de confirmación.
  - `Components/Pages/MyMaintenances.razor`: botón "Confirmar" en cada orden pendiente.
  - `Components/Pages/MaintenanceList.razor`: botón "Confirmar" en las órdenes pendientes, solo para el Administrador (contingencia).
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceConfirmTests.cs`.
  - `Architecture/SecurityArchitectureTests.cs`: la regla "toda operación que modifica datos es solo de Administrador" admite una excepción explícita y única para `MaintenancesController.ConfirmMaintenance`, que debe exigir exactamente los roles Técnico y Administrador.

## Modelo de Datos y Persistencia
- Columna `PanelOutcome` (entero, nullable) en `MaintenanceOrders`.
- `ResolutionNotes` pasa de `varchar(500)` a `varchar(1000)`.
- Migración: `AddMaintenancePanelOutcome`.

## Servicios y Lógica de Dominio Pura
- `ConfirmMaintenance(id, request)`, en este orden (el primer error corta):
  1. Orden inexistente -> `Fail(NotFound, "El mantenimiento solicitado no existe.")`.
  2. `Status != Pending` (Completado o Cancelado) -> `Fail(Conflict, "Este mantenimiento ya fue completado previamente.")`.
  3. Observaciones (con trim) vacías o menores a 10 -> `Fail(Validation, "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres.")`; mayores a 1000 -> `Fail(Validation, "Las observaciones técnicas no pueden superar los 1000 caracteres.")`.
  4. Componentes reemplazados (con trim) mayores a 300 -> `Fail(Validation, "Los componentes reemplazados no pueden superar los 300 caracteres.")`; vacíos se guardan como `null`.
  5. Estado del panel vacío o distinto de `Operational` / `NeedsFollowUp` -> `Fail(Validation, "Seleccioná el estado del panel tras la tarea.")`.
  6. Asigna `Status = Completed`, `ResolutionNotes`, `ReplacedParts`, `PanelOutcome`, `CompletedAtUtc` y `UpdatedAtUtc` con la hora UTC, persiste con `Update` y retorna `Ok()`.
- No se valida quién confirma: cualquier Técnico o Administrador puede confirmar cualquier orden pendiente (decisión del equipo).
- Fechas con `DateTime.UtcNow`, igual que el resto de `MaintenanceService`.

## API y Comunicación
- `POST /api/maintenances/{id}/confirm`
  - Body: `ConfirmMaintenanceRequest`
  - Respuestas: `200 OK` con `{ message }`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.

## Interfaz de Usuario (Blazor Server)
- `/mantenimientos/{id}/confirmar`:
  - Resumen del panel intervenido y del motivo de la reserva original.
  - Área de texto obligatoria para "Observaciones técnicas".
  - Campo opcional "Componentes reemplazados".
  - Selector obligatorio "Estado del panel tras la tarea" ("Operativo / Normal" o "Requiere seguimiento adicional").
  - Botones "Confirmar y Finalizar Mantenimiento" y "Cancelar" (vuelve a la lista sin cambios).
  - Mensaje de éxito y regreso a `/mis-mantenimientos` (o a `/mantenimientos` si lo abrió un Administrador).
  - Los errores de la API se muestran en la pantalla.

## Decisiones específicas de esta spec
- RF-3 se difiere a la spec 031: hoy no existe entidad de alertas, y crear un repositorio vacío sería código sin uso.
- El estado del panel tras la tarea se guarda en la orden y no modifica el panel.

## Estrategia de Tests
- **Tests de arquitectura**: siguen en verde, con la excepción explícita de `ConfirmMaintenance`.
- **Tests unitarios** (solo reglas del servicio):
  - Confirmación exitosa pasa la orden a Completed y guarda notas, componentes, estado del panel y fecha UTC.
  - Orden inexistente devuelve NotFound.
  - Orden ya completada o cancelada devuelve Conflict y no modifica nada.
  - Observaciones vacías, solo espacios, de menos de 10 o de más de 1000 caracteres devuelven Validation; los límites de 10 y 1000 son válidos.
  - Componentes de más de 300 caracteres devuelven Validation; vacíos o solo espacios se guardan como nulos; 300 es válido.
  - Estado del panel ausente o inválido devuelve Validation.
  - El orden de validación respeta el definido arriba (primero existencia y estado, después los campos).
- **UI Blazor**: verificación manual en navegador (sin bUnit).
