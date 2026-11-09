# MEMORY.md — Estado de Proyecto y Memoria Operativa de SolarUY

## Estado General del Proyecto
- **Proyecto**: SolarUY — Sistema de gestión, monitoreo en tiempo real y mantenimiento de parques fotovoltaicos.
- **Arquitectura base**: Solución .NET 9 (todos los proyectos en `net9.0`; Aspire 13 igual necesita tener instalado el SDK de .NET 10 para compilar el AppHost) con .NET Aspire (`SolarUY.AppHost`), Web Frontend (`SolarUY.Web` - Blazor Server), API de negocio (`SolarUY.ApiService`), y librerías compartidas (`SolarUY.ServiceDefaults`).
- **Persistencia**: PostgreSQL con EF Core (planificado soporte TimescaleDB para telemetría de series de tiempo).
- **Autenticación**: Keycloak OIDC containerizado en Aspire (Roles: `Administrador`, `Técnico`).
- **Fase actual**: SDD en marcha. Specs 001–010 implementadas; spec 011 (modificar nodo) con backend y UI escritos pero T4 sin validar; spec 017 (roles) en validación; specs 015/016 con código completo esperando prueba con Keycloak real. Hay **1 test de arquitectura en rojo** (ver "Estado Actual de Verificación").

---

## Módulos del Sistema y Backlog (40 funcionalidades)

