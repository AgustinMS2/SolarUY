# Spec 028 — Ver Generación en Tiempo Real del Nodo

Estado: borrador
Depende de: Spec 010 (Listar Nodos), Spec 027 (Monitoreo Parque)

## Contexto y objetivo
Proporcionar una vista detallada de telemetría a nivel de nodo e inversor, permitiendo analizar el voltaje, corriente y potencia activa en tiempo real para diagnosticar el comportamiento de un sector puntual del parque solar.

## Usuarios / Actores
- **Administrador** y **Técnico**: analizan el desempeño del inversor y nodo.

## Historias de usuario
- **HU-1**: Como Técnico, quiero consultar la telemetría en vivo de un nodo específico para verificar si el inversor está operando dentro de sus parámetros nominales.

## Métricas del nodo

| Métrica | Unidad | Rango esperado |
| :--- | :--- | :--- |
| Potencia activa | kW | 0 a capacidad del inversor |
| Voltaje DC de entrada | V | 200 a 1000 V |
| Corriente DC | A | 0 a 100 A |
| Voltaje AC de salida | V | 220 / 380 V trifásico |
| Frecuencia de red | Hz | 49.5 a 50.5 Hz |
| Temperatura interna del inversor | °C | 20 a 85 °C |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario seleccione un nodo del parque, EL SISTEMA mostrará un panel de telemetría en tiempo real conectado al grupo SignalR de ese nodo.
- **RF-2**: MIENTRAS se reciban lecturas del inversor, EL SISTEMA actualizará en vivo los valores eléctricos y una mini-gráfica de tendencia de los últimos 10 minutos de potencia.
- **RF-3**: SI la temperatura del inversor supera los 75 °C, ENTONCES EL SISTEMA mostrará una advertencia de "Sobretemperatura en inversor (<temperatura> °C)".
- **RF-4**: SI la frecuencia de red se desvía más allá de 50 ± 0.5 Hz, ENTONCES EL SISTEMA alertará "Anomalía en sincronismo de red eléctrica."
- **RF-5**: SI el nodo no tiene inversor asignado, ENTONCES EL SISTEMA mostrará "Este nodo no posee un inversor configurado." y no intentará abrir la conexión de telemetría.

## Requisitos no funcionales
- Rendimiento eficiente en navegadores móviles para técnicos situados junto al inversor.

## Casos límite
- Desconexión del inversor: los indicadores muestran "--" y estado "Sin señal".

## Fuera de alcance
- Detener o reiniciar el inversor (spec 032).

## Criterios de finalización
- Verificación interactiva en Blazor Server recibiendo eventos SignalR.
- Tests de arquitectura en verde.

## Dudas abiertas
- Ninguna.
