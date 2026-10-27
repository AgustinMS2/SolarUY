# Tareas: Spec 007 — Modificar Panel Solar

- [ ] **T1. Servicio `SolarPanelService.UpdatePanel` + DTOs.**
  - Hecho cuando: compila y el servicio tiene el método `UpdatePanel(Guid id, request)` con validaciones RF-2, RF-4, RF-5, RF-7.

- [ ] **T2. `PanelsController` con `PUT /api/panels/{id}`.**
  - Hecho cuando: el PUT responde 200 / 400 / 404 / 409 según el caso y `dotnet test` sigue en verde.

- [ ] **T3. `PanelsApiClient` con método `UpdatePanelAsync`.**
  - Hecho cuando: compila y puede actualizar un panel desde el cliente Blazor.

- [ ] **T4. Página `/paneles/editar/{id}` (PanelEdit.razor).**
  - Hecho cuando: en el navegador carga los datos del panel, permite editarlos, muestra los mensajes de error y el botón "Cancelar" vuelve al listado sin cambios. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `UpdatePanel` (opcional, reglas de negocio).**
  - Hecho cuando: tests por datos válidos, serie duplicada, panel inexistente y caso "propio nombre", todos en verde.