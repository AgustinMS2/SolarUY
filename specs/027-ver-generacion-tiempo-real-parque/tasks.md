# Tareas: Spec 027 — Ver Generación en Tiempo Real del Parque

- [x] **T1. DTO de telemetría y `ParkTelemetryService` con tests unitarios (tests primero).** RF-1, RF-3
  - Hecho cuando: los tests de cálculo de porcentaje de capacidad, alerta diurna ante caída bajo umbral y supresión nocturna/inactiva pasan en verde con `dotnet test`.

- [x] **T2. Hub SignalR `ParkTelemetryHub` con suscripción por grupos y seguridad.** RF-1, RF-5
  - Hecho cuando: el hub está mapeado en `/hubs/park-telemetry`, protegido con `[Authorize(Roles = Roles.Any)]` y los tests de arquitectura siguen en verde.

- [x] **T3. `ParkTelemetryBackgroundService` para emisión periódica de lecturas.** RF-2
  - Hecho cuando: el servicio en segundo plano se ejecuta periódicamente (cada 3-5 s) y transmite las métricas calculadas a los grupos del hub SignalR.

- [x] **T4. Pantalla Blazor `/parques/{id:guid}/monitoreo` (`ParkMonitoring.razor`).** RF-1, RF-2, RF-3, RF-4, RF-5
  - Hecho cuando: la página se conecta al hub, dibuja el tacómetro SVG reactivo, actualiza las tarjetas de métricas en vivo, muestra alertas ante caída bajo umbral, refleja el estado del enlace y se desuscribe al salir.

- [x] **T5. Botón de acceso directo en el listado de parques (`SolarParkList.razor`).** RF-1
  - Hecho cuando: en `/parques` cada parque activo cuenta con el botón "Monitorear" que navega a su monitor en vivo y se verifica en el navegador.

