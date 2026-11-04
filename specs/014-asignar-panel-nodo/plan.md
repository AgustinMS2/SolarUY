# Plan Técnico: Spec 014 — Asignar Panel a Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/SolarPanel.cs`: propiedad `Guid? SolarNodeId { get; set; }` y navegación `SolarNode? SolarNode { get; set; }`.
  - `Data/SolarUyDbContext.cs`: relación opcional entre `SolarPanel` y `SolarNode` (`DeleteBehavior.Restrict`) e índice (no único) sobre `SolarNodeId`.
  - `Repositories/ISolarPanelRepository.cs` + implementación: `IReadOnlyList<SolarPanel> GetByIds(IReadOnlyCollection<Guid> ids)`, `IReadOnlyList<SolarPanel> GetByNodeId(Guid nodeId)`, `void UpdateRange(IReadOnlyList<SolarPanel> panels)` (un solo guardado para que el lote sea atómico) y `IReadOnlyDictionary<Guid, int> CountByNodeIds(IReadOnlyCollection<Guid> nodeIds)`. `GetAll` y `GetById` incluyen el nodo.
  - `Services/ISolarPanelService.cs` y `SolarPanelService.cs`: `ServiceResult AssignPanelsToNode(Guid nodeId, AssignPanelsRequest request)` y `ServiceResult UnassignPanelsFromNode(Guid nodeId, UnassignPanelsRequest request)`. `SolarPanelService` recibe además `ISolarNodeRepository` e `IInverterRepository`. El mapeo del listado de paneles incluye el nombre del nodo.
  - `Services/SolarNodeService.cs`: el DTO de nodo muestra la cantidad real de paneles (hoy fijo en 0, pendiente heredado de la spec 010); recibe además `ISolarPanelRepository`.
  - `Dtos/AssignPanelsRequest.cs` `(IReadOnlyList<Guid>? PanelIds, bool Confirm)` y `Dtos/UnassignPanelsRequest.cs` `(IReadOnlyList<Guid>? PanelIds)`.
  - `Dtos/SolarPanelDto.cs`: se agregan al final `string? SolarNodeName = null` y `Guid? SolarNodeId = null` (opcionales para no romper el uso existente; el modal necesita el id para distinguir los paneles de este nodo de los de otros).
  - `Controllers/SolarNodesController.cs`: `POST` y `DELETE /api/solar-nodes/{id}/panels`, ambos `[Authorize(Roles = Roles.Administrator)]`. El controlador recibe además `ISolarPanelService`.
- `SolarUY.Web`:
  - `SolarNodeApiClient`: métodos `AssignPanelsAsync` y `UnassignPanelsAsync` (la clase vive hoy en `SolarUY.ApiService/Services`, ubicación heredada de la spec 009; se extiende donde está).
  - `Services/PanelsApiClient.cs`: el `SolarPanelDto` del Web incorpora `SolarNodeId` y `SolarNodeName` (opcionales al final).
  - `Components/Pages/SolarNodePanelsModal.razor`: modal con los paneles del nodo (para desasociar) y los paneles activos del mismo parque (para asociar o reasignar), con selección múltiple.
  - `Components/Pages/SolarNodeList.razor`: botón "Paneles" por nodo (solo Administrador).
  - `Components/Pages/PanelList.razor`: columna "Nodo" con el nombre o "Sin asignar".
- `SolarUY.Tests`:
  - `Fakes/FakeSolarPanelRepository.cs` (extender con los métodos nuevos).
  - `Services/SolarPanelAssignNodeTests.cs`, `Services/SolarPanelUnassignNodeTests.cs` y tests de listado de paneles y nodos.
  - Los tests existentes que construyen `SolarPanelService` y `SolarNodeService` pasan las dependencias nuevas.

## Modelo de Datos y Persistencia
- Columna `SolarNodeId` (uuid, nullable) en la tabla `SolarPanels`, con llave foránea restrictiva hacia `SolarNodes` e índice.
- Migración: `AddSolarNodeToSolarPanels`.

