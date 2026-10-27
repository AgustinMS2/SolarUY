# Tareas: Spec 008 — Baja de Panel Solar

- [ ] **T1. Servicio `SolarPanelService.DeletePanel`.**
  - Hecho cuando: compila y el servicio tiene el método `DeletePanel(Guid id)` con validaciones RF-1 a RF-6.

- [ ] **T2. `PanelsController` con `DELETE /api/panels/{id}`.**
  - Hecho cuando: el DELETE responde 200 / 404 / 400 según el caso y `dotnet test` sigue en verde.

- [ ] **T3. `PanelsApiClient` con método `DeletePanelAsync`.**
  - Hecho cuando: compila y puede dar de baja un panel desde el cliente Blazor.

- [ ] **T4. Página `/paneles/baja/{id}` (PanelDelete.razor).**
  - Hecho cuando: en el navegador muestra el diálogo de confirmación, al confirmar baja el panel y muestra el mensaje correcto, al cancelar no pasa nada. El botón "Dar de baja" no aparece para panels inactivos. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `DeletePanel` (opcional, reglas de negocio).**
  - Hecho cuando: tests por panel activo, panel ya inactivo y panel inexistente, todos en verde.