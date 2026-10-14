# Reglas SDD (Spec-Driven Development) para SolarUY

1. **La spec manda**: Antes de proponer, refactorizar o escribir cualquier código en la solución SolarUY, revisa `docs/constitution.md` y la especificación activa dentro de `specs/NNN-*/`.
2. **Prohibido saltar fases**: Sigue estrictamente el flujo: Constitución → Spec (`spec.md`) → Clarificación → Plan (`plan.md`) → Tareas (`tasks.md`) → Implementación → Validación. No pases a la siguiente fase sin la confirmación explícita del usuario.
3. **EARS estricto**: Todo requisito funcional en `spec.md` debe usar sintaxis EARS en español (`CUANDO... EL SISTEMA...`, `SI... ENTONCES EL SISTEMA...`, `MIENTRAS... EL SISTEMA...`, `EL SISTEMA...`).
4. **TDD tarea a tarea**: Implementa una única tarea a la vez. Escribe el test primero (`dotnet test` en rojo), implementa hasta verde (`dotnet test`), marca la tarea en `tasks.md` y detente para feedback del usuario.
5. **Memoria del proyecto**: Actualiza `MEMORY.md` cada vez que se complete una fase o cambie el estado de una especificación.
