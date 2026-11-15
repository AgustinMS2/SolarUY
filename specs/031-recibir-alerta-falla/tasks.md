# Tareas: Spec 031 — Recibir Alerta de Falla

- [x] **T1. Entidad `Alert`, enums, migración `AddAlerts` y `IAlertRepository`.** RF-1
  - Hecho cuando: la migración crea la tabla `Alerts` con sus índices, el repositorio con su implementación EF y el repositorio falso de tests compilan y `dotnet test` sigue en verde.

- [x] **T2. `AlertService`: registro, deduplicado y consolidación (tests primero).** RF-1, RF-2, RF-6
  - Hecho cuando: los tests de alerta nueva, duplicado a los 59 s y a los 61 s, consolidación a la tercera detección, contador de instancias y claves distintas fallaron antes y pasan en verde.

- [x] **T3. `AlertService`: reconocer, descartar, contador y purga (tests primero).** RF-3, RF-4, RF-5, RF-7
  - Hecho cuando: los tests de contador de Pendientes, reconocer (idempotente), descartar (solo Reconocidas, 409), alerta inexistente y purga (solo Descartadas nocturnas de más de 24 h) fallaron antes y pasan en verde.

- [x] **T4. Reglas de detección y `AlertPublisher`; llamadas desde los servicios de telemetría.** RF-1, RF-2
  - Hecho cuando: los tests de las reglas de detección (inversor > 75 °C, parque bajo su umbral, panel en Falla Crítica) fallaron antes y pasan en verde, los servicios en segundo plano de las specs 027, 028 y 030 llaman al publicador, y con la aplicación corriendo se registran alertas Advertencia reales sin duplicados en 60 s.

- [x] **T5. `AlertsHub`, `AlertsController` y `AlertPurgeBackgroundService`.** RF-3, RF-4, RF-5, RF-7
  - Hecho cuando: `GET /api/alerts`, `acknowledge` y `dismiss` responden 200/404/409 según el caso con los roles definidos, el hub `/hubs/alerts` emite los eventos `New`, `Updated` y `StatusChanged`, la purga corre cada hora, y `dotnet test` (con la excepción explícita de reconocer en los tests de arquitectura) sigue en verde.

- [x] **T6. Agrupación de avisos en ráfaga (tests primero).** RF-8
  - Hecho cuando: los tests de 4 alertas (avisos individuales), 5 alertas del mismo parque (un aviso "Se detectaron N fallas simultáneas en <Parque>") y parques distintos fallaron antes y pasan en verde.

- [x] **T7. Campana, centro de alertas y avisos en la barra superior (`AlertBell.razor`).** RF-3, RF-8
  - Hecho cuando: en el navegador la campana muestra el contador de Pendientes en vivo, el centro de alertas lista las alertas con su color, los avisos aparecen con cada alerta nueva y desaparecen a los 8 s o al cerrarlos, la campana sigue funcionando al navegar entre pantallas y se ve bien en celular.

- [x] **T8. Detalle de la alerta, "Agendar Mantenimiento" y "Descartar".** RF-4, RF-7
  - Hecho cuando: en el navegador, al hacer clic en una alerta se reconoce y se abre su detalle; el Administrador ve "Agendar Mantenimiento" en las alertas de panel (abre `/mantenimientos/nuevo` con el panel y el motivo pre-cargados) y "Descartar" en las reconocidas; el Técnico no ve esos botones; la campana de todos los usuarios baja al reconocer o descartar.
