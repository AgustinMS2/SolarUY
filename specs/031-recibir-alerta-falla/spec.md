# Spec 031 — Recibir Alerta de Falla

Estado: implementada
Depende de: Spec 027 (Monitoreo Parque), Spec 028 (Monitoreo Nodo; temperatura del inversor), Spec 030 (Salud Paneles; falla crítica de un panel)
Se relaciona con: Spec 021 (agendar mantenimiento) y Spec 026 (su RF-3 resuelve las alertas de un panel al completar la orden; tarea T5 pendiente de esta spec)

## Contexto y objetivo
Alertar de forma reactiva e inmediata a los operadores y técnicos conectados cuando un sensor, panel o inversor detecta una falla operativa (como sobrecalentamiento o caída abrupta de generación), visualizándose como notificaciones toast y en una campana de alertas en la barra superior.

## Usuarios / Actores
- **Administrador** y **Técnico**: reciben avisos inmediatos para actuar. Solo el Administrador agenda mantenimientos y descarta alertas.

## Historias de usuario
- **HU-1**: Como operador, quiero recibir notificaciones visuales inmediatas cuando se produce una falla para coordinar la reparación con el equipo técnico sin demoras.

## Tipos y niveles de alerta

> **Eventos provisorios.** Hoy solo se generan alertas con los datos que ya existen (las lecturas simuladas de las specs 027, 028 y 030). La spec 037 (simulador de fallas) ya agrega la falla de aislamiento del inversor (Crítico) y la degradación severa de un panel (Advertencia); el cortocircuito y el paro de emergencia no están en el simulador, y los informativos los producirá la spec 033 (inyección a la red); **cuando existan, esta spec deberá ampliarse** para generarlos.

| Nivel | Severidad | Evento desencadenante | ¿Se genera hoy? |
| :--- | :--- | :--- | :--- |
| **Crítico** | Rojo | Panel sin generación con sol: menos del 10 % de lo esperado (la Falla Crítica de la spec 030). | Sí |
| **Crítico** | Rojo | Fallo de aislamiento del inversor (falla simulada de la spec 037). | Sí (spec 037) |
| **Crítico** | Rojo | Cortocircuito, paro de emergencia del inversor. | No |
| **Advertencia** | Amarillo | Generación del parque por debajo de su umbral de alerta durante horas de sol (spec 027). | Sí |
| **Advertencia** | Amarillo | Temperatura interna del inversor > 75 °C (spec 028). | Sí |
| **Advertencia** | Amarillo | Degradación severa de un panel (falla simulada de la spec 037). | Sí (spec 037) |
| **Informativo** | Azul | Desconexión programada, inicio/fin de inyección a red. | No (spec 033) |

## Estados de la alerta

| Estado | Significado |
| :--- | :--- |
| **Pendiente** | Alerta nueva que nadie reconoció (cuenta en la campana). |
| **Reconocida** | Alguien abrió la alerta. El estado es **global**: lo reconoce un usuario y queda reconocida para todos. |
| **Descartada** | Un Administrador la descartó del centro de alertas; queda para la purga. |
| **Atendida** | La marca la spec 026 (RF-3, tarea T5) al completar un mantenimiento del panel. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el sistema de telemetría detecte una condición de falla de las definidas en esta spec, EL SISTEMA creará un registro de alerta en estado Pendiente con: Parque, Equipo (parque, nodo o panel), Severidad, Descripción del fallo, Identificador Único (UUID) y Fecha/hora UTC, y lo emitirá por SignalR a todos los usuarios conectados.
- **RF-2**: EL SISTEMA aplicará deduplicado de alertas: dentro de una ventana de 60 segundos, si se detecta otra alerta con la misma Severidad y Equipo (en cualquier estado), EL SISTEMA la descartará y no generará un nuevo evento SignalR.
- **RF-3**: EL SISTEMA mostrará en el icono de campana de la barra de navegación el contador de alertas en estado Pendiente, igual para todos los usuarios y actualizado en vivo. El contador deja de incluir una alerta cuando pasa a Reconocida, Descartada o Atendida, o cuando se purga.
- **RF-4**: CUANDO el usuario haga clic en una alerta del aviso o del centro de alertas, EL SISTEMA marcará la alerta como Reconocida si estaba Pendiente y mostrará el detalle de la falla. SI la alerta es de un panel y el usuario es Administrador, el detalle incluirá un botón "Agendar Mantenimiento" que abre la reserva de la spec 021 con el panel y la descripción de la falla pre-cargados.
- **RF-5**: EL SISTEMA purgará automáticamente las alertas con estado Descartada que tengan más de 24 horas y que hayan sido creadas de noche (entre las 00:00 y las 06:00, hora de Uruguay), liberando espacio en el historial.
- **RF-6**: SI al detectarse una falla ya existen 2 o más alertas Pendientes o Reconocidas con la misma Severidad y Equipo dentro de los últimos 5 minutos, ENTONCES EL SISTEMA las agrupará en una única alerta consolidada con el contador de instancias (3 o más), y cada nueva detección dentro de esa ventana incrementará el contador y emitirá la actualización por SignalR.
- **RF-7**: CUANDO el Administrador presione "Descartar" en una alerta Reconocida, EL SISTEMA cambiará su estado a Descartada y actualizará el contador y el centro de alertas de todos los usuarios conectados.
- **RF-8**: CUANDO llegue una alerta nueva o una actualización de una alerta consolidada, EL SISTEMA mostrará un aviso (toast) que desaparece a los 8 segundos o al cerrarlo, y la alerta permanecerá en el centro de alertas. SI llegan 5 o más alertas nuevas del mismo parque dentro de 10 segundos, ENTONCES EL SISTEMA mostrará un único aviso "Se detectaron <N> fallas simultáneas en <Parque>".

