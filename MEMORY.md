# MEMORY.md — Estado de Proyecto y Memoria Operativa de SolarUY

## Estado General del Proyecto
- **Proyecto**: SolarUY — Sistema de gestión, monitoreo en tiempo real y mantenimiento de parques fotovoltaicos.
- **Arquitectura base**: Solución .NET 9 con .NET Aspire (`SolarUY.AppHost`), Web Frontend (`SolarUY.Web` - Blazor Server), API de negocio (`SolarUY.ApiService`), y librerías compartidas (`SolarUY.ServiceDefaults`).
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
| **Parques** | Alta Parque / Baja Parque / Modificar Parque | Administrador | En especificación (`001-gestion-parques`) |
| | Listar Parques | Todos | En especificación (`001-gestion-parques`) |
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
| `001-gestion-parques` | **Aprobada** | **Tareas listas** | 0 / 10 | Iniciar **T1** (entidad + EF Core) → `dotnet test` rojo → código → verde |

---

## Decisiones Técnicas y Convenciones Activas
1. **Constitución**: Documentada y vigente en `docs/constitution.md`.
2. **Metodología SDD**: Automatizada mediante la skill `.agents/skills/sdd/SKILL.md` y comandos en `.opencode/commands/`.
3. **Flujo de desarrollo**: Spec-anchored (las specs se mantienen vivas y guían la evolución del código).
4. **Validación**: `dotnet test` para tests unitarios/integración; Chrome DevTools / navegador para componentes Blazor Server y flujos en tiempo real.
