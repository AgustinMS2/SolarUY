# SolarUY

Sistema de gestión de parques solares: administración de parques, nodos, inversores y paneles, mantenimiento, monitoreo en tiempo real, estado de salud de los paneles, alertas de falla, información histórica de generación y consultas en lenguaje natural. Proyecto del Taller de Programación .NET 2026.

**Tecnología:** .NET 9, Blazor Server con SignalR, API REST con controladores, Entity Framework Core con PostgreSQL, autenticación OpenID Connect con Keycloak y orquestación con .NET Aspire. Como en esta etapa no se usa la API de un inversor real, un simulador dentro de la API ocupa su lugar.

## Cómo correrlo

Requisitos: .NET 9 SDK y Docker (Aspire levanta PostgreSQL y Keycloak como contenedores).

```
dotnet run --project SolarUY.AppHost
```

El AppHost abre el panel de Aspire; la aplicación web queda en `http://localhost:5205` y Keycloak en `https://localhost:8080` (los usuarios de prueba `admin` y `tecnico` vienen en el realm importado, en `SolarUY.AppHost/Realms`). Los datos de ejemplo (parques, nodos, inversores y paneles) se cargan al arrancar y la historia de generación se completa en segundo plano.

Para los tests: `dotnet test`.

## Documentación

- [`docs/arquitectura.md`](docs/arquitectura.md): diagramas técnicos (componentes, capas, modelo de datos, tiempo real, simulador, historia, autenticación, estados, lenguaje natural y metodología).
- [`docs/constitution.md`](docs/constitution.md): principios de diseño del proyecto.
- [`specs/`](specs/): una carpeta por funcionalidad (`spec.md`, `plan.md` y `tasks.md`).
- [`MEMORY.md`](MEMORY.md): estado del proyecto, decisiones y siguiente paso.
- [`AGENTS.md`](AGENTS.md): reglas del flujo de trabajo con IA (Spec-Driven Development).
