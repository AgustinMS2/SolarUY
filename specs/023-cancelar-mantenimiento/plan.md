# Plan Técnico: Spec 023 — Cancelar Mantenimiento

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/IMaintenanceRepository.cs`: métodos `GetById(Guid id)`, `Update(MaintenanceOrder order)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `CancelReservation(Guid id, CancelMaintenanceRequest request)`.
  - `Dtos/CancelMaintenanceRequest.cs`: DTO con `string Reason`.
  - `Controllers/MaintenancesController.cs`: endpoint `POST /api/maintenances/{id}/cancel`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `CancelAsync(Guid id, string reason)`.
  - `Components/Pages/MaintenanceCancelModal.razor`: modal con TextArea para ingresar motivo obligatorio de cancelación.
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceCancelTests.cs`: tests unitarios de validación de motivo y estados previos.

## Servicios y Lógica de Dominio Pura
- `CancelReservation(Guid id, CancelMaintenanceRequest request)`:
  - Recupera la orden. Si es `null` -> `Fail(NotFound, "El mantenimiento solicitado no existe.")`.
  - Si `order.Status != MaintenanceStatus.Pending` -> `Fail(Conflict, "El mantenimiento ya se encuentra finalizado o cancelado.")`.
  - Valida que `request.Reason` tenga al menos 10 caracteres. Si no -> `Fail(Validation, "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.")`.
  - Asigna `Status = MaintenanceStatus.Cancelled`, `CancellationReason = request.Reason.Trim()`.
  - Guarda cambios y retorna `ServiceResult.Ok()`.

## API y Comunicación
- `POST /api/maintenances/{id}/cancel`
  - Body: `{ reason: "..." }`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.

## Estrategia de Tests
- **Tests unitarios**:
  - Cancelación exitosa guarda motivo y cambia estado a Cancelled.
  - Motivo menor a 10 caracteres devuelve Validation.
  - Cancelar orden ya completada devuelve Conflict.
