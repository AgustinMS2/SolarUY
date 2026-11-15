# Plan Técnico: Spec 032 — Activar y Desactivar Panel

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Domain/`: nuevo valor `SolarPanelStatus.Deactivated`, nuevo enum `PanelControlAction` y nueva entidad `PanelControlEvent` (bitácora).
  - `Data/`: `SolarUyDbContext` (DbSet + configuración) y nueva migración `AddPanelControlEvents`.
  - `Repositories/`: `IPanelControlEventRepository` + `PanelControlEventRepository`; extensión de `IMaintenanceRepository` con `HasPendingOrderForPanel`.
  - `Services/`: `IPanelControlService` + `PanelControlService` (reglas de negocio), `IInverterCommandSender` + stub `InMemoryInverterCommandSender`.
  - `Controllers/`: `PanelsController` — dos endpoints nuevos.
  - `Migrations/`: migración generada con `dotnet ef migrations add AddPanelControlEvents --project SolarUY.ApiService`.
- `SolarUY.Web`:
  - `Services/`: `PanelsApiClient` — métodos `DeactivateAsync` / `ActivateAsync` con mapeo `ServiceResult` (patrón `MaintenanceApiClient`).
  - `Components/Pages/`: nueva página de detalle `PanelDetail.razor` (`/paneles/{id:guid}`) con modal `PanelControlModal.razor`; ajuste de badge/estados en `PanelList.razor`; aviso "Desactivado manualmente" en `PanelMonitoring.razor` (la spec 029 lo reservó para la 032).
- `SolarUY.AppHost`: sin cambios (no hay recursos nuevos).
- `SolarUY.Tests`: tests unitarios de las reglas del servicio (TDD) + tests de arquitectura existentes en verde.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

1. **`SolarPanelStatus`** (enum existente, almacenado como `int`): se agrega `Deactivated = 3`. No requiere cambio de esquema (sin check constraints). Semántica:
   - `Active` = "Operativo" (RF-4), `Inactive` = baja lógica (spec 008, intacto), `Deactivated` = desactivación eléctrica remota (RF-2).
2. **Nueva entidad `PanelControlEvent`** (bitácora de control, RF-2/NFR "quién, cuándo, panel, orden"):
   - `Guid Id` (PK), `Guid SolarPanelId` (FK → `SolarPanels`, `DeleteBehavior.Restrict`), navegación `SolarPanel`.
   - `PanelControlAction Action` — enum nuevo `{ Deactivate = 1, Activate = 2 }`.
   - `string OrderedBy` (usuario autenticado, máx. 256), `DateTime OrderedAtUtc` (UTC, constitución).
   - `bool OrderSentToInverter` (false si el panel no tiene nodo/inversor) y `string? InverterSerieNumber` (máx. 100, nullable).
3. **Configuración en `OnModelCreating`**: tabla `PanelControlEvents`, FK Restrict, índice en `SolarPanelId` (consulta por panel a futuro).
4. **Migración**: `AddPanelControlEvents` (solo crea la tabla; el nuevo valor del enum no altera el esquema).

## Servicios y Lógica de Dominio Pura

### `IInverterCommandSender` + `InMemoryInverterCommandSender` (stub provisorio)
- Interfaz en `Services/` con métodos sincrónicos: `bool SendDeactivate(string panelSerieNumber, string? inverterEndpoint)` y `bool SendActivate(...)`.
- El stub (registrado en DI) acepta la orden y la registra con `ILogger`. **Se reemplaza por el cliente HTTP real cuando exista el simulador de la spec 036.** Los tests usan un fake propio (exitoso y uno que falla) para las reglas de negocio.

### `IPanelControlService` + `PanelControlService` (reglas, sincrónico según constitución)
- `ServiceResult Deactivate(Guid panelId, string orderedBy)`:
  - Panel inexistente → `NotFound`. Estado `Deactivated` o `Inactive` → `Conflict` (defensivo; la UI nunca ofrece el botón en esos estados).
  - Si el panel tiene nodo con inversor: envía la orden vía `IInverterCommandSender`; si falla → `Unavailable` con "Error de comunicación con el inversor. La orden no pudo confirmarse." y **no** cambia estado ni registra bitácora. Si el panel no tiene nodo o el nodo no tiene inversor: continúa sin enviar orden (`OrderSentToInverter = false`) — decisión de clarificación.
  - Éxito: `Status = Deactivated`, `Update` del panel, alta en bitácora (`Action = Deactivate`) y `ILogger.LogInformation`.
- `ServiceResult Activate(Guid panelId, string orderedBy)`:
  - Panel inexistente → `NotFound`. Estado distinto de `Deactivated` → `Conflict` (defensivo).
  - **RF-5**: si `IMaintenanceRepository.HasPendingOrderForPanel(panelId)` (cualquier orden `Pending`, no completada ni cancelada — decisión de clarificación) → `Conflict` "No se puede reactivar el panel mientras haya personal trabajando en un mantenimiento activo."
  - Mismo envío/fallo/bitácora que `Deactivate`; éxito: `Status = Active` + evento `Activate`.
- Nota: las reservas de mantenimiento (spec 021) exigen panel `Active`, así que un panel `Deactivated` automáticamente no puede agendarse; no se modifica ese servicio.

## API y Comunicación

`PanelsController` (`[Authorize(Roles = Roles.Any)]` a nivel clase, como hoy):
- `POST /api/panels/{id:guid}/deactivate` → `Deactivate`, **`[Authorize(Roles = Roles.Administrator)]`** a nivel método (exigido por `SecurityArchitectureTests` para todo endpoint mutante).
- `POST /api/panels/{id:guid}/activate` → `Activate`, ídem.
- `orderedBy` se toma del usuario autenticado (`User` claims de Keycloak).
- Códigos: `200` con el nuevo estado del panel, `400` (Validation), `404`, `409` (Conflict, incluye RF-5) y **`503` para `Unavailable`** (pérdida de comunicación con el inversor, convención de `MEMORY.md`). RF-6: Técnico → 403 por el `[Authorize]` de rol.

## Interfaz de Usuario (Blazor Server)

1. **Nueva página de detalle `PanelDetail.razor`** en `/paneles/{Id:guid}` (RF-1/RF-3 hablan de la "vista de detalle", que hoy no existe): ficha del panel (número de serie en fuente mono, parque, nodo, potencia, eficiencia, badge de estado), enlace "Ver detalle" desde cada fila de `PanelList.razor` y breadcrumb de vuelta al listado.
2. **Botones por rol y estado** (RF-6): dentro de `<AuthorizeView Roles="@AppRoles.Administrator">`; "Desactivar panel" solo si estado Activo, "Activar panel" solo si Desactivado. El Técnico no los ve.
3. **Modal `PanelControlModal.razor`** (patrón `MaintenanceCancelModal.razor`, sin JS interop): textos exactos de RF-1 y RF-3 con advertencia de seguridad en la desactivación; `spinner-border spinner-border-sm` en el botón mientras la orden viaja (NFR); éxito → mensajes exactos de RF-2/RF-4; error → mensaje del `ServiceResult` (409 RF-5 / 503 comunicación). Botón deshabilitado durante el guardado (`_isSaving`).
4. **`PanelList.razor`**: mapeo del nuevo badge "Desactivado" (ámbar, estilo SolarOps Clean) junto a "Activo"/"Inactivo".
5. **`PanelMonitoring.razor`** (spec 029): mostrar "Desactivado manualmente" cuando el panel esté `Deactivated`; si el simulador de lecturas no filtra por estado, un panel desactivado debe reportar 0 W.
6. Sin menú ni navegación nueva en `NavMenu`.

## Decisiones específicas de esta spec

- **Stub de orden al inversor**: no existe API real ni simulador (spec 036 pendiente); se define `IInverterCommandSender` con stub en memoria para que el flujo completo sea verificable en navegador. El caso de pérdida de comunicación se ejercita con un fake que falla en los tests unitarios.
- **Métodos sincrónicos** en servicio y sender según la constitución; la implementación HTTP de la 036 podrá requerir `async` y se discutirá en esa spec.
- **"En Proceso" se interpreta como cualquier orden `Pending`** del panel (conservador), según clarificación del usuario.
- **Panel sin nodo/inversor**: la operación se permite sin enviar orden y se registra en la bitácora, según clarificación del usuario.

## Estrategia de Tests

- **Tests unitarios (TDD, primero en rojo)**: `PanelControlServiceTests` en `SolarUY.Tests/Services/` con fakes (`FakeSolarPanelRepository`, fake de `IMaintenanceRepository` y fakes exitoso/fallante de `IInverterCommandSender`):
  1. `Deactivate_PanelActivo_CambiaEstadoYRegistraBitacora` (orden enviada).
  2. `Deactivate_PanelInexistente_DevuelveNotFound`.
  3. `Deactivate_PanelYaDesactivado_DevuelveConflict`.
  4. `Deactivate_PanelDadoDeBaja_DevuelveConflict`.
  5. `Deactivate_SinInversor_CambiaEstadoSinOrden` (`OrderSentToInverter = false`).
  6. `Deactivate_FallaComunicacion_DevuelveUnavailableYNoCambiaEstado` (sin evento en bitácora).
  7. `Activate_PanelDesactivado_CambiaEstadoYRegistraBitacora`.
  8. `Activate_PanelInexistente_DevuelveNotFound`.
  9. `Activate_PanelActivo_DevuelveConflict`.
  10. `Activate_ConMantenimientoPendiente_BloqueaConMensajeRF5`.
  11. `Activate_FallaComunicacion_DevuelveUnavailable`.
- **Tests de arquitectura**: sin cambios; el nuevo endpoint mutante lleva `[Authorize(Roles = Roles.Administrator)]` y la allow-list de técnicos no se toca.
- **UI Blazor**: verificación manual en navegador (sin bUnit): flujo completo como Administrador, ocultamiento de botones como Técnico, desktop y móvil, consola sin errores.
