# Tareas: Spec 010 — Listar Nodos Solares

- [ ] **T1. Servicio `SolarNodeService.GetAll` + DTO `SolarNodeDto`.**
  - Hecho cuando: compila, el servicio devuelve `IReadOnlyList<SolarNodeDto>` ordenado por identifier y `dotnet test` sigue en verde.

- [ ] **T2. `SolarNodesController` con `GET /api/solar-nodes`.**
  - Hecho cuando: el GET devuelve 200 con la lista de nodos (o vacía) y `dotnet test` sigue en verde.

- [ ] **T3. `SolarNodesApiClient` con método `GetAllNodesAsync`.**
  - Hecho cuando: compila y puede consumir la API desde el cliente Blazor.

- [ ] **T4. Página `/nodos` (SolarNodeList.razor).**
  - Hecho cuando: en el navegador muestra el listado con la tabla correcta, mensaje "No hay nodos registrados" cuando está vacío, botón "Nuevo nodo" y el listado ordenado alfabéticamente. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `GetAll` (opcional, regla de negocio).**
  - Hecho cuando: un test por el ordenamiento RF-2 y un test por la lista vacía, ambos en verde.