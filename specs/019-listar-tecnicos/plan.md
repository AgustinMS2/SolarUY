# Plan Técnico: Spec 019 — Listar Técnicos

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/ITechnicianRepository.cs`: método `IReadOnlyList<Technician> GetAll()`.
  - `Services/ITechnicianService.cs` y `TechnicianService.cs`: método `IReadOnlyList<TechnicianDto> GetAll()`.
  - `Dtos/TechnicianDto.cs`: DTO con `(Guid Id, string FullName, string DocumentId, string Email, string PhoneNumber, string? Specialty, int ActiveMaintenancesCount, string Status)`.
  - `Controllers/TechniciansController.cs`: endpoint `GET /api/technicians`.
- `SolarUY.Web`:
  - `Services/TechnicianApiClient.cs`: método `GetAllAsync()`.
  - `Components/Pages/TechnicianList.razor` (ruta `/tecnicos`): vista de tabla con buscador y filtros de estado.
- `SolarUY.Tests`:
  - `Services/TechnicianServiceGetAllTests.cs`: tests unitarios de ordenamiento y filtrado.

## Servicios y Lógica de Dominio Pura
- `GetAll()`:
  - Recupera los técnicos del repositorio.
  - Ordena alfabéticamente por `FullName`.
  - Mapea el estado a "Activo" o "Inactivo".
  - Retorna `IReadOnlyList<TechnicianDto>`.

## API y Comunicación
- `GET /api/technicians`
  - Respuestas: `200 OK` con lista de técnicos.

## Interfaz de Usuario (Blazor Server)
- `/tecnicos`:
  - Campo de búsqueda por texto libre (filtra en cliente o query por nombre/documento).
  - Filtro por estado: Todos / Activos / Inactivos.
  - Botón "Nuevo técnico" (visible solo para Administrador).

## Estrategia de Tests
- **Tests unitarios**:
  - Lista vacía cuando no hay registros.
  - Ordenamiento alfabético estricto por nombre completo.
