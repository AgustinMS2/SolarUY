# Plan Técnico: Spec 022 — Modificar Reserva de Mantenimiento

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/IMaintenanceRepository.cs`: métodos `GetById(Guid id)`, `Update(MaintenanceOrder order)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `UpdateReservation(Guid id, UpdateMaintenanceReservationRequest request)`.
  - `Dtos/UpdateMaintenanceReservationRequest.cs`: DTO con `DateTime ScheduledDateUtc`, `Guid TechnicianId`, `string Description`.
  - `Controllers/MaintenancesController.cs`: endpoint `PUT /api/maintenances/{id}`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `UpdateReservationAsync(Guid id, UpdateMaintenanceReservationRequest request)`.
  - `Components/Pages/MaintenanceEdit.razor` (ruta `/mantenimientos/{id:guid}/editar`): vista de edición para órdenes pendientes.
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceUpdateReservationTests.cs`: tests unitarios de reprogramación y bloqueo de órdenes cerradas.

## Servicios y Lógica de Dominio Pura
- `UpdateReservation(Guid id, UpdateMaintenanceReservationRequest request)`:
  - Recupera la orden con `GetById(id)`. Si es `null` -> `Fail(NotFound, "El mantenimiento solicitado no existe.")`.
  - Si `order.Status != MaintenanceStatus.Pending` -> `Fail(Conflict, "No se pueden modificar mantenimientos que ya han sido completados o cancelados.")`.
  - Valida que la nueva fecha sea futura (`> DateTime.UtcNow`).
  - Valida que el nuevo técnico exista y esté Activo.
  - Actualiza propiedades, persiste y retorna `ServiceResult.Ok()`.

## API y Comunicación
- `PUT /api/maintenances/{id}`
  - Respuestas: `204 No Content`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.

## Estrategia de Tests
- **Tests unitarios**:
  - Modificación exitosa de fecha y técnico en orden pendiente.
  - Intentar modificar una orden Completada o Cancelada devuelve Conflict.
  - Fecha en el pasado devuelve Validation.
