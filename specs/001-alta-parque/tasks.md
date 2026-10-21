# Tareas: Spec 001 — Alta de Parque Solar

- [ ] **T1. Proyecto `SolarUY.Tests` + tests de arquitectura.**
  - Hecho cuando: el proyecto xUnit está en `SolarUY.slnx`, referencia a `SolarUY.ApiService`, tiene los 3 tests de arquitectura del plan y `dotnet test` pasa en verde.

- [ ] **T2. PostgreSQL en Aspire + entidad `SolarPark` + `SolarUyDbContext` + migración.** RF-1, RF-3
  - Hecho cuando: `AppHost` levanta PostgreSQL, la migración `InitialSolarPark` crea la tabla `SolarParks` con índice único en `NormalizedName` y la solución compila.

- [ ] **T3. Repositorio `ISolarParkRepository` / `SolarParkRepository` (`ExistsByNormalizedName`, `Add`).** RF-1, RF-3
  - Hecho cuando: compila, está registrado en `Program.cs` y los tests existentes siguen en verde.

- [ ] **T4. `ServiceResult` + `SolarParkService.Create` con tests unitarios (tests primero).** RF-1, RF-2, RF-3
  - Hecho cuando: los tests unitarios del plan se escribieron antes, fallaron, y ahora pasan en verde junto con los de arquitectura.

- [ ] **T5. `SolarParksController` con `POST /api/solar-parks`.** RF-1, RF-2, RF-3
  - Hecho cuando: el POST responde 201 / 400 / 409 según el caso (probado desde el archivo `.http` o el OpenAPI) y `dotnet test` sigue en verde.

- [ ] **T6. `SolarParkApiClient` + página `/parques/nuevo`.** RF-1, RF-2, RF-3, RF-4
  - Hecho cuando: en el navegador se puede dar de alta un parque, se ven los mensajes de error de RF-2 y RF-3, el formulario se limpia tras el alta y la pantalla se ve bien en celular.
