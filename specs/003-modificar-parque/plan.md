# Plan Técnico: Spec 003 — Modificar Parque Solar

## Archivos afectados
- `SolarUY.ApiService/Dtos/UpdateSolarParkRequest.cs` (nuevo; mismos campos que `CreateSolarParkRequest`).
- `Repositories/ISolarParkRepository.cs` + implementación: agregar `SolarPark? GetById(Guid id)`, `bool ExistsByNormalizedName(string normalizedName, Guid excludeId)` y `void Update(SolarPark park)`.
- `Services/ISolarParkService.cs` + implementación: agregar `ServiceResult<SolarParkDto> GetById(Guid id)` y `ServiceResult Update(Guid id, UpdateSolarParkRequest request)`.
- `Controllers/SolarParksController.cs`: agregar `GET /api/solar-parks/{id}` y `PUT /api/solar-parks/{id}`.
- `SolarUY.Web/Services/SolarParkApiClient.cs`: `GetByIdAsync`, `UpdateAsync`.
- `SolarUY.Web/Components/Pages/SolarParkEdit.razor` (ruta `/parques/{Id:guid}/editar`); botón "Editar" en cada fila de `SolarParkList.razor`.
- `SolarUY.Tests/Services/SolarParkServiceUpdateTests.cs`

## Servicio
Las validaciones de campos son las mismas que en `Create`: se extraen a un método privado `Validate(...)` dentro de `SolarParkService` para no duplicarlas.

`Update(id, request)`, en este orden:
1. `GetById` → si es `null`, `Fail(NotFound, "El parque solicitado no existe.")` (RF-5).
2. `Validate` → `Fail(Validation, ...)` (RF-3).
3. `ExistsByNormalizedName(nombreNormalizado, excludeId: id)` → `Fail(Conflict, ...)` (RF-4). Al excluir el propio Id, cambiar solo mayúsculas del propio nombre es válido.
4. Actualiza campos, `NormalizedName` y `UpdatedAtUtc = DateTime.UtcNow`; llama a `Update`; log `Information` (RF-2). No toca `Status` (RF-7).

## API
- `GET /api/solar-parks/{id}` → 200 con `SolarParkDto` / 404.
- `PUT /api/solar-parks/{id}` → 204 / 400 / 404 / 409.

## UI
- `SolarParkEdit.razor`: carga con `GetByIdAsync`; si 404 muestra el mensaje de RF-5 con link a `/parques`. Mismo formulario y validaciones que el alta (se puede extraer un componente `SolarParkFormFields.razor` compartido con `SolarParkCreate.razor`). Botones "Guardar" y "Cancelar" (RF-6). Tras 204 navega a `/parques` mostrando "Parque actualizado correctamente."

## Tests
- Unitarios (fake repositorio): actualización válida cambia datos y fecha (RF-2); nombre de otro parque → Conflict (RF-4); cambiar mayúsculas del propio nombre → Success; Id inexistente → NotFound (RF-5); un caso de validación (RF-3).
- Arquitectura: siguen en verde.
- UI: manual.