| Módulo | Funcionalidad | Rol Principal | Estado Spec |
| :--- | :--- | :--- | :--- |
| **Usuarios** | Iniciar Sesión / Cerrar Sesión | Todos | En implementación (`015`, `016`) |
| | Gestionar Roles de Usuario | Administrador | **Código completo**, validación pendiente (`017`) |
| | Alta Técnico / Listar Técnicos | Administrador | Specs aprobadas (`018`, `019`) |
| **Parques** | Alta Parque | Administrador | **Implementada** (`001-alta-parque`) |
| | Listar Parques | Todos | **Implementada** (`002-listar-parques`) |
| | Modificar Parque | Administrador | **Implementada** (`003-modificar-parque`) |
| | Baja Parque | Administrador | **Implementada** (`004-baja-parque`) |
| **Nodos** | Alta Nodo | Administrador | **Implementada** (`009-alta-nodo`) |
| | Listar Nodos | Todos | **Implementada** (`010-listar-nodos`) |
| | Modificar Nodo | Administrador | Código completo, T4 sin validar (`011`) |
| | Baja Nodo | Administrador | Spec aprobada (`012`) |
| | Asignar Inversor a Nodo | Administrador | Spec aprobada (`013`) |
| **Paneles** | Alta / Baja / Modificar / Listar Paneles | Administrador / Todos | **Implementadas** (`005`–`008`) |
| | Asignar Panel a Nodo | Administrador | Spec aprobada (`014`) |
| **Mantenimiento** | Reservar Fecha / Asignar Técnico / Cancelar / Listar | Administrador | Specs aprobadas (`021`–`024`) |
| | Ver Mis Mantenimientos / Confirmar Mantenimiento | Técnico | Specs aprobadas (`025`, `026`) |
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
| `011-modificar-nodo` | **Código completo** | Validación | 3 / 4 | T1–T3 en verde; T4 (página `/nodos/{id}/editar` + botón "Editar") escrita pero sin marcar ni validar en navegador. **Regresión**: el `PUT /api/solar-nodes/{id}` no tiene `[Authorize(Roles = Roles.Administrator)]` y rompe el test de arquitectura de seguridad. |
| `012-baja-nodo` | **Implementada** | Completada | 3 / 3 | Finalizada. |
| `013-asignar-inversor-nodo` | **Código completo** | Validación | 4 / 5 | Spec/plan/tareas ampliados (desvincular, RF-7 a RF-10; 5 tareas). Verificadas T1–T4 (migración, `AssignInverter`/`UnassignInverter` con 31 tests, endpoints `POST`/`DELETE`). La lista de nodos muestra la serie del inversor. **Falta probar en navegador la T5**: abrir el modal, asignar, reemplazar con confirmación, desvincular y vista en celular. |
| `014-asignar-panel-nodo` | **Código completo** | Validación | 5 / 6 | Spec/plan/tareas ampliados (desasociar, límite de 50, confirmación de reasignación y de potencia, listados; 6 tareas). Verificadas T1–T5: `SolarNodeId` + migración, `AssignPanelsToNode` (25 tests), `UnassignPanelsFromNode` (10), conteo de paneles y nodo en los listados (6 tests), endpoints `POST`/`DELETE /api/solar-nodes/{id}/panels`; 193 tests en verde. **Falta probar en navegador la T6**: modal de paneles del nodo (asociar, reasignar con confirmación, advertencia de potencia, desasociar) y vista en celular. |
| `015-iniciar-sesion-keycloak` | En implementación | Validación | 0 / 4 verificadas | Código de T1–T4 escrito. **Ninguna tarea se marca hasta cumplir su "Hecho cuando"**: falta probar con Docker (Keycloak + login en navegador). El botón "Cerrar sesión" apunta a `/account/logout` (Spec 016 T1). |
| `016-cerrar-sesion-keycloak` | En implementación | Validación | 0 / 3 verificadas | Código de T1 (`/account/logout`), T2 (modal) y T3 (RF-4: aviso "La sesión ha finalizado.", opción A) escrito. Falta probar con Keycloak real. Spec/plan/tasks actualizados con RF-4. |
| `017-gestionar-roles-usuario` | **Código completo** | Validación | 5 / 8 | T1–T8 escritas. Verificadas T2–T6 (API y Web con Keycloak simulado). **Faltan con Keycloak real / navegador**: T1 (mapper de roles), T7 (guardar un rol, responsive) y T8 (mensaje de último administrador). RF-3/RF-5/RF-6/RF-7 ya están en la spec. |
| `018-alta-tecnico` | Implementada | Validación | 5 / 5 | T1-T5 completadas: entidad Technician, repositorio, servicio con tests, controlador POST y API client + vista. Finalizada. |
| `019-listar-tecnicos` | **Código completo** | Validación | 3 / 3 | T1: `GetAll` en repo + `TechnicianService` (orden alfabético y filtro por estado, 3 tests). T2: `GET /api/technicians` (controlador restableció también el POST de spec 018 con `[Authorize(Roles = Roles.Administrator)]` a nivel método, que exige el test de arquitectura). T3: vista `/tecnicos` con buscador, filtro de estado y botón "Nuevo técnico" (solo admin); `GetAllAsync` en `TechnicianApiClient` + registro en DI del Web. 216 tests en verde. **Falta verificar la vista en el navegador**. |
| `020-baja-tecnico` | **Código completo** | Validación | 3 / 3 | T1: `TechnicianService.Deactivate` (bloqueo por pendientes RF-2, baja lógica RF-3, inexistente→404, ya inactivo→409; 4 tests). Se creó `IMaintenanceRepository` **placeholder** (`EmptyMaintenanceRepository` devuelve 0 pendientes) hasta spec 021: al implementarse mantenimientos, reemplazar por la consulta real. Se agregó `GetById`/`Update` a `ITacticianRepository` (interfaz + fake + nuevo `TacticianRepository` EF real). T2: `DELETE /api/technicians/{id}` (204/404/409) + **cableado DI de técnicos** (`TacticianRepository`, `IMaintenanceRepository`, `ITechnicianService` — spec 018 nunca los había registrado). T3: botón "Dar de baja" (solo activos) + modal de confirmación en `/tecnicos`; `DeactivateAsync` en `TechnicianApiClient`. 220 tests en verde. **Falta verificar en el navegador**. |
| `020-baja-tecnico` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
| `021-reservar-fecha-mantenimiento` | Pendiente | Spec aprobada | 0 / 4 | Spec + plan + tasks listos; sin implementar. |
| `022-modificar-reserva-mantenimiento` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
| `023-cancelar-mantenimiento` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
| `024-listar-mantenimientos` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
| `025-ver-mis-mantenimientos-tecnico` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
| `026-confirmar-mantenimiento` | Pendiente | Spec aprobada | 0 / 3 | Spec + plan + tasks listos; sin implementar. |
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

## Estado Actual de Verificación (2026-11-04)

- **Rama**: `master` (estado al integrar la spec 013 sobre las specs 011 y 017).
- **Compilación**: la solución compila.
- **Tests**: `dotnet test` → **193 en verde, 0 en rojo**.
  - Corregido: `SolarNodesController.Update` (spec 011) ahora exige `[Authorize(Roles = Roles.Administrator)]`, igual que su página de edición.
- **Deuda inmediata**:
  1. ~~Corregir el `[Authorize]` de `SolarNodesController.Update`~~ — hecho, `dotnet test` en verde.
  2. Validar en navegador la T4 de la spec 011 (editar nodo, parque bloqueado, "Cancelar" sin guardar y listado actualizado) y marcarla.
  3. Probar las specs 015/016/017 con Keycloak real (Docker) y navegador antes de marcar sus tareas como verificadas.
  4. Continuar con las specs 012–014 (baja de nodo y asignaciones) o con las 018–026 según prioridad del usuario.
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
