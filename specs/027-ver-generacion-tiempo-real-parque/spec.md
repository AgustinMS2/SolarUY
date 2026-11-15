# Spec 027 — Ver Generación en Tiempo Real del Parque

Estado: aprobada
Depende de: Spec 002 (Listar Parques)

## Contexto y objetivo
Visualizar en vivo la generación eléctrica instantánea de un parque solar mediante una conexión en tiempo real con SignalR, proporcionando a los operadores métricas actualizadas de potencia (kW), energía diaria acumulada (kWh) y comparación contra la capacidad máxima y umbral de alerta.

## Usuarios / Actores
- **Administrador** y **Técnico**: supervisan la producción del parque.

## Historias de usuario
- **HU-1**: Como operador, quiero ver un panel de control con la potencia generada en tiempo real de un parque solar para supervisar el rendimiento energético continuo.

## Métricas en tiempo real

| Métrica | Unidad | Descripción |
| :--- | :--- | :--- |
| Potencia actual | kW | Potencia instantánea entregada a la red por el conjunto de inversores. |
| Porcentaje de capacidad | % | (Potencia actual / Capacidad máxima del parque) * 100. |
| Energía acumulada del día | kWh | Total de energía generada desde las 00:00 del día actual (hora Uruguay). |
| Estado del enlace SignalR | - | Conectado (verde) / Reconectando (amarillo) / Desconectado (rojo). |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario ingrese a la vista de monitoreo en tiempo real de un parque, EL SISTEMA establecerá una conexión SignalR y renderizará un indicador visual (gauge/tacómetro y tarjeta numérica) con la potencia instantánea generada.
- **RF-2**: MIENTRAS la conexión SignalR esté activa, EL SISTEMA actualizará automáticamente los valores en pantalla cada vez que el hub emita una nueva lectura (frecuencia típica: cada 3 a 5 segundos) sin recargar la página.
- **RF-3**: SI la potencia generada cae por debajo del umbral de alerta configurado para el parque durante horas de sol, EL SISTEMA resaltará el indicador en color amarillo/ámbar y mostrará "Generación por debajo del umbral de alerta (<umbral> kW)."
- **RF-4**: SI se pierde la conexión con el hub de SignalR, ENTONCES EL SISTEMA mostrará un indicador de estado "Reconectando con el parque..." y mantendrá visible el último valor recibido con una marca de advertencia.
- **RF-5**: CUANDO el usuario navegue a otra pantalla, EL SISTEMA cerrará la suscripción al grupo SignalR del parque para optimizar recursos del servidor.

## Requisitos no funcionales
- Latencia de actualización visual < 500 ms tras la emisión del evento en el servidor.
- Animaciones fluidas en la aguja/gráfico de potencia sin parpadeos.

## Casos límite
- Generación de noche: muestra potencia 0.0 kW y energía acumulada del día fija.
- Parque sin inversores o inactivo muestra "El parque se encuentra inactivo o sin equipos asociados."

## Cambios posteriores (2026-11-15)
- **Spec 036 (simulador)**: la potencia del parque deja de inventarse en este servicio: es la **suma de la potencia de los inversores** de sus nodos activos, calculada por el motor único de simulación, y la **energía acumulada del día** es la suma real de lo generado desde las 00:00. Las "horas de sol" en las que se evalúa la alerta de generación baja y en las que el parque genera son las del calendario solar de la fecha (de 07:42 a 17:48 el 21 de junio; de 05:36 a 19:54 el 21 de diciembre), no un horario fijo. La lectura se emite cada `Simulation:IntervalSeconds` (5 segundos por defecto, antes 3) y el rótulo "Actualización 3s" pasó a "En vivo". Con los datos de ejemplo, Salto Grande llega a unos 210 kW al mediodía y Cerro del Marco a unos 100 kW.
- **Parque sin inversores**: su lectura en vivo es 0 kW y no dispara la alerta de generación baja (no tiene equipos que generen).
- **Spec 033 (inyección a la red)**: con la inyección detenida administrativamente, la potencia se reporta en 0 kW y no se genera la alerta de generación baja.

## Fuera de alcance
- Gráficas históricas de meses anteriores (spec 034).
- Envío de comandos de apagado (spec 032).

## Criterios de finalización
- Verificación en navegador con el hub de SignalR emitiendo lecturas simuladas.
- Tests de arquitectura en verde.

## Dudas abiertas
- Ninguna.
