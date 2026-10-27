# Plan Técnico: Spec 025 — Ver Mis Mantenimientos (Técnico)

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/IMaintenanceRepository.cs`: método `GetByTechnicianEmail(string email)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `GetMyMaintenances(string technicianEmail)`.
  - `Controllers/MaintenancesController.cs`: endpoint `GET /api/maintenances/mine` decorado con `[Authorize(Roles = "Técnico")]`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `GetMyMaintenancesAsync()`.
  - `Components/Pages/MyMaintenances.razor` (ruta `/mis-mantenimientos`): vista táctil optimizada para celular.
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceMyTasksTests.cs`: tests unitarios de filtrado por usuario autenticado.

## Servicios y Lógica de Dominio Pura
- `GetMyMaintenances(string technicianEmail)`:
  - Busca el técnico correspondiente al email del usuario autenticado en OIDC/Keycloak.
  - Si el técnico no existe -> `Fail(NotFound, "No se encontró un perfil de técnico asociado a tu usuario.")`.
  - Recupera las órdenes del técnico ordenadas con prioridad: primero las `Pending` (por fecha ascendente), luego las `Completed`.
  - Retorna lista de DTOs simplificada para móvil.

## API y Comunicación
- `GET /api/maintenances/mine`
  - Headers: `Authorization: Bearer <token>`
  - Respuestas: `200 OK` con lista de órdenes, `401 Unauthorized`, `403 Forbidden`.

## Interfaz de Usuario (Blazor Server)
- `/mis-mantenimientos`:
  - Tarjetas táctiles por cada orden con badges claros de tipo (Preventivo/Correctivo).
  - Botón grande "Comenzar / Confirmar Mantenimiento" que navega a `/mantenimientos/{id}/confirmar`.
  - Mensaje amigable cuando no hay tareas pendientes para hoy.

## Estrategia de Tests
- **Tests unitarios**:
  - Técnico recibe solo sus órdenes asignadas.
  - Priorización de tareas pendientes en el ordenamiento.
