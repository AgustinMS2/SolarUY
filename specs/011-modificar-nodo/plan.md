# Plan Técnico: Spec 011 — Modificar Nodo Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarNodeService.cs`: agregar método `UpdateNode(Guid id, UpdateSolarNodeRequest request)`.
  - `Services/SolarNodeService.cs`: implementar `UpdateNode` con las validaciones RF-2, RF-4, RF-5, RF-7.
  - `Dtos/UpdateSolarNodeRequest.cs`: nuevo DTO para el request de actualización.
  - `Dtos/SolarNodeDto.cs`: ya existe (ver spec 010), se reutiliza.
- `SolarUY.Web`:
  - `Services/SolarNodesApiClient.cs`: agregar método `UpdateNodeAsync(Guid id, UpdateSolarNodeRequest request)`.
  - `Components/Pages/SolarNodeEdit.razor` (ruta `/nodos/editar/{id}`): formulario de edición con datos cargados.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceUpdateTests.cs`: tests unitarios del servicio Update.

## Requisitos del servicio

`UpdateNode` hace, en este orden:
1. Validar que el nodo existe (GetById) → si no, `Fail(NotFound, "El nodo solicitado no existe.")` (RF-5).
2. `Trim` del identificador y validaciones de RF-2 (mismas reglas que spec 009) → `Fail(Validation, <mensaje>)` con el primer error encontrado.
3. `ExistsByIdentifier` (excluyendo el nodo actual) → si existe otro nodo con misma identificador, `Fail(Conflict, "Ya existe un nodo con el identificador "<identificador>".`" (RF-4).
4. Actualiza la entidad, registra `UpdatedAtUtc = DateTime.UtcNow`, muestra "Nodo actualizado correctamente." y retorna OK (RF-2).
5. Si el usuario presiona "Cancelar", retornar sin cambios (RF-6).

## API

`SolarNodesController : ControllerBase`, `[ApiController]`, `[Route("api/solar-nodes")]`.

- `PUT /api/solar-nodes/{id}` con `UpdateSolarNodeRequest { Identifier, Name, Type }`
  - 200 + mensaje "Nodo actualizado correctamente."
  - 400 con mensaje (validación) / 404 si no existe / 409 con mensaje (identificador duplicado)

## UI (Blazor Server)

- `SolarNodeEdit.razor`: página en `/nodos/editar/{id}` que:
  - En el OnInitialized, llama a `SolarNodesApiClient.GetNodeByIdAsync(id)` para cargar los datos.
  - `EditForm` con los campos Identifier, Name, Type y `DataAnnotationsValidator`.
  - Muestra el mensaje de error que devuelve la API (400/404/409) o "Nodo actualizado correctamente."
  - Botón "Cancelar" que vuelve al listado sin guardar cambios (RF-6).
  - El campo Identificador es de solo lectura (no se puede cambiar desde aquí).

## Estrategia de tests

- **Unitarios** de `SolarNodeService.UpdateNode` usando `FakeSolarNodeRepository`:
  - Datos válidos → Success, fecha de modificación actualizada (RF-2).
  - Un test por cada mensaje de RF-2.
  - Identificador duplicado con otras mayúsculas → Conflict (RF-4).
  - Nodo inexistente → NotFound (RF-5).
  - Cancelar sin cambios → sin modificar la entidad.