# Spec 010 — Listar Nodos

Estado: borrador
Depende de: Spec 009 (Alta de Nodo)

## Contexto y objetivo
Visualizar todos los nodos registrados en el sistema, permitiendo filtrar por parque solar y conocer cuántos paneles e inversores tienen asociados, además de su estado operativo.

## Usuarios / Actores
- **Administrador** y **Técnico**: consultan la topología y nodos de los parques.

## Historias de usuario
- **HU-1**: Como usuario, quiero ver el listado de nodos con su parque, cantidad de paneles y estado para tener visibilidad de la infraestructura del parque.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario ingrese a la pantalla de nodos, EL SISTEMA mostrará una tabla con todos los nodos registrados: Nombre, Parque solar, Inversor asignado, Cantidad de paneles asociados y Estado ("Activo" / "Inactivo").
- **RF-2**: EL SISTEMA ordenará el listado alfabéticamente por nombre de parque y luego por nombre de nodo.
- **RF-3**: EL SISTEMA permitirá filtrar el listado mediante un selector desplegable de parque solar.
- **RF-4**: SI no hay nodos registrados en el sistema o para el parque seleccionado, ENTONCES EL SISTEMA mostrará el mensaje "No hay nodos registrados." en lugar de la tabla.
- **RF-5**: EL SISTEMA mostrará un botón "Nuevo nodo" que navegará a la pantalla de alta (spec 009).
- **RF-6**: SI la API no responde, ENTONCES EL SISTEMA mostrará "No se pudo cargar el listado de nodos. Intentá nuevamente." sin cerrar la pantalla.

## Requisitos no funcionales
- Estados visualmente diferenciados con distintivos de color (Activo en verde, Inactivo en gris/rojo).
- Diseño responsive adaptable a tablets y smartphones.

## Casos límite
- Nodo sin paneles muestra "0 paneles".
- Nodo sin inversor muestra "Sin inversor asignado".
- Nodos inactivos se listan con su estado correspondiente.

## Fuera de alcance
- Modificación y baja (specs 011 y 012).
- Control de roles.

## Criterios de finalización
- Tests unitarios en verde para las reglas de ordenamiento y filtrado del servicio.
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
