# Spec 030 — Ver Estado de Salud de Paneles

Estado: borrador
Depende de: Spec 006 (Paneles), Spec 027 (Monitoreo Parque)

## Contexto y objetivo
Ofrecer una vista panorámica (tipo mapa térmico o matriz de celdas) de todos los paneles de un parque solar, donde cada panel se represente con un color representativo de su estado de salud operativo (Normal, Rendimiento Degradado, Falla Crítica o Inactivo).

## Usuarios / Actores
- **Administrador** y **Técnico**: identifican visualmente anomalías masivas en el parque.

## Historias de usuario
- **HU-1**: Como operador, quiero ver un mapa visual del estado de salud de todos los paneles del parque para ubicar rápidamente qué módulo presenta fallas físicas.

## Estados de salud definidos

| Estado de Salud | Código de Color | Criterio de Activación |
| :--- | :--- | :--- |
| **Óptimo / Normal** | Verde | Generación ≥ 80% del valor esperado. Sin fallas activas. |
| **Degradado / Alerta** | Amarillo / Ámbar | Generación entre 40% y 79% del valor esperado, o temperatura elevada. |
| **Falla Crítica** | Rojo | Generación < 40%, circuito abierto, microinversor desconectado o reporte de avería. |
| **Inactivo / Apagado** | Gris | Panel desactivado administrativamente o en mantenimiento programado. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario seleccione la pestaña "Mapa de Salud" de un parque, EL SISTEMA dibujará una cuadrícula interactiva con todos los paneles del parque agrupados por nodo.
- **RF-2**: Cada celda representará un panel mostrando su número de serie y coloreada dinámicamente según su estado de salud actual.
- **RF-3**: CUANDO el usuario haga clic o pase el cursor sobre un panel de la cuadrícula, EL SISTEMA desplegará una tarjeta emergente (popover/tooltip) con: Número de serie, Nodo, Potencia actual, Eficiencia actual, Último mantenimiento y botón "Ver detalle".
- **RF-4**: EL SISTEMA incluirá contadores resumen en la cabecera: Total de paneles, Cantidad en estado Normal, Cantidad en Alerta y Cantidad en Falla.
- **RF-5**: MIENTRAS se reciban eventos SignalR de cambio de estado o nuevas lecturas, la celda del panel actualizará su color en tiempo real de forma reactiva.

## Requisitos no funcionales
- Renderizado fluido para parques de hasta 2000 paneles (usando componentes Blazor virtualizados o Canvas/SVG ligero).
- Leyenda explicativa de colores fija en pantalla.

## Casos límite
- Parque sin paneles muestra "No hay paneles registrados en este parque para generar el mapa de salud."

## Fuera de alcance
- Diseño 3D o vista satelital (se representa como grilla o matriz de strings).

## Criterios de finalización
- Verificación manual en navegador interactuando con la cuadrícula.
- Tests de arquitectura en verde.

## Dudas abiertas
- Ninguna.