## Requisitos no funcionales
- Emisión inmediata del evento SignalR tras la detección (< 300 ms).
- Notificación no invasiva: el aviso desaparece solo, pero la alerta permanece en el centro de alertas.
- Diseño responsive "SolarOps Clean": la campana y el centro de alertas deben usarse en el celular.

## Casos límite
- Falla persistente: mientras la condición continúe se detecta en cada ciclo; el deduplicado (RF-2) y la consolidación (RF-6) evitan saturar al usuario.
- Alerta de un parque o de un nodo: no ofrece "Agendar Mantenimiento", porque una reserva necesita un panel.
- Dos usuarios reconocen la misma alerta a la vez: la primera gana y la segunda no cambia nada ni da error.
- Descartar una alerta que no está Reconocida: se rechaza con "Solo se pueden descartar alertas reconocidas."
- Alerta inexistente: "La alerta solicitada no existe."
- Todas las marcas de tiempo se almacenan y transmiten en UTC (según la constitución del proyecto), con conversión a hora local solo para mostrarlas.

## Fuera de alcance
- Envío de SMS o emails automáticos (solo notificaciones in-app vía SignalR).
- Notificaciones sonoras: la historia de usuario las menciona, pero ningún requisito las define.
- Alertas por cortocircuito y paro de emergencia del inversor, y alertas informativas (spec 033). El fallo de aislamiento y la degradación severa las agrega la spec 037.
- Alertas por anomalía de frecuencia de red (spec 028) y por rendimiento bajo de un panel (spec 029): se excluyen porque con las lecturas simuladas actuales generarían una alerta por minuto y por equipo.
- Un disparador de alertas de prueba solo para desarrollo: se dejó para más adelante.
- Pasar las alertas a "Atendida": lo hace la tarea T5 de la spec 026, una vez creada la entidad de alertas.

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio de alertas (registro, deduplicado, consolidación, contador, reconocer, descartar, purga y detección).
- Verificación de la campana, el aviso y el centro de alertas en el navegador con las alertas que generen las lecturas simuladas (inversor > 75 °C cerca del mediodía; parque bajo su umbral al amanecer o al atardecer).
- La alerta crítica y la ráfaga de avisos se prueban ahora en el navegador con el simulador de fallas (spec 037); antes solo con tests.
- Tests de arquitectura en verde.

## Decisiones de la clarificación (2026-11-15)
- Se implementan los seis requisitos del borrador juntos (RF-1 a RF-6), más RF-7 y RF-8.
- La campana cuenta **todas las alertas Pendientes desde el primer momento**. El borrador se contradecía: la RF-3 contaba solo las Pendientes de más de 5 minutos y los casos límite excluían las de más de 5 minutos. Ambas lecturas esconden fallas, por lo que se descartó la condición de los 5 minutos.
- Solo se generan las alertas con datos hoy (tabla de arriba, marcada como provisoria); el resto se suma con las specs 037 y 033.
- "Reconocida" es global (una columna de estado de la alerta), no por usuario.
- Una alerta pasa a "Descartada" con un botón "Descartar" que se agrega en el centro de alertas; el borrador purgaba las Descartadas pero no definía cómo se descartaban. La purga es literal: solo las nocturnas de más de 24 horas.
- Se deja fuera el disparador de alertas de prueba.
- Agregados respecto al borrador, a confirmar en la aprobación:
  - El Equipo puede ser el parque (la alerta de generación bajo el umbral no pertenece a un nodo ni a un panel).
  - Cualquier rol reconoce alertas; solo el Administrador descarta y agenda mantenimientos.
  - La consolidación (RF-6) se interpreta así: la tercera detección dentro de 5 minutos fusiona las alertas de la ventana en una sola con contador.
  - La ráfaga de avisos (RF-8) se define como 5 o más alertas del mismo parque en 10 segundos; el borrador no daba valores.
  - La pantalla de reserva de mantenimiento (spec 021) acepta el panel y la descripción por la dirección para pre-cargarse.
  - Los servicios en segundo plano de las specs 027, 028 y 030 llaman a la publicación de alertas cuando detectan una condición.

## Dudas abiertas
- Ninguna.
