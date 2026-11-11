# MEMORY.md — Estado de Proyecto y Memoria Operativa de SolarUY

## Estado General del Proyecto
- **Proyecto**: SolarUY — Sistema de gestión, monitoreo en tiempo real y mantenimiento de parques fotovoltaicos.
- **Arquitectura base**: Solución .NET 9 (todos los proyectos en `net9.0`; Aspire 13 igual necesita tener instalado el SDK de .NET 10 para compilar el AppHost) con .NET Aspire (`SolarUY.AppHost`), Web Frontend (`SolarUY.Web` - Blazor Server), API de negocio (`SolarUY.ApiService`), y librerías compartidas (`SolarUY.ServiceDefaults`).
- **Persistencia**: PostgreSQL con EF Core (planificado soporte TimescaleDB para telemetría de series de tiempo).
- **Autenticación**: Keycloak OIDC containerizado en Aspire (Roles: `Administrador`, `Técnico`).
- **Fase actual**: SDD en marcha. Specs 001–025 implementadas y verificadas (las pantallas de la 011 a la 025, probadas en navegador con Docker el 2026-11-11); spec 026 con T1–T4 verificadas y T5 bloqueada por la 031; 300 tests en verde (uno depende de la hora, ver más abajo).

---

## Módulos del Sistema y Backlog (40 funcionalidades)

| Módulo | Funcionalidad | Rol Principal | Estado Spec |
| :--- | :--- | :--- | :--- |
| **Usuarios** | Iniciar Sesión / Cerrar Sesión | Todos | Implementadas (`015`, `016`) |
| | Gestionar Roles de Usuario | Administrador | Implementada (`017`) |
| | Alta Técnico / Listar Técnicos | Administrador | Implementadas (`018`, `019`) |
| **Parques** | Alta Parque | Administrador | **Implementada** (`001-alta-parque`) |
| | Listar Parques | Todos | **Implementada** (`002-listar-parques`) |
| | Modificar Parque | Administrador | **Implementada** (`003-modificar-parque`) |
| | Baja Parque | Administrador | **Implementada** (`004-baja-parque`) |
| **Nodos** | Alta Nodo | Administrador | **Implementada** (`009-alta-nodo`) |
| | Listar Nodos | Todos | **Implementada** (`010-listar-nodos`) |
| | Modificar Nodo | Administrador | Implementada (`011`) |
| | Baja Nodo | Administrador | Spec aprobada (`012`) |
| | Asignar Inversor a Nodo | Administrador | Implementada (`013`) |
| **Paneles** | Alta / Baja / Modificar / Listar Paneles | Administrador / Todos | **Implementadas** (`005`–`008`) |
| | Asignar Panel a Nodo | Administrador | Implementada (`014`) |
| **Mantenimiento** | Reservar Fecha / Asignar Técnico / Cancelar / Listar | Administrador | Implementadas (`021`–`024`) |
| | Ver Mis Mantenimientos / Confirmar Mantenimiento | Técnico | `025` implementada; `026` con T5 bloqueada por la `031` |
| **Monitoreo** | Ver Generación Tiempo Real (Parque, Nodo, Panel) | Todos (SignalR) | Borrador (`027`–`029`) |
| | Ver Estado de Salud de Paneles / Recibir Alerta de Falla | Todos | Borrador (`030`, `031`) |
| **Control Inversor** | Activar / Desactivar Panel | Administrador | Borrador (`032`) |
| | Activar / Desactivar Inyección a la Red | Administrador | Borrador (`033`) |
| **Históricos** | Consultar Generación (Día, Mes, Año) | Todos | Borrador (`034`) |
| | Consultar en Lenguaje Natural (LLM) | Todos | Borrador (`035`) |
| **Simulador** | Simular Lecturas del Inversor | Sistema (Background) | Borrador (`036`) |
| | Simular Fallas de Paneles | Sistema (Mock) | Borrador (`037`) |

---

## Registro de Especificaciones SDD (`specs/`)

