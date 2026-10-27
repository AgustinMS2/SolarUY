# Plan Técnico: Spec 006 — Listar Paneles Solares

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarPanelService.cs`: agregar método `GetAllPanels()`.
  - `Services/SolarPanelService.cs`: implementar `GetAllPanels` que devuelva `IReadOnlyList<SolarPanelDto>`.
  - `Dtos/SolarPanelDto.cs`: nuevo DTO para el panel (Id, SerieNumber, PowerW, Efficiency, SolarParkName, Status).
- `SolarUY.Web`:
  - `Services/PanelsApiClient.cs`: agregar método `GetAllPanelsAsync()`.
  - `Components/Pages/PanelList.razor` (ruta `/paneles`): página que muestra la tabla con el listado.
- `SolarUY.Tests`:
  - `Services/PanelServiceGetAllTests.cs`: tests unitarios del servicio GetAll.

## Modelo de datos (DTO)

```csharp
public record SolarPanelDto(
    Guid Id,
    string SerieNumber,
    int PowerW,
    double Efficiency,
    string SolarParkName,
    string Status
);
```

## API

`PanelsController : ControllerBase`, `[ApiController]`, `[Route("api/panels")]`.

- `GET /api/panels` -> `IReadOnlyList<SolarPanelDto>`
  - Devuelve todos los panels con su nombre de parque asociado.
  - Si no hay panels, devuelve una lista vacía (la UI mostrará "No hay panels registrados").

## UI (Blazor Server)

- `PanelList.razor`: página en `/paneles` que llama a `PanelsApiClient.GetAllPanelsAsync()` y muestra:
  - Tabla con columnas: Número de serie, Potencia (W), Eficiencia (%), Parque solar, Estado.
  - Si la lista está vacía, muestra "No hay panels registrados."
  - Botón "Nuevo panel" que navega a `/paneles/nuevo`.
  - El estado se muestra con colores: verde para "Activo", gris para "Inactivo".
  - El listado está ordenado alfabéticamente por número de serie (sin distinguir mayúsculas).

## Estrategia de tests

- **Unitarios** de `SolarPanelService.GetAll` usando `FakeSolarPanelRepository`:
  - Devuelve lista vacía cuando no hay panels.
  - Devuelve panels ordenados alfabéticamente por serieNumber (RF-2).
  - El estado se mapea correctamente a "Activo"/"Inactivo".
- **Arquitectura**: igual que la spec 005, verificar que services y repositories no dependen de controllers.