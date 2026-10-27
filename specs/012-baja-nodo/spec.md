# Spec 012 — Baja de Nodo Solar

Estado: pendiente

Depende de: Spec 009 (Alta), Spec 010 (Listar) y Spec 011 (usa `GetById`)

## Contexto y objetivo
Desactivar un nodo que ya no se usa, sin borrarlo de la base de datos, para conservar su historial. El nodo pasa a estado "Inactivo" pero sigue apareciendo en el listado (spec 010).

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-4**: Como Administrador, quiero dar de baja un nodo solar para desactivarlo sin perder su registro.

## Definiciones

- **Baja lógica**: el nodo pasa a estado "Inactivo". El registro se conserva y sigue apareciendo en el listado (spec 010).

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Dar de baja" en la fila de un nodo Activo del listado, EL SISTEMA pedirá confirmación con el texto "¿Confirmás la baja del nodo "<identificador>"?" y los botones "Confirmar" y "Cancelar".
- **RF-2**: CUANDO el Administrador confirme, EL SISTEMA cambiará el estado del nodo a "Inactivo", registrará la fecha de modificación en UTC, mostrará "Nodo dado de baja correctamente." y actualizará el listado.
- **RF-3**: CUANDO el usuario presione "Cancelar" en la confirmación, EL SISTEMA no hará ningún cambio.
- **RF-4**: EL SISTEMA no mostrará el botón "Dar de baja" en los nodos que ya están Inactivos.
- **RF-5**: SI se intenta dar de baja un nodo que ya está Inactivo (por ejemplo, dos usuarios a la vez), ENTONCES EL SISTEMA no hará cambios y mostrará "El nodo ya está inactivo."
- **RF-6**: SI el nodo no existe, ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe."

## Casos límite

- Un nodo dado de baja ocupa su identificador: no se puede dar de alta otro con el mismo identificador (spec 009, RF-4).

## Fuera de alcance

- Reactivar un nodo.
- Borrado físico.
- Efectos sobre nodos, inversores, paneles o mantenimientos (se definen en sus specs).
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Tests unitarios en verde para RF-2, RF-5 y RF-6 (reglas del servicio).
- Tests de arquitectura en verde.
- Baja probada a mano en el navegador (confirmar y cancelar).