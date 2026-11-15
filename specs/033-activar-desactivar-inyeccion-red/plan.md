# Plan Técnico: Spec 033 — Activar y Desactivar Inyección a la Red

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Domain/`: `SolarPark.GridInjectionEnabled` y `Inverter.GridInjectionEnabled` (bool, default true); nueva entidad `GridInjectionEvent` con enums `InjectionTarget` e `InjectionAction`.
  - `Data/`: configuración en `SolarUyDbContext` + migración `AddGridInjectionState`.
  - `Repositories/`: `IGridInjectionEventRepository` + `GridInjectionEventRepository`; `GetByParkId` en `ISolarNodeRepository`.
  - `Services/`: `IGridInjectionService` + `GridInjectionService` (reglas); extensión de `IInverterCommandSender` con órdenes de inyección; mapeo de los nuevos flags en `SolarParkService`/`SolarNodeService` (DTOs).
  - `Services/` (telemetría): `ParkTelemetryService` (027) y `NodeTelemetryService` (028) respetan el corte administrativo.
  - `Controllers/`: `SolarParksController` y `SolarNodesController` — cuatro endpoints; log de RF-6 en `OnForbidden` de `Program.cs`.
  - `Migrations/`: `dotnet ef migrations add AddGridInjectionState --project SolarUY.ApiService`.
- `SolarUY.Web`:
  - `Services/`: `SolarParkApiClient` — `CutInjectionAsync` / `ResumeInjectionAsync`; `SolarNodeApiClient` (en `SolarUY.ApiService/Services`) — ídem para el inversor del nodo.
  - `Components/Pages/`: `ParkMonitoring.razor` — banner rojo fijo RF-4 + botones Admin; `NodeMonitoring.razor` — ídem para el inversor del nodo (caso límite); modal nuevo `GridInjectionModal.razor` con confirmación tipada "CONFIRMAR" y motivo opcional.
- `SolarUY.AppHost`: sin cambios.
- `SolarUY.Tests`: tests unitarios del servicio de inyección y de las reglas de telemetría nuevas + arquitectura en verde.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

1. **Estado de inyección** (clarificación 2026-11-15): `bool GridInjectionEnabled = true` en `SolarPark` y en `Inverter`. Migración con `defaultValue: true` para las columnas nuevas. La constitución exige separar estado administrativo de estado operativo, como ya se hizo en la 032 (`Deactivated` ≠ `Inactive`).
2. **Nueva entidad `GridInjectionEvent`** (auditoría inmutable, RF-2/NFR):
   - `Guid Id`, `InjectionTarget TargetType` (enum `{ Park = 1, Inverter = 2 }`), `Guid TargetId`, `string TargetName` (nombre del parque o serie del inversor, snapshot), `Guid ParkId` (FK → `SolarParks`, Restrict; para objetivo inversor es su parque).
   - `InjectionAction Action` (enum `{ Cut = 1, Resume = 2 }`), `string OrderedBy` (máx. 256), `DateTime OrderedAtUtc`, `string? RemoteIp` (máx. 45, IPv6), `string? Reason` (máx. 500, motivo opcional).
   - `int OrdersSent` (cantidad de órdenes confirmadas: 1 en corte por inversor, N en corte de parque; 0 si el objetivo no tenía inversor).
   - Sin métodos de edición/borrado: solo `Add` (inmutabilidad del NFR).
3. **Configuración**: tabla `GridInjectionEvents`, FK Restrict, índice `(ParkId, OrderedAtUtc)`.
4. **Mapeo de DTOs**: `SolarParkDto` agrega `GridInjectionEnabled` y `SolarNodeDto` agrega `InverterGridInjectionEnabled` (bool?, null cuando el nodo no tiene inversor) para que las páginas de monitoreo sepan el estado antes de recibir telemetría.

## Servicios y Lógica de Dominio Pura

### Extensión de `IInverterCommandSender`
- Dos métodos sincrónicos nuevos: `bool SendInjectionCut(string inverterSerieNumber, string? endpoint)` y `bool SendInjectionResume(...)`. El stub `InMemoryInverterCommandSender` los acepta y registra (log); el reemplazo HTTP llega con la 036. `FakeInverterCommandSender` se actualiza con las firmas nuevas.

### `ISolarNodeRepository.GetByParkId(Guid parkId)`
- Para resolver los inversores de un parque: `GetByParkId` → `IInverterRepository.GetByNodeIds` (ya existe).

### `IGridInjectionService` + `GridInjectionService` (sincrónico, constitución)
- `ServiceResult CutParkInjection(Guid parkId, string confirmationText, string orderedBy, string? remoteIp, string? reason)`:
  - Parque inexistente → `NotFound`; parque `Inactive` → `Conflict` (defensivo).
  - **RF-3**: `confirmationText` distinto de "CONFIRMAR" exacto → `Validation` con "La confirmación no coincide. Escribí exactamente 'CONFIRMAR' para proceder." (la UI además mantiene el botón deshabilitado).
  - Ya cortada (`GridInjectionEnabled == false`) → `Conflict` "La inyección a la red ya se encuentra detenida."
  - Envía la orden de corte a **todos** los inversores del parque; si alguno no confirma → `Unavailable` "Error de comunicación con el inversor <serie>. La orden no pudo confirmarse." sin cambiar estados ni registrar evento (todo o nada).
  - Éxito: `park.GridInjectionEnabled = false`, el flag de **todos** sus inversores también a false (los banners de nodo reflejan el corte), `Update` de entidades, evento en bitácora (`Target = Park`, `OrdersSent = N`) y `ILogger.LogInformation`.
- `ServiceResult ResumeParkInjection(...)`: simétrico; ya activa → `Conflict`; éxito restaura parque e inversores y registra el evento.
- `ServiceResult CutInverterInjection(Guid nodeId, ...)`: inversor inexistente → `NotFound`; ya cortado → `Conflict`; envía la orden solo a ese inversor; éxito cambia su flag y registra el evento (`Target = Inverter`, `OrdersSent = 1`).
- `ServiceResult ResumeInverterInjection(...)`: simétrico.
- El `motivo` (`reason`) se recorta y persiste tal cual (opcional); el `remoteIp` lo entrega el controlador.

### RF-6 — registro del intento de violación
- El 403 lo produce el middleware JWT (`OnForbidden` en `Program.cs` del ApiService): se agrega un `ILogger` que registra "Intento de violación de seguridad: ..." con usuario y ruta. La UI oculta los botones (`AuthorizeView`).

## API y Comunicación

- `SolarParksController` (`api/solar-parks`):
  - `POST /{id:guid}/cut-injection` y `POST /{id:guid}/resume-injection`, `[Authorize(Roles = Roles.Administrator)]` a nivel método (arquitectura).
  - Body: `CutInjectionRequest(string ConfirmationText, string? Reason)`.
- `SolarNodesController` (`api/solar-nodes`, un inversor por nodo según la 013):
  - `POST /{id:guid}/inverter/cut-injection` y `POST /{id:guid}/inverter/resume-injection`, Admin.
- `RemoteIp` se toma de `HttpContext.Connection.RemoteIpAddress?.ToString()`.
- Respuestas: `200 { status = "Detenida" | "Activa" }`, `400` (confirmación RF-3), `404`, `409` (estados repetidos), `503` (`Unavailable`), `403` automático para Técnico con log RF-6.
- NFR < 1 s: el stub responde de inmediato; la medición real se hará con el simulador de la 036.

## Interfaz de Usuario (Blazor Server)

1. **`ParkMonitoring.razor`** (`/parques/{id}/monitoreo`, 027):
   - **RF-4**: banner fijo rojo al inicio de la página (`alert alert-danger`, sticky) "INYECCIÓN A LA RED DETENIDA ADMINISTRATIVAMENTE" mientras `GridInjectionEnabled == false`; oculta los avisos de generación baja del propio corte (la telemetría va a 0 y `IsAlertActive` queda en false).
   - **RF-1/RF-5**: bloque `AuthorizeView Roles=Administrator` con botón "Corte de Inyección a Red" (activo) o "Reanudar Inyección a Red" (cortado).
2. **`NodeMonitoring.razor`** (`/nodos/{id}/monitoreo`, 028): ídem para el inversor del nodo (caso límite), con su propio banner y botones cuando `InverterGridInjectionEnabled == false`.
3. **`GridInjectionModal.razor`** (patrón `PanelControlModal.razor`):
   - Advertencia crítica RF-1 con el texto exacto y encabezado rojo; campo obligatorio de confirmación con `@bind` y botón **deshabilitado hasta coincidencia exacta** con "CONFIRMAR" (RF-3, comparación sin `Trim` ni mayúsculas forzadas); textarea opcional de motivo; spinner durante el envío.
   - Éxito → mensajes exactos RF-2 ("Inyección a la red desactivada. El parque no está entregando energía a la red.") y RF-5 ("Inyección a la red reanudada exitosamente."); error → mensaje del servidor (409/503). El padre recarga el estado del parque/nodo.
4. **Clients**: `SolarParkApiClient.CutInjectionAsync/ResumeInjectionAsync(parkId, confirmationText, reason)` y `SolarNodeApiClient.CutInverterInjectionAsync/ResumeInverterInjectionAsync(nodeId, ...)` con el patrón `PanelApiResponse` (mensaje del servidor).
5. Sin cambios en `NavMenu`.

## Decisiones específicas de esta spec

- **Todo o nada** en los cortes de parque: cualquier inversor que no confirme deja la operación sin efecto (consistente con la 032; el caso está en los casos límite de la spec aclarada).
- **Auditoría separada**: `GridInjectionEvents` es una entidad nueva (no se generaliza `PanelControlEvents` de la 032) porque el dominio, objetivo y campos (IP, motivo, N órdenes) difieren.
- **Estado administrativo en la entidad** (`GridInjectionEnabled`), no un enum de estado operativo, según decisión de clarificación.
- **Telemetría en 0 y sin alertas** durante el corte administrativo, replicando el patrón "nocturno/nodo inactivo" de 027/028 (reglas nuevas con tests unitarios).
- **RF-6** se resuelve con el `[Authorize]` existente (403 + log en `OnForbidden`); no hay endpoint nuevo para registrar intentos.

## Estrategia de Tests

- **Tests unitarios (TDD, primero en rojo)** en `SolarUY.Tests/Services/`:
  - `GridInjectionServiceTests` con fakes (park/node/inverter/eventos/sender): confirmación incorrecta → 400 con mensaje exacto (parque e inversor), confirmación correcta → corte de parque (estados de parque e inversores en false + evento con `OrdersSent = N`), reanudación (true + evento), repetido → 409, inexistente → 404, parque inactivo → 409, fallo de un inversor → `Unavailable` sin cambios ni evento, corte/reanudación por inversor individual, motivo e IP persistidos, motivo vacío permitido.
  - `ParkTelemetryServiceTests`: `CalculateMetrics_WhenInjectionIsCut_SetsPowerToZeroAndSuppressesAlert` (de día, por debajo del umbral).
  - `NodeTelemetryServiceTests`: `CalculateMetrics_WhenInverterInjectionIsCut_SetsValuesToZeroAndSuppressesAlerts`.
- **Tests de arquitectura**: sin cambios; los cuatro endpoints mutantes nuevos llevan `[Authorize(Roles = Roles.Administrator)]`.
- **UI Blazor**: verificación manual en navegador (sin bUnit): flujo admin con "CONFIRMAR" (y rechazo con texto incorrecto), banner RF-4 en vivo por SignalR, corte por inversor, Técnico sin botones, consola sin errores, escritorio y móvil.
