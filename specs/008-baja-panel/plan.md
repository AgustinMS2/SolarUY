# Plan Técnico: Spec 008 — Baja de Panel Solar

## Proyectos y archivos afectados
- `SolarUY.ApiService`:
  - `Services/ISolarPanelService.cs`: agregar método `DeletePanel(Guid id)`.
  - `Services/SolarPanelService.cs`: implementar `DeletePanel` con las validaciones RF-1 a RF-6.
- `SolarUY.Web`:
  - `Services/PanelsApiClient.cs`: agregar método `DeletePanelAsync(Guid id)`.
  - `Components/Pages/PanelDelete.razor` (ruta `/paneles/baja/{id}`): pantalla de confirmación de baja.
- `SolarUY.Tests`:
  - `Services/PanelServiceDeleteTests.cs`: tests unitarios del servicio Delete.

## Requisitos del servicio

`DeletePanel` hace, en este orden:
1. Validar que el panel existe → si no, `Fail(NotFound, "El panel solicitado no existe.")` (RF-6).
2. Validar que el panel no está já inactivo → si ya está inactivo, `Fail(Validation, "El panel ya está inactivo.")` (RF-5).
3. Si el panel está activo, cambiar su estado a `Inactive` y registrar `UpdatedAtUtc = DateTime.UtcNow` (baja lógica) (RF-2).
4. Mostrar "Panel dado de baja correctamente." y retornar OK.
5. Si el usuario cancelara (a través de la UI), no hacer cambios (RF-3).

## API

`PanelsController : ControllerBase`, `[ApiController]`, `[Route("api/panels")]`.

- `DELETE /api/panels/{id}`
  - 200 + mensaje "Panel dado de baja correctamente."
  - 404 si no existe.
  - 400 con mensaje "El panel ya está inactivo." si ya estaba inactivo.

## UI (Blazor Server)

- `PanelDelete.razor`: página en `/paneles/baja/{id}` que:
  - Muestra un diálogo de confirmación: "¿Confirmás la baja del panel "<número de serie>"?" con botones "Confirmar" y "Cancelar".
  - Al presionar "Confirmar", llama a `PanelsApiClient.DeletePanelAsync(id)`.
  - Al presionar "Cancelar", cierra el diálogo sin hacer nada (RF-3).
  - Muestra el mensaje "Panel dado de baja correctamente." y recarga el listado.
  - El botón "Dar de baja" no aparece si el panel ya está inactivo (RF-4).

## Estrategia de tests

- **Unitarios** de `SolarPanelService.DeletePanel` usando `FakeSolarPanelRepository`:
  - Panel activo → cambia a inactivo, muestra mensaje correcto (RF-2).
  - Panel ya inactivo → no cambia, muestra "El panel ya está inactivo." (RF-5).
  - Panel inexistente → NotFound (RF-6).
  - Cancelar en la UI → no hacer cambios.