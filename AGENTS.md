# AGENTS.md — Reglas y Contexto para Agentes de IA en SolarUY

SolarUY es una plataforma integral para el monitoreo, gestión y control de parques solares fotovoltaicos, nodos, inversores y paneles.
Stack tecnológico: .NET 9, C#, Blazor Server con SignalR, .NET Aspire, Entity Framework Core con PostgreSQL y autenticación OpenID Connect con Keycloak.

## Comandos del Proyecto
- **Ejecutar tests**: `dotnet test`
- **Ejecutar solución completa con Aspire (Dashboard, Web, API)**: `dotnet run --project SolarUY.AppHost`
- **Crear migración de base de datos**: `dotnet ef migrations add <NombreMigracion> --project SolarUY.ApiService`
- **Aplicar migraciones**: `dotnet ef database update --project SolarUY.ApiService`

## Reglas Innegociables
- **La spec manda**: Lee obligatoriamente `docs/constitution.md` y la especificación activa (`specs/NNN-*/`) antes de tocar código. Si una funcionalidad o comportamiento no figura en la spec aprobada, **no se implementa**. Ante cualquier duda, detente y pregunta.
- **Flujo SDD riguroso**: Constitución → Spec (`spec.md`) → Clarificación → Plan (`plan.md`) → Tareas (`tasks.md`) → Implementación → Validación → Cambio. No saltes ninguna fase sin aprobación explícita del usuario.
- **Foco único y tests pragmáticos**: En la fase de implementación, trabaja **MÁXIMO TRES tareas a la vez** (ej. T1). Solo las tareas con reglas de negocio en servicios llevan tests unitarios (escritos primero, en rojo y luego en verde). El resto de las tareas no agrega tests: basta con compilar y que `dotnet test` (incluidos los tests de arquitectura) siga en verde. Marca la tarea en `tasks.md` y **PÁRATE**. No continúes con la siguiente tarea automáticamente.
- **Alcance controlado**: Pide y especifica funcionalidades chicas (ej. primero el CRUD de parques, después el de nodos). No generes specs amplias tipo "gestión de X" de una sola vez.
- **Verificación UI / Blazor**: No hay tests automáticos para todos los componentes de interfaz. Después de cada cambio de UI, verifica en el navegador (utilizando herramientas de inspección o Chrome DevTools): abre la aplicación, prueba la interacción y reactividad de SignalR, revisa la consola por errores y comprueba la vista responsive (escritorio y móvil).
- **Persistencia de Estado**: Al finalizar cada fase del flujo SDD, actualiza `MEMORY.md` reflejando el progreso del proyecto, las tareas completadas y el siguiente paso a ejecutar.
