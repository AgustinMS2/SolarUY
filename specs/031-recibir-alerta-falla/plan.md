# Plan Técnico: Spec 031 — Recibir Alerta de Falla

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Domain/Alert.cs`, `AlertSeverity.cs`, `AlertEquipmentType.cs`, `AlertStatus.cs`: entidad y enums (`Informational`, `Warning`, `Critical`; `Park`, `Node`, `Panel`; `Pending`, `Acknowledged`, `Dismissed`, `Attended`).
  - `Data/SolarUyDbContext.cs` y migración `AddAlerts`.
  - `Repositories/IAlertRepository.cs` y `AlertRepository.cs`.
  - `Dtos/AlertDtos.cs`: `AlertDto`, `AlertListDto` (contador y lista), `AlertEventDto` (evento de SignalR) y `FaultDetection` (condición detectada).
  - `Services/IAlertService.cs` y `AlertService.cs`: reglas de registro, estados, contador y purga.
  - `Services/AlertDetectionRules.cs`: reglas puras que convierten una lectura en una condición de falla.
  - `Services/AlertNotificationGrouping.cs`: regla pura de agrupación de avisos en ráfaga. Recibe las llegadas (alerta y hora de llegada) y devuelve los avisos; la campana la llama con un pequeño retardo para juntar las alertas que llegan en la misma tanda (T7).
  - `Services/IAlertPublisher.cs` y `AlertPublisher.cs`: registra la falla y la emite por SignalR (solo cuando la alerta se creó o se consolidó) y emite también los cambios de estado (`PublishStatusChangeAsync`, que usa el controlador). Nunca lanza: un fallo de alertas no corta la telemetría.
  - `Services/AlertPurgeBackgroundService.cs`: ejecuta la purga cada hora.
  - `Hubs/AlertsHub.cs`: hub en `/hubs/alerts`, `[Authorize(Roles = Roles.Any)]`, grupo único de alertas.
  - `Controllers/AlertsController.cs`: `GET /api/alerts`, `POST /api/alerts/{id}/acknowledge` y `POST /api/alerts/{id}/dismiss`.
  - `Services/ParkTelemetryBackgroundService.cs`, `NodeTelemetryBackgroundService.cs` y `ParkHealthBackgroundService.cs`: cada uno llama a `IAlertPublisher` cuando su lectura cumple una condición (cambio mínimo en servicios de otras specs).
  - `Program.cs`: registros y mapeo del hub.
- `SolarUY.Web`:
  - `Services/AlertsApiClient.cs`: lista, reconocer y descartar.
  - `Components/Layout/AlertBell.razor`: campana con contador, centro de alertas, avisos (toast) y detalle de la alerta; se monta en `MainLayout.razor` junto al botón de cerrar sesión.
  - `Components/Pages/MaintenanceCreate.razor`: lee `panelId` y `description` de la dirección para pre-cargar la reserva.
- `SolarUY.Tests`:
  - `Services/AlertServiceTests.cs`, `AlertDetectionRulesTests.cs`, `AlertNotificationGroupingTests.cs` y un `FakeAlertRepository`.
  - `Architecture/SecurityArchitectureTests.cs`: agregar `AlertsController.Acknowledge` a la lista de excepciones explícitas (el Técnico también reconoce).

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Nueva tabla `Alerts`** con migración `AddAlerts`: `Id` (UUID), `SolarParkId` (FK a parques, `Restrict`), `ParkName` (nombre del parque guardado en el momento de la alerta, máx. 200), `EquipmentType`, `EquipmentId`, `EquipmentName` (texto guardado en el momento de la alerta), `Severity`, `Status`, `Description` (máx. 500), `OccurrenceCount` (por defecto 1), `CreatedAtUtc`, `LastOccurrenceAtUtc`, `AcknowledgedAtUtc?`, `DismissedAtUtc?` y `AttendedAtUtc?`.
- Índices: `(Status)` para el contador; `(Severity, EquipmentType, EquipmentId, LastOccurrenceAtUtc)` para el deduplicado y la consolidación; `(SolarParkId)`.
- Todas las fechas en UTC.

---

## Lógica de Dominio y Reglas de Negocio

- `IAlertService` (usa `TimeProvider` para poder probar el reloj):
  - `AlertRegistration Register(FaultDetection fault)` → `Created`, `Discarded` o `Consolidated`, con la alerta resultante.
  - `ServiceResult<AlertDto> Acknowledge(Guid id)` y `ServiceResult<AlertDto> Dismiss(Guid id)`.
  - `AlertListDto GetRecent(int take)` y `int PurgeDismissed()`.
- **Registro**, en este orden, para una clave (Severidad + Tipo de equipo + Equipo):
  1. **Deduplicado (RF-2)**: si existe una alerta de la clave, en cualquier estado, con `LastOccurrenceAtUtc` dentro de los últimos 60 s → `Discarded`.
  2. **Consolidación (RF-6)**: se toman las alertas Pendientes o Reconocidas de la clave con `LastOccurrenceAtUtc` dentro de los últimos 5 minutos.
     - Si ya hay una consolidada (`OccurrenceCount >= 3`): se incrementa su contador y se actualiza `LastOccurrenceAtUtc` → `Consolidated`.
     - Si hay 2 o más sin consolidar: se fusionan en la más antigua (contador = suma + 1, estado Pendiente si alguna lo estaba) y se eliminan las demás → `Consolidated`.
  3. En otro caso se crea una alerta Pendiente con contador 1 → `Created`.
- **Estados**: `Acknowledge` pasa Pendiente → Reconocida (si ya estaba Reconocida o más avanzada no cambia nada y no da error, RF-4); `Dismiss` solo acepta Reconocida (RF-7), si no → 409; ambos dan 404 si la alerta no existe.
- **Contador (RF-3)**: cantidad de alertas en estado Pendiente.
- **Purga (RF-5)**: elimina las Descartadas con más de 24 h desde `CreatedAtUtc` y creadas entre las 00:00 y las 06:00 hora de Uruguay.
- **Detección** (`AlertDetectionRules`, funciones puras):
  - Temperatura del inversor > 75 °C (`NodeTelemetryDto.IsOverheating`) → Advertencia, equipo = nodo.
  - Generación del parque bajo su umbral con sol (el indicador que ya calcula la spec 027) → Advertencia, equipo = parque.
  - Panel en estado `Critical` de la spec 030 → Crítico, equipo = panel.
- **Agrupación de avisos (RF-8)**: `AlertNotificationGrouping` recibe las alertas nuevas llegadas en los últimos 10 s y devuelve avisos individuales o, para un parque con 5 o más, un único aviso "Se detectaron N fallas simultáneas en <Parque>".

---

## API y Comunicación en Tiempo Real

- **REST**:
  - `GET /api/alerts?take=50`: alertas recientes (por `LastOccurrenceAtUtc` descendente) y contador de Pendientes. Cualquier rol.
  - `POST /api/alerts/{id}/acknowledge`: cualquier rol; 200 / 404.
  - `POST /api/alerts/{id}/dismiss`: solo Administrador; 200 / 404 / 409.
- **Hub `AlertsHub`** (`/hubs/alerts`): todo usuario autenticado entra al grupo de alertas al conectarse. Evento `ReceiveAlertEvent(AlertEventDto)` con la alerta, el contador y el tipo (`New`, `Updated` por consolidación, `StatusChanged` por reconocer o descartar). Los tipos `New` y `Updated` generan aviso; `StatusChanged` solo actualiza la campana y la lista de todos los usuarios.
- **`AlertPublisher`**: registra la falla y emite el evento solo si el resultado es `Created` o `Consolidated`; los duplicados no emiten nada (RF-2).
- **`AlertPurgeBackgroundService`**: una vez por hora llama a `PurgeDismissed`.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **`AlertBell.razor`** en la barra superior (solo usuarios autenticados):
  - Campana con el contador de Pendientes (oculto en 0).
  - Centro de alertas desplegable: lista reciente con color por severidad (rojo, ámbar, azul), equipo, parque, hora local y contador de instancias; marca en negrita las Pendientes.
  - Avisos (toast): aparecen con cada alerta nueva o consolidada, se cierran a los 8 s o con la X; agrupados por parque en una ráfaga.
  - Al hacer clic en una alerta o aviso: se reconoce y se abre el detalle (descripción, equipo, parque, fecha, instancias, estado) con "Agendar Mantenimiento" (Administrador y alerta de panel) y "Descartar" (Administrador y alerta Reconocida).
  - Se conecta a `/hubs/alerts` con reconexión automática y se desuscribe al destruirse.
- **`MaintenanceCreate.razor`**: acepta `?panelId=...&description=...` y pre-selecciona el panel y rellena el motivo.
- Riesgo a verificar en el navegador: que la campana mantenga su conexión al navegar entre pantallas (el diseño la monta en el diseño común de la aplicación).

---

## Estrategia de Testing

- **Unit tests (TDD, rojo → verde)**:
  - `AlertServiceTests`: registro (alerta nueva en Pendiente con UUID y fecha UTC; duplicado a los 59 s descartado y a los 61 s no; el deduplicado vale para cualquier estado), consolidación (tercera detección en 5 minutos fusiona y cuenta 3, detecciones siguientes incrementan, fuera de la ventana no consolida, claves distintas no se mezclan), contador (solo Pendientes), reconocer (Pendiente → Reconocida, idempotente, inexistente), descartar (solo Reconocidas, 409, inexistente) y purga (Descartada de noche con más de 24 h se elimina; de día, reciente o no descartada se conserva).
  - `AlertDetectionRulesTests`: cada lectura genera su alerta con severidad, equipo y texto correctos, y las lecturas normales no generan nada.
  - `AlertNotificationGroupingTests`: 4 alertas del parque dan avisos individuales, 5 dan un aviso agrupado, parques distintos no se mezclan.
- **Tests de arquitectura**: `SecurityArchitectureTests` en verde (los endpoints exigen rol; la excepción de reconocer figura en la lista explícita).
- **UI Blazor y SignalR**: verificación manual en navegador con las alertas que generen las lecturas simuladas. La alerta crítica y la ráfaga solo se prueban con tests hasta que haya simulador de fallas (spec 037) o un disparador de prueba.