| Código y Nombre | Estado | Fase Actual | Tareas Completadas | Siguiente Acción |
| :--- | :--- | :--- | :--- | :--- |
| `001-alta-parque` | **Implementada** | Completada | 6 / 6 | Spec 001 finalizada. Iniciar **Spec 002** (`002-listar-parques`) |
| `002-listar-parques` | **Implementada** | Completada | 3 / 3 | Spec 002 finalizada. Iniciar **Spec 003** (`003-modificar-parque`) |
| `003-modificar-parque` | **Implementada** | Completada | 4 / 4 | Spec 003 finalizada. Iniciar **Spec 004** (`004-baja-parque`) |
| `004-baja-parque` | **Implementada** | Completada | 3 / 3 | Spec 004 finalizada. Iniciar siguiente spec del backlog |
| `005-alta-panel` | **Implementada** | Completada | 6 / 6 | Spec 005 finalizada. |
| `006-listar-panel` | **Implementada** | Completada | 5 / 5 | Spec 006 finalizada. |
| `007-modificar-panel` | **Implementada** | Completada | 5 / 5 | Spec 007 finalizada. Iniciar **Spec 008** (`008-baja-panel`) |
| `008-baja-panel` | **Implementada** | Completada | 5 / 5 | Finalizada. |
| `009-alta-nodo` | **Implementada** | Completada | 5 / 5 | Finalizada. |
| `010-listar-nodos` | **Implementada** | Completada | 3 / 3 | Finalizada. |
| `011-modificar-nodo` | **Implementada** | Completada | 4 / 4 | T1–T4 verificadas. T4 (`/nodos/{id}/editar` + botón "Editar") probada en navegador el 2026-11-11. `PUT /api/solar-nodes/{id}` ya exige rol Administrador (test de arquitectura en verde). |
| `012-baja-nodo` | **Implementada** | Completada | 3 / 3 | Finalizada. |
| `013-asignar-inversor-nodo` | **Implementada** | Completada | 5 / 5 | Asignar, reemplazar (con confirmación) y desvincular inversor. T1–T4 verificadas (migración, `AssignInverter`/`UnassignInverter` con 31 tests, endpoints `POST`/`DELETE`); T5 (modal en `/nodos`) probada en navegador el 2026-11-11. |
| `014-asignar-panel-nodo` | **Implementada** | Completada | 6 / 6 | Asociar, reasignar (con confirmación), advertencia de potencia y desasociar paneles. T1–T5 verificadas (`SolarNodeId` + migración, `AssignPanelsToNode` 25 tests, `UnassignPanelsFromNode` 10, conteos y nodo en listados 6, endpoints `POST`/`DELETE /api/solar-nodes/{id}/panels`); T6 (modal de paneles y columna "Nodo") probada en navegador el 2026-11-11. |
| `015-iniciar-sesion-keycloak` | **Implementada** | Completada | 4 / 4 | T1–T4 verificadas con Keycloak real el 2026-11-11: el contenedor levanta con el realm importado, la API responde 401 con `WWW-Authenticate: Bearer` a las llamadas anónimas, las rutas protegidas redirigen al login de Keycloak y vuelven autenticadas, y la barra superior muestra el nombre y el rol. El botón "Cerrar sesión" apunta a `/account/logout` (spec 016). |
| `016-cerrar-sesion-keycloak` | **Implementada** | Completada | 3 / 3 | T1 (`/account/logout`), T2 (modal de confirmación) y T3 (RF-4: aviso "La sesión ha finalizado." al invalidarse la sesión desde otra pestaña) probadas con Keycloak real el 2026-11-11: confirmar cierra la sesión y vuelve al inicio, cancelar no cierra y "Atrás" no la reabre. |
| `017-gestionar-roles-usuario` | **Implementada** | Completada | 8 / 8 | T1–T8 verificadas. T2–T6 con API y Web con Keycloak simulado; T1 (mapeo de roles), T7 (`/usuarios`: cambiar el rol de `tecnico`) y T8 (no deja quitar el rol al último Administrador) probadas con Keycloak real en navegador el 2026-11-11. |
| `018-alta-tecnico` | **Implementada** | Completada | 5 / 5 | T1-T5 completadas: entidad Technician, repositorio, servicio con tests, controlador POST y API client + vista. Finalizada; alta probada en navegador el 2026-11-11. |
| `019-listar-tecnicos` | **Implementada** | Completada | 3 / 3 | T1: `GetAll` en repo + `TechnicianService` (orden alfabético y filtro por estado, 3 tests). T2: `GET /api/technicians` (controlador restableció también el POST de spec 018 con `[Authorize(Roles = Roles.Administrator)]` a nivel método, que exige el test de arquitectura). T3: vista `/tecnicos` con buscador, filtro de estado y botón "Nuevo técnico" (solo admin); `GetAllAsync` en `TechnicianApiClient` + registro en DI del Web. 216 tests en verde. Vista `/tecnicos` verificada en navegador el 2026-11-11. |
| `020-baja-tecnico` | **Implementada** | Completada | 3 / 3 | T1: `TechnicianService.Deactivate` (bloqueo por pendientes RF-2, baja lógica RF-3, inexistente→404, ya inactivo→409; 4 tests). Se creó `IMaintenanceRepository` **placeholder** (`EmptyMaintenanceRepository` devuelve 0 pendientes) hasta spec 021: al implementarse mantenimientos, reemplazar por la consulta real. Se agregó `GetById`/`Update` a `ITechnicianRepository` (interfaz + fake + nuevo `TechnicianRepository` EF real). T2: `DELETE /api/technicians/{id}` (204/404/409) + **cableado DI de técnicos** (`TechnicianRepository`, `IMaintenanceRepository`, `ITechnicianService` — spec 018 nunca los había registrado). T3: botón "Dar de baja" (solo activos) + modal de confirmación en `/tecnicos`; `DeactivateAsync` en `TechnicianApiClient`. 220 tests en verde. Baja verificada en navegador el 2026-11-11. La baja se bloquea mientras el técnico tenga mantenimientos pendientes (RF-2), no por ser el único técnico. |
| `021-reservar-fecha-mantenimiento` | **Implementada** | Completada | 4 / 4 | **T1-T4 completadas (233 tests en verde)**. T1: entidad `MaintenanceOrder` + enums `MaintenanceType`/`MaintenanceStatus`, `DbSet` + config (FKs Restrict, índice `SolarPanelId+Status` para RF-3) y migración `AddMaintenanceOrderEntity` — la migración también crea la tabla `Technicians` (spec 018 nunca había generado migración). T2: `IMaintenanceRepository` extendida (`Add`, `HasPendingForPanel` por día calendario uruguayo UTC-3, `CountPendingByTechnician` real), `MaintenanceService.CreateReservation` + `IMaintenanceService`, repositorio EF real `MaintenanceRepository` (reemplaza el placeholder en DI), 13 tests. T3: `POST /api/maintenances` (201/400/404/409-requiresConfirmation RF-3). T4: `MaintenanceApiClient` (`ServiceResult<Guid>` con mapeo `ConfirmationRequired`) + formulario `/mantenimientos/nuevo` (selectores de paneles activos vía `PanelsApiClient` y técnicos activos vía `TechnicianApiClient`, datetime-local en hora de Uruguay convertida a UTC con `TimeZoneInfo`, confirmación RF-3 con "Agendar de todas formas", limpiado RF-4 + botón a `/mantenimientos` — página que agregará la spec 024). **Migraciones se aplican sola al arrancar**: `Database.Migrate()` agregado al `Program.cs` del ApiService (Aspire crea la BD vacía, sin esto las migraciones nunca corrían). **BD verificada en Docker**: tablas `Technicians` + `MaintenanceOrders` con FKs/índices correctos. **Flujos probados en navegador el 2026-11-11** (reservar, incluido el arreglo del tipo de intervención) (conflicto resuelto: nginx de `01_1_webApiEjemploModificado` ocupaba el puerto 8080 de Keycloak — bajarlo o no correrlo en simultáneo). |
| `022-modificar-reserva-mantenimiento` | **Implementada** | Completada | 3 / 3 | **T1-T3 completadas (244 tests en verde)**. T1: `MaintenanceService.UpdateReservation` (inexistente→404, no pendiente→409 RF-3, fecha pasada→400 RF-4, técnico inexistente/inactivo→404/400, descripción 5-500; registra `UpdatedAtUtc` RF-2; 11 tests). **Nuevo campo `UpdatedAtUtc` en `MaintenanceOrder`** + migración `AddUpdatedAtUtcToMaintenanceOrders` (RF-2 pedía fecha de modificación y no existía). `IMaintenanceRepository` extendida (`GetById` con includes de técnico/panel/parque, `Update`). **Necesario para RF-1 pero no previsto en el plan: `GET /api/maintenances/{id}` + `MaintenanceOrderDto`** (precarga de la edición) + `GetById` en el servicio. T2: `PUT /api/maintenances/{id}` (204/400/404/409) + `GET` (200/404); ambos con `[Authorize]` requerido por arquitectura. T3: `/mantenimientos/{id:guid}/editar` (pre-cargado RF-1, bloqueo de cerradas RF-3, aviso si el técnico original ya no está activo, datetime-local Uruguay→UTC, "Reserva de mantenimiento actualizada correctamente." RF-2, Cancelar→`/mantenimientos` RF-5) + `GetOrderAsync`/`UpdateReservationAsync` en `MaintenanceApiClient`. **El botón "Modificar" ya existe en el listado de la spec 024.** Edición verificada en navegador el 2026-11-11. |
| `023-cancelar-mantenimiento` | **Implementada** | Completada | 3 / 3 | **T1-T3 completadas (254 tests en verde)**. T1: `MaintenanceService.CancelReservation` (inexistente→404, no pendiente→409 "finalizado o cancelado" RF-4, motivo <10→400 RF-3 "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.", motivo trim, >500→400, `UpdatedAtUtc` como fecha de cancelación RF-2; 10 tests). T2: `POST /api/maintenances/{id}/cancel` (200/400/404/409, método con `[Authorize(Roles = Roles.Administrator)]`). T3: `MaintenanceCancelModal.razor` (TextArea obligatoria, RF-3 en vivo, "Volver" cierra sin cancelar RF-5, msg de éxito RF-2) + `CancelAsync` en `MaintenanceApiClient`. **Montaje**: mientras no exista el listado de la spec 024, el modal está integrado en la página de edición `/mantenimientos/{id}/editar` con botón "Cancelar mantenimiento" que solo aparece en órdenes Pendientes (RF-4); al recargar la página, si quedó Cancelada el formulario se bloquea. **El modal ya se reutiliza en el listado de la 024 (`MaintenanceList.razor`).** Cancelación probada en navegador el 2026-11-11 (la columna `CancellationReason` no se revisó a mano en la BD). |
| `024-listar-mantenimientos` | **Implementada** | Completada | 4 / 4 | **T1-T4 completadas**. T1-T3 con 262 tests en verde. T1: `MaintenanceService.GetAll(MaintenanceFilter)` — nuevo DTO `MaintenanceFilter` (Status/ParkId/TechnicianId/FromUtc/ToUtc con `[FromQuery(Name=...)]` para binding como `status/parkId/techId/from/to`) + `GetAll` en `IMaintenanceRepository`, repositorio EF real (includes Technician + Panel→Park, filtros combinables SQL y `OrderByDescending(ScheduledDateUtc)` RF-2), fake con lista `Panels` para resolver el filtro por parque, DTO `MaintenanceOrderDto` extendido con `TechnicianName` (y `GetById` de 022 ahora resuelve nombres vía repos si las navegaciones son null); 8 tests. T2: `GET /api/maintenances` con `[FromQuery] MaintenanceFilter` (hereda `[Authorize]` de clase — admin y técnico ven el listado según HU-1). T3: vista `/mantenimientos` (`MaintenanceList.razor`) — grilla RF-1 completa (Código 8 chars con title, Parque, Panel, Técnico, Tipo, Fecha dd/MM/yyyy HH:mm Uruguay, Estado badge, Acciones), filtros combinables RF-3 (select Estado/Parque/Técnicos activos+inactivos + rango de fechas date inputs con conversión Uruguay→UTC, extremo "Hasta" inclusivo = fin del día), mensaje vacío RF-4 "No se encontraron mantenimientos para los criterios seleccionados.", badges RF-5 (Pendiente amarillo, Completado verde, Cancelado gris), botón "Nuevo mantenimiento" (solo admin), paginación client-side (20/pág, Anterior/Siguiente), botones "Modificar" (→ `/mantenimientos/{id}/editar`) y "Cancelar" (modal `MaintenanceCancelModal` de 023, exito→recarga + "Mantenimiento cancelado correctamente.") **solo admin y solo Pendientes**; `GetAllAsync(MaintenanceFilter)` en `MaintenanceApiClient` (query `status/parkId/techId/from/to`). **T4 (RF-6)**: botón "Nuevo mantenimiento" siempre visible para el Administrador y ausente para el Técnico, verificado en navegador el 2026-11-11 junto con el listado y el botón "Confirmar". Filtros de estado, parque, técnico y fechas probados a mano el 2026-11-11 (los de fecha se quedaban cargando; corregido y reprobado). |
| `025-ver-mis-mantenimientos-tecnico` | **Implementada** | Completada | 3 / 3 | T1: `MaintenanceService.GetMyMaintenances` (filtrado por email del técnico, pendientes primero por fecha ascendente, completados/cancelados descendente; 7 tests). T2: `GET /api/maintenances/mine` con `[Authorize(Roles = Roles.Any)]`. T3: vista móvil `/mis-mantenimientos` (`MyMaintenances.razor`) con tarjetas táctiles, badges, botón "Confirmar Mantenimiento", mensaje cuando no hay tareas para hoy, filtros rápidos y selector de técnico para admin; probada en navegador y en vista móvil el 2026-11-11. |
| `026-confirmar-mantenimiento` | **Código completo** (T5 bloqueada) | Validación | 4 / 5 | Spec/plan/tareas ajustados (campo "estado del panel", mensajes faltantes, límite de observaciones 500→1000, RF-3 diferido a la spec 031 porque las alertas no existen; cualquier Técnico o Administrador puede confirmar cualquier orden, decisión del equipo). Verificadas T1 (campo `PanelOutcome` + migración), T2 (`ConfirmMaintenance`, 30 tests), T3 (`POST /api/maintenances/{id}/confirm`) y T4 (formulario `/mantenimientos/{id}/confirmar` y botón "Confirmar" en la lista del Administrador), esta última probada en navegador el 2026-11-11 (validaciones, confirmar, ya completado, "Cancelar", doble pestaña y vista móvil). **T5** (RF-3, resolver alertas) bloqueada por la spec 031. |
| `027-ver-generacion-tiempo-real-parque` | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 028-ver-generacion-tiempo-real-nodo | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 029-ver-generacion-tiempo-real-panel | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 030-ver-estado-salud-paneles | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 031-recibir-alerta-falla | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 032-activar-desactivar-panel | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 033-activar-desactivar-inyeccion-red | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 034-consultar-generacion-historica | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 035-consulta-lenguaje-natural-llm | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 036-simular-lecturas-inversor | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |
| 037-simular-fallas-paneles | Pendiente | Borrador spec | 0 / 0 | Clarificación y aprobación |

