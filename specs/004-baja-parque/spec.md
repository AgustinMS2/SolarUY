# Spec 004 — Baja de Parque Solar

Estado: aprobada
Depende de: Spec 001 (Alta), Spec 002 (Listar) y Spec 003 (usa `GetById`)

## Contexto y objetivo
Desactivar un parque que ya no se usa, sin borrarlo de la base de datos, para conservar su historial.

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-4**: Como Administrador, quiero dar de baja un parque solar para desactivarlo sin perder su registro.

## Definiciones
- **Baja lógica**: el parque pasa a estado "Inactivo". El registro se conserva y sigue apareciendo en el listado (spec 002).

## Requisitos funcionales (EARS)
- **RF-1**: CUANDO el usuario presione "Dar de baja" en la fila de un parque Activo del listado, EL SISTEMA pedirá confirmación con el texto "¿Confirmás la baja del parque "<nombre>"?" y los botones "Confirmar" y "Cancelar".
- **RF-2**: CUANDO el Administrador confirme, EL SISTEMA cambiará el estado del parque a "Inactivo", registrará la fecha de modificación en UTC, mostrará "Parque dado de baja correctamente." y actualizará el listado.
- **RF-3**: CUANDO el usuario presione "Cancelar" en la confirmación, EL SISTEMA no hará ningún cambio.
- **RF-4**: EL SISTEMA no mostrará el botón "Dar de baja" en los parques que ya están Inactivos.
- **RF-5**: SI se intenta dar de baja un parque que ya está Inactivo (por ejemplo, dos usuarios a la vez), ENTONCES EL SISTEMA no hará cambios y mostrará "El parque ya está inactivo."
- **RF-6**: SI el parque no existe, ENTONCES EL SISTEMA mostrará "El parque solicitado no existe."

## Casos límite
- Un parque dado de baja sigue ocupando su nombre: no se puede dar de alta otro con el mismo nombre (spec 001, RF-3).

## Fuera de alcance
- Reactivar un parque.
- Borrado físico.
- Efectos sobre nodos, inversores, paneles o mantenimientos (se definen en sus specs).
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización
- Tests unitarios en verde para RF-2, RF-5 y RF-6 (reglas del servicio).
- Tests de arquitectura en verde.
- Baja probada a mano en el navegador (confirmar y cancelar).
