# Spec 002 — Listar Parques Solares

Estado: implementada
Depende de: Spec 001 (Alta de Parque Solar)

## Contexto y objetivo
Ver todos los parques registrados y su estado en una sola pantalla. Esta pantalla es además el punto de entrada para el alta (001), la modificación (003) y la baja (004).

## Usuarios / Actores
- **Administrador** y **Técnico**: consultan el listado. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-2**: Como usuario de SolarUY, quiero ver el listado de parques solares con su estado para conocer qué parques existen y cuáles están activos.

## Requisitos funcionales (EARS)
- **RF-1**: CUANDO un usuario abra la pantalla de parques, EL SISTEMA mostrará todos los parques registrados, activos e inactivos, en una tabla con las columnas: Nombre, Latitud, Longitud, Capacidad máxima (kW), Umbral de alerta (kW) y Estado.
- **RF-2**: EL SISTEMA ordenará el listado alfabéticamente por nombre, sin distinguir mayúsculas/minúsculas.
- **RF-3**: EL SISTEMA mostrará el estado como "Activo" o "Inactivo", visualmente diferenciados (por ejemplo, con colores distintos).
- **RF-4**: SI no hay parques registrados, ENTONCES EL SISTEMA mostrará el mensaje "No hay parques registrados." en lugar de la tabla.
- **RF-5**: EL SISTEMA mostrará un botón "Nuevo parque" que lleva a la pantalla de alta (spec 001).
- **RF-6**: SI la API no responde, ENTONCES EL SISTEMA mostrará "No se pudo cargar el listado de parques. Intentá nuevamente." sin cerrar la pantalla.

## Formato de los datos
- Latitud y longitud con 4 decimales.
- Capacidad y umbral con hasta 2 decimales, seguidos de "kW".

## Casos límite
- "alfa", "Beta" y "gamma" se ordenan alfa, Beta, gamma (sin importar mayúsculas).
- Un parque inactivo aparece en el listado igual que uno activo.

## Fuera de alcance
- Filtros, búsqueda y paginación.
- Pantalla de detalle de un parque.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización
- Test unitario en verde para el orden de RF-2 (regla del servicio).
- Tests de arquitectura en verde.
- Listado probado a mano: con parques, sin parques y con la API detenida.