---

## Estado Actual de Verificación (2026-11-10)

- **Rama**: `master` (estado al integrar la spec 013 sobre las specs 011 y 017).
- **Compilación**: la solución compila.
- **Tests**: `dotnet test` → **300 en verde** (2026-11-11, 08:18 UTC). Hay un test que depende de la hora y puede fallar entre las 00:00 y las 05:00 de Uruguay: `MaintenanceServiceReservationTests.CreateReservation_WithPendingOrderSameDay_RequiresConfirmation` (spec 021): depende de la hora y falla entre las 00:00 y las 05:00 de Uruguay; no es un bug del producto.
  - Corregido: `SolarNodesController.Update` (spec 011) ahora exige `[Authorize(Roles = Roles.Administrator)]`, igual que su página de edición.
- **Deuda inmediata**:
  1. ~~Corregir el `[Authorize]` de `SolarNodesController.Update`~~ — hecho, `dotnet test` en verde.
  2. ~~Validar en navegador la T4 de la spec 011~~ — hecho (2026-11-11).
  3. Specs 015, 016 y 017 verificadas con Keycloak real.
  4. Continuar con las specs 027–037 (primero aclarar y aprobar; la 027 depende de la 036) según prioridad del usuario.
  5. Decidir si Postgres conserva los datos entre ejecuciones (`WithDataVolume()` + `ContainerLifetime.Persistent` en `SolarUY.AppHost/AppHost.cs`). Hoy el contenedor se recrea en cada arranque y se pierden parques, nodos, paneles, técnicos y reservas; Keycloak sí conserva usuarios y roles porque reimporta el realm. Es un archivo compartido: avisar al equipo.
