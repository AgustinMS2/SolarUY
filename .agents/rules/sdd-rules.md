# Reglas SDD (Spec-Driven Development) para SolarUY

1. **La spec manda**: Antes de proponer, refactorizar o escribir cualquier código en la solución SolarUY, revisa `docs/constitution.md` y la especificación activa dentro de `specs/NNN-*/`.
2. **Prohibido saltar fases**: Sigue estrictamente el flujo: Constitución → Spec (`spec.md`) → Clarificación → Plan (`plan.md`) → Tareas (`tasks.md`) → Implementación → Validación. No pases a la siguiente fase sin la confirmación explícita del usuario.
3. **EARS estricto**: Todo requisito funcional en `spec.md` debe usar sintaxis EARS en español (`CUANDO... EL SISTEMA...`, `SI... ENTONCES EL SISTEMA...`, `MIENTRAS... EL SISTEMA...`, `EL SISTEMA...`).
4. **Una tarea a la vez, tests pragmáticos**: Implementa una única tarea a la vez. Solo las reglas de negocio de los servicios llevan tests unitarios (primero en rojo, luego en verde). Los tests de arquitectura deben seguir en verde siempre. Sin tests de integración, Testcontainers ni bUnit salvo pedido explícito. Marca la tarea en `tasks.md` y detente para feedback del usuario.
5. **Memoria del proyecto**: Actualiza `MEMORY.md` cada vez que se complete una fase o cambie el estado de una especificación.
6. **Reglas generales en la constitución**: No crear tablas de decisiones por caso de uso. Las reglas que aplican a todo el proyecto van en `docs/constitution.md`, sin mezclar niveles de abstracción.
