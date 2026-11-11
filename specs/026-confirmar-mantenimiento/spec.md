# Spec 026 — Confirmar Mantenimiento

Estado: aprobada
Depende de: Spec 025 (Mis Mantenimientos). El RF-3 depende además de la Spec 031 (Alertas).

## Contexto y objetivo
Permitir al Técnico reportar la finalización de una intervención de mantenimiento, registrando las observaciones de la inspección, repuestos o componentes reparados/reemplazados y el resultado final, cambiando el estado de la orden a "Completado".

## Usuarios / Actores
- **Técnico**: registra la ejecución de la tarea.
- **Administrador**: puede registrar la confirmación en caso de contingencia.

## Historias de usuario
- **HU-1**: Como Técnico, quiero confirmar el mantenimiento completado detallando las observaciones para documentar la reparación y reestablecer el estado operativo del panel.

## Datos de la confirmación

| Campo | Obligatorio | Regla (tipo, rango, largo) |
| :--- | :--- | :--- |
| Observaciones técnicas | Sí | Texto. Trim al inicio y fin. 10 a 1000 caracteres. Descripción detallada de las acciones realizadas. |
| Componentes reemplazados | No | Texto libre (ej. "Fusible de string 15A", "Conector MC4"). Trim al inicio y fin. Máx. 300 caracteres. Si queda vacío se guarda sin valor. |
| Estado del panel tras la tarea | Sí | Selección: "Operativo / Normal" o "Requiere seguimiento adicional". Se guarda en la orden. |
| Fecha y hora real de finalización | Sí | Asignada automáticamente por el sistema al confirmar (UTC). |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el técnico envíe el formulario de confirmación con observaciones válidas, EL SISTEMA actualizará el estado de la orden a "Completado", guardará la fecha/hora de finalización en UTC y mostrará "Mantenimiento confirmado y completado correctamente."
- **RF-2**: SI las observaciones están vacías o contienen menos de 10 caracteres, ENTONCES EL SISTEMA rechazará el envío y mostrará "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres."
  - SI las observaciones superan los 1000 caracteres, ENTONCES EL SISTEMA mostrará "Las observaciones técnicas no pueden superar los 1000 caracteres."
  - SI los componentes reemplazados superan los 300 caracteres, ENTONCES EL SISTEMA mostrará "Los componentes reemplazados no pueden superar los 300 caracteres."
  - SI no se selecciona el estado del panel tras la tarea, ENTONCES EL SISTEMA mostrará "Seleccioná el estado del panel tras la tarea."
- **RF-3**: CUANDO la orden quede en estado "Completado", si el panel asociado poseía alertas activas de falla, EL SISTEMA resolverá automáticamente dichas alertas marcándolas como "Atendidas". *(Pendiente: se implementa junto con la spec 031, que es la que crea las alertas. Hasta entonces la confirmación no modifica alertas.)*
- **RF-4**: SI la orden ya se encuentra en estado "Completado" o "Cancelado", ENTONCES EL SISTEMA impedirá una segunda confirmación y mostrará "Este mantenimiento ya fue completado previamente."
- **RF-5**: CUANDO el técnico presione "Cancelar", EL SISTEMA regresará a la lista de tareas sin modificar el estado pendiente.
- **RF-6**: SI el mantenimiento no existe, ENTONCES EL SISTEMA mostrará "El mantenimiento solicitado no existe."

## Requisitos no funcionales
- Interfaz en español, responsive para escritorio y celular (el técnico confirma desde el celular en campo).
- La confirmación solo está disponible para los roles Técnico y Administrador.

## Casos límite
- Intervención donde no fue necesario cambiar repuestos: campo "Componentes reemplazados" queda vacío de forma válida.
- Orden de otro técnico: cualquier Técnico o Administrador puede confirmar cualquier orden pendiente (decisión del equipo; no se valida que el técnico logueado sea el asignado).
- "Requiere seguimiento adicional" solo queda registrado en la orden: no cambia el estado del panel ni agenda un mantenimiento nuevo.
- Orden de un técnico o de un panel dados de baja posteriormente: se puede confirmar igual mientras esté pendiente.
- Orden de fecha futura: se puede confirmar (el sistema no exige que la fecha programada haya llegado).

## Fuera de alcance
- Carga de fotografías de la reparación (se prevé para una fase posterior).
- Firma digital manuscrita.
- Agendar automáticamente un mantenimiento de seguimiento y cambiar el estado del panel.

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (RF-1, RF-2, RF-4 y RF-6; RF-3 queda para la spec 031).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna. Decisiones tomadas: cualquier Técnico o Administrador puede confirmar cualquier orden pendiente; el estado del panel tras la tarea se guarda en la orden; RF-3 se difiere a la spec 031 porque las alertas todavía no existen.