- **Hallazgos de la primera prueba real con Docker (2026-11-11)**:
  1. **Blazor sin interactividad**: en una PC con solo el runtime de ASP.NET Core 10, los proyectos `net9.0` con `RollForward=LatestMajor` corrían sobre el runtime 10, que ya no sirve `blazor.web.js` (404) y los formularios fallaban con "The POST request does not specify which form is being submitted". Solución: instalar los runtimes de .NET 9 (`winget install Microsoft.DotNet.AspNetCore.9` y `Microsoft.DotNet.Runtime.9`) y quitar el `RollForward` de `SolarUY.Web.csproj`. Pendiente de decisión del equipo: quitarlo también del resto o pasar todo a `net10.0` (la constitución dice .NET 9).
  2. **Spec 021, pantalla de reservar**: `MaintenanceCreate.razor` hacía `int.Parse` sobre el tipo ("Preventive"/"Corrective") y fallaba siempre; corregido con `Enum.TryParse`.
  3. **Spec 024, lista de mantenimientos**: el botón "Nuevo mantenimiento" solo aparecía cuando ya había órdenes, así que el Administrador no podía crear la primera reserva. Corregido (spec 024 RF-6, tarea T4): ahora está siempre visible en el encabezado, solo para el Administrador. Verificado en navegador el 2026-11-11. El menú lateral sigue sin enlace a `/mantenimientos/nuevo` (opcional).
  4. **Keycloak** responde por `https://localhost:8080` (no http) con esta versión de Aspire; el Web ya lo usa bien.
  5. **Spec 024, filtros de fecha**: `OnFromChanged`/`OnToChanged` eran `void` y llamaban a `CargarDatosAsync()` sin `await`, por lo que la lista quedaba en "Cargando..." hasta cambiar otro filtro. Ahora son `async Task` y esperan la carga. Corregido y reprobado en navegador.
