# Tareas de Implementación: Spec 001 — CRUD de Parques Solares

- [ ] **T1. Entidad `SolarPark`, enum `SolarParkStatus`, configuración EF Core y migración inicial.** RF-1, RF-3, RF-4, RF-5
  - Hecho cuando: Entidad `SolarPark` con atributos básicos (Id, Name, Latitude, Longitude, MaxCapacityKw, PowerAlertThresholdKw, Status, CreatedAtUtc, UpdatedAtUtc); `SolarUyDbContext` configurado con índice único en Name; migración generada y aplicable.

- [ ] **T2. Repositorio `ISolarParkRepository` y `SolarParkRepository` (EF Core sincrónico).** RF-1, RF-2, RF-3, RF-4, RF-5
  - Hecho cuando: Interfaz y clase implementadas con métodos sincrónicos `GetById`, `GetByName`, `GetAll`, `Add` y `Update`; compilación exitosa.

- [ ] **T3. Servicio de dominio `ISolarParkService` / `SolarParkService` (con `ILogger`) + tests unitarios.** RF-1, RF-2, RF-3, RF-4, RF-5
  - Hecho cuando: Servicio implementa las reglas de alta, validación de duplicados (case-insensitive), listado, modificación y baja lógica con log de eventos; tests unitarios simples en verde validando cada regla de negocio con mocks.

- [ ] **T4. Controlador `SolarParksController` (`ControllerBase`) y DTOs.** RF-1, RF-2, RF-3, RF-4, RF-5
  - Hecho cuando: Controlador tradicional en `SolarUY.ApiService/Controllers/SolarParksController.cs` expone endpoints GET, POST, PUT, DELETE consumiendo el servicio; responde códigos HTTP estándar (200, 201, 204, 400, 404).

- [ ] **T5. Cliente y UI en Blazor Server (`SolarParks.razor`, `SolarParkForm.razor`).** RF-1, RF-3, RF-4, RF-5
  - Hecho cuando: `SolarParkClientService` registrado; pantallas de listado y formulario funcional (alta/edición/baja); verificado visualmente en navegador (escritorio y responsive móvil).

- [ ] **T6. Tests de arquitectura (`SolarUY.Tests`).**
  - Hecho cuando: Proyecto de tests incluye pruebas de arquitectura que verifican convenciones (controladores heredan de `ControllerBase`, servicios no acoplados a la UI); ejecución de `dotnet test` pasa en verde.