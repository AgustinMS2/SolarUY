# Spec 036 — Simular Lecturas del Inversor

Estado: borrador
Depende de: Spec 013 (Inversor a Nodo)

## Contexto y objetivo
Implementar un servicio en background (`IHostedService` / BackgroundService) orquestado mediante .NET Aspire que simule lecturas telemétricas continuas y realistas de los inversores fotovoltaicos en base a la curva solar astronómica de Uruguay (irradiancia y temperatura ambiente según la hora del día), alimentando el hub de SignalR y la persistencia de series de tiempo.

## Usuarios / Actores
- **Sistema (Background Worker)**: genera y transmite las lecturas automáticamente.
- **Administrador**: puede configurar la frecuencia o pausar la simulación en entorno de pruebas.

## Historias de usuario
- **HU-1**: Como desarrollador y operador, quiero que el simulador genere lecturas periódicas continuas y verosímiles para probar el monitoreo en tiempo real y poblar los reportes históricos sin depender de hardware físico.

## Parámetros de la simulación

| Parámetro | Regla / Comportamiento |
| :--- | :--- |
| Frecuencia de emisión | Configurable (por defecto: cada 5 segundos). |
| Curva de generación solar | Modelo de campana sinusoidal entre la salida y puesta del sol (06:00 a 19:30 según época). |
| Noche (sin radiación) | Potencia = 0.0 kW, Voltaje DC residual bajo. |
| Mediodía solar (pico) | Potencia cercana a la capacidad nominal del parque/inversor (ajustada por eficiencia de paneles). |
| Factor de ruido / nubes | Variación aleatoria controlada (± 5% a 10%) para simular nubosidad pasajera y realismo. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO la aplicación y el host de Aspire inicien, EL SISTEMA arrancará el servicio de simulación en background para todos los parques y nodos que se encuentren en estado "Activo".
- **RF-2**: MIENTRAS el servicio de simulación esté en ejecución, EL SISTEMA calculará cada N segundos la potencia instantánea de cada inversor según la hora local de Uruguay y emitirá el paquete de telemetría a través del Hub de SignalR.
- **RF-3**: EL SISTEMA persistirá periódicamente (o en intervalos de agregación configurables, ej. cada 1 minuto) las lecturas generadas en la base de datos para alimentar los reportes históricos.
- **RF-4**: SI un parque o nodo es dado de baja o pasa a estado "Inactivo", ENTONCES EL SISTEMA suspenderá la generación de lecturas para ese nodo en el siguiente ciclo.
- **RF-5**: SI la inyección a red del parque está desactivada administrativamente (spec 033), ENTONCES EL SISTEMA emitirá potencia 0 kW a la red, simulando desacople de contactores.

## Requisitos no funcionales
- Bajo consumo de CPU y memoria en el worker (cálculos en funciones puras sin bloqueo de hilos).
- Orquestación nativa mediante .NET Aspire (`SolarUY.AppHost`).

## Casos límite
- Parques sin inversores: el simulador los omite sin generar errores en los logs.
- Reinicio del servidor: retoma la simulación en la hora solar correspondiente al instante actual.

## Fuera de alcance
- Modelado meteorológico complejo por satélite (se utiliza aproximación sinusoidal con ruido estocástico).

## Criterios de finalización
- Tests unitarios en verde para las funciones matemáticas de cálculo de curva solar.
- Tests de arquitectura en verde.
- Comprobación de recepción continua de paquetes en el cliente Blazor mediante SignalR.

## Dudas abiertas
- Ninguna.