- **Nota de mantenimiento**: el commit `2a0b56b` guardó `MEMORY.md` con bytes NUL en los ceros iniciales de los códigos de spec; ya fue reparado en esta actualización. Evitar herramientas que introduzcan NUL.

---

## Decisiones Técnicas y Convenciones Activas
1. **Constitución**: Documentada y vigente en `docs/constitution.md`.
2. **Modelo Sincrónico**: Métodos normales sincrónicos en repositorios y servicios para mantener el control y simplicidad.
3. **Controladores Tradicionales**: `[ApiController]` heredando de `ControllerBase` en `SolarUY.ApiService/Controllers/`.
4. **Servicios y Observabilidad**: Servicios de dominio con soporte nativo para `ILogger<T>`.
5. **Testing Pragmático**: Tests de arquitectura como base estructural + tests unitarios simples focalizados en reglas de negocio (sin sobreingeniería de `bUnit` ni Testcontainers por use-case).
6. **Persistencia Estándar**: PostgreSQL con EF Core sin extensiones (`citext` descartado). Nombre único sin distinguir mayúsculas mediante columna `NormalizedName` (minúsculas) con índice único.
7. **Verificación UI**: Blazor Server probado interactivamente en navegador / DevTools.
8. **Una spec por requerimiento funcional**: el antiguo `001-gestion-parques` se dividió en 001 Alta, 002 Listar, 003 Modificar y 004 Baja.
9. **Roles**: el control de acceso por rol se resuelve en la spec de Usuarios; las specs de Parques no lo incluyen.
10. **Errores de servicio**: `ServiceResult` con `Validation` → 400, `NotFound` → 404, `Conflict` → 409, `Unavailable` → 503 y `ConfirmationRequired` → 409 con `requiresConfirmation: true` (la pantalla pide confirmar antes de repetir el pedido).
11. **Tokens en Blazor Server (spec 015)**: los circuitos de SignalR no tienen `HttpContext`; los tokens de Keycloak se guardan en `UserTokenStore` (memoria del servidor, clave `sid`) y `BearerTokenHandler` los adjunta a las llamadas a la API, renovándolos con el refresh token. Si el servidor se reinicia, la cookie se rechaza y se pide login de nuevo.
12. **Keycloak local**: puerto fijo 8080, realm `SolarUY` importado desde `SolarUY.AppHost/Realms/SolarUY-realm.json`, usuario de prueba `admin` / `admin123` (rol Administrador).
13. **Roles en Keycloak (spec 017)**: realm roles `Administrador` y `Técnico`, expuestos en el claim `roles`; el realm define el cliente de servicio `solaruy-api-admin` (secreto de desarrollo `solaruy-api-admin-dev-secret`) con permisos de `realm-management` sobre `realmRoles` para listar usuarios y reemplazar su rol.
14. **Archivo del realm**: el JSON se llama `SolarUY-realm.json` (renombrado en el último merge; el nombre anterior era `solaruy-realm.json`).
