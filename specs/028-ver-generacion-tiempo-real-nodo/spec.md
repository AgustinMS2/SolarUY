# Spec 028 — Ver Generación en Tiempo Real del Nodo

Estado: implementada
Depende de: Spec 010 (Listar Nodos), Spec 013 (Asignar Inversor a Nodo), Spec 027 (Monitoreo Parque), Spec 036 (Simulador)
Actualizada el 2026-11-15 por las specs 033, 036 y 037 (ver "Cambios posteriores").

## Contexto y objetivo
Proporcionar una vista detallada de telemetría a nivel de nodo e inversor, permitiendo analizar el voltaje, corriente, potencia activa, frecuencia de red y temperatura en tiempo real mediante SignalR, para diagnosticar el comportamiento eléctrico y térmico de un sector puntual del parque solar.

## Usuarios / Actores
- **Administrador** y **Técnico**: supervisan la operación del nodo y del inversor asociado.

## Historias de usuario
- **HU-1**: Como Técnico u Operador, quiero consultar la telemetría en vivo de un nodo específico para verificar si el inversor está operando dentro de sus parámetros nominales de potencia, frecuencia y temperatura.

## Métricas del nodo

| Métrica | Unidad | Rango esperado | Descripción |
| :--- | :--- | :--- | :--- |
| Potencia activa | kW | 0 a capacidad nominal del inversor | Potencia instantánea suministrada por el inversor |
| Voltaje DC de entrada | V | 200 a 1000 V de día; de noche, un voltaje residual de unos pocos voltios (spec 036) | Tensión continua entregada por las ramas de paneles |
| Corriente DC | A | 0 a 100 A | Corriente continua suministrada al inversor |
| Voltaje AC de salida | V | 220 / 380 V | Tensión alterna inyectada |
| Frecuencia de red | Hz | 49.5 a 50.5 Hz | Frecuencia de red (nominal 50 Hz en Uruguay) |
| Temperatura interna | °C | 20 a 85 °C (86 a 90 °C con la falla simulada de sobretemperatura, spec 037) | Temperatura en el disipador/electrónica del inversor |
| Estado del enlace SignalR | - | Conectado / Reconectando / Desconectado | Conectividad con el hub de telemetría |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario acceda a la vista de monitoreo en tiempo real de un nodo (`/nodos/{id:guid}/monitoreo`), EL SISTEMA establecerá una conexión SignalR al hub de telemetría de nodos, se suscribirá al grupo `node-{nodeId}` y renderizará el panel de control eléctrico.
- **RF-2**: MIENTRAS la conexión SignalR esté activa, EL SISTEMA actualizará en vivo los valores eléctricos (potencia, tensiones, corriente, frecuencia y temperatura) y una mini-gráfica SVG reactiva con la serie de las lecturas recientes de potencia sin recargar la página.
- **RF-3**: SI la temperatura interna del inversor supera los 75 °C, ENTONCES EL SISTEMA mostrará una advertencia destacada en ámbar/rojo indicando "Sobretemperatura en inversor (<temperatura> °C)".
- **RF-4**: SI la frecuencia de red se desvía más allá de 50 ± 0.5 Hz (< 49.5 Hz o > 50.5 Hz), ENTONCES EL SISTEMA mostrará una alerta de "Anomalía en sincronismo de red eléctrica (<frecuencia> Hz)".
- **RF-5**: SI el nodo no tiene inversor asignado, ENTONCES EL SISTEMA mostrará un mensaje "Este nodo no posee un inversor configurado." y no intentará abrir la conexión de telemetría en vivo.
- **RF-6**: SI se pierde la conexión con el hub de SignalR, ENTONCES EL SISTEMA mostrará "Reconectando con el inversor..." y mantendrá visible la última lectura recibida con una indicación visual de advertencia.
- **RF-7**: CUANDO el usuario navegue a otra pantalla, EL SISTEMA cerrará la suscripción al grupo SignalR del nodo para optimizar recursos del servidor.
- **RF-8** *(agregado por la spec 037)*: SI el inversor del nodo tiene una falla simulada vigente, ENTONCES EL SISTEMA mostrará un banner "En Falla" con el tipo de falla; con la falla de aislamiento, el inversor figura apagado (potencia, corriente, tensión de red y frecuencia en cero) y no se muestran las alarmas de sobretemperatura ni de frecuencia.

## Requisitos no funcionales
- Rendimiento fluido y adaptable en navegadores móviles (diseño responsive "SolarOps Clean").
- Latencia de actualización visual < 500 ms tras la emisión de cada lectura en el servidor.
- Consumo eficiente de memoria en el cliente (búfer rodante de lecturas recientes para la gráfica).

## Casos límite
- Nodo sin inversor asignado: cartel informativo bloqueando el intento de enlace SignalR.
- Nodo inactivo: valores en cero e indicación de inactividad operativa.
- Desconexión del inversor o enlace: valores marcados con advertencia y mensaje de reconexión.
- Inyección a la red detenida administrativamente (spec 033): el inversor no entrega potencia, por lo que se reportan los valores eléctricos en cero y sin alarmas, igual que un nodo inactivo.
- De noche la potencia es 0 kW y el voltaje DC es residual; el amanecer y el atardecer dependen de la fecha (spec 036).

## Cambios posteriores (2026-11-15)
- **Spec 036 (simulador)**: las lecturas del nodo dejaron de inventarse en este servicio y salen del motor único de simulación (`ISolarSimulationEngine`), con la curva solar según la fecha. Se emiten cada `Simulation:IntervalSeconds` (5 segundos por defecto, antes 3) en los mismos instantes que las pantallas de parque, panel y salud, por lo que muestran los mismos valores. El rótulo "Actualización 3s" pasó a "En vivo".
- **Spec 033 (inyección a la red)**: con la inyección detenida el nodo se reporta en cero (ver casos límite).
- **Spec 037 (simulador de fallas)**: agrega RF-8 y el campo `Fault` de la telemetría.

## Fuera de alcance
- Detener o reiniciar el inversor (spec 032).
- Comandos de configuración remota de firmware.

## Criterios de finalización
- Verificación interactiva en Blazor Server recibiendo eventos de SignalR.
- Botón "Monitorear" accesible desde el listado de nodos (`/nodos`).
- Tests unitarios de cálculo y reglas de alerta en verde.
- Tests de arquitectura en verde.

## Dudas abiertas
- Ninguna.
