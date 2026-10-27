# Spec 023 — Cancelar Mantenimiento

Estado: aprobada
Depende de: Spec 021 (Reservar Mantenimiento)

## Contexto y objetivo
Cancelar una orden de mantenimiento que ya no es necesaria o que fue agendada por error, exigiendo el registro de un motivo de cancelación explícito para fines de auditoría.

## Usuarios / Actores
- **Administrador**: cancela la orden.

## Historias de usuario
- **HU-1**: Como Administrador, quiero cancelar un mantenimiento pendiente indicando el motivo para mantener la auditoría y liberar la agenda del técnico.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador presione "Cancelar mantenimiento" en una orden "Pendiente", EL SISTEMA abrirá un diálogo solicitando obligatoriamente el motivo de cancelación (mínimo 10 caracteres).
- **RF-2**: CUANDO el Administrador ingrese un motivo válido y confirme, EL SISTEMA cambiará el estado de la orden a "Cancelado", registrará el motivo, la fecha en UTC y mostrará "Mantenimiento cancelado correctamente."
- **RF-3**: SI el motivo de cancelación está vacío o contiene menos de 10 caracteres, ENTONCES EL SISTEMA rechazará la acción y mostrará "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres."
- **RF-4**: SI el mantenimiento ya se encuentra en estado "Completado" o "Cancelado", ENTONCES EL SISTEMA no mostrará la opción de cancelación y rechazará cualquier intento con el mensaje "El mantenimiento ya se encuentra finalizado o cancelado."
- **RF-5**: CUANDO el usuario presione "Volver" en el diálogo, EL SISTEMA cerrará la ventana sin cancelar la orden.

## Casos límite
- Cancelar un mantenimiento libera automáticamente al técnico y al panel para nuevas asignaciones.

## Fuera de alcance
- Reactivación de mantenimientos cancelados (se debe crear una nueva reserva si se vuelve a requerir).

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (motivo obligatorio y validación de estado previo).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
