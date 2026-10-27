# Spec 006 — Listar Paneles Solares

Estado: aprobada

Depende de: Spec 005 (Alta de Panel Solar)

## Contexto y objetivo
Ver todos los panels registrados y su estado en una sola pantalla. Esta pantalla es además el punto de entrada para el alta (005), la modificación (007) y la baja (008). El listado muestra los panels agrupados por parque solar.

## Usuarios / Actores
- **Administrador** y **Técnico**: consultan el listado. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-2**: Como usuario de SolarUY, quiero ver el listado de panels solares con su estado y parque asociado para conocer qué panels existen y cuáles están activos.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario abra la pantalla de panels, EL SISTEMA mostrará todos los panels registrados, activos e inactivos, en una tabla con las columnas: Número de serie, Potencia (W), Eficiencia (%), Parque solar y Estado.
- **RF-2**: EL SISTEMA ordenará el listado alfabéticamente por número de serie, sin distinguir mayúsculas/minúsculas.
- **RF-3**: EL SISTEMA mostrará el estado como "Activo" o "Inactivo", visualmente diferenciados (por ejemplo, con colores distintos).
- **RF-4**: SI no hay panels registrados, ENTONCES EL SISTEMA mostrará el mensaje "No hay panels registrados." en lugar de la tabla.
- **RF-5**: EL SISTEMA mostrará un botón "Nuevo panel" que lleva a la pantalla de alta (spec 005).
- **RF-6**: SI la API no responde, ENTONCES EL SISTEMA mostrará "No se pudo cargar el listado de panels. Intentá nuevamente." sin cerrar la pantalla.

## Formato de los datos

- Número de serie con trim (espacios al inicio y al final removidos).
- Potencia conEnteros (W).
- Eficiencia con 2 decimales, seguidos de "%".
- Nombre del parque solar asociado.

## Casos límite

- "alfa", "Beta" y "gamma" se ordenan alfa, Beta, gamma (sin importar mayúsculas).
- Un panel inactivo aparece en el listado igual que uno activo.
- Si se elimina un parque, los panels asociados siguen apareciendo con el nombre del parque (aunque este esté inactivo).

## Fuera de alcance

- Filtros, búsqueda y paginación.
- Pantalla de detalle de un panel.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Test unitario en verde para el orden de RF-2 (regla del servicio).
- Tests de arquitectura en verde.
- Listado probado a mano: con panels, sin panels y con la API detenida.