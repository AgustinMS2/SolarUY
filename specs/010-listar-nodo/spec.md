# Spec 010 — Listar Nodos Solares

Estado: pendiente

Depende de: Spec 009 (Alta de Nodo Solar)

## Contexto y objetivo
Ver todos los nodos registrados y su estado en una sola pantalla. Esta pantalla es además el punto de entrada para el alta (009), la modificación (011) y la baja (012). El listado muestra los nodos agrupados por parque solar.

## Usuarios / Actores
- **Administrador** y **Técnico**: consultan el listado. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-2**: Como usuario de SolarUY, quiero ver el listado de nodos solares con su estado y parque asociado para conocer qué nodos existen y cuáles están activos.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario abra la pantalla de nodos, EL SISTEMA mostrará todos los nodos registrados, activos e inactivos, en una tabla con las columnas: Identificador, Nombre, Tipo, Parque solar y Estado.
- **RF-2**: EL SISTEMA ordenará el listado alfabéticamente por identificador, sin distinguir mayúsculas/minúsculas.
- **RF-3**: EL SISTEMA mostrará el estado como "Activo" o "Inactivo", visualmente diferenciados (por ejemplo, con colores distintos).
- **RF-4**: SI no hay nodos registrados, ENTONCES EL SISTEMA mostrará el mensaje "No hay nodos registrados." en lugar de la tabla.
- **RF-5**: EL SISTEMA mostrará un botón "Nuevo nodo" que lleva a la pantalla de alta (spec 009).
- **RF-6**: SI la API no responde, ENTONCES EL SISTEMA mostrará "No se pudo cargar el listado de nodos. Intentá nuevamente." sin cerrar la pantalla.

## Formato de los datos

- Identificador con trim (espacios al inicio y al final removidos).
- Nombre tal como fue ingresado.
- Tipo del catálogo fijo.
- Nombre del parque solar asociado.

## Casos límite

- "alfa", "Beta" y "gamma" se ordenan alfa, Beta, gamma (sin importar mayúsculas).
- Un nodo inactivo aparece en el listado igual que uno activo.
- Si se elimina un parque, los nodos asociados siguen apareciendo con el nombre del parque (aunque este esté inactivo).

## Fuera de alcance

- Filtros, búsqueda y paginación.
- Pantalla de detalle de un nodo.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Test unitario en verde para el orden de RF-2 (regla del servicio).
- Tests de arquitectura en verde.
- Listado probado a mano: con nodos, sin nodos y con la API detenida.