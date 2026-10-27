# Plan Técnico: Spec 010 — Listar Nodos Solares

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarNodeService.cs`: agregar método `GetAllNodes()`.
  - `Services/SolarNodeService.cs`: implementar `GetAllNodes` que devuelva `IReadOnlyList<SolarNodeDto>`.
  - `Dtos/SolarNodeDto.cs`: nuevo DTO para el nodo (Id, Identifier, Name, Type, SolarParkName, Status).
- `SolarUY.Web`:
  - `Services/SolarNodesApiClient.cs`: agregar método `GetAllNodesAsync()`.
  - `Components/Pages/SolarNodeList.razor` (ruta `/nodos`): página que muestra la tabla con el listado.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceGetAllTests.cs`: tests unitarios del servicio GetAll.

## Modelo de datos (DTO)

```csharp
public record SolarNodeDto(
    Guid Id,
    string Identifier,
    string Name,
    string Type,
    string SolarParkName,
    string Status
);
```

## API

`SolarNodesController : ControllerBase`, `[ApiController]`, `[Route("api/solar-nodes")]`.

- `GET /api/solar-nodes` -> `IReadOnlyList<SolarNodeDto>`
  - Devuelve todos los nodos con su nombre de parque asociado.
  - Si no hay nodos, devuelve una lista vacía (la UI mostrará "No hay nodos registrados").

## UI (Blazor Server)

- `SolarNodeList.razor`: página en `/nodos` que llama a `SolarNodesApiClient.GetAllNodesAsync()` y muestra:
  - Tabla con columnas: Identificador, Nombre, Tipo, Parque solar, Estado.
  - Si la lista está vacía, muestra "No hay nodos registrados."
  - Botón "Nuevo nodo" que navega a `/nodos/nuevo`.
  - El estado se muestra con colores: verde para "Activo", gris para "Inactivo".
  - El listado está ordenado alfabéticamente por identificador (sin distinguir mayúsculas).

## Estrategia de tests

- **Unitarios** de `SolarNodeService.GetAll` usando `FakeSolarNodeRepository`:
  - Devuelve lista vacía cuando no hay nodos.
  - Devuelve nodos ordenados alfabéticamente por identifier (RF-2).
  - El estado se mapea correctamente a "Activo"/"Inactivo".
- **Arquitectura**: igual que la spec 009, verificar que services y repositories no dependen de controllers.