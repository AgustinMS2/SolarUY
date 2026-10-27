# Plan Técnico: Spec 012 — Baja de Nodo Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarNodeService.cs`: agregar método `DeleteNode(Guid id)`.
  - `Services/SolarNodeService.cs`: implementar `DeleteNode` con las validaciones RF-1 a RF-6.
- `SolarUY.Web`:
  - `Services/SolarNodesApiClient.cs`: agregar método `DeleteNodeAsync(Guid id)`.
  - `Components/Pages/SolarNodeDelete.razor` (ruta `/nodos/baja/{id}`): pantalla de confirmación de baja.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceDeleteTests.cs`: tests unitarios del servicio Delete.

## Requisitos del servicio

`DeleteNode` hace, en este orden:
1. Validar que el nodo existe → si no, `Fail(NotFound, "El nodo solicitado no existe.")` (RF-6).
2. Validar que el nodo no está já inactivo → si ya está inactivo, `Fail(Validation, "El nodo ya está inactivo.")` (RF-5).
3. Si el nodo está activo, cambiar su estado a `Inactive` y registrar `UpdatedAtUtc = DateTime.UtcNow` (baja lógica) (RF-2).
4. Mostrar "Nodo dado de baja correctamente." y retornar OK.
5. Si el usuario cancelara (a través de la UI), no hacer cambios (RF-3).

## API

`SolarNodesController : ControllerBase`, `[ApiController]`, `[Route("api/solar-nodes")]`.

- `DELETE /api/solar-nodes/{id}`
  - 200 + mensaje "Nodo dado de baja correctamente."
  - 404 si no existe.
  - 400 con mensaje "El nodo ya está inactivo." si ya estaba inactivo.

## UI (Blazor Server)

- `SolarNodeDelete.razor`: página en `/nodos/baja/{id}` que:
  - Muestra un diálogo de confirmación: "¿Confirmás la baja del nodo "<identificador>"?" con botones "Confirmar" y "Cancelar".
  - Al presionar "Confirmar", llama a `SolarNodesApiClient.DeleteNodeAsync(id)`.
  - Al presionar "Cancelar", cierra el diálogo sin hacer nada (RF-3).
  - Muestra el mensaje "Nodo dado de baja correctamente." y recarga el listado.
  - El botón "Dar de baja" no aparece si el nodo ya está inactivo (RF-4).

## Estrategia de tests

- **Unitarios** de `SolarNodeService.DeleteNode` usando `FakeSolarNodeRepository`:
  - Nodo activo → cambia a inactivo, muestra mensaje correcto (RF-2).
  - Nodo ya inactivo → no cambia, muestra "El nodo ya está inactivo." (RF-5).
  - Nodo inexistente → NotFound (RF-6).
  - Cancelar en la UI → no hacer cambios.