---
description: SDD · Genera el plan técnico de una spec aprobada
agent: plan
---
Lee docs/constitution.md, AGENTS.md y specs/$1/spec.md. Usa la skill sdd.
NO escribas código.

Genera specs/$1/plan.md con:
- Proyectos afectados de la solución (.NET 9, SolarUY.ApiService, SolarUY.Web, SolarUY.AppHost, SolarUY.Tests).
- Entidades C#, mapeos y DbContext de EF Core (PostgreSQL).
- Funciones puras de lógica y cálculo (fechas pasadas como parámetro, sin dependencias ocultas).
- Endpoints REST / Hubs SignalR y DTOs requeridos.
- Componentes Blazor Server y maquetación de interfaz.
- Decisiones técnicas justificadas (con su alternativa descartada).
- Estrategia de tests unitarios y de integración con `dotnet test`.

Todo debe respetar la constitución y cubrir todos los RF de la spec. Marca qué RF cubre cada parte.
Si la spec no está aprobada o tiene dudas abiertas [NECESITA ACLARACIÓN], para y avísame.
