# Tareas: Spec 005 — Alta de Panel Solar

- [ ] **T1. Proyecto `SolarUY.Tests` + tests de arquitectura.**
  - Hecho cuando: el proyecto xUnit está en `SolarUY.slnx`, referencia a `SolarUY.ApiService`, tiene los tests de arquitectura del plan y `dotnet test` pasa en verde.

- [ ] **T2. Entidad `SolarPanel` + `SolarUyDbContext` + migración.**
  - RF-1, RF-3
  - Hecho cuando: la migración crea la tabla `SolarPanels` con índice único en `NormalizedSerieNumber` y la solución compila.

- [ ] **T3. Repositorio `ISolarPanelRepository` / `SolarParkRepository` (`ExistsBySerieNumber`, `Add`).**
  - RF-1, RF-3
  - Hecho cuando: compila, está registrado y los tests existentes siguen en verde.

- [ ] **T4. `ServiceResult` + `SolarPanelService.Create` con tests unitarios (tests primero).**
  - RF-1, RF-2, RF-3
  - Hecho cuando: los tests unitarios del plan se escribieron antes, fallaron, y ahora pasan en verde junto con los de arquitectura.

- [ ] **T5. `PanelsController` con `POST /api/panels`.**
  - RF-1, RF-2, RF-3
  - Hecho cuando: el POST responde 201 / 400 / 409 según el caso (probado desde el archivo `.http` o el OpenAPI) y `dotnet test` sigue en verde.

- [ ] **T6. `PanelsApiClient` + página `/paneles/nuevo`.**
  - RF-1, RF-2, RF-3, RF-4
  - Hecho cuando: en el navegador se puede dar de alta un panel, se ven los mensajes de error de RF-2 y RF-3, el formulario se limpia tras el alta y la pantalla se ve bien en celular.