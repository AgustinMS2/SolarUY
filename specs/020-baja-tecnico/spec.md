# Spec 020 — Baja de Técnico

Estado: aprobada
Depende de: Spec 018 (Alta), Spec 019 (Listar)

## Contexto y objetivo
Desactivar a un técnico que cesa funciones en la empresa o se encuentra temporalmente desafectado, mediante baja lógica, asegurando que no se pierda el historial de mantenimientos que haya confirmado en el pasado.

## Usuarios / Actores
- **Administrador**: ejecuta la baja.

## Historias de usuario
- **HU-1**: Como Administrador, quiero dar de baja a un técnico para que no pueda recibir nuevas asignaciones de trabajo sin alterar los registros históricos que completó.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador presione "Dar de baja" en la fila de un técnico Activo, EL SISTEMA solicitará confirmación: "¿Confirmás la baja del técnico \"<nombre>\"?".
- **RF-2**: SI el técnico posee mantenimientos pendientes o programados en el futuro, ENTONCES EL SISTEMA impedirá la baja y mostrará: "No se puede dar de baja al técnico porque tiene <cantidad> mantenimiento(s) pendiente(s) de resolución. Reasigna o cancela esos mantenimientos previamente."
- **RF-3**: CUANDO el Administrador confirme y el técnico no tenga tareas pendientes, EL SISTEMA cambiará su estado a "Inactivo", registrará la fecha en UTC y mostrará "Técnico dado de baja correctamente."
- **RF-4**: CUANDO el usuario presione "Cancelar", EL SISTEMA cerrará el modal sin aplicar modificaciones.
- **RF-5**: EL SISTEMA no mostrará el botón "Dar de baja" en técnicos que ya estén Inactivos.

## Casos límite
- El técnico dado de baja no aparece en el listado desplegable de selección de técnicos para nuevas reservas de mantenimiento (spec 021).
- Su correo y documento siguen reservados para evitar suplantaciones históricas.

## Fuera de alcance
- Reactivación de técnicos.
- Borrado físico en base de datos.

## Criterios de finalización
- Tests unitarios en verde para validación de tareas pendientes (RF-2) y cambio de estado (RF-3).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
