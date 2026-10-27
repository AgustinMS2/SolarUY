# Plan Técnico: Spec 024 — Listar Mantenimientos

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/IMaintenanceRepository.cs`: método `IReadOnlyList<MaintenanceOrder> GetAll(MaintenanceFilter filter)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `IReadOnlyList<MaintenanceOrderDto> GetAll(MaintenanceFilter filter)`.
  - `Dtos/MaintenanceOrderDto.cs`: DTO con `(Guid Id, string ParkName, string PanelSerie, string TechnicianName, string Type, DateTime ScheduledDateUtc, string Status, string Description)`.
  - `Controllers/MaintenancesController.cs`: endpoint `GET /api/maintenances`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `GetAllAsync(MaintenanceFilter filter)`.
  - `Components/Pages/MaintenanceList.razor` (ruta `/mantenimientos`): vista general con barra de filtros y grilla paginada.
- `SolarUY.Tests`:
  - `Services/MaintenanceServiceGetAllTests.cs`: tests unitarios de ordenamiento cronológico y filtros combinados.

## Servicios y Lógica de Dominio Pura
- `GetAll(MaintenanceFilter filter)`:
  - Recupera las órdenes del repositorio aplicando filtros de estado, parque, técnico y rango de fechas.
  - Ordena por `ScheduledDateUtc` de forma descendente.
  - Mapea a DTOs para la vista.

## API y Comunicación
- `GET /api/maintenances?status=...&parkId=...&techId=...&from=...&to=...`
  - Respuestas: `200 OK` con lista de órdenes.

## Interfaz de Usuario (Blazor Server)
- `/mantenimientos`:
  - Filtros combinables: Selector de Estado, Selector de Parque, Selector de Técnico, Rango Desde/Hasta.
  - Grilla con indicadores de estado en color: Pendiente (amarillo), Completado (verde), Cancelado (gris).
  - Enlaces a "Modificar" y "Cancelar" en órdenes pendientes para Administradores.

## Estrategia de Tests
- **Tests unitarios**:
  - Filtro por estado retorna exclusivamente las órdenes de ese estado.
  - Ordenamiento por fecha descendente.
  - Lista vacía cuando ningún registro coincide.
