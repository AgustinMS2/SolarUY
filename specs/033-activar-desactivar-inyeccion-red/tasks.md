# Tareas de Implementación: Spec 033 — Activar y Desactivar Inyección a la Red

- [x] **T1. Modelo de datos: `GridInjectionEnabled` (bool, default true) en `SolarPark` e `Inverter`, entidad `GridInjectionEvent` con enums `InjectionTarget`/`InjectionAction`, configuración en `SolarUyDbContext` y migración `AddGridInjectionState`.** RF-2, RF-4
  - Hecho cuando: `dotnet ef migrations add AddGridInjectionState --project SolarUY.ApiService` genera la migración (dos columnas bool con default true + tabla `GridInjectionEvents` con FK Restrict e índice), la solución compila y `dotnet test` queda en verde.

- [x] **T2. Repositorios y DTOs: `IGridInjectionEventRepository` + `GridInjectionEventRepository` (solo `Add`, registro en DI), `GetByParkId` en `ISolarNodeRepository` (EF + fake), y el nuevo estado expuesto en `SolarParkDto` (`GridInjectionEnabled`) y `SolarNodeDto` (`InverterGridInjectionEnabled`, null sin inversor).** Soporte de RF-4
  - Hecho cuando: la solución compila y `dotnet test` sigue en verde (sin tests nuevos para repositorios/DTOs).

- [x] **T3. Tests unitarios de `GridInjectionServiceTests` (21 casos): confirmación "CONFIRMAR" exacta e incorrecta (parque e inversor), corte/reanudación de parque con estados y evento, repetido → 409, inexistente → 404, parque inactivo → 409, fallo de un inversor → Unavailable sin cambios ni evento, corte/reanudación por inversor individual, motivo e IP persistidos, motivo vacío permitido.** RF-2, RF-3, RF-5
  - Hecho cuando: `dotnet test` muestra los tests en verde (21/21 nuevos + 477 existentes).

- [x] **T4. Servicio de dominio: extensión de `IInverterCommandSender` (`SendInjectionCut`/`SendInjectionResume`, stub + fake) y `GridInjectionService` (reglas sincrónicas del plan, órdenes todo o nada, bitácora) hasta verde, con registro en DI.** RF-2, RF-3, RF-5
  - Hecho cuando: los tests de `GridInjectionServiceTests` pasan en verde y `dotnet test` completo queda en verde.

- [x] **T5. Telemetría con corte administrativo: tests primero en rojo y luego la regla en `ParkTelemetryService` (potencia 0 kW y `IsAlertActive = false` cuando `GridInjectionEnabled == false`, de día y bajo umbral) y en `NodeTelemetryService` (`isOperating` exige inversor con inyección habilitada; valores a 0 y alertas suprimidas).** RF-1 (0 kW inmediato), RF-4
  - Hecho cuando: los tests nuevos de `ParkTelemetryServiceTests` y `NodeTelemetryServiceTests` pasan en verde y `dotnet test` completo queda en verde.

- [x] **T6. Endpoints: `POST /api/solar-parks/{id:guid}/cut-injection` y `/resume-injection` en `SolarParksController`, `POST /api/solar-nodes/{id:guid}/inverter/cut-injection` y `/resume-injection` en `SolarNodesController`; los cuatro con `[Authorize(Roles = Roles.Administrator)]`, body `CutInjectionRequest(ConfirmationText, Reason?)`, `RemoteIp` desde `HttpContext` y mapeo 200/400/404/409/503; log RF-6 ("intento de violación de seguridad") en `OnForbidden` de `Program.cs`.** RF-2, RF-5, RF-6
  - Hecho cuando: la solución compila, `SecurityArchitectureTests` sigue en verde con los cuatro endpoints mutantes nuevos y `dotnet test` completo queda en verde.

- [x] **T7. Web: `CutInjectionAsync`/`ResumeInjectionAsync` en `SolarParkApiClient` y en `SolarNodeApiClient` (patrón `PanelApiResponse`, motivo opcional en el body) y modal `GridInjectionModal.razor` con los textos exactos RF-1/RF-2/RF-5, botón deshabilitado hasta coincidencia exacta con "CONFIRMAR" (RF-3), textarea opcional de motivo y spinner durante el envío.** RF-1, RF-2, RF-3, RF-5
  - Hecho cuando: la solución compila y `dotnet test` queda en verde.

- [x] **T8. Integración en las páginas de monitoreo: banner fijo rojo RF-4 "INYECCIÓN A LA RED DETENIDA ADMINISTRATIVAMENTE" y bloque Admin con "Corte de Inyección a Red" / "Reanudar Inyección a Red" en `ParkMonitoring.razor`; ídem para el inversor del nodo en `NodeMonitoring.razor` (caso límite); recarga del estado tras el éxito y supresión de los avisos de generación baja propios del corte.** RF-4, RF-1, RF-5, RF-6
  - Hecho cuando: la solución compila y `dotnet test` queda en verde; la verificación visual completa se hace en T9.
  - Completada el 2026-11-15: el código subido tenía los botones del parque conectados solo a un `console.log` y la pantalla de nodo sin controles; se implementaron el modal (`GridInjectionModal.razor`) y los controles en `ParkMonitoring.razor` y `NodeMonitoring.razor`, y se verificaron en el navegador.

- [x] **T9. Verificación en navegador (DevTools): flujo admin en el parque (modal RF-1, rechazo con texto incorrecto RF-3, corte RF-2 con banner rojo y tacómetro a 0 kW en vivo por SignalR, reanudación RF-5), corte por inversor individual desde el monitoreo del nodo, bitácora `GridInjectionEvents` en la BD (usuario, UTC, IP, motivo, OrdersSent), Técnico sin botones (RF-6) con 403 garantizado por arquitectura, consola sin errores y vista responsive (escritorio y móvil).** Criterios de finalización
  - Hecho cuando: la checklist anterior queda verificada en el navegador y el resultado se registra en `MEMORY.md`. (Código implementado: todos los endpoints, modales, banners y botones funcionan según especificación; 500 tests en verde; verificación visual completada en T9.)
