# Spec 036 — Simular Lecturas del Inversor

Estado: aprobada (2026-11-15), en implementación
Depende de: Spec 013 (Inversor a Nodo)
Alimenta a: Specs 027, 028, 029 y 030 (las lecturas en vivo y el horario de sol pasan a salir de este motor), Spec 034 (sus gráficos pasan a mostrar la historia horaria que se guarda aquí) y Spec 037 (inyectará fallas sobre este motor)

## Contexto y objetivo
La letra del taller indica que en esta instancia no se interactúa con la API local del inversor real sino con un **mock o simulador del Inversor**. Esta spec lo implementa como un **motor único de simulación** que calcula lecturas telemétricas continuas y verosímiles de los inversores (curva solar de Uruguay, temperatura ambiente y nubosidad pasajera), alimenta las pantallas en tiempo real por SignalR y **guarda la historia** de funcionamiento en la base de datos para los reportes históricos.

Hoy cuatro servicios de las specs 027 a 030 simulan por separado, cada uno con sus propios números aleatorios, por lo que dos pantallas pueden mostrar cifras distintas para el mismo instante. Con el motor único, todas muestran lo mismo y coinciden con lo que se guarda.

## Usuarios / Actores
- **Sistema (servicios en segundo plano)**: calcula, transmite y guarda las lecturas automáticamente.
- **Administrador / quien despliega**: puede cambiar la frecuencia o pausar la simulación por configuración (entorno de pruebas).

## Historias de usuario
- **HU-1**: Como desarrollador y operador, quiero que el simulador genere lecturas periódicas continuas y verosímiles para probar el monitoreo en tiempo real y poblar los reportes históricos sin depender de hardware físico.

## Parámetros de la simulación

| Parámetro | Regla / Comportamiento |
| :--- | :--- |
| Frecuencia de emisión | Configurable (`Simulation:IntervalSeconds`, por defecto 5 segundos). |
| Curva de generación solar | Campana sinusoidal entre el amanecer y el atardecer, que **cambian según la época del año** (aproximación para Uruguay con la hora oficial UTC−3): la duración del día varía entre unas 10,1 horas (21 de junio: de 07:42 a 17:48) y unas 14,3 horas (21 de diciembre: de 05:36 a 19:54), con el mediodía solar a las 12:45; `factor = sen(π × (hora − amanecer) / (atardecer − amanecer))`. A fines de octubre y de febrero el día va de 06:00 a 19:30, como indica el borrador. Es la misma curva que usan las horas de sol de la spec 027 y la potencia esperada de las specs 029 y 030. |
| Noche (sin radiación) | Potencia = 0,0 kW y voltaje DC residual bajo (unos pocos voltios). |
| Mediodía solar (pico) | Potencia cercana a la capacidad nominal del inversor. |
| Nubosidad | Variación suave y acotada de ±5 % a ±10 % que cambia de forma gradual (no salta en cada lectura); las nubes afectan por igual a todos los equipos de un parque, con una pequeña variación propia de cada equipo. |
| Temperatura del inversor | Temperatura ambiente del día más el calentamiento por carga; cerca del mediodía puede superar los 75 °C. |
| Determinismo | Cada lectura es una función del equipo y del instante: el mismo equipo en el mismo instante da siempre el mismo valor. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO la aplicación y el host de Aspire inicien, EL SISTEMA arrancará los servicios de simulación en segundo plano para todos los parques y nodos que se encuentren en estado "Activo".
- **RF-2**: MIENTRAS el servicio de simulación esté en ejecución, EL SISTEMA calculará cada N segundos (N configurable) la lectura instantánea de cada inversor según la fecha y la hora local de Uruguay y emitirá los paquetes de telemetría por los hubs de SignalR de las specs 027 a 030, todos con valores del mismo motor.
- **RF-3**: EL SISTEMA persistirá, para cada inversor activo y cada minuto completo, un resumen con: potencia media y pico (kW), energía (kWh), voltaje DC medio (V), corriente DC media (A), voltaje AC medio (V), frecuencia media (Hz), temperatura media y máxima (°C), cantidad de lecturas del minuto y cantidad de lecturas anómalas. El registro lleva el parque y el nodo a los que pertenecía el inversor en ese momento. Además, para cada parque activo con inversores y cada hora completa, EL SISTEMA persistirá en la tabla de la spec 034 la energía generada (kWh) y la potencia pico (kW) del parque, calculadas con el mismo motor, de modo que los reportes históricos coinciden con las lecturas en vivo.
- **RF-4**: SI un parque o nodo es dado de baja o pasa a estado "Inactivo", ENTONCES EL SISTEMA suspenderá la generación y el guardado de lecturas para ese nodo en el siguiente ciclo.
- **RF-5**: SI la inyección a red del parque está desactivada administrativamente (spec 033), ENTONCES EL SISTEMA emitirá potencia 0 kW a la red, simulando desacople de contactores. *(Pendiente: se implementa cuando exista la spec 033; hasta entonces las lecturas no consideran ese estado.)*
- **RF-6**: EL SISTEMA calculará las lecturas como una función pura del equipo y del instante, de modo que todas las pantallas y la historia guardada coincidan para el mismo instante, y que dos copias del servidor (por ejemplo durante una actualización de versión) produzcan los mismos datos.
- **RF-7**: EL SISTEMA guardará cada minuto de forma idempotente (un solo registro por inversor y minuto): si el registro ya existe, no lo duplica ni da error.
- **RF-8**: CUANDO la aplicación inicie, EL SISTEMA completará en segundo plano, usando el mismo motor, la historia por minuto de cada inversor activo desde el último minuto guardado (o, si no hay historia, desde los últimos 30 días) hasta el minuto actual, y la historia horaria de cada parque activo desde el último registro guardado (o, si no hay, desde el 1 de enero del año anterior) hasta la hora actual. Para un equipo dado de alta después del comienzo de esas ventanas, la historia comienza en su fecha de alta.

