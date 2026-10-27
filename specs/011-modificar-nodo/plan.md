# Plan Técnico: Spec 011 — Modificar Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Repositories/ISolarNodeRepository.cs`: métodos `GetById(Guid id)`, `ExistsByName(Guid parkId, string normalizedName, Guid excludeId)`, `Update(SolarNode node)`.
  - `Services/ISolarNodeService.cs` y `SolarNodeService.cs`: métodos `GetById(Guid id)`, `Update(Guid id, UpdateSolarNodeRequest request)`.
  - `Dtos/UpdateSolarNodeRequest.cs`: DTO con `string Name`.
  - `Controllers/SolarNodesController.cs`: endpoints `GET /api/solar-nodes/{id}` y `PUT /api/solar-nodes/{id}`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: métodos `GetByIdAsync(Guid id)`, `UpdateAsync(Guid id, UpdateSolarNodeRequest request)`.
  - `Components/Pages/SolarNodeEdit.razor` (ruta `/nodos/{id:guid}/editar`): vista de edición con nombre modificable y parque de solo lectura.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceUpdateTests.cs`: tests unitarios de edición, exclusión de Id propio y validaciones.

## Modelo de Datos y Persistencia
- Modificación en memoria de la entidad recuperada y persistencia mediante `_context.SaveChanges()`.
- Actualización automática de `UpdatedAtUtc = DateTime.UtcNow`.

## Servicios y Lógica de Dominio Pura
- `Update(Guid id, UpdateSolarNodeRequest request)`:
  - Recupera nodo con `GetById(id)`. Si es `null` -> `Fail(NotFound, "El nodo solicitado no existe.")`.
  - Valida nombre (longitud, vacío).
  - Comprueba unicidad en el parque excluyendo el propio nodo: `ExistsByName(node.SolarParkId, normalizedName, excludeId: id)`. Si existe -> `Fail(Conflict)`.
  - Asigna nombre, normalizado y fecha UTC. Llama a `Update(node)`. Retorna `Ok()`.

## API y Comunicación
- `GET /api/solar-nodes/{id}` -> `200 OK` (`SolarNodeDto`) / `404 Not Found`.
- `PUT /api/solar-nodes/{id}` -> `204 No Content` / `400 Bad Request` / `404 Not Found` / `409 Conflict`.

## Interfaz de Usuario (Blazor Server)
- `/nodos/{id}/editar`:
  - InputText con el nombre del nodo.
  - Campo informativo no editable con el nombre del Parque Solar.
  - Botones "Guardar cambios" y "Cancelar" (redirige al listado).
  - Alerta de éxito: "Nodo actualizado correctamente."

## Estrategia de Tests
- **Tests unitarios**:
  - Actualización válida con persistencia de fecha `UpdatedAtUtc`.
  - Mantener el mismo nombre del nodo no arroja conflicto.
  - Nombre duplicado con otro nodo del mismo parque arroja Conflict.
  - Nodo inexistente arroja NotFound.
