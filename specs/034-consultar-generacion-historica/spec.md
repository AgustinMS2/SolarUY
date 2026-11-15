# Spec 034 — Consultar Generación Histórica

Estado: aprobada
Depende de: Spec 001 (Alta Parque), Spec 002 (Listar Parques), Spec 027 (Monitoreo en Tiempo Real Parque)

## Contexto y objetivo
Permitir a los usuarios analizar la evolución de la energía generada (kWh y MWh) a lo largo del tiempo, visualizando métricas consolidadas, gráficas interactivas y reportes comparativos agrupados por hora (vista diaria), por día (vista mensual) y por mes (vista anual), además de exportar las series de datos a formato CSV para auditorías y análisis externos.

## Usuarios / Actores
- **Administrador** y **Técnico**: consultan métricas, analizan tendencias de generación, estacionalidad y descargan reportes.

## Historias de usuario
- **HU-1**: Como operador (Administrador o Técnico), quiero consultar gráficos de generación histórica por día, mes o año para evaluar si el parque cumplió las metas de producción previstas.
- **HU-2**: Como operador, quiero exportar las series de datos temporales a formato CSV para realizar análisis comparativos externos o reportes operativos.

## Dimensiones de consulta y unidades

| Granularidad | Rango temporal | Agrupación | Unidad de Energía |
| :--- | :--- | :--- | :--- |
| **Día** | Fecha seleccionada (00:00 a 23:59 en hora de Uruguay) | 24 intervalos horarios (00:00 a 23:00) | kWh |
| **Mes** | Mes y año seleccionado (1 al último día del mes) | Días del mes (28 a 31 puntos) | kWh |
| **Año** | Año calendario seleccionado (1 de enero al 31 de diciembre) | 12 meses (Enero a Diciembre) | MWh |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario ingrese a la vista de generación histórica (`/historicos` o `/parques/{id}/historico`), EL SISTEMA presentará un selector de parque solar y tres pestañas de granularidad temporal: "Día", "Mes" y "Año".
- **RF-2**: CUANDO el usuario seleccione la pestaña "Día" y una fecha, EL SISTEMA consultará y graficará la energía generada por cada una de las 24 horas del día en hora local de Uruguay (`America/Montevideo`), en unidades de kWh.
- **RF-3**: CUANDO el usuario seleccione la pestaña "Mes", un mes y un año, EL SISTEMA consultará y graficará la energía generada en cada día de dicho mes, en unidades de kWh.
- **RF-4**: CUANDO el usuario seleccione la pestaña "Año" y un año, EL SISTEMA consultará y graficará la energía generada en cada uno de los 12 meses del año, en unidades de MWh (donde $1\text{ MWh} = 1000\text{ kWh}$).
- **RF-5**: EL SISTEMA calculará y mostrará un bloque de métricas resumen para el período seleccionado compuesto por:
  - **Generación Total Acumulada**: suma total de energía generada en el período (kWh en Día/Mes, MWh en Año).
  - **Pico Máximo de Potencia**: potencia máxima instantánea registrada (kW) y la fecha/hora local exacta en que ocurrió.
  - **Horas de Sol Equivalente (HSP)**: cociente entre la energía total generada en kWh y la capacidad nominal del parque en kW ($\text{HSP} = \frac{\text{Energía total (kWh)}}{\text{Capacidad nominal (kW)}}$), redondeado a 1 decimal.
  - **Comparativa con período anterior**: diferencia porcentual respecto al período inmediatamente anterior (mismo día de la semana anterior para vista día, mes anterior para vista mes, año anterior para vista año).
- **RF-6**: CUANDO el usuario interactúe o pase el cursor sobre una barra o punto de la gráfica, EL SISTEMA desplegará un tooltip con la etiqueta del período (hora, día o mes) y la energía generada formateada con dos decimales.
- **RF-7**: CUANDO el usuario presione el botón "Exportar a CSV", EL SISTEMA generará y descargará inmediatamente un archivo `.csv` con codificación UTF-8 (con BOM) y encabezados: `Fecha_Hora_Local,Parque,Energia_kWh,Potencia_Pico_kW`.
- **RF-8**: SI no existen lecturas de generación registradas para el parque y período seleccionado, ENTONCES EL SISTEMA mostrará un mensaje de estado vacío: *"No hay datos de generación registrados para el período seleccionado."*, manteniendo los selectores y la interfaz operativos.
- **RF-9**: SI el usuario no tiene una sesión autenticada con rol `Administrador` o `Técnico`, ENTONCES EL SISTEMA denegará el acceso redirigiendo a la pantalla de inicio de sesión o respondiendo HTTP 401/403.

## Requisitos no funcionales
- **Visualización (SolarOps Clean)**: Gráficas vectoriales interactivas SVG nativas de Blazor, responsivas y accesibles (WCAG AA), con paleta de colores del sistema (`#0284c7`, `#f59e0b`, `#10b981`).
- **Zona Horaria Estricta**: Todas las agrupaciones horarias, diarias y mensuales deben calcularse en el huso horario oficial de Uruguay (`America/Montevideo` / UTC-3), sin desfasajes en días bisiestos o bordes de medianoche.
- **Eficiencia y Persistencia**: Consultas agregadas indexadas en PostgreSQL sobre la tabla `ParkHourlyGenerations`, precargada con datos históricos representativos en el arranque del sistema.
- **Rendimiento**: Tiempo de respuesta de consulta de agregación < 200 ms.

## Casos límite
- Selección de fechas futuras: EL SISTEMA muestra las barras en 0 kWh sin fallar.
- Años bisiestos: Febrero muestra correctamente 29 días en vista mes.
- Parque sin generación registrada: Métricas en 0 y aviso de estado vacío (RF-8).

## Fuera de alcance
- Pronósticos meteorológicos y modelos predictivos de irradiación futura por satélite.
- Modificación o edición manual de registros de generación históricos.

## Criterios de finalización
- Tests unitarios en verde para las funciones de cálculo de agregación temporal, HSP y comparativas porcentuales.
- Tests de arquitectura en verde asegurando autorización en endpoints.
- Verificación en navegador de las tres vistas temporales (Día, Mes, Año), métricas resumen y descarga funcional del archivo CSV.
