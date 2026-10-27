# Spec 022 — Modificar Reserva de Mantenimiento

Estado: aprobada
Depende de: Spec 021 (Reservar Fecha de Mantenimiento)

## Contexto y objetivo
Permitir al Administrador reprogramar la fecha/hora, reasignar el técnico o ajustar la descripción de una orden de mantenimiento que aún se encuentre en estado "Pendiente".

## Usuarios / Actores
- **Administrador**: ajusta la planificación de mantenimientos.

## Historias de usuario
- **HU-1**: Como Administrador, quiero modificar la fecha o el técnico de un mantenimiento pendiente para adaptarme a imprevistos climáticos o de disponibilidad de personal.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador presione "Modificar" en una orden en estado "Pendiente", EL SISTEMA abrirá el formulario de edición con los datos actuales pre-cargados.
- **RF-2**: CUANDO el usuario guarde una nueva fecha/hora válida o un nuevo técnico activo, EL SISTEMA actualizará la orden, registrará la fecha de modificación en UTC y mostrará "Reserva de mantenimiento actualizada correctamente."
- **RF-3**: SI el mantenimiento ya se encuentra en estado "Completado" o "Cancelado", ENTONCES EL SISTEMA impedirá su edición y mostrará "No se pueden modificar mantenimientos que ya han sido completados o cancelados."
- **RF-4**: SI la nueva fecha programada se sitúa en el pasado, ENTONCES EL SISTEMA mostrará "La nueva fecha programada debe ser posterior al momento actual."
- **RF-5**: CUANDO el usuario presione "Cancelar", EL SISTEMA volverá al listado sin persistir modificaciones.

## Casos límite
- Cambiar únicamente el técnico manteniendo la misma fecha es válido.
- Cambiar la descripción agregando detalles de herramientas requeridas es válido.

## Fuera de alcance
- Cancelación total del mantenimiento (spec 023).
- Ejecución de la tarea (spec 026).

## Criterios de finalización
- Tests unitarios en verde para validaciones de estado y fechas.
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
