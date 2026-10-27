# Plan Técnico: Spec 009 — Alta de Nodo Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Domain/SolarNode.cs`: nueva entidad de nodo.
  - `Data/SolarUyDbContext.cs`: agregar `DbSet<SolarNode>` y configuración del índice único en `NormalizedIdentifier`.
  - `Repositories/ISolarNodeRepository.cs`: nuevo repositorio con métodos `ExistsByIdentifier`, `Add`.
  - `Repositories/SolarNodeRepository.cs`: implementar el repositorio.
  - `Services/ServiceResult.cs`, `Services/ISolarNodeService.cs`, `Services/SolarNodeService.cs`: nuevo servicio de nodo.
  - `Dtos/CreateSolarNodeRequest.cs`: nuevo DTO para el request de alta.
  - `Controllers/SolarNodesController.cs`: nuevo controlador con `POST /api/solar-nodes`.
- `SolarUY.AppHost`: asegurar que PostgreSQL y la conexión a `solaruydb` estén disponibles.
- `SolarUY.Web`:
  - `Services/SolarNodesApiClient.cs`: cliente API asíncrona para crear nodo.
  - `Components/Pages/SolarNodeCreate.razor` (ruta `/nodos/nuevo`): formulario de alta con validación.
- `SolarUY.Tests` (nuevo, xUnit):
  - `Architecture/ArchitectureTests.cs`: tests de arquitectura para la nueva capa.
  - `Fakes/FakeSolarNodeRepository.cs`: repositorio falso en memoria para tests.
  - `Services/SolarNodeServiceCreateTests.cs`: tests unitarios del servicio.

## Modelo de datos

```csharp
public class SolarNode
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty;           // como lo escribió el usuario (con trim)
    public string NormalizedIdentifier { get; set; } = string.Empty; // Identifier.Trim().ToLowerInvariant()
    public string Name { get; set; } = string.Empty;                 // Nombre del nodo
    public string Type { get; set; } = string.Empty;                 // Tipo: Monitoreo, Control, Medición, Protección
    public Guid SolarParkId { get; set; }                            // FK al parque
    public SolarParkStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

`SolarUyDbContext`: tabla `SolarNodes`, `Identifier` y `NormalizedIdentifier` con largo máximo 50, **índice único en `NormalizedIdentifier`**. Así la base garantiza la unicidad sin distinguir mayúsculas (RF-4) sin usar extensiones de PostgreSQL.

## Resultado de los servicios

Los servicios no lanzan excepciones para errores de negocio: devuelven un resultado que el controlador traduce a código HTTP.

```csharp
public class ServiceResult { ... } (mismo que el spec 001)

public class ServiceResult<T> : ServiceResult { ... } (mismo que el spec 001)
```

## Repositorio (sincrónico)

```csharp
public interface ISolarNodeRepository
{
    bool ExistsByIdentifier(string normalizedIdentifier);
    void Add(SolarNode node);
}
```

## Servicio (sincrónico, con `ILogger<SolarNodeService>`)

```csharp
public interface ISolarNodeService
{
    ServiceResult<Guid> Create(CreateSolarNodeRequest request);
}
```

`Create` hace, en este orden:
1. `Trim` del identificador y validaciones de RF-2 → `Fail(Validation, <mensaje de la spec>)` con el primer error encontrado.
2. `ExistsByIdentifier` → si existe, `Fail(Conflict, "Ya existe un nodo con el identificador "<identificador>".`" (RF-4).
3. Crea la entidad con `Id = Guid.NewGuid()`, `Status = Active` y `CreatedAtUtc = DateTime.UtcNow`, llama a `Add` y registra un log `Information` con el Id (RF-1).

## API

`SolarNodesController : ControllerBase`, `[ApiController]`, `[Route("api/solar-nodes")]`.

- `POST /api/solar-nodes` con `CreateSolarNodeRequest { Identifier, Name, Type, SolarParkId }`
  - 201 + Id del nodo creado
  - 400 con mensaje (validación) / 409 con mensaje (identificador duplicado)

## UI (Blazor Server)

- `SolarNodesApiClient`: llama al POST con `HttpClient`. **Es asincrónico** (`PostAsJsonAsync`) porque así funciona `HttpClient`.
- `SolarNodeCreate.razor`: `EditForm` con los campos Identifier, Name, Type (dropdown), SolarParkId (dropdown) y `DataAnnotationsValidator`. Muestra el mensaje de error que devuelve la API (400/409) o "Nodo creado correctamente." y limpia el formulario (RF-3).

## Estrategia de tests

- **Arquitectura** (NetArchTest, en `SolarUY.Tests`):
  1. Las clases en `SolarUY.ApiService.Controllers` heredan de `ControllerBase`.
  2. `Services` y `Repositories` no dependen de `Controllers` ni de `Microsoft.AspNetCore.Mvc`.
  3. `Controllers` no dependen de `Repositories` (pasan siempre por el servicio).
- **Unitarios** de `SolarNodeService.Create` usando `FakeSolarNodeRepository` (lista en memoria, sin librería de mocks):
  - Datos válidos → Success, estado Activo, identificador con trim (RF-1).
  - Un test por cada mensaje de RF-2.
  - Identificador duplicado con otras mayúsculas → Conflict (RF-4).
- UI: prueba manual en el navegador.