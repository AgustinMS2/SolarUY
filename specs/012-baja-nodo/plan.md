# Plan Técnico: Spec 012 — Baja de Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Services/ISolarNodeService.cs` y `SolarNodeService.cs`: método `ServiceResult Deactivate(Guid id)`.
  - `Controllers/SolarNodesController.cs`: endpoint `DELETE /api/solar-nodes/{id}`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: método `DeactivateAsync(Guid id)`.
  - `Components/Pages/SolarNodeList.razor`: botón "Dar de baja" solo en nodos activos y modal de confirmación.
- `SolarUY.Tests`:
  - `Services/SolarNodeServiceDeactivateTests.cs`: tests unitarios de desactivación lógica.

## Servicios y Lógica de Dominio Pura
- `Deactivate(Guid id)`:
  1. `GetById(id)` -> `null` ⇒ `Fail(NotFound, "El nodo solicitado no existe.")` (RF-6).
  2. `Status == Inactive` ⇒ `Fail(Conflict, "El nodo ya está inactivo.")` (RF-5).
  3. `Status = Inactive`, `UpdatedAtUtc = DateTime.UtcNow`, llamada a `Update(node)`, registro en log con `ILogger` (RF-2).
  4. Retorna `ServiceResult.Ok()`.

## API y Comunicación
- `DELETE /api/solar-nodes/{id}`
  - Respuestas: `204 No Content`, `404 Not Found`, `409 Conflict`.

## Interfaz de Usuario (Blazor Server)
- En `SolarNodeList.razor`:
  - Botón "Dar de baja" visible únicamente si el nodo está "Activo" (RF-4).
  - Modal de confirmación: "¿Confirmás la baja del nodo \"<nombre>\"?" con "Confirmar" y "Cancelar".
  - Tras confirmación exitosa, recarga la grilla y muestra banner de éxito: "Nodo dado de baja correctamente."

## Estrategia de Tests
- **Tests unitarios**:
  - Nodo activo pasa a estado Inactivo con fecha `UpdatedAtUtc`.
  - Nodo inexistente devuelve NotFound.
  - Nodo ya inactivo devuelve Conflict.
