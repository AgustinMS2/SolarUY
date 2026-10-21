# Plan Técnico: Spec 001 — Alta de Parque Solar

Como es la primera spec, este plan también arma la base que reutilizan las specs 002, 003 y 004: proyecto de tests, PostgreSQL en Aspire, entidad, DbContext, repositorio, servicio y controlador.

## Proyectos y archivos afectados
- `SolarUY.AppHost/AppHost.cs`: agregar PostgreSQL (`AddPostgres("postgres").AddDatabase("solaruydb")`) y referenciarlo desde `apiservice`.
- `SolarUY.ApiService`:
  - `Domain/SolarPark.cs`, `Domain/SolarParkStatus.cs`
  - `Data/SolarUyDbContext.cs` + migración `InitialSolarPark`
  - `Repositories/ISolarParkRepository.cs`, `Repositories/SolarParkRepository.cs`
  - `Services/ServiceResult.cs`, `Services/ISolarParkService.cs`, `Services/SolarParkService.cs`
  - `Dtos/CreateSolarParkRequest.cs`
  - `Controllers/SolarParksController.cs`
  - `Program.cs`: `AddNpgsqlDbContext<SolarUyDbContext>("solaruydb")`, `AddControllers()`, `MapControllers()`, registro de repositorio y servicio (`AddScoped`).
- `SolarUY.Web`:
  - `Services/SolarParkApiClient.cs`
  - `Components/Pages/SolarParkCreate.razor` (ruta `/parques/nuevo`) + link en el menú.
- `SolarUY.Tests` (nuevo, xUnit): `Architecture/ArchitectureTests.cs`, `Services/SolarParkServiceCreateTests.cs`, `Fakes/FakeSolarParkRepository.cs`.

## Modelo de datos
```csharp
public class SolarPark
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;           // como lo escribió el usuario (con trim)
    public string NormalizedName { get; set; } = string.Empty; // Name.Trim().ToLowerInvariant()
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double MaxCapacityKw { get; set; }
    public double PowerAlertThresholdKw { get; set; }
    public SolarParkStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum SolarParkStatus { Active = 1, Inactive = 2 }
```
`SolarUyDbContext`: tabla `SolarParks`, `Name` y `NormalizedName` con largo máximo 100, **índice único en `NormalizedName`**. Así la base garantiza la unicidad sin distinguir mayúsculas (RF-3) sin usar extensiones de PostgreSQL.

## Resultado de los servicios
Los servicios no lanzan excepciones para errores de negocio: devuelven un resultado que el controlador traduce a código HTTP.
```csharp
public enum ServiceErrorType { None, Validation, NotFound, Conflict }

public class ServiceResult
{
    public bool Success { get; init; }
    public ServiceErrorType ErrorType { get; init; }
    public string? ErrorMessage { get; init; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(ServiceErrorType type, string message)
        => new() { Success = false, ErrorType = type, ErrorMessage = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }
    public static ServiceResult<T> Ok(T value) => new() { Success = true, Value = value };
    public static new ServiceResult<T> Fail(ServiceErrorType type, string message)
        => new() { Success = false, ErrorType = type, ErrorMessage = message };
}
```
Traducción en el controlador: `Validation` → 400, `NotFound` → 404, `Conflict` → 409.

## Repositorio (sincrónico)
```csharp
public interface ISolarParkRepository
{
    bool ExistsByNormalizedName(string normalizedName);
    void Add(SolarPark park);   // Add + SaveChanges
}
```

## Servicio (sincrónico, con `ILogger<SolarParkService>`)
```csharp
public interface ISolarParkService
{
    ServiceResult<Guid> Create(CreateSolarParkRequest request);
}
```
`Create` hace, en este orden:
1. `Trim` del nombre y validaciones de RF-2 → `Fail(Validation, <mensaje de la spec>)` con el primer error encontrado.
2. `ExistsByNormalizedName` → si existe, `Fail(Conflict, "Ya existe un parque con el nombre \"<nombre>\".")` (RF-3).
3. Crea la entidad con `Id = Guid.NewGuid()`, `Status = Active` y `CreatedAtUtc = DateTime.UtcNow`, llama a `Add` y registra un log `Information` con el Id (RF-1).

## API
`SolarParksController : ControllerBase`, `[ApiController]`, `[Route("api/solar-parks")]`.
- `POST /api/solar-parks` con `CreateSolarParkRequest { Name, Latitude, Longitude, MaxCapacityKw, PowerAlertThresholdKw }`
  - 201 + Id del parque creado
  - 400 con mensaje (validación) / 409 con mensaje (nombre duplicado)

## UI (Blazor Server)
- `SolarParkApiClient`: llama al POST con `HttpClient`. **Es asincrónico** (`PostAsJsonAsync`) porque así funciona `HttpClient`; el modelo sincrónico de la constitución aplica a servicios y repositorios de la API.
- `SolarParkCreate.razor`: `EditForm` con los 5 campos y `DataAnnotationsValidator` para los mismos rangos de RF-2 (validación rápida en pantalla; la regla de verdad está en el servicio). Muestra el mensaje de error que devuelve la API (400/409) o "Parque creado correctamente." y limpia el formulario (RF-4).

## Estrategia de tests
- **Arquitectura** (NetArchTest, en `SolarUY.Tests`):
  1. Las clases en `SolarUY.ApiService.Controllers` heredan de `ControllerBase`.
  2. `Services` y `Repositories` no dependen de `Controllers` ni de `Microsoft.AspNetCore.Mvc`.
  3. `Controllers` no dependen de `Repositories` (pasan siempre por el servicio).
- **Unitarios** de `SolarParkService.Create` usando `FakeSolarParkRepository` (lista en memoria, sin librería de mocks):
  - Datos válidos → Success, estado Activo, nombre con trim (RF-1).
  - Un test por cada mensaje de RF-2.
  - Nombre duplicado con otras mayúsculas → Conflict (RF-3).
- UI: prueba manual en el navegador.
