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
- Controladores REST (`ControllerBase` en `Controllers/`) / Hubs SignalR y DTOs requeridos. Servicios y repositorios sincrónicos.
- Componentes Blazor Server y maquetación de interfaz.
- Decisiones específicas de esta spec solo si se apartan de la constitución (pocas líneas, sin tabla, sin mezclar niveles de abstracción).
- Estrategia de tests: tests de arquitectura + tests unitarios solo de las reglas de negocio del servicio. Sin tests de integración, Testcontainers ni bUnit salvo pedido explícito.

Todo debe respetar la constitución y cubrir todos los RF de la spec. Marca qué RF cubre cada parte.
Si la spec no está aprobada o tiene dudas abiertas [NECESITA ACLARACIÓN], para y avísame.
