# MEMORY.md — Estado de Proyecto y Memoria Operativa de SolarUY

## Estado General del Proyecto
- **Proyecto**: SolarUY — Sistema de gestión, monitoreo en tiempo real y mantenimiento de parques fotovoltaicos.
- **Arquitectura base**: Solución .NET 9 (todos los proyectos en `net9.0`; Aspire 13 igual necesita tener instalado el SDK de .NET 10 para compilar el AppHost) con .NET Aspire (`SolarUY.AppHost`), Web Frontend (`SolarUY.Web` - Blazor Server), API de negocio (`SolarUY.ApiService`), y librerías compartidas (`SolarUY.ServiceDefaults`).
- **Persistencia**: PostgreSQL con EF Core (planificado soporte TimescaleDB para telemetría de series de tiempo).
- **Autenticación**: Keycloak OIDC containerizado en Aspire (Roles: `Administrador`, `Técnico`).
- **Fase actual**: Adopción de la metodología Spec-Driven Development (SDD), constitución aprobada y catálogo de backlog estructurado.

---

## Módulos del Sistema y Backlog (40 funcionalidades)

| Módulo | Funcionalidad | Rol Principal | Estado Spec |
| :--- | :--- | :--- | :--- |
| **Usuarios** | Iniciar Sesión / Cerrar Sesión | Todos | Pendiente |
| | Gestionar Roles de Usuario | Administrador | Pendiente |
| | Alta Técnico / Listar Técnicos | Administrador | Pendiente |
| **Parques** | Alta Parque | Administrador | **Implementada** (`001-alta-parque`) |
| | Listar Parques | Todos | **Implementada** (`002-listar-parques`) |
| | Modificar Parque | Administrador | Aprobada (`003-modificar-parque`) |
| | Baja Parque | Administrador | Aprobada (`004-baja-parque`) |
| **Nodos** | Alta / Baja / Modificar / Listar Nodos | Administrador / Todos | Pendiente |
| | Asignar Inversor a Nodo | Administrador | Pendiente |
| **Paneles** | Alta / Baja / Modificar / Listar Paneles | Administrador / Todos | Pendiente |
| | Asignar Panel a Nodo | Administrador | Pendiente |
| **Mantenimiento** | Reservar Fecha / Asignar Técnico / Cancelar / Listar | Administrador | Pendiente |
| | Ver Mis Mantenimientos / Confirmar Mantenimiento | Técnico | Pendiente |
| **Monitoreo** | Ver Generación Tiempo Real (Parque, Nodo, Panel) | Todos (SignalR) | Pendiente |
| | Ver Estado de Salud de Paneles / Recibir Alerta de Falla | Todos | Pendiente |
| **Control Inversor** | Activar / Desactivar Panel | Administrador | Pendiente |
| | Activar / Desactivar Inyección a la Red | Administrador | Pendiente |
| **Históricos** | Consultar Generación (Día, Mes, Año) | Todos | Pendiente |
| | Consultar en Lenguaje Natural (LLM) | Todos | Pendiente |
| **Simulador** | Simular Lecturas del Inversor | Sistema (Background) | Pendiente |
| | Simular Fallas de Paneles | Sistema (Mock) | Pendiente |

---

## Registro de Especificaciones SDD (`specs/`)

| Código y Nombre | Estado | Fase Actual | Tareas Completadas | Siguiente Acción |
| :--- | :--- | :--- | :--- | :--- |
| `001-alta-parque` | **Implementada** | Completada | 6 / 6 | Spec 001 finalizada. Iniciar **Spec 002** (`002-listar-parques`) |
| `002-listar-parques` | **Implementada** | Completada | 3 / 3 | Spec 002 finalizada. Iniciar **Spec 003** (`003-modificar-parque`) |
| `003-modificar-parque` | **Aprobada** | Tareas listas | 0 / 4 | Implementar después de 002 |
| `004-baja-parque` | **Aprobada** | Tareas listas | 0 / 3 | Implementar después de 003 |

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
10. **Errores de servicio**: `ServiceResult` con `Validation` → 400, `NotFound` → 404, `Conflict` → 409.
