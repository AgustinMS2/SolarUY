# Plan Técnico: Spec 014 — Asignar Panel a Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/SolarPanel.cs`: propiedad `Guid? SolarNodeId { get; set; }` y navegación `SolarNode? SolarNode { get; set; }`.
  - `Data/SolarUyDbContext.cs`: configuración de relación opcional entre `SolarPanel` y `SolarNode`.
  - `Repositories/ISolarPanelRepository.cs`: métodos `GetPanelsByPark(Guid parkId)`, `AssignToNode(IReadOnlyList<Guid> panelIds, Guid nodeId)`.
  - `Services/ISolarPanelService.cs`: método `AssignPanelsToNode(Guid nodeId, AssignPanelsRequest request)`.
  - `Dtos/AssignPanelsRequest.cs`: DTO con `IReadOnlyList<Guid> PanelIds`.
  - `Controllers/SolarNodesController.cs`: endpoint `POST /api/solar-nodes/{id}/panels`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: método `AssignPanelsAsync(Guid nodeId, List<Guid> panelIds)`.
  - `Components/Pages/SolarNodePanelsModal.razor`: modal con selector múltiple de paneles disponibles del mismo parque.
- `SolarUY.Tests`:
  - `Services/SolarPanelAssignNodeTests.cs`: tests unitarios de validación de pertenencia al mismo parque, paneles inactivos y reasignación.

## Modelo de Datos y Persistencia
- Columna `SolarNodeId` (nullable) en tabla `SolarPanels`.
- Llave foránea restrictiva o nula al eliminar nodo.
- Migración: `AddSolarNodeToSolarPanels`.

## Servicios y Lógica de Dominio Pura
- `AssignPanelsToNode(Guid nodeId, AssignPanelsRequest request)`:
  - Recupera nodo. Verifica que esté Activo.
  - Para cada panel:
    - Verifica que exista y esté Activo.
    - Verifica que `panel.SolarParkId == node.SolarParkId` (mismo parque). Si no -> `Fail(Validation, "No se pueden asociar paneles de otro parque.")`.
  - Asigna `panel.SolarNodeId = nodeId` para cada panel en lote.
  - Persiste cambios y retorna `ServiceResult.Ok()`.

## API y Comunicación
- `POST /api/solar-nodes/{id}/panels`
  - Body: `{ panelIds: ["guid", "guid"] }`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`.

## Estrategia de Tests
- **Tests unitarios**:
  - Asignación exitosa actualiza `SolarNodeId` en los paneles.
  - Paneles de parque distinto son rechazados.
  - Nodos o paneles inactivos son rechazados.
