# Tareas: Spec 011 — Modificar Nodo Solar

- [ ] **T1. Servicio `SolarNodeService.UpdateNode` + DTOs.**
  - Hecho cuando: compila y el servicio tiene el método `UpdateNode(Guid id, request)` con validaciones RF-2, RF-4, RF-5, RF-7.

- [ ] **T2. `SolarNodesController` con `PUT /api/solar-nodes/{id}`.**
  - Hecho cuando: el PUT responde 200 / 404 / 409 según el caso y `dotnet test` sigue en verde.

- [ ] **T3. `SolarNodesApiClient` con método `UpdateNodeAsync`.**
  - Hecho cuando: compila y puede actualizar un nodo desde el cliente Blazor.

- [ ] **T4. Página `/nodos/editar/{id}` (SolarNodeEdit.razor).**
  - Hecho cuando: en el navegador carga los datos del nodo, permite editarlos, muestra los mensajes de error y el botón "Cancelar" vuelve al listado sin cambios. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `UpdateNode` (opcional, reglas de negocio).**
  - Hecho cuando: tests por datos válidos, identificador duplicado, nodo inexistente y caso "propio nombre", todos en verde.