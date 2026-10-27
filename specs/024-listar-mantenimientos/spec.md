# Spec 024 — Listar Mantenimientos

Estado: borrador
Depende de: Spec 021 (Reservar Mantenimiento)

## Contexto y objetivo
Proporcionar una vista global consolidada de todos los mantenimientos registrados en la plataforma SolarUY, permitiendo filtrar por estado, técnico, parque y rango de fechas.

## Usuarios / Actores
- **Administrador**: supervisa el plan maestro de mantenimiento.
- **Técnico**: visualiza la carga general de trabajo.

## Historias de usuario
- **HU-1**: Como usuario, quiero ver el listado de todos los mantenimientos con filtros avanzados para controlar el cumplimiento del plan preventivo y correctivo.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario acceda a la sección de mantenimientos, EL SISTEMA mostrará una grilla con: Código de orden, Parque solar, Panel afectado, Técnico asignado, Tipo (Preventivo/Correctivo), Fecha programada, Estado ("Pendiente", "Completado", "Cancelado") y Acciones disponibles.
- **RF-2**: EL SISTEMA ordenará por defecto los mantenimientos de forma cronológica descendente por fecha programada.
- **RF-3**: EL SISTEMA proveerá filtros combinables por:
  - Estado (Todos, Pendiente, Completado, Cancelado).
  - Parque solar.
  - Técnico asignado.
  - Rango de fechas (Desde - Hasta).
- **RF-4**: SI no existen mantenimientos que cumplan los filtros aplicados, ENTONCES EL SISTEMA mostrará "No se encontraron mantenimientos para los criterios seleccionados."
- **RF-5**: Los estados se representarán visualmente con colores diferenciados:
  - Pendiente: Amarillo / Ámbar.
  - Completado: Verde.
  - Cancelado: Gris / Rojo tenue.

## Requisitos no funcionales
- Paginación ágil y carga en diferido de listados extensos.
- Formato de fecha en hora local de Uruguay (`DD/MM/AAAA HH:mm`).

## Casos límite
- Órdenes sin técnico asignado (si se admitieran) muestran "Sin técnico".

## Fuera de alcance
- Exportación a PDF/Excel (se contempla en specs de Reportes).

## Criterios de finalización
- Tests unitarios en verde para los filtros y ordenamiento del servicio.
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna.
