# Tareas: Spec 009 — Alta de Nodo Solar

- [ ] **T1. Proyecto `SolarUY.Tests` + tests de arquitectura.**
  - Hecho cuando: el proyecto xUnit está en `SolarUY.slnx`, referencia a `SolarUY.ApiService`, tiene los tests de arquitectura del plan y `dotnet test` pasa en verde.

- [ ] **T2. Entidad `SolarNode` + `SolarUyDbContext` + migración.**
  - RF-1, RF-4
  - Hecho cuando: la migración crea la tabla `SolarNodes` con índice único en `NormalizedIdentifier` y la solución compila.

- [ ] **T3. Repositorio `ISolarNodeRepository` / `SolarNodeRepository` (`ExistsByIdentifier`, `Add`).**
  - RF-1, RF-4
  - Hecho cuando: compila, está registrado y los tests existentes siguen en verde.

- [ ] **T4. `ServiceResult` + `SolarNodeService.Create` con tests unitarios (tests primero).**
  - RF-1, RF-2, RF-4
  - Hecho cuando: los tests unitarios del plan se escribieron antes, fallaron, y ahora pasan en verde junto con los de arquitectura.

- [ ] **T5. `SolarNodesController` con `POST /api/solar-nodes`.**
  - RF-1, RF-2, RF-4
  - Hecho cuando: el POST responde 201 / 400 / 409 según el caso (probado desde el archivo `.http` o el OpenAPI) y `dotnet test` sigue en verde.

- [ ] **T6. `SolarNodesApiClient` + página `/nodos/nuevo`.**
  - RF-1, RF-2, RF-4
  - Hecho cuando: en el navegador se puede dar de alta un nodo, se ven los mensajes de error de RF-2 y RF-4, el formulario se limpia tras el alta y la pantalla se ve bien en celular.

- [ ] **T7. Validación del catálogo de tipos (opcional, regla de negocio).**
  - Hecho cuando: un test verifica que solo se admiten los tipos del catálogo definido, en verde.