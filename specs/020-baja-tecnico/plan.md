# Plan Técnico: Spec 020 — Baja de Técnico

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/ITechnicianRepository.cs`: método `GetById(Guid id)`, `Update(Technician tech)`.
  - `Repositories/IMaintenanceRepository.cs`: método `CountPendingByTechnician(Guid techId)`.
  - `Services/ITechnicianService.cs` y `TechnicianService.cs`: método `ServiceResult Deactivate(Guid id)`.
  - `Controllers/TechniciansController.cs`: endpoint `DELETE /api/technicians/{id}`.
- `SolarUY.Web`:
  - `Services/TechnicianApiClient.cs`: método `DeactivateAsync(Guid id)`.
  - `Components/Pages/TechnicianList.razor`: botón "Dar de baja" y modal de confirmación.
- `SolarUY.Tests`:
  - `Services/TechnicianServiceDeactivateTests.cs`: tests unitarios de validación de tareas pendientes y baja lógica.

## Servicios y Lógica de Dominio Pura
- `Deactivate(Guid id)`:
  - Recupera técnico con `GetById(id)`. Si es `null` -> `Fail(NotFound, "El técnico solicitado no existe.")`.
  - Si `tech.Status == Inactive` -> `Fail(Conflict, "El técnico ya está inactivo.")`.
  - Comprueba si posee tareas en curso: `CountPendingByTechnician(id) > 0`. Si tiene tareas -> `Fail(Conflict, $"No se puede dar de baja al técnico porque tiene {count} mantenimiento(s) pendiente(s) de resolución.")`.
  - Pasa a `Status = Inactive`. Guarda cambios con `Update`. Retorna `Ok()`.

## API y Comunicación
- `DELETE /api/technicians/{id}`
  - Respuestas: `204 No Content`, `404 Not Found`, `409 Conflict`.

## Estrategia de Tests
- **Tests unitarios**:
  - Técnico sin tareas pendientes pasa a Inactivo exitosamente.
  - Técnico con tareas pendientes devuelve Conflict y mensaje explicativo.
  - Técnico inexistente devuelve NotFound.
