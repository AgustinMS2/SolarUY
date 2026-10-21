---
description: SDD · Valida la spec RF por RF (tests + Chrome DevTools)
agent: build
---
Recorre specs/$1/spec.md requisito por requisito. Para cada RF indica si lo cubre un test unitario (y su resultado con `dotnet test`) o cómo se verificó manualmente. Confirma además que los tests de arquitectura están en verde. No exijas un test por cada RF.

Los RF de interfaz (componentes Blazor Server, renderizado en tiempo real SignalR, formularios) que no se cubran solo con tests unitarios, verifícalos en el navegador (usando DevTools, comprobando consola libre de errores y probando vista responsive de escritorio y móvil).

Si algún RF no está cubierto o falla, indícalo claramente. NO arregles nada todavía.
Después comprueba los criterios de finalización de la spec y entrega un veredicto formal: ¿la spec está cumplida y lista para ser marcada como 'implementada'?