## Servicios y Lógica de Dominio Pura
- `AssignPanelsToNode(nodeId, request)`, en este orden (el primer error corta):
  1. Nodo inexistente -> `Fail(NotFound, "El nodo solicitado no existe.")`.
  2. Lista vacía -> `Fail(Validation, "Seleccioná al menos un panel.")`; más de 50 ids distintos -> `Fail(Validation, "Se pueden asignar hasta 50 paneles a la vez.")`. Los ids repetidos se descartan.
  3. Nodo inactivo -> `Fail(Validation, "No se pueden asociar paneles inactivos o a nodos inactivos.")`.
  4. Algún panel inexistente -> `Fail(NotFound, "Uno o más paneles seleccionados no existen.")`.
  5. Algún panel inactivo -> `Fail(Validation, "No se pueden asociar paneles inactivos o a nodos inactivos.")`.
  6. Algún panel de otro parque (`panel.SolarParkId != node.SolarParkId`) -> `Fail(Validation, "No se pueden asociar paneles de otro parque.")`.
  7. Advertencias (si `Confirm` es falso y hay alguna -> `Fail(ConfirmationRequired, mensajes unidos por salto de línea)`):
     - Reasignación (RF-3): paneles con `SolarNodeId` distinto del nodo y no nulo; el nombre del nodo origen se obtiene con `ISolarNodeRepository`.
     - Potencia (RF-6): si el nodo tiene inversor, potencia acumulada = paneles activos del nodo después de la asignación (W / 1000) frente a `NominalCapacityKw`; los números se formatean con cultura invariante y hasta dos decimales.
  8. Se asigna `SolarNodeId` a los paneles que todavía no pertenecen al nodo, se actualiza `UpdatedAtUtc` de cada panel modificado y se guardan juntos con `UpdateRange`. Retorna `Ok()`.
- `UnassignPanelsFromNode(nodeId, request)`: nodo inexistente -> `NotFound`; lista vacía o de más de 50 -> `Validation`; panel inexistente -> `NotFound`; panel que no pertenece al nodo -> `Fail(Validation, "El panel \"<serie>\" no está asociado a este nodo.")`; si todo es válido, `SolarNodeId = null` en todos y `UpdateRange`. Se permite en nodos inactivos y con paneles inactivos.
- `SolarNodeService.GetAll/GetById`: `PanelsCount` sale de `CountByNodeIds` (una sola consulta para el listado).
- Fechas con `DateTime.UtcNow`, igual que el resto de los servicios existentes.

## API y Comunicación
- `POST /api/solar-nodes/{id}/panels`
  - Body: `{ "panelIds": ["guid", "guid"], "confirm": false }`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict` con `{ message, requiresConfirmation: true }` cuando hay advertencias por confirmar.
- `DELETE /api/solar-nodes/{id}/panels`
  - Body: `{ "panelIds": ["guid", "guid"] }`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`.

## Interfaz de Usuario (Blazor Server)
- Lista de nodos: botón "Paneles" (solo Administrador) que abre el modal; la columna de paneles muestra el conteo real.
- Modal: sección "Paneles del nodo" con casillas y botón "Desasociar seleccionados"; sección "Paneles del parque" con los paneles activos del mismo parque (serie, potencia y "Sin asignar" o el nodo actual) con casillas y botón "Asociar seleccionados". Máximo 50 por operación.
- Si la API pide confirmación, el modal muestra las advertencias con "Confirmar" y "Cancelar"; "Confirmar" reenvía el pedido con `confirm = true` y "Cancelar" no cambia nada.
- Lista de paneles: columna "Nodo" con el nombre o "Sin asignar".

## Decisiones específicas de esta spec
- Las advertencias de reasignación y de potencia se devuelven juntas en una sola respuesta `ConfirmationRequired` para que una única confirmación cubra todo lo que el Administrador vio.
- La asignación y la desasociación son operaciones en lote atómicas: si un panel falla, no se modifica ninguno.
- `UpdateRange` guarda todo el lote en un solo `SaveChanges`.

## Estrategia de Tests
- **Tests de arquitectura**: siguen en verde (los endpoints nuevos son solo de Administrador).
- **Tests unitarios** (solo reglas del servicio):
  - Asignación exitosa actualiza `SolarNodeId` en todos los paneles del lote.
  - Lista vacía y más de 50; ids repetidos se cuentan una vez.
  - Nodo inexistente, nodo inactivo, panel inexistente, panel inactivo.
  - Panel de otro parque rechaza todo el lote sin modificar nada.
  - Panel de otro nodo pide confirmación (con y sin varios paneles) y con confirmación reasigna; paneles que ya están en el nodo se ignoran.
  - Advertencia de potencia: sin inversor no advierte; con inversor y potencia mayor advierte; potencia igual no advierte; combinada con la reasignación en un solo mensaje; con confirmación asigna.
  - Desasociar: éxito, panel que no pertenece al nodo, lote vacío, nodo inactivo permitido, panel inactivo permitido.
  - Listados: nombre del nodo o ninguno en paneles, y cantidad de paneles en nodos.
- **UI Blazor**: verificación manual en navegador (sin bUnit).
