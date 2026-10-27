# Plan Técnico: Spec 007 — Modificar Panel Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarPanelService.cs`: agregar método `UpdatePanel(Guid id, UpdateSolarPanelRequest request)`.
  - `Services/SolarPanelService.cs`: implementar `UpdatePanel` con las validaciones RF-2, RF-4, RF-5, RF-7.
  - `Dtos/UpdateSolarPanelRequest.cs`: nuevo DTO para el request de actualización.
  - `Dtos/SolarPanelDto.cs`: ya existe (ver spec 006), se reutiliza.
- `SolarUY.Web`:
  - `Services/PanelsApiClient.cs`: agregar método `UpdatePanelAsync(Guid id, UpdateSolarPanelRequest request)`.
  - `Components/Pages/PanelEdit.razor` (ruta `/paneles/editar/{id}`): formulario de edición con datos cargados.
- `SolarUY.Tests`:
  - `Services/PanelServiceUpdateTests.cs`: tests unitarios del servicio Update.

## Requisitos del servicio

`UpdatePanel` hace, en este orden:
1. Validar que el panel existe (GetById) → si no, `Fail(NotFound, "El panel solicitado no existe.")` (RF-5).
2. `Trim` de la serie número y validaciones de RF-2 (mismas reglas que spec 005) → `Fail(Validation, <mensaje>)` con el primer error encontrado.
3. `ExistsBySerieNumber` (excluyendo el panel actual) → si existe otro panel con misma serie, `Fail(Conflict, "Ya existe un panel con el número de serie "<serie>".`" (RF-4).
4. Actualiza la entidad, registra `UpdatedAtUtc = DateTime.UtcNow`, muestra "Panel actualizado correctamente." y retorna OK (RF-2).
5. Si el usuario presiona "Cancelar", retornar sin cambios (RF-6).

## API

`PanelsController : ControllerBase`, `[ApiController]`, `[Route("api/panels")]`.

- `PUT /api/panels/{id}` con `UpdateSolarPanelRequest { SerieNumber, PowerW, Efficiency }`
  - 200 + mensaje "Panel actualizado correctamente."
  - 400 con mensaje (validación) / 404 si no existe / 409 con mensaje (serie duplicada)

## UI (Blazor Server)

- `PanelEdit.razor`: página en `/paneles/editar/{id}` que:
  - En el OnInitialized, llama a `PanelsApiClient.GetPanelByIdAsync(id)` para cargar los datos.
  - `EditForm` con los campos SerieNumber, PowerW, Efficiency y `DataAnnotationsValidator`.
  - Muestra el mensaje de error que devuelve la API (400/404/409) o "Panel actualizado correctamente."
  - Botón "Cancelar" que vuelve al listado sin guardar cambios (RF-6).
  - El campo Estado es de solo lectura (no se puede cambiar desde aquí).

## Estrategia de tests

- **Unitarios** de `SolarPanelService.UpdatePanel` usando `FakeSolarPanelRepository`:
  - Datos válidos → Success, fecha de modificación actualizada (RF-2).
  - Un test por cada mensaje de RF-2.
  - Serie duplicada con otras mayúsculas → Conflict (RF-4).
  - Panel inexistente → NotFound (RF-5).
  - Cancelar sin cambios → sin modificar la entidad.