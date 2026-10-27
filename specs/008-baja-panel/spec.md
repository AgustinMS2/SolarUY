# Spec 008 — Baja de Panel Solar

Estado: aprobada

Depende de: Spec 005 (Alta), Spec 006 (Listar) y Spec 007 (usa `GetById`)

## Contexto y objetivo
Desactivar un panel que ya no se usa, sin borrarlo de la base de datos, para conservar su historial. El panel pasa a estado "Inactivo" pero sigue apareciendo en el listado (spec 006).

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-4**: Como Administrador, quiero dar de baja un panel solar para desactivarlo sin perder su registro.

## Definiciones

- **Baja lógica**: el panel pasa a estado "Inactivo". El registro se conserva y sigue apareciendo en el listado (spec 006).

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Dar de baja" en la fila de un panel Activo del listado, EL SISTEMA pedirá confirmación con el texto "¿Confirmás la baja del panel "<número de serie>"?" y los botones "Confirmar" y "Cancelar".
- **RF-2**: CUANDO el Administrador confirme, EL SISTEMA cambiará el estado del panel a "Inactivo", registrará la fecha de modificación en UTC, mostrará "Panel dado de baja correctamente." y actualizará el listado.
- **RF-3**: CUANDO el usuario presione "Cancelar" en la confirmación, EL SISTEMA no hará ningún cambio.
- **RF-4**: EL SISTEMA no mostrará el botón "Dar de baja" en los panels que ya están Inactivos.
- **RF-5**: SI se intenta dar de baja un panel que ya está Inactivo (por ejemplo, dos usuarios a la vez), ENTONCES EL SISTEMA no hará cambios y mostrará "El panel ya está inactivo."
- **RF-6**: SI el panel no existe, ENTONCES EL SISTEMA mostrará "El panel solicitado no existe."

## Casos límite

- Un panel dado de baja ocupa su número de serie: no se puede dar de alta otro con el mismo número de serie (spec 005, RF-4).

## Fuera de alcance

- Reactivar un panel.
- Borrado físico.
- Efectos sobre nodos, inversores o mantenimientos (se definen en sus specs).
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Tests unitarios en verde para RF-2, RF-5 y RF-6 (reglas del servicio).
- Tests de arquitectura en verde.
- Baja probada a mano en el navegador (confirmar y cancelar).