# Plan Técnico: Spec 004 — Baja de Parque Solar

## Archivos afectados
- `Services/ISolarParkService.cs` + implementación: agregar `ServiceResult Deactivate(Guid id)`.
- `Controllers/SolarParksController.cs`: agregar `DELETE /api/solar-parks/{id}`.
- `SolarUY.Web/Services/SolarParkApiClient.cs`: `DeactivateAsync`.
- `SolarUY.Web/Components/Pages/SolarParkList.razor`: botón "Dar de baja" y diálogo de confirmación.
- `SolarUY.Tests/Services/SolarParkServiceDeactivateTests.cs`
- El repositorio no cambia: usa `GetById` y `Update` de la spec 003.

## Servicio
`Deactivate(id)`, en este orden:
1. `GetById` → `null` ⇒ `Fail(NotFound, "El parque solicitado no existe.")` (RF-6).
2. `Status == Inactive` ⇒ `Fail(Conflict, "El parque ya está inactivo.")` (RF-5).
3. `Status = Inactive`, `UpdatedAtUtc = DateTime.UtcNow`, `Update`, log `Information` (RF-2).

## API
- `DELETE /api/solar-parks/{id}` → 204 / 404 / 409. Se usa DELETE aunque sea baja lógica porque, para quien consume la API, el parque deja de estar activo; el registro se conserva.

## UI
- En `SolarParkList.razor`, botón "Dar de baja" solo en filas con estado Activo (RF-4).
- Confirmación con un modal de Bootstrap dentro del componente (sin JavaScript propio) con el texto de RF-1 (RF-1, RF-3).
- Tras 204 recarga el listado y muestra el mensaje de RF-2; con 404/409 muestra el mensaje que devuelve la API.

## Tests
- Unitarios (fake repositorio): parque activo → queda Inactivo con fecha (RF-2); ya inactivo → Conflict (RF-5); inexistente → NotFound (RF-6).
- Arquitectura: siguen en verde.
- UI: manual.