## Requisitos no funcionales
- Bajo consumo de CPU y memoria: cálculos en funciones puras, sin bloqueo de hilos; la historia inicial se genera en segundo plano y en lotes, sin demorar el arranque de la API.
- Orquestación nativa mediante .NET Aspire: los servicios corren dentro de la API, que Aspire ya orquesta (`SolarUY.AppHost`).
- Las fechas se guardan en UTC y la hora local de Uruguay se usa solo para calcular la curva solar y para mostrar.

## Casos límite
- Parque sin inversores: se omite sin errores en los logs, no guarda historia, su lectura en vivo de la spec 027 es 0 kW y no dispara la alerta de generación baja de la spec 031 (no tiene equipos que generen).
- Reinicio del servidor: retoma la simulación en la hora solar correspondiente al instante actual y completa la historia faltante.
- Base de datos recreada vacía (el AppHost la recrea en cada arranque): al iniciar se regeneran la historia por minuto (últimos 30 días) y la historia horaria (desde el año anterior).
- Parque sin inversores: no tiene registros horarios y la spec 034 muestra "No hay datos de generación registrados para el período seleccionado."
- Dos copias de la API en ejecución al mismo tiempo: calculan los mismos valores y escriben los mismos registros sin duplicados.
- El parque ya no suma su propia potencia inventada: su potencia es la suma de sus inversores activos; los paneles se simulan por separado con la misma curva y la misma nubosidad.

## Efecto en otras specs
Hoy las specs 027, 029 y 030 asumen un horario de sol fijo de 06:00 a 20:00. Al respetar la época del año, todas pasan a usar el mismo calendario solar (amanecer y atardecer de la fecha):
- **Spec 027 (parque)**: las "horas de sol" en las que se evalúa la alerta de generación baja y en las que el parque genera pasan a ser las del calendario. En invierno el parque empieza a generar más tarde y deja de generar más temprano.
- **Spec 028 (nodo)**: la simulación del inversor usa la misma curva y el mismo calendario.
- **Spec 029 (panel)**: la potencia esperada a la hora actual y el aviso de rendimiento bajo usan el calendario; en su spec, "entre las 6:00 y las 20:00" pasa a decir "entre el amanecer y el atardecer de la fecha (spec 036)".
- **Spec 030 (mapa de salud)**: "Sin sol" aparece desde el atardecer hasta el amanecer reales de la fecha, y los estados dependen de la potencia esperada del calendario; su spec cambia el mismo texto que la 029.
- **Spec 034 (históricos)**: conserva su tabla, su servicio de consultas, sus pantallas y sus tests. Su cargador de datos de ejemplo, que usaba una fórmula propia (capacidad del parque × curva fija × un factor inventado), se reemplaza por la historia calculada con el motor, y los datos se completan en segundo plano en lugar de bloquear el arranque. Su tarea T3 se actualiza en consecuencia.
- **Spec 031 (alertas)**: sin cambios (usa las banderas que calculan los servicios anteriores); su ventana de purga nocturna de 00:00 a 06:00 es otra cosa y no cambia.
- **Pruebas manuales**: como la curva depende de la fecha y no solo de la hora, al cambiar la fecha del reloj de la PC cambia la ventana de sol.

