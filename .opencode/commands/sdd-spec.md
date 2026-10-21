---
description: SDD · Entrevista y genera la spec (uso - /sdd-spec 002-nombre idea inicial)
agent: plan
---
NO escribas código en ningún momento. Lee docs/constitution.md y MEMORY.md, y usa la skill sdd.

Carpeta de la spec: specs/$1/
Idea inicial (el texto que va después del nombre de la carpeta): $ARGUMENTS

Tu trabajo:
0. Si la idea abarca más de un requerimiento funcional (ej. "gestión de X" o "CRUD de X"), PARA y propón dividirla en una spec por requerimiento (Alta, Listar, Modificar, Baja…).
1. Hazme preguntas de UNA en UNA para eliminar ambigüedades (casos límite, errores, qué queda fuera de esta versión, actores/roles de SolarUY involucrados). Máximo 5 preguntas.
2. Con mis respuestas, genera specs/$1/spec.md con los campos y sus reglas, los mensajes exactos al usuario y el comportamiento de la pantalla, siguiendo la plantilla de la skill sdd, con los requisitos funcionales en notación EARS y "Estado: borrador".
3. Solo el QUÉ y el POR QUÉ: nada de stack técnico, C#, tablas de EF Core ni nombres de archivos (eso irá en el plan).
