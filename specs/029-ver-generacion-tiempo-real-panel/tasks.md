# Tareas: Spec 029 — Ver Generación en Tiempo Real del Panel

- [x] **T1. DTO de telemetría y `PanelTelemetryService` con tests unitarios (tests primero).** RF-2, RF-3
  - Hecho cuando: los tests de la curva esperada (0 W de noche, placa a las 13:00), porcentaje de la placa, eficiencia estimada (nula con esperada 0) y rendimiento bajo (< 40 % de lo esperado, sin alerta con el 40 % exacto ni de noche) fallaron antes y pasan en verde con `dotnet test`.

- [x] **T2. Hub SignalR `PanelTelemetryHub` con suscripción por grupos y seguridad.** RF-1, RF-6
  - Hecho cuando: el hub está mapeado en `/hubs/panel-telemetry`, protegido con `[Authorize(Roles = Roles.Any)]` y los tests de arquitectura siguen en verde.

- [x] **T3. `PanelTelemetryBackgroundService` para emisión periódica de lecturas.** RF-2
  - Hecho cuando: el servicio emite cada 3-5 s lecturas simuladas a los grupos `panel-{id}` solo de paneles activos con nodo activo, y la solución compila con `dotnet test` en verde.

- [x] **T4. Pantalla Blazor `/paneles/{id:guid}/monitoreo` (`PanelMonitoring.razor`).** RF-1 a RF-6
  - Hecho cuando: en el navegador la página se conecta al hub, muestra los datos de placa y la potencia en vivo, resalta el rendimiento bajo, muestra los mensajes de panel sin nodo, inactivo, nodo inactivo e inexistente, indica "Reconectando con el panel..." al perder el enlace y se desuscribe al salir; se ve bien en celular.

- [x] **T5. Botón "Monitorear" en el listado de paneles (`PanelList.razor`).** RF-1
  - Hecho cuando: en `/paneles` cada panel activo tiene el botón "Monitorear" que abre `/paneles/{id}/monitoreo` y los paneles inactivos no lo muestran, verificado en el navegador.
