# Tareas: Spec 034 — Consultar Generación Histórica

- [x] **T1. Entidad `ParkHourlyGeneration`, configuración en `SolarUyDbContext` y migración EF Core `AddParkHourlyGeneration`.**
  - Hecho cuando: la entidad esté mapeada con índice único compuesto `(SolarParkId, DateLocal, HourLocal)`, la migración EF Core esté generada y la solución compile.

- [x] **T2. Repositorio `IParkGenerationRepository`, DTOs y servicio `ParkHistoricalGenerationService` con tests unitarios (tests primero).** RF-2, RF-3, RF-4, RF-5, RF-8
  - Hecho cuando: las suites de tests unitarios para agregación de Día (24 h), Mes (días en kWh), Año (12 meses en MWh), cálculo de HSP, pico de potencia y comparativas pasen en verde con `dotnet test`.

- [x] **T3. Precarga de datos históricos y cableado en DI.**
  - Hecho cuando: la tabla se precargue con datos históricos deterministas de los parques existentes al iniciar el sistema.
  - Actualizada el 2026-11-15 (spec 036, opción A): el cargador propio (`HistoricalGenerationSeeder`) se reemplazó por la historia horaria calculada con el motor único de simulación, que se completa en segundo plano al arrancar (`SimulationHistoryService`); el repositorio incorporó `AddMissing` y `GetLastHourByPark`.

- [x] **T4. Endpoints REST en `SolarParksController` (`daily`, `monthly`, `yearly`) con seguridad.** RF-1, RF-9
  - Hecho cuando: los endpoints estén expuestos, protegidos con `[Authorize(Roles = Roles.Any)]` y los tests de arquitectura sigan en verde.

- [x] **T5. Cliente `ParkHistoricalApiClient`, navegación en `NavMenu.razor` y vista `/historicos` con selectores y métricas.** RF-1, RF-2, RF-3, RF-4, RF-5, RF-8
  - Hecho cuando: la pantalla `/historicos` (y `/parques/{id}/historico`) permita cambiar de parque y granularidad temporal, mostrando las tarjetas de métricas consolidadas.

- [x] **T6. Gráfica interactiva SVG con tooltips en hover y descarga de reporte en CSV.** RF-6, RF-7
  - Hecho cuando: la gráfica SVG dibuje las barras con tooltips informativos al pasar el cursor y el botón "Exportar a CSV" descargue el archivo tabulado con codificación UTF-8 con BOM.

