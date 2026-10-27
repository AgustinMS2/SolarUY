# Plan Técnico: Spec 005 — Alta de Panel Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Domain/SolarPanel.cs`: nueva entidad de panel.
  - `Data/SolarUyDbContext.cs`: agregar `DbSet<SolarPanel>` y configuración del índice único en `NormalizedSerieNumber`.
  - `Repositories/ISolarParkRepository.cs`: agregar método `ExistsBySerieNumber(string normalizedSerieNumber)` y `GetParkById(Guid parkId)`.
  - `Repositories/SolarParkRepository.cs`: implementar los nuevos métodos.
  - `Services/ServiceResult.cs`, `Services/ISolarPanelService.cs`, `Services/SolarPanelService.cs`: nuevo servicio de panel.
  - `Dtos/CreateSolarPanelRequest.cs`: nuevo DTO para el request de alta.
  - `Controllers/PanelsController.cs`: nuevo controlador con `POST /api/panels`.
- `SolarUY.AppHost`: asegurar que PostgreSQL y la conexión a `solaruydb` estén disponibles (no se requiere nuevo database, usa el existente).
- `SolarUY.Web`:
  - `Services/PanelsApiClient.cs`: cliente API asíncrona para crear panel.
  - `Components/Pages/PanelCreate.razor` (ruta `/paneles/nuevo`): formulario de alta con validación.
- `SolarUY.Tests` (nuevo, xUnit):
  - `Architecture/ArchitectureTests.cs`: tests de arquitectura para la nueva capa.
  - `Fakes/FakeSolarPanelRepository.cs`: repositorio falso en memoria para tests.
  - `Services/PanelServiceCreateTests.cs`: tests unitarios del servicio.

## Modelo de datos

```csharp
public class SolarPanel
{
    public Guid Id { get; set; }
    public string SerieNumber { get; set; } = string.Empty;           // como lo escribió el usuario (con trim)
    public string NormalizedSerieNumber { get; set; } = string.Empty; // SerieNumber.Trim().ToLowerInvariant()
    public int PowerW { get; set; }                                     // Potencia en Watts, entero > 0
    public double Efficiency { get; set; }                              // Eficiencia en %, entre 0.1 y 100
    public Guid SolarParkId { get; set; }                               // FK al parque
    public SolarParkStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum SolarParkStatus { Active = 1, Inactive = 2 }
```

`SolarUyDbContext`: tabla `SolarPanels`, `SerieNumber` y `NormalizedSerieNumber` con largo máximo 50, **índice único en `NormalizedSerieNumber`**. Así la base garantiza la unicidad sin distinguir mayúsculas (RF-3) sin usar extensiones de PostgreSQL.

## Resultado de los servicios

Los servicios no lanzan excepciones para errores de negocio: devuelven un resultado que el controlador traduce a código HTTP.

```csharp
public class ServiceResult { ... } (mismo que el spec 001)

public class ServiceResult<T> : ServiceResult { ... } (mismo que el spec 001)
```

## Repositorio (sincrónico)

```csharp
public interface ISolarPanelRepository
{
    bool ExistsBySerieNumber(string normalizedSerieNumber);
    void Add(SolarPanel panel);
    SolarPark GetParkById(Guid parkId); // ayuda al servicio
}
```

## Servicio (sincrónico, con `ILogger<SolarPanelService>`)

```csharp
public interface ISolarPanelService
{
    ServiceResult<Guid> Create(CreateSolarPanelRequest request);
}
```

`Create` hace, en este orden:
1. `Trim` de la serie número y validaciones de RF-2 → `Fail(Validation, <mensaje de la spec>)` con el primer error encontrado.
2. `ExistsBySerieNumber` → si existe, `Fail(Conflict, "Ya existe un panel con el número de serie "<serie>".`" (RF-4).
3. Validar que el `SolarParkId` existe y está activo → si no, `Fail(Validation, "El parque solar seleccionado no existe o no está activo.")`.
4. Crea la entidad con `Id = Guid.NewGuid()`, `Status = Active` y `CreatedAtUtc = DateTime.UtcNow`, llama a `Add` y registra un log `Information` con el Id (RF-1).

## API

`PanelsController : ControllerBase`, `[ApiController]`, `[Route("api/panels")]`.

- `POST /api/panels` con `CreateSolarPanelRequest { SerieNumber, PowerW, Efficiency, SolarParkId }`
  - 201 + Id del panel creado
  - 400 con mensaje (validación) / 409 con mensaje (serie duplicada)

## UI (Blazor Server)

- `PanelsApiClient`: llama al POST con `HttpClient`. **Es asincrónico** (`PostAsJsonAsync`) porque así funciona `HttpClient`.
- `PanelCreate.razor`: `EditForm` con los campos SerieNumber, PowerW, Efficiency, SolarParkId (dropdown con parques activos) y `DataAnnotationsValidator`. Muestra el mensaje de error que devuelve la API (400/409) o "Panel creado correctamente." y limpia el formulario (RF-4).

## Estrategia de tests

- **Arquitectura** (NetArchTest, en `SolarUY.Tests`):
  1. Las clases en `SolarUY.ApiService.Controllers` heredan de `ControllerBase`.
  2. `Services` y `Repositories` no dependen de `Controllers` ni de `Microsoft.AspNetCore.Mvc`.
  3. `Controllers` no dependen de `Repositories` (pasan siempre por el servicio).
- **Unitarios** de `SolarPanelService.Create` usando `FakeSolarPanelRepository` (lista en memoria, sin librería de mocks):
  - Datos válidos → Success, estado Activo, serie con trim (RF-1).
  - Un test por cada mensaje de RF-2.
  - Serie duplicada con otras mayúsculas → Conflict (RF-4).
- UI: prueba manual en el navegador.