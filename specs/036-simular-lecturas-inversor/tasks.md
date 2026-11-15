# Tareas: Spec 036 — Simular Lecturas del Inversor

- [x] **T1. Calendario solar estacional y curva solar (tests primero).** RF-2, RF-6
  - Hecho cuando: los tests de `SolarCalendar` (amanecer y atardecer por fecha en solsticios y a fines de octubre, duración del día entre 10,1 y 14,3 horas, 0 de noche, 1 al mediodía solar y simetría) fallaron antes y pasan en verde con `dotnet test`.

- [x] **T2. Ruido determinista y motor de lecturas (tests primero).** RF-2, RF-6
  - Hecho cuando: los tests de `DeterministicNoise` y `SolarSimulationEngine` (determinismo, noche, tope de capacidad, rango de temperatura, potencia del parque como suma de inversores y energía del día) fallaron antes y pasan en verde.

- [x] **T3. Entidad `InverterMinuteReading`, migración y repositorio.** RF-3, RF-7
  - Hecho cuando: la migración crea la tabla con clave `(InverterId, MinuteUtc)` y sus índices (verificado en una base Postgres descartable), el repositorio guarda los minutos que faltan sin duplicar y la solución compila con `dotnet test` en verde.

- [x] **T4. Resúmenes por minuto y por hora y planificador de huecos (funciones puras, tests primero).** RF-3, RF-8
  - Hecho cuando: los tests de `MinuteAggregator`, `ParkHourSummary` (energía igual a la suma de los minutos, pico, noche) y `HistoryGapPlanner` (historia vacía, hueco, equipo nuevo, límites de la ventana, para minutos y para horas) fallaron antes y pasan en verde.

- [x] **T5. Servicio de historia que guarda las dos tablas (tests primero).** RF-3, RF-4, RF-7, RF-8
  - Hecho cuando: los tests de `SimulationHistoryService` (un registro por inversor y minuto y uno por parque y hora, guardado doble sin duplicados, nodos y parques inactivos, parque sin inversores, inversores dados de alta a mitad de período, relleno de huecos) fallaron antes y pasan en verde, con `AddMissing` y `GetLastHourByPark` agregados al repositorio de la spec 034 y a su repositorio falso.

- [x] **T6. Servicio en segundo plano de historia, opciones y reemplazo del cargador de la spec 034.** RF-1, RF-3, RF-8
  - Hecho cuando: con la aplicación corriendo sobre una base real se completan en segundo plano los 30 días por minuto y la historia horaria desde el año anterior sin demorar el inicio de la API, se agregan registros cada minuto y cada hora, `Simulation:Enabled` en falso detiene todo, dos copias de la API sobre la misma base no generan duplicados, el cargador de la spec 034 ya no existe y la tabla que lee la 034 se llena con la historia horaria del motor (la pantalla de la 034 se revisa en el navegador en la T8).

- [x] **T7. Conectar las specs 027 y 028 al motor y al calendario.** RF-2, RF-4, RF-6
  - Hecho cuando: el parque y el nodo emiten lecturas del motor con el intervalo configurado y con las horas de sol del calendario, la energía del día es la suma real, un parque sin inversores no emite ni dispara la alerta de generación baja, los tests de las specs 027, 028 y 031 (actualizados al calendario los de la 027) están en verde y los rótulos de esas pantallas dicen "En vivo".

- [x] **T8. Conectar las specs 029 y 030 al motor y al calendario y eliminar el simulador duplicado.** RF-2, RF-6
  - Hecho cuando: el panel y el mapa de salud usan el motor y el calendario (se elimina `PanelReadingSimulator`), los textos de las specs 029 y 030 dicen "entre el amanecer y el atardecer de la fecha", sus tests actualizados están en verde y, en el navegador, las pantallas de las specs 027 a 030 muestran valores coherentes entre sí para el mismo instante y reciben paquetes de forma continua, y la pantalla de históricos de la spec 034 muestra la historia del motor con la estacionalidad.
