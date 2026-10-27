# Plan Técnico: Spec 026 — Confirmar Mantenimiento

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/IMaintenanceRepository.cs`: métodos `GetById(Guid id)`, `Update(MaintenanceOrder order)`.
  - `Repositories/IAlertRepository.cs`: método `ResolveAlertsForPanel(Guid panelId)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `ConfirmMaintenance(Guid id, ConfirmMaintenanceRequest request)`.
  - `Dtos/ConfirmMaintenanceRequest.cs`: DTO con `string Notes`, `string? ReplacedParts`, `string ResultStatus`.
  - `Controllers/MaintenancesController.cs`: endpoint `POST /api/maintenances/{id}/confirm`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `ConfirmAsync(Guid id, ConfirmMaintenanceRequest request)`.
  - `Components/Pages/MaintenanceConfirm.razor` (ruta `/mantenimientos/{id:guid}/confirmar`): formulario de confirmación con observaciones y selector de estado resultante.
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceConfirmTests.cs`: tests unitarios de observaciones obligatorias, cierre de orden y resolución de alertas.

## Servicios y Lógica de Dominio Pura
- `ConfirmMaintenance(Guid id, ConfirmMaintenanceRequest request)`:
  - Recupera la orden. Si no existe -> `Fail(NotFound, "El mantenimiento solicitado no existe.")`.
  - Si `order.Status != MaintenanceStatus.Pending` -> `Fail(Conflict, "Este mantenimiento ya fue completado previamente.")`.
  - Valida que `request.Notes` tenga entre 10 y 1000 caracteres. Si no -> `Fail(Validation, "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres.")`.
  - Asigna `Status = MaintenanceStatus.Completed`, `ResolutionNotes = request.Notes`, `ReplacedParts = request.ReplacedParts`, `CompletedAtUtc = DateTime.UtcNow`.
  - Llama a `ResolveAlertsForPanel(order.SolarPanelId)` para cerrar alertas activas del panel intervenido.
  - Guarda cambios y retorna `ServiceResult.Ok()`.

## API y Comunicación
- `POST /api/maintenances/{id}/confirm`
  - Body: `ConfirmMaintenanceRequest`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.

## Interfaz de Usuario (Blazor Server)
- `/mantenimientos/{id}/confirmar`:
  - Resumen del panel intervenido y motivo de la reserva original.
  - TextArea obligatorio para "Observaciones técnicas".
  - InputText opcional para "Componentes reemplazados".
  - Botón "Confirmar y Finalizar Mantenimiento".
  - Alerta de éxito y navegación de regreso a `/mis-mantenimientos`.

## Estrategia de Tests
- **Tests unitarios**:
  - Confirmación exitosa pasa orden a Completed, guarda notas y fecha UTC.
  - Observaciones menores a 10 caracteres devuelve Validation.
  - Intentar confirmar orden ya completada devuelve Conflict.
  - Cierre automático de alertas activas del panel intervenido.
