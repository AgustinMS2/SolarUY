# Tareas de Implementación: Spec 032 — Activar y Desactivar Panel

- [x] **T1. Modelo de datos: valor `SolarPanelStatus.Deactivated = 3`, enum `PanelControlAction`, entidad `PanelControlEvent` (bitácora), configuración en `SolarUyDbContext` y migración `AddPanelControlEvents`.** RF-2, RF-4
  - Hecho cuando: `dotnet ef migrations add AddPanelControlEvents --project SolarUY.ApiService` genera la migración (solo tabla `PanelControlEvents` con FK Restrict e índice por panel), la solución compila y `dotnet test` (incluidos los de arquitectura) queda en verde.

- [x] **T2. Repositorios: `IPanelControlEventRepository` + `PanelControlEventRepository` (alta de eventos, registro en DI) y `HasPendingOrderForPanel(panelId)` en `IMaintenanceRepository` con su implementación EF y fake de tests actualizado.** RF-5
  - Hecho cuando: la solución compila y `dotnet test` sigue en verde (sin tests nuevos para repositorios).

- [x] **T3. Tests unitarios en rojo de `PanelControlServiceTests`: los 11 casos del plan (éxito desactivar/reactivar con bitácora, NotFound, Conflict por estado, RF-5 con mantenimiento pendiente, Unavailable por falla de comunicación sin cambiar estado ni bitácora, panel sin inversor con orden no enviada).** RF-2, RF-4, RF-5
  - Hecho cuando: `dotnet test` muestra los 11 tests nuevos fallando (rojo) por falta de la implementación.

- [x] **T4. Servicio de dominio: `IInverterCommandSender` + stub `InMemoryInverterCommandSender` y `PanelControlService` (reglas sincrónicas del plan) hasta verde, con registro en DI.** RF-2, RF-4, RF-5
  - Hecho cuando: los 11 tests de `PanelControlServiceTests` pasan en verde y `dotnet test` completo queda en verde.

- [x] **T5. Endpoints en `PanelsController`: `POST /api/panels/{id:guid}/deactivate` y `POST /api/panels/{id:guid}/activate`, con `[Authorize(Roles = Roles.Administrator)]` a nivel método, `orderedBy` desde el usuario autenticado y mapeo 200/400/404/409/503 (503 para `Unavailable`).** RF-2, RF-4, RF-6
  - Hecho cuando: la solución compila, `SecurityArchitectureTests` sigue en verde con los dos endpoints mutantes nuevos y `dotnet test` completo queda en verde.

- [x] **T6. Cliente del Web: `DeactivateAsync` / `ActivateAsync` en `PanelsApiClient` con mapeo `ServiceResult` (patrón `MaintenanceApiClient`, incluido `Unavailable` para 503).** Soporte de RF-2 y RF-4
  - Hecho cuando: la solución compila y `dotnet test` queda en verde.
  - Nota de implementación: se reutilizó el patrón propio de `PanelsApiClient` (`PanelApiResponse` con el mensaje del servidor) en lugar de duplicar el mapeo `ServiceResult`; los mensajes exactos de la spec llegan desde la API.

- [x] **T7. Interfaz Blazor: página de detalle `PanelDetail.razor` (`/paneles/{id:guid}`) con enlace "Ver detalle" desde `PanelList.razor`, modal `PanelControlModal.razor` con los textos exactos RF-1/RF-3 y mensajes RF-2/RF-4, spinner durante el envío, botones solo para Administrador y según estado (RF-6), badge "Desactivado" en el listado y aviso "Desactivado manualmente" en `PanelMonitoring.razor` (spec 029), con lecturas en 0 W para panel desactivado.** RF-1, RF-2, RF-3, RF-4, RF-6
  - Hecho cuando: la solución compila y `dotnet test` queda en verde; la verificación visual completa se hace en T8.

- [x] **T8. Verificación en navegador (DevTools): flujo completo como Administrador (confirmación RF-1/RF-3, spinner, mensajes RF-2/RF-4, badge actualizado, evento en la tabla `PanelControlEvents` de la BD), bloqueo RF-5 con una orden Pendiente del panel, Técnico sin botones ni acceso a los endpoints (403), consola sin errores y vista responsive (escritorio y móvil).** Criterios de finalización
  - Hecho cuando: la checklist anterior queda verificada en el navegador y el resultado se registra en `MEMORY.md`.
  - Resultado (2026-11-15): verificado con Edge vía chrome-devtools-mcp como `admin` y `tecnico`. RF-1/RF-3 con textos exactos, mensajes RF-2/RF-4, badge "Desactivado" (ámbar) en ficha y listado, bloqueo RF-5 exacto en PANEL-SG-002 (mantenimiento Pendiente), PANEL-SG-005 sin nodo desactivado sin orden (`OrderSentToInverter = f` en BD), bitácora `PanelControlEvents` con 4 eventos correctos (usuario, UTC, acción, inversor), aviso "El panel se encuentra desactivado manualmente." en monitoreo (029), Técnico sin botones (RF-6, 403 garantizado por el `[Authorize]` + test de arquitectura), consola sin errores y detalle responsive en 375 px.
