# Tareas: Spec 006 — Listar Paneles Solares

- [ ] **T1. Servicio `SolarPanelService.GetAll` + DTO `SolarPanelDto`.**
  - Hecho cuando: compila, el servicio devuelve `IReadOnlyList<SolarPanelDto>` ordenado por serieNumber y `dotnet test` sigue en verde.

- [ ] **T2. `PanelsController` con `GET /api/panels`.**
  - Hecho cuando: el GET devuelve 200 con la lista de panels (o vacía) y `dotnet test` sigue en verde.

- [ ] **T3. `PanelsApiClient` con método `GetAllPanelsAsync`.**
  - Hecho cuando: compila y puede consumir la API desde el cliente Blazor.

- [ ] **T4. Página `/paneles` (PanelList.razor).**
  - Hecho cuando: en el navegador muestra el listado con la tabla correcta, mensaje "No hay panels registrados" cuando está vacío, botón "Nuevo panel" y el listado ordenado alfabéticamente. Prueba en desktop y móvil.

- [ ] **T5. Tests unitarios de `GetAll` (opcional, regla de negocio).**
  - Hecho cuando: un test por el ordenamiento RF-2 y un test por la lista vacía, ambos en verde.