# Plan Técnico: Spec 002 — Listar Parques Solares

Reutiliza la entidad, el DbContext, el repositorio, el servicio y el controlador creados en la spec 001.

## Archivos afectados
- `SolarUY.ApiService/Dtos/SolarParkDto.cs` (nuevo)
- `Repositories/ISolarParkRepository.cs` + implementación: agregar `IReadOnlyList<SolarPark> GetAll()`.
- `Services/ISolarParkService.cs` + implementación: agregar `IReadOnlyList<SolarParkDto> GetAll()`.
- `Controllers/SolarParksController.cs`: agregar `GET /api/solar-parks`.
- `SolarUY.Web/Services/SolarParkApiClient.cs`: agregar `GetAllAsync()`.
- `SolarUY.Web/Components/Pages/SolarParkList.razor` (ruta `/parques`) + link en el menú.
- `SolarUY.Tests/Services/SolarParkServiceGetAllTests.cs`

## DTO
```csharp
public record SolarParkDto(Guid Id, string Name, double Latitude, double Longitude,
    double MaxCapacityKw, double PowerAlertThresholdKw, string Status); // "Activo" | "Inactivo"
```

## Servicio
`GetAll()` pide todo al repositorio, ordena por `NormalizedName` (RF-2) y lo convierte a `SolarParkDto`, traduciendo el estado a "Activo"/"Inactivo".
El orden se hace en el servicio (no en el repositorio) para poder probarlo con un test unitario.

## API
- `GET /api/solar-parks` → 200 con la lista (vacía si no hay parques).

## UI
- `SolarParkList.razor`: tabla Bootstrap con las columnas de RF-1; badge verde "Activo" / gris "Inactivo" (RF-3); mensaje de lista vacía (RF-4); botón "Nuevo parque" → `/parques/nuevo` (RF-5); `try/catch` alrededor de `GetAllAsync()` con el mensaje de RF-6.
- Formato numérico: `ToString("F4")` para coordenadas y `ToString("0.##")` + " kW" para potencias.

## Tests
- Unitario: con el fake repositorio cargado con "gamma", "alfa", "Beta" → `GetAll()` devuelve alfa, Beta, gamma.
- Arquitectura: los de la spec 001 siguen en verde.
- UI: manual.
