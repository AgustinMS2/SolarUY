# Directorio de Especificaciones SDD (`specs/`)

Este directorio contiene las especificaciones que guían todo el desarrollo de **SolarUY**, organizadas según la metodología **Spec-Driven Development (SDD)**.

## Convención de Nomenclatura
Cada funcionalidad o módulo especificado debe residir en su propia carpeta:
`specs/NNN-nombre-funcionalidad/` (donde `NNN` es un número secuencial de tres dígitos, p. ej. `001-alta-parque`). Una carpeta por requerimiento funcional.

## Estructura de cada Especificación
Dentro de cada carpeta deben coexistir exactamente los tres artefactos del flujo:
1. `spec.md`: Define el **QUÉ** y el **POR QUÉ** del requerimiento utilizando sintaxis EARS, historias de usuario, casos límite y criterios de aceptación. Sin detalles de código ni arquitectura.
2. `plan.md`: Define el **CÓMO**. Arquitectura, modelos de EF Core, endpoints de API REST, componentes Blazor Server y estrategia de testing con `dotnet test`.
3. `tasks.md`: Lista desglosada de tareas atómicas (20-30 min) con checkboxes y criterios verificables "Hecho cuando:".

## Comandos Disponibles
- `/sdd-spec <NNN-nombre> <idea inicial>` : Entrevista interactiva para generar el borrador de `spec.md`.
- `/sdd-clarify <NNN-nombre>` : Revisión estilo QA para detectar ambigüedades y vacíos.
- `/sdd-plan <NNN-nombre>` : Generación del plan técnico a partir de una spec aprobada.
- `/sdd-tasks <NNN-nombre>` : Desglose del plan en tareas atómicas.
- `/sdd-implement <NNN-nombre> <Tn>` : Implementación guiada por TDD de una única tarea.
- `/sdd-validate <NNN-nombre>` : Validación requisito por requisito.
- `/sdd-status <NNN-nombre>` : Consulta del estado actual y siguiente paso.
- `/sdd-change <NNN-nombre> <nuevo requisito>` : Adaptación de la spec ante cambios de alcance.
