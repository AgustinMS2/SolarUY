# Tareas: Spec 037 — Simular Fallas de Paneles

- [x] **T1. Catálogo de fallas y línea de tiempo (reglas puras, tests primero).** RF-3, casos límite
  - Hecho cuando: los tests de `FaultCatalog` (equipo, severidad y etiqueta de los 4 tipos) y de `FaultTimeline` (prevalencia de la crítica, límites de inicio y fin, equipos sin fallas, línea vacía) fallaron antes y pasan en verde con `dotnet test`.

- [x] **T2. Efecto de las fallas en el simulador (tests primero).** RF-2, RF-7
  - Hecho cuando: los tests del motor con fallas (corte total en 0 W, degradación entre 10 % y 40 % de lo esperado, sobretemperatura entre 86 y 90 °C con la misma potencia, aislamiento con todo en cero, cambio exacto al inicio y al fin) y los del resumen por minuto, el resumen horario y la energía del día con fallas fallaron antes y pasan en verde, y los tests existentes del motor siguen en verde sin fallas.

- [x] **T3. Entidad `SimulatedFault`, migración y repositorio.** RF-4, RF-5
  - Hecho cuando: la migración `AddSimulatedFaults` crea la tabla con su índice (verificado en una base Postgres descartable), el repositorio agrega, lista y termina fallas por equipo y todas a la vez (con un repositorio falso para los tests) y la solución compila con `dotnet test` en verde.

- [x] **T4. Servicio de fallas simuladas y configuración (tests primero).** RF-1, RF-4, RF-5, RF-6
  - Hecho cuando: los tests de `SimulationFaultService` (inyección válida con el mensaje de la spec, tipo que no corresponde, equipo inexistente o que no emite, falla duplicada, simulador deshabilitado, restablecer uno y todos, listado) fallaron antes y pasan en verde, y `Simulation:FaultInjectionEnabled` queda en `true` en desarrollo y `false` por defecto.

- [x] **T5. Conectar las lecturas, las alertas y la historia a las fallas (tests primero).** RF-2, RF-3, RF-7
  - Hecho cuando: los tests de las reglas de alerta nuevas (aislamiento Crítica, degradación Advertencia), de la falla copiada a la lectura del nodo (con el inversor apagado sin alarmas espurias) y del panel y del mapa de salud fallaron antes y pasan en verde, y los servicios de emisión de las specs 027 a 030 y la historia usan las fallas de la base; con la aplicación corriendo, una falla inyectada por SQL se refleja en la siguiente lectura y en los registros por minuto.

- [x] **T6. Controlador `SimulationFaultsController`.** RF-1, RF-4, RF-5, RF-8
  - Hecho cuando: los cuatro endpoints están expuestos solo para el Administrador, los errores devuelven `{ message }` con 400, 404, 409 y 503 según corresponda, los tests de arquitectura siguen en verde y la solución compila.

- [x] **T7. Web: cliente y pantalla `/simulacion-fallas`.** RF-1, RF-4, RF-5, RF-6, RF-8
  - Hecho cuando: el Administrador ve la pantalla con la etiqueta "ENTORNO DE SIMULACIÓN Y PRUEBAS", inyecta y restablece fallas con los mensajes de la spec, la entrada del menú solo aparece para el Administrador y la pantalla se ve bien en escritorio y móvil.

- [x] **T8. Web: insignia "En Falla" en el panel y el nodo, y motivo en el mapa de salud.** RF-2
  - Hecho cuando: las pantallas de panel (029), nodo (028) y mapa de salud (030) muestran la falla activa en la siguiente lectura y la quitan al restablecerla.

- [x] **T9. Verificación del ciclo completo, documentación y estado del proyecto.** RF-3, RF-7
  - Hecho cuando: en el navegador el ciclo inyectar → aviso de alerta → mapa en rojo → restablecer → mapa en verde funciona (también la degradación en ámbar, la sobretemperatura y la falla de aislamiento), la falla de aislamiento queda en la historia, el Técnico no accede, no hay errores en la consola, y se actualizan los textos de las specs 029, 030 y 031 y `MEMORY.md`.
