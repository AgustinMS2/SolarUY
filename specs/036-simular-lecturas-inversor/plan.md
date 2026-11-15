# Plan Técnico: Spec 036 — Simular Lecturas del Inversor

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Simulation/SolarCalendar.cs`: calendario solar puro de Uruguay: `Sunrise(fecha)`, `Sunset(fecha)`, `IsDaylight(fecha y hora)` y `Factor(fecha y hora)` (la curva sinusoidal entre amanecer y atardecer).
  - `Simulation/DeterministicNoise.cs`: ruido suave y reproducible a partir de una semilla (equipo o parque) y un instante.
  - `Simulation/SolarSimulationEngine.cs` e `ISolarSimulationEngine`: lecturas de inversor, panel y parque como funciones puras del equipo y del instante, y energía acumulada del día. Registrado como singleton.
  - `Simulation/MinuteAggregator.cs`: resumen de un minuto de un inversor a partir de las lecturas del motor (una cada 10 s).
  - `Simulation/SolarSimulationEngine.cs`: además, `ParkHourSummary` (energía y pico del parque en una hora, con las mismas muestras de 10 s).
  - `Simulation/HistoryGapPlanner.cs`: calcula qué minutos (por inversor) y qué horas (por parque) faltan por guardar.
  - `Simulation/SimulationClock.cs`: redondea el instante al intervalo y calcula la espera hasta el próximo ciclo, para que todos los servicios en vivo emitan sobre el mismo instante.
  - `Services/SimulationEmitterBackgroundService.cs`: base de los servicios en vivo de las specs 027 a 030 (intervalo, pausa por `Simulation:Enabled` y reloj compartido).
  - `Simulation/SimulationOptions.cs`: `Simulation:Enabled`, `Simulation:IntervalSeconds` (5) y `Simulation:BackfillDays` (30).
  - `Domain/InverterMinuteReading.cs`, `Data/SolarUyDbContext.cs` y la migración `AddInverterMinuteReadings`.
  - `Repositories/IInverterMinuteReadingRepository.cs` y `InverterMinuteReadingRepository.cs`.
  - `Repositories/IParkGenerationRepository.cs` y `ParkGenerationRepository.cs` (de la spec 034): se agregan `AddMissing` (idempotente, por la clave única parque + fecha + hora) y `GetLastHourByPark`; el repositorio falso de tests se actualiza.
  - `Repositories/IHistoryCatchUpLock.cs` y `HistoryCatchUpLock.cs`: candado de PostgreSQL (`pg_try_advisory_lock`) para que, con varias copias de la API, solo una complete la historia a la vez; las demás esperan al minuto siguiente. Si una copia se cae, la base libera el candado.
  - `Services/ISimulationHistoryService.cs` y `SimulationHistoryService.cs`: guarda minutos y horas (idempotente) y completa la historia de ambas tablas.
  - `Services/SimulationHistoryBackgroundService.cs`: completa la historia al arrancar (en lotes, sin bloquear) y guarda cada minuto y cada hora cerrados.
  - Se eliminan `Services/HistoricalGenerationSeeder.cs` e `IHistoricalGenerationSeeder.cs` (spec 034) y su llamada en `Program.cs`; se ajusta el texto de la T3 de `specs/034-*/tasks.md`.
  - `Data/DbInitializer.cs`: la fecha de alta de los inversores de ejemplo pasa a hace 2 años.
  - Cambios en servicios de otras specs (los que dejan de inventar valores): `ParkTelemetryBackgroundService` (027), `NodeTelemetryBackgroundService` (028), `PanelTelemetryBackgroundService` (029), `ParkHealthSnapshotBuilder` (030). Se elimina `PanelReadingSimulator`.
  - Cambios por el calendario solar (el horario de sol deja de ser fijo): `ParkTelemetryService` (027, `IsDaylight` por fecha), `PanelTelemetryService.CalculateExpectedPowerW` (029, recibe fecha y hora locales en lugar de solo la hora) y `PanelHealthService.ClassifyHealth` (030, ídem).
  - `ParkTelemetryService` (027): parámetro opcional `hasEquipment` para que un parque sin inversores no dispare la alerta de generación baja.
  - `Program.cs`: registros y lectura de las opciones.
- `SolarUY.Web`: en las pantallas de monitoreo de las specs 027, 028 y 029, el rótulo "Actualización 3s" pasa a "En vivo".
- `specs/029-*/spec.md` y `specs/030-*/spec.md`: el texto "entre las 6:00 y las 20:00" pasa a "entre el amanecer y el atardecer de la fecha (spec 036)".
- `SolarUY.Tests`: `SolarCalendarTests`, `DeterministicNoiseTests`, `SolarSimulationEngineTests`, `MinuteAggregatorTests`, `HistoryGapPlannerTests`, `SimulationHistoryServiceTests` y un repositorio falso.

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Nueva tabla `InverterMinuteReadings`** con migración `AddInverterMinuteReadings`:
  - Clave primaria compuesta `(InverterId, MinuteUtc)`: garantiza un solo registro por inversor y minuto (RF-7).
  - `SolarParkId` y `SolarNodeId`: el parque y el nodo del inversor en ese momento (la historia conserva su atribución).
  - `MinuteUtc` (inicio del minuto, UTC), `AvgPowerKw`, `MaxPowerKw`, `EnergyKwh`, `AvgDcVoltageV`, `AvgDcCurrentA`, `AvgAcVoltageV`, `AvgFrequencyHz`, `AvgTemperatureC`, `MaxTemperatureC`, `SampleCount`, `AnomalousSamples`.
  - Índices `(SolarParkId, MinuteUtc)` y `(SolarNodeId, MinuteUtc)` para las consultas por período de la spec 034.
- Volumen: 1440 filas por inversor por día. Con los 4 inversores de ejemplo, unos 173 mil registros para los 30 días iniciales.
- El guardado agrega los minutos que faltan: consulta los existentes del rango, inserta el resto y, si otra copia insertó el mismo minuto en el medio (conflicto de clave), vuelve a consultar qué existe y reintenta solo con lo que falta (hasta 4 veces el lote completo y, como última defensa, de a un registro), sin dejar huecos.

### La historia de la spec 034 (decidido el 2026-11-15: opción A)
La spec 034 se implementó el mismo día con **su propia tabla** `ParkHourlyGenerations` (una fila por parque y hora: `EnergyGeneratedKwh` y `PeakPowerKw`, con clave única `(SolarParkId, DateLocal, HourLocal)`) y un cargador `HistoricalGenerationSeeder` con una fórmula propia, sin relación con el motor. Para que sus gráficos coincidan con las lecturas en vivo:
- El servicio de historia también escribe `ParkHourlyGenerations` a partir del motor, una fila por parque activo con inversores y por hora, y solo agrega las horas que faltan (idempotente por la clave única).
- Su cargador se reemplaza por esa historia, que cubre desde el 1 de enero del año anterior; su servicio de consultas, sus pantallas y sus tests no cambian.
- Volumen: 8760 filas por año por parque, unas 17 mil para el año actual y el anterior.

---

## Lógica de Dominio y Reglas de Negocio (Puras)

- **`SolarCalendar`** (Uruguay, hora oficial UTC−3, mediodía solar a las 12:45): duración del día `D(día del año) = 12,2 + 2,1 × cos(2π × (día − 355) / 365)` horas, es decir de unas 10,1 horas (21 de junio) a unas 14,3 (21 de diciembre); `amanecer = 12,75 − D/2` y `atardecer = 12,75 + D/2`. `Factor = sen(π × (hora − amanecer) / (atardecer − amanecer))` entre ambos y 0 fuera. Ejemplos: 15 de noviembre de 05:48 a 19:42; 21 de junio de 07:42 a 17:48. Esta función la usan el motor, las horas de sol de la spec 027 y la potencia esperada del panel (specs 029 y 030).
- **`DeterministicNoise`**: ruido en [−1, 1] derivado por un hash de `(semilla, instante)` e interpolado entre puntos de control cada minuto, por lo que cambia de forma gradual y es reproducible.
- **Lectura del inversor** (`ReadInverter(inversorId, parqueId, capacidadNominalKw, instante)`):
  - Potencia = capacidad × curva × nubosidad del parque (±5 a 10 %) × pequeña variación propia, acotada entre 0 y la capacidad.
  - Voltaje DC: bajo y residual de noche; de día, 740 V menos la caída por carga, con variación leve y siempre entre 200 y 1000 V.
  - Corriente DC = potencia / voltaje DC. Voltaje AC ≈ 380 V con variación mínima.
  - Frecuencia: 50 Hz con variación leve y desvíos ocasionales fuera de 50 ± 0,5 Hz, reproducibles.
  - Temperatura = ambiente del día + calentamiento por carga; cerca del mediodía y a plena carga supera los 75 °C una fracción de las lecturas, sin llegar a 85 °C.
- **Lectura del panel** (`ReadPanelPowerW`): potencia de placa × curva × nubosidad del parque × variación propia, con sombreados ocasionales reproducibles para ejercitar la alerta de rendimiento bajo.
- **Potencia del parque** = suma de la potencia de los inversores activos; un parque sin inversores da 0.
- **Energía acumulada del día** = suma de las muestras de 10 s desde las 00:00 locales hasta el instante, cada una × 10 s / 3600; para los minutos completos coincide con la suma de los resúmenes por minuto.
- **`ParkHourSummary`**: para una hora completa, recorre las 360 muestras de 10 s sumando la potencia de los inversores del parque; la energía es la suma de cada muestra × 10 s / 3600 y el pico es el máximo de la potencia del parque. Las horas enteras de noche no se muestrean (energía y pico 0).
- **`MinuteAggregator`**: toma las lecturas de las 6 muestras del minuto (cada 10 s) y arma la fila (medias, pico, energía = potencia media / 60, cantidad de muestras y de anómalas).
- **`HistoryGapPlanner`**: dados el último minuto (o la última hora) guardado, la fecha de alta, el instante actual y el comienzo de la ventana (30 días hacia atrás para los minutos y el 1 de enero del año anterior para las horas), devuelve el rango faltante: desde `max(comienzo de la ventana, alta, último + 1)` hasta el último minuto (u hora) completo.
- **Servicio de historia**: guarda los minutos de los inversores activos (nodo y parque activos, RF-4) y las horas de los parques activos con inversores, omite parques sin inversores y no duplica (RF-7). En cada hora solo suman los inversores dados de alta hasta entonces.
- Las funciones no usan estado compartido ni azar propio de la instancia, por lo que dos copias de la API calculan lo mismo (RF-6).

---

## API y Comunicación en Tiempo Real

- No hay endpoints nuevos ni hubs nuevos: los hubs de las specs 027 a 030 siguen emitiendo, ahora con valores del motor.
- **Cada servicio en segundo plano** pasa a leer su intervalo de `SimulationOptions` y se detiene si `Simulation:Enabled` es falso.
- **`SimulationEmitterBackgroundService`** (nuevo, base de los cuatro servicios en vivo de las specs 027 a 030): todos despiertan en los mismos instantes, múltiplos de `Simulation:IntervalSeconds` (`SimulationClock`), y calculan sobre ese mismo instante, que viaja como `TimestampUtc` del paquete; por eso las pantallas muestran los mismos números.
- **`SimulationHistoryBackgroundService`**: al arrancar completa la historia (primero la horaria, que consulta la spec 034, y después la de minutos) en lotes de unos 2000 registros, sin bloquear el inicio de la API, y luego, cada minuto, guarda el minuto recién cerrado de cada inversor activo y, al cerrar cada hora, la hora del parque.
- Spec 027: la energía acumulada del día sale de `EnergyTodayKwh`. Un parque sin inversores no emite potencia inventada ni la alerta de generación baja.
- Spec 031: la detección de alertas no cambia; usa las mismas banderas que calculan los servicios de telemetría, ahora con valores del motor.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- Sin pantallas nuevas.
- Se cambia el rótulo "Actualización 3s" por "En vivo" en las pantallas de monitoreo de parque, nodo y panel, porque la frecuencia pasa a ser configurable.

---

## Estrategia de Testing

- **Unit tests (TDD, rojo → verde)**:
  - `SolarCalendarTests`: amanecer y atardecer en los solsticios y a fines de octubre (de 06:00 a 19:30), duración del día siempre entre 10,1 y 14,3 horas, 0 de noche y en el borde, 1 al mediodía solar (12:45) y simétrica alrededor.
  - Se actualizan los tests existentes de las specs 027, 029 y 030 (`ParkTelemetryServiceTests`, `PanelTelemetryServiceTests` y `PanelHealthServiceTests`) para usar fechas y horas del calendario; se escriben primero y deben fallar antes del cambio.
  - `DeterministicNoiseTests`: el mismo (semilla, instante) da el mismo valor, está acotado a [−1, 1] y cambia de forma suave entre instantes cercanos.
  - `SolarSimulationEngineTests`: determinismo; de noche potencia 0 y voltaje DC residual; de día la potencia nunca supera la capacidad y se acerca a ella al mediodía; temperatura en un rango verosímil; la potencia del parque es la suma de sus inversores y 0 sin inversores; la energía del día coincide con la suma de los resúmenes por minuto.
  - `MinuteAggregatorTests`: medias, pico, energía, cantidad de muestras y de anómalas.
  - `ParkHourSummary` (en `SolarSimulationEngineTests`): la energía de la hora es la suma de los resúmenes por minuto, el pico es el máximo de la potencia del parque y las horas de noche valen 0.
  - `HistoryGapPlannerTests`: historia vacía (30 días y año anterior), historia al día (nada), hueco por reinicio, equipo nuevo (desde su alta) y límites de la ventana, para minutos y para horas.
  - `SimulationHistoryServiceTests`: guarda un registro por inversor activo y minuto y uno por parque y hora, guarda dos veces sin duplicar, omite nodos o parques inactivos y parques sin inversores, solo suma los inversores dados de alta hasta cada hora y completa los huecos de ambas tablas.
- **Tests de arquitectura**: en verde (sin controladores nuevos).
- **Verificación con la aplicación corriendo**: con una base real y el navegador, las pantallas de las specs 027 a 030 muestran valores coherentes entre sí, los paquetes llegan de forma continua, las dos tablas de historia se completan al arrancar (30 días por minuto y desde el año anterior por hora) y suman registros cada minuto y cada hora, y los gráficos de la spec 034 coinciden con las lecturas en vivo. Con dos copias de la API sobre la misma base, no aparecen duplicados.
