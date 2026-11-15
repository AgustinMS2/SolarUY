# Spec 030 — Ver Estado de Salud de Paneles

Estado: aprobada
Depende de: Spec 006 (Paneles), Spec 027 (Monitoreo Parque), Spec 029 (Monitoreo Panel; enlace "Ver detalle")

## Contexto y objetivo
Ofrecer una vista panorámica (tipo mapa térmico o matriz de celdas) de todos los paneles de un parque solar, donde cada panel se represente con un color representativo de su estado de salud operativo (Normal, Degradado, Falla Crítica, Sin sol o Inactivo). Complementa a la spec 027 (un solo número para todo el parque) y a la spec 029 (un panel en detalle): permite ubicar de un vistazo qué módulo presenta problemas entre cientos de paneles.

## Usuarios / Actores
- **Administrador** y **Técnico**: identifican visualmente anomalías masivas en el parque.

## Historias de usuario
- **HU-1**: Como operador, quiero ver un mapa visual del estado de salud de todos los paneles del parque para ubicar rápidamente qué módulo presenta fallas físicas.

## Estados de salud definidos

> **Criterios provisorios.** Hoy el estado se calcula únicamente a partir de la generación contra lo esperado a esa hora. Los eventos de falla (circuito abierto, microinversor desconectado, reporte de avería) y la temperatura del panel se suman cuando existan las specs 031 (alertas) y 037 (simulador de fallas); esta spec deberá ampliarse entonces.

La **potencia esperada** es la misma de la spec 029: potencia de placa × sen(π × (hora − 6) / 14) entre las 6:00 y las 20:00 (hora Uruguay); 0 W fuera de ese horario.

| Estado de Salud | Color | Criterio de Activación |
| :--- | :--- | :--- |
| **Óptimo / Normal** | Verde `#16A34A` | Generación ≥ 80 % de lo esperado a la hora actual. |
| **Degradado / Alerta** | Ámbar `#F59E0B` | Generación entre el 10 % (inclusive) y el 80 % (exclusivo) de lo esperado. Por debajo del 40 % la tarjeta del panel indica "Rendimiento bajo / Posible sombreado o suciedad" (misma regla que la spec 029). |
| **Falla Crítica** | Rojo `#DC2626` | Generación casi nula: menor al 10 % de lo esperado, con sol (potencia esperada > 0 W). |
| **Sin sol** | Índigo `#6366F1` | Panel activo con lecturas y potencia esperada de 0 W (de noche). No se evalúa su rendimiento. |
| **Inactivo / Sin lecturas** | Gris `#94A3B8` | Panel Inactivo (dado de baja, spec 008). También un panel Activo sin nodo o con nodo Inactivo, que no recibe lecturas (spec 029); la tarjeta indica el motivo. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario abra el mapa de salud de un parque (`/parques/{id}/salud`, desde el botón "Salud" del listado de parques), EL SISTEMA dibujará una cuadrícula interactiva con todos los paneles del parque agrupados por nodo; los paneles sin nodo se agrupan bajo "Sin nodo asignado".
- **RF-2**: EL SISTEMA representará cada panel como una celda que muestra su número de serie y está coloreada según su estado de salud actual.
- **RF-3**: CUANDO el usuario haga clic o pase el cursor sobre un panel de la cuadrícula, EL SISTEMA desplegará una tarjeta con: número de serie, nodo, estado de salud, potencia actual (W), eficiencia actual (%), último mantenimiento y un botón "Ver detalle" que abre `/paneles/{id}/monitoreo`. El último mantenimiento es la fecha de cierre (hora Uruguay) de la orden Completada más reciente del panel; si no tiene ninguna se muestra "Sin mantenimientos". Para paneles sin lecturas, la potencia y la eficiencia se muestran como "—" junto con el motivo.
- **RF-4**: EL SISTEMA incluirá contadores resumen en la cabecera: Total de paneles, Normal, Alerta (Degradado), Falla, Sin sol e Inactivos.
- **RF-5**: MIENTRAS se reciban eventos SignalR con nuevas lecturas, EL SISTEMA actualizará en tiempo real el color de cada celda, los contadores y la tarjeta abierta, sin recargar la página.
- **RF-6**: SI se pierde la conexión con el hub de SignalR, ENTONCES EL SISTEMA mostrará "Reconectando con el parque..." y mantendrá visible el último estado recibido con una marca de advertencia.
- **RF-7**: CUANDO el usuario navegue a otra pantalla, EL SISTEMA cerrará la suscripción al grupo SignalR del parque para optimizar recursos del servidor.

## Requisitos no funcionales
- Renderizado fluido para parques de hasta 2000 paneles: celdas livianas y un solo evento SignalR por parque en cada ciclo (no uno por panel).
- Leyenda explicativa de colores fija en pantalla.
- Diseño responsive "SolarOps Clean", usable en navegadores móviles.

## Casos límite
- Parque sin paneles: "No hay paneles registrados en este parque para generar el mapa de salud."
- Parque Inactivo: "El parque se encuentra inactivo." sin abrir la conexión en vivo.
- Parque inexistente: "El parque solicitado no existe."
- Panel con orden de mantenimiento pendiente: sigue funcionando y se colorea según su generación (el sistema no tiene un estado "en mantenimiento"; una orden no apaga el panel).

## Fuera de alcance
- Diseño 3D o vista satelital (se representa como grilla o matriz de strings).
- Estados por "panel desactivado manualmente" (spec 032), "circuito abierto", "microinversor desconectado", "reporte de avería" y "temperatura elevada del panel" (specs 031 y 037). Estaban en el borrador de esta spec.
- Estado "en mantenimiento programado": hoy una orden de mantenimiento es solo una anotación (Pendiente, Completado o Cancelado) y no existe un estado "en curso". Estaba en el borrador de esta spec.
- Unificar la simulación de lecturas con las pantallas de las specs 027 a 029: hasta la spec 036, el mapa y la pantalla de un panel simulan sus lecturas por separado y pueden diferir.

## Criterios de finalización
- Verificación manual en navegador interactuando con la cuadrícula.
- Botón "Salud" accesible desde el listado de parques (`/parques`) para los parques activos.
- Tests unitarios de la clasificación de salud, los contadores y el último mantenimiento en verde.
- Tests de arquitectura en verde.

## Decisiones de la clarificación (2026-11-15)
- La pantalla es una ruta nueva `/parques/{id}/salud` con botón "Salud" en el listado de parques; no se agregan pestañas a la pantalla de la spec 027.
- El gris es solo para paneles Inactivos (y sin lecturas); un panel con orden de mantenimiento pendiente no se pinta gris porque sigue funcionando.
- El color sale solo de la generación contra lo esperado (provisorio, se amplía con las specs 031 y 037).
- Para alinear la spec con las specs 029, 031 y 037, que tratan la baja generación como advertencia (ámbar) y la generación cero con sol como crítica (rojo), el rojo se reserva a la generación casi nula (menos del 10 % de lo esperado); el borrador pintaba de rojo todo lo que bajaba del 40 %. El valor del 10 % lo propone esta clarificación para definir "casi nula" y debe confirmarse.
- De noche los paneles activos se ven en índigo con la leyenda "Sin sol", y se agregan los contadores "Sin sol" e "Inactivos" a los cuatro del borrador.
- Agregados respecto al borrador para alinearla con las specs 027 a 029 (a confirmar en la aprobación): RF-6 y RF-7 (reconexión y cierre de suscripción), los casos límite de parque inactivo e inexistente, y el gris para paneles activos sin nodo o con nodo inactivo.

## Dudas abiertas
- Ninguna.
