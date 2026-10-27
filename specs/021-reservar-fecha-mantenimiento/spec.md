# Spec 021 — Reservar Fecha de Mantenimiento

Estado: borrador
Depende de: Spec 005 (Paneles), Spec 018 (Técnicos)

## Contexto y objetivo
Planificar y agendar una intervención de mantenimiento preventivo o correctivo sobre un panel solar o inversor específico, asignando una fecha/hora y un técnico calificado para su ejecución.

## Usuarios / Actores
- **Administrador**: crea y coordina la reserva de mantenimiento.

## Historias de usuario
- **HU-1**: Como Administrador, quiero reservar una fecha y asignar un técnico a un panel solar para planificar la reparación o revisión preventiva.

## Datos de la reserva

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Panel Solar | Sí | Debe existir y estar en estado Activo. |
| Técnico asignado | Sí | Técnico existente y en estado Activo. |
| Tipo de intervención | Sí | Selección: "Preventivo" o "Correctivo". |
| Fecha y hora programada | Sí | Fecha/hora futura (al menos 1 hora posterior al momento actual). En hora local de Uruguay, persistida en UTC. |
| Motivo / Descripción | Sí | Texto. 5 a 500 caracteres. Descripción del trabajo a realizar. |

Datos que asigna el sistema: Identificador (GUID), Estado (**Pendiente**), Fecha de registro (UTC).

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de reserva con datos válidos, EL SISTEMA creará la orden en estado "Pendiente", persistirá la fecha en UTC y mostrará "Mantenimiento reservado correctamente."
- **RF-2**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA rechazará el guardado y mostrará:
  - Panel no seleccionado o inactivo → "Debés seleccionar un panel solar activo."
  - Técnico no seleccionado o inactivo → "Debés seleccionar un técnico activo."
  - Fecha en el pasado → "La fecha y hora del mantenimiento debe ser posterior al momento actual."
  - Descripción vacía o menor a 5 caracteres → "La descripción del trabajo debe contener al menos 5 caracteres."
- **RF-3**: SI el panel solar seleccionado ya tiene otro mantenimiento programado en estado "Pendiente" para el mismo día, ENTONCES EL SISTEMA solicitará confirmación: "El panel ya tiene un mantenimiento asignado para esa fecha. ¿Deseás agendar una segunda intervención?".
- **RF-4**: CUANDO la reserva sea exitosa, EL SISTEMA limpiará el formulario y ofrecerá un botón para ver el listado general de mantenimientos.

## Requisitos no funcionales
- Calendario con selector de fecha y hora accesible.
- Fechas presentadas en hora local de Uruguay (`America/Montevideo`).

## Casos límite
- Agendar un mantenimiento correctivo para un panel con alerta de falla activa es el flujo principal recomendado.

## Fuera de alcance
- Notificaciones push al celular del técnico (se visualiza en su portal Blazor).
- Reprogramación y cancelación (specs 022 y 023).

## Criterios de finalización
- Tests unitarios en verde para validaciones de fecha y estados de panel/técnico.
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna.
