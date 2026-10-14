---
description: SDD · Divide el plan en tareas pequeñas y verificables
agent: plan
---
A partir de specs/$1/spec.md y specs/$1/plan.md, genera specs/$1/tasks.md siguiendo el formato de la skill sdd:
- Tareas pequeñas (máximo 20-30 minutos cada una), en estricto orden de dependencia (ej. Entidades/DB -> Lógica pura -> Endpoints -> Vistas Blazor).
- Cada una con los RF que cubre y una línea "Hecho cuando:" con comprobación verificable (ej: tests en verde con `dotnet test` o verificación en navegador).
- Formato con Checkboxes (`- [ ] **Tn. Descripción...**`).
- Intenta que no sean más de 10 tareas: si salen más, propón dividir la spec en fases.
