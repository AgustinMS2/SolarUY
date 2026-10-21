---
description: SDD · Implementa UNA tarea, tests primero (uso - /sdd-implement 002-nombre T3)
agent: build
---
Implementa SOLO la tarea $2 de specs/$1/tasks.md, siguiendo specs/$1/plan.md, docs/constitution.md y la skill sdd.

1. Si la tarea implementa reglas de negocio en un servicio: escribe primero los tests unitarios de esas reglas y comprueba que fallan con `dotnet test`. Para cualquier otra tarea (entidad, migración, repositorio, controlador, DTO, UI) NO agregues tests nuevos.
2. Escribe el código mínimo necesario en la solución SolarUY.
3. Ejecuta `dotnet test` (incluye los tests de arquitectura) y muéstrame el resultado en verde. Si es UI, verifícala en el navegador.
4. Marca la tarea $2 como hecha (`- [x]`) en specs/$1/tasks.md e indica qué RF cubre.

Después PÁRATE INMEDIATAMENTE. No empieces la siguiente tarea.
