# Tareas de Implementación: Spec 001 — Gestión de Parques Solares

- [ ] **T1. Entidad `SolarPark`, enum `SolarParkStatus` y configuración EF Core.** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: `dotnet build` compila; entidad con propiedades, factory methods, validaciones privadas; `DbContext` con `citext` en Name, check constraints, `RowVersion`, índice único case-insensitive.

- [ ] **T2. Migración EF Core `AddSolarParks` y aplicación a BD.** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: `dotnet ef migrations add AddSolarParks --project SolarUY.ApiService` genera migración correcta; `dotnet ef database update` aplica sin errores; tabla `SolarParks` visible en PostgreSQL con constraints.

- [ ] **T3. Repositorio `ISolarParkRepository` y `SolarParkRepository` (implementación EF Core).** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: interfaz y clase implementada; métodos `GetByIdAsync`, `GetByNameAsync` (case-insensitive), `GetAllAsync` (ordenado por Name), `AddAsync`, `UpdateAsync` (lanza `DbUpdateConcurrencyException`), `HasActiveInjectingNodesAsync`; compila y tests unitarios de repo (mock) pasan.

- [ ] **T4. Servicio de dominio `ISolarParkService` / `SolarParkService` + tests unitarios (TDD).** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: tests unitarios escritos **primero** (rojo), luego servicio implementado (verde); cubre: creación con validaciones, duplicado, listado ordenado, actualización con concurrencia, baja con verificación de nodos inyectando, baja exitosa; `DateTime` inyectado via `TimeProvider`; `dotnet test` en proyecto Unit pasa.

- [ ] **T5. Endpoints API (Minimal API) + autorización + tests de integración.** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: `MapSolarParksEndpoints` con 5 rutas; policies `RequireAdministratorRole` (POST/PUT/DELETE) y `RequireAnyRole` (GET); DTOs con `RowVersion`; tests de integración con `WebApplicationFactory` + Testcontainers PostgreSQL: POST 201/409, GET 200 ordenado, PUT 204/409, DELETE 204/400; `dotnet test` en proyecto Integration pasa.

- [ ] **T6. Servicio cliente Blazor `SolarParkClientService` (HttpClient tipado).** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6
  - Hecho cuando: clase registrada en DI; métodos `CreateAsync`, `GetAllAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`; manejo de errores 400/409/404 → excepciones tipadas o `Result`; compila.

- [ ] **T7. Página listado `Pages/SolarParks.razor` + componente `SolarParkCard.razor` (opcional).** RF-3
  - Hecho cuando: tabla responsiva renderiza Nombre, Estado (badge), Nodos, Potencia, Acciones; botón "Nuevo parque" navega a formulario; fila clickeable navega a edición; spinner carga; toast errores; verificado en navegador (escritorio + móvil) sin errores consola.

- [ ] **T8. Formulario `Pages/SolarParkForm.razor` (crear/editar) + validaciones.** RF-1, RF-2, RF-4, RF-5, RF-6
  - Hecho cuando: `EditForm` con `DataAnnotationsValidator`; campos: Nombre (required, trim, max 200, validación remota único), Latitud/Longitud (rangos, 6 decimales), Zona horaria (select IANA), Capacidad máxima (min 0.1), Umbral alerta (min 0, warning si > capacidad); botones Guardar/Cancelar/Eliminar (solo edición, modal confirmación); campo oculto `RowVersion` en edición; toast éxito/error; verificado en navegador (escritorio + móvil).

- [ ] **T9. Tests de componente Blazor (bUnit) para listado y formulario.** RF-3, RF-1, RF-2, RF-4
  - Hecho cuando: tests bUnit cubren renderizado tabla ordenada, badges estado, navegación a formulario; formulario: validaciones cliente, envío válido, error duplicado, concurrencia (RowVersion), modal baja; `dotnet test` en proyecto Components pasa.

- [ ] **T10. Verificación visual final y ajustes UI (responsive, accesibilidad, SignalR ready).** RF-3, RF-4, RF-6
  - Hecho cuando: aplicación corriendo (`dotnet run --project SolarUY.AppHost`); flujo completo: crear → listar → editar → baja (con y sin nodos inyectando) probado en Chrome DevTools; responsive (mobile/desktop), sin errores consola, badges estados correctos, feedback usuario claro; listo para merge.