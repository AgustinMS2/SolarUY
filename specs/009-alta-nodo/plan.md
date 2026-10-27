# Plan Técnico: Spec 009 — Alta de Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/SolarNode.cs`: entidad de nodo (Id, Name, NormalizedName, SolarParkId, Status, CreatedAtUtc, UpdatedAtUtc).
  - `Data/SolarUyDbContext.cs`: `DbSet<SolarNode>`, índice único compuesto en `(SolarParkId, NormalizedName)`.
  - `Repositories/ISolarNodeRepository.cs` e implementación `SolarNodeRepository.cs`: métodos `ExistsByName(Guid parkId, string normalizedName)` y `Add(SolarNode node)`.
  - `Services/ISolarNodeService.cs` y `SolarNodeService.cs`: método sincrónico `Create(CreateSolarNodeRequest request)`.
  - `Dtos/CreateSolarNodeRequest.cs`: DTO con `string Name`, `Guid SolarParkId`.
  - `Controllers/SolarNodesController.cs`: endpoint `POST /api/solar-nodes`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: cliente API asíncrono para consumir `POST /api/solar-nodes`.
  - `Components/Pages/SolarNodeCreate.razor` (ruta `/nodos/nuevo`): formulario con selector de parque activo y validaciones.
- `SolarUY.Tests`:
  - `Fakes/FakeSolarNodeRepository.cs`: repositorio en memoria para tests.
  - `Services/SolarNodeServiceCreateTests.cs`: tests unitarios de las reglas de negocio del servicio.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)
```csharp
public class SolarNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public Guid SolarParkId { get; set; }
    public SolarPark? SolarPark { get; set; }
    public SolarNodeStatus Status { get; set; } = SolarNodeStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum SolarNodeStatus { Active = 1, Inactive = 2 }
```
- Configuración en DbContext:
  - Nombre máximo 100 caracteres.
  - Índice único: `.HasIndex(n => new { n.SolarParkId, n.NormalizedName }).IsUnique()`.
  - Llave foránea restrictiva hacia `SolarParks`.
- Migración requerida: `AddSolarNodeEntity`.

## Servicios y Lógica de Dominio Pura
- `ISolarNodeService.Create(CreateSolarNodeRequest request)`:
  - Valida nombre (no vacío, trim, máx 100).
  - Verifica que el parque solar exista y esté activo en `ISolarParkRepository`.
  - Verifica unicidad en el mismo parque con `ExistsByName(request.SolarParkId, normalizedName)`.
  - Retorna `ServiceResult<Guid>.Ok(node.Id)` o `Fail(Validation | Conflict | NotFound)`.

## API y Comunicación
- `POST /api/solar-nodes`
  - Body: `CreateSolarNodeRequest`
  - Respuestas: `201 Created` con `{ id }`, `400 Bad Request`, `404 Not Found` (parque no existe), `409 Conflict` (duplicado en parque).

## Interfaz de Usuario (Blazor Server)
- `/nodos/nuevo`:
  - Dropdown con parques solares activos obtenidos mediante `SolarParkApiClient`.
  - InputText para nombre con feedback de validación inline.
  - Mensaje de confirmación: "Nodo creado correctamente." y limpieza del formulario.

## Estrategia de Tests
- **Tests unitarios** (`SolarNodeServiceCreateTests`):
  - Creación válida devuelve Id y persiste en estado Activo.
  - Nombre vacío o > 100 devuelve error de validación.
  - Nombre duplicado en el mismo parque devuelve Conflict.
  - Nombre repetido en parques distintos es válido.
  - Parque inexistente o inactivo devuelve NotFound / Validation.
- **Tests de arquitectura**: siguen en verde.
