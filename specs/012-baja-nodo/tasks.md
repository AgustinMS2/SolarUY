# Tareas: Spec 012 — Baja de Nodo Solar

- [ ] **T1. Servicio `SolarNodeService.DeleteNode`.**
  - Hecho cuando: compila y el servicio tiene el método `DeleteNode(Guid id)` con validaciones RF-1 a RF-6.

- [ ] **T2. `SolarNodesController` con `DELETE /api/solar-nodes/{id}`.**
  - Hecho cuando: el DELETE responde 200 / 404 / 400 según el caso y `dotnet test` sigue en verde.

- [ ] **T3. `SolarNodesApiClient` con método `DeleteNodeAsync`.**
  - Hecho cuando: compila y puede dar de baja un nodo desde el cliente Blazor.

- [ ] **T4. Página `/nodos/baja/{id}` (SolarNodeDelete.razor).**
  - Hecho cuando: en el navegador muestra el diálogo de confirmación, al confirmar baja el nodo y muestra el mensaje correcto, al cancelar no pasa nada. El botón "Dar de baja" no aparece para nodos inactivos. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `DeleteNode` (opcional, reglas de negocio).**
  - Hecho cuando: tests por nodo activo, nodo ya inactivo y nodo inexistente, todos en verde.