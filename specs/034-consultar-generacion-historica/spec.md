# Spec 034 — Consultar Generación Histórica

Estado: borrador
Depende de: Spec 002 (Listar Parques), Spec 027 (Monitoreo)

## Contexto y objetivo
Permitir a los usuarios analizar la evolución de la energía generada (kWh y MWh) a lo largo del tiempo, visualizando reportes y gráficas comparativas agrupadas por hora (vista día), por día (vista mes) y por mes (vista año), además de exportar los datos para análisis externos.

## Usuarios / Actores
- **Administrador** y **Técnico**: analizan tendencias de generación y estacionalidad.

## Historias de usuario
- **HU-1**: Como usuario, quiero consultar gráficos de generación histórica por día, mes o año para evaluar si el parque cumplió las metas de producción previstas.

## Dimensiones de consulta

| Granularidad | Rango temporal | Agrupación | Unidad |
| :--- | :--- | :--- | :--- |
| **Día** | Fecha seleccionada (00:00 a 23:59) | Por hora (24 barras/puntos) | kWh |
| **Mes** | Mes y año seleccionado | Por día (28 a 31 barras) | kWh o MWh |
| **Año** | Año seleccionado | Por mes (12 barras) | MWh |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario seleccione un parque solar y una vista temporal (Día, Mes o Año), EL SISTEMA renderizará una gráfica de barras/área interactiva con la energía generada en cada período.
- **RF-2**: EL SISTEMA mostrará un bloque de métricas resumen que incluirá:
  - Generación total acumulada en el período.
  - Pico máximo de potencia alcanzado y su hora/fecha.
  - Horas de sol equivalente (HSP) estimadas.
  - Comparativa con el período anterior (ej. % de incremento respecto al mes anterior).
- **RF-3**: CUANDO el usuario pase el cursor sobre un punto o barra de la gráfica, EL SISTEMA mostrará un tooltip con la fecha/hora exacta y la energía generada con dos decimales.
- **RF-4**: EL SISTEMA proveerá un botón "Exportar a CSV" que descargará el conjunto de datos tabulado con columnas: `Fecha_Hora_Local`, `Parque`, `Energia_kWh`, `Potencia_Pico_kW`.
- **RF-5**: SI no existen lecturas registradas para el período seleccionado, ENTONCES EL SISTEMA mostrará "No hay datos de generación registrados para el período seleccionado." manteniendo la interfaz limpia.

## Requisitos no funcionales
- Gráficos renderizados con librerías livianas de Canvas/SVG compatibles con Blazor.
- Consulta optimizada en base de datos mediante índices o tablas de agregación temporal (TimescaleDB / PostgreSQL).
- Fechas y agrupaciones siempre calculadas en zona horaria de Uruguay (`America/Montevideo`).

## Casos límite
- Días con generación nula (ej. parque desconectado): la gráfica muestra barras en cero sin romper el eje X.

## Fuera de alcance
- Pronósticos meteorológicos predictivos a futuro.

## Criterios de finalización
- Tests unitarios en verde para las funciones puras de cálculo de agregación temporal.
- Tests de arquitectura en verde.
- Verificación de renderizado de gráficos y descarga de CSV en navegador.

## Dudas abiertas
- Ninguna.
