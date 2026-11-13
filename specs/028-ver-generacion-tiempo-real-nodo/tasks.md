# Tareas: Spec 028 — Ver Generación en Tiempo Real del Nodo

- [x] **T1. DTO de telemetría y `NodeTelemetryService` con tests unitarios (tests primero).** RF-2, RF-3, RF-4
  - Hecho cuando: las pruebas de sobretemperatura (> 75 °C), anomalías de frecuencia (< 49.5 o > 50.5 Hz) y supresión para nodos inactivos/sin inversor pasen en verde con `dotnet test`.

- [x] **T2. Hub SignalR `NodeTelemetryHub` con suscripción por grupos y seguridad.** RF-1, RF-7
  - Hecho cuando: el hub esté mapeado en `/hubs/node-telemetry`, protegido con `[Authorize(Roles = Roles.Any)]` y los tests de arquitectura continúen en verde.

- [x] **T3. `NodeTelemetryBackgroundService` para emisión periódica de lecturas.** RF-2
  - Hecho cuando: el servicio en segundo plano emita periódicamente (cada 3-5 s) la telemetría calculada a los grupos SignalR `node-{nodeId}` para nodos activos con inversor.

- [x] **T4. Pantalla Blazor `/nodos/{id:guid}/monitoreo` (`NodeMonitoring.razor`).** RF-1, RF-2, RF-3, RF-4, RF-5, RF-6, RF-7
  - Hecho cuando: la página se conecte al hub, presente las 6 tarjetas eléctricas, renderice la mini-gráfica SVG de potencia reactiva, refleje alertas de sobretemperatura/frecuencia y gestione la desconexión.

- [x] **T5. Botón de acceso directo en el listado de nodos (`SolarNodeList.razor`).** RF-1
  - Hecho cuando: en `/nodos` cada nodo cuente con el botón "Monitorear" y navegue a `/nodos/{id}/monitoreo`.