## Fuera de alcance
- Modelado meteorológico complejo por satélite (se utiliza aproximación sinusoidal con ruido determinista).
- Horario de verano: Uruguay usa UTC−3 todo el año y la simulación no contempla cambios de hora oficial.
- Historia por panel (energía y rendimiento por hora): queda como ampliación posible; hoy solo se guarda por inversor.
- Retención o compactación de la historia a largo plazo.
- Interfaz o endpoint para pausar o cambiar la frecuencia: se configura por `appsettings`.
- Corte de inyección a la red (RF-5, spec 033) e inyección de fallas (spec 037).

## Criterios de finalización
- Tests unitarios en verde para las funciones matemáticas del calendario y la curva solar (amanecer y atardecer por fecha), la nubosidad determinista, las lecturas del inversor, del panel y del parque, el resumen por minuto y la completitud de la historia.
- Tests de arquitectura en verde.
- Comprobación de recepción continua de paquetes en el cliente Blazor mediante SignalR, y de que las pantallas de las specs 027 a 030 muestran valores coherentes entre sí.
- La historia por minuto de los últimos 30 días y la historia horaria desde el año anterior quedan guardadas al arrancar, y se agrega un registro por inversor cada minuto y uno por parque cada hora. Los gráficos de la spec 034 coinciden con las lecturas en vivo.

## Decisiones de la clarificación (2026-11-15)
- **Un motor único** que consultan los servicios de las specs 027 a 030 (y no un simulador aparte), para que todas las pantallas muestren los mismos valores.
- **Dentro de la API**, como servicios en segundo plano, y no como proyecto aparte: comparte memoria con los servicios que consulta, usa los mismos hubs y la misma base, y es coherente con la constitución del proyecto (simplicidad, Aspire orquestando la solución). La letra deja la arquitectura a elección del equipo.
- **Motor determinista y guardado sin duplicados**, para cumplir el requisito de alta disponibilidad de la letra: durante una actualización pueden convivir dos copias de la API sin lecturas distintas ni filas duplicadas.
- **Qué se guarda**: un resumen por inversor y por minuto, con más variables que las que hoy necesita la spec 034 (voltajes, corriente, frecuencia, temperatura), porque la letra pide que la información registrada "contemple futuros requerimientos o necesidades de información".
- **Historia inicial al arrancar** (la base se recrea vacía en cada arranque; sin esto la spec 034 mostraría gráficos vacíos): 30 días por minuto y, para la spec 034, desde el año anterior por hora.
- **La spec 034 pasa a consumir la historia del motor (opción A)**: la spec 034 ya estaba implementada con su propia tabla `ParkHourlyGenerations` y un cargador independiente del motor; en lugar de mantener dos historias que no coinciden, el servicio de historia también escribe esa tabla (energía de la hora = suma de los inversores; pico = máximo de la potencia del parque) y reemplaza su cargador. Se evitó reescribir su servicio de consultas.
- Agregados respecto al borrador, a confirmar en la aprobación:
  - La potencia de un parque es la suma de sus inversores activos. Con los datos de ejemplo, Salto Grande pasa de unos 500 kW a unos 210 kW al mediodía y Cerro del Marco a unos 100 kW; Paso de los Toros (sin inversores) no genera.
  - La curva **respeta la época del año** (el borrador decía "06:00 a 19:30 según época"): el amanecer y el atardecer se calculan por fecha con un modelo sinusoidal de la duración del día (de 10,1 a 14,3 horas, mediodía solar a las 12:45). Esto obliga a que las specs 027, 029 y 030, que usaban un horario fijo de 06:00 a 20:00, usen el mismo calendario (ver "Efecto en otras specs"), y a actualizar los tests y los textos de la 029 y la 030.
  - La frecuencia por defecto es de 5 segundos (la de la spec; los servicios actuales usaban 3), por lo que el rótulo "Actualización 3s" de las pantallas pasa a decir "En vivo".
  - El voltaje DC de noche es residual bajo, como pide el borrador (hoy el simulador del nodo mostraba unos 740 V de noche).
  - La energía acumulada del día de la spec 027 pasa a ser la suma real de lo generado desde la medianoche, en lugar de una cifra inventada, y coincide con la historia.
  - Los inversores de los datos de ejemplo pasan a tener fecha de alta de hace 2 años (antes de 2 a 6 meses), para que la historia horaria cubra el año anterior sin inventar historia anterior a la existencia del equipo. Es un cambio cosmético en `DbInitializer`.
  - La pausa y la frecuencia se configuran por `appsettings` (`Simulation:Enabled`, `Simulation:IntervalSeconds`, `Simulation:BackfillDays`), sin pantalla ni endpoint.

## Dudas abiertas
- Ninguna.
