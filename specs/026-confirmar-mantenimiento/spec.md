# Spec 026 — Confirmar Mantenimiento

Estado: aprobada
Depende de: Spec 025 (Mis Mantenimientos)

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
| Observaciones técnicas | Sí | Texto. 10 a 1000 caracteres. Descripción detallada de las acciones realizadas. |
| Componentes reemplazados | No | Texto libre (ej. "Fusible de string 15A", "Conector MC4"). Máx. 300 caracteres. |
| Estado del panel tras la tarea | Sí | Selección: "Operativo / Normal" o "Requiere seguimiento adicional". |
| Fecha y hora real de finalización | Sí | Asignada automáticamente por el sistema al confirmar (UTC). |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el técnico envíe el formulario de confirmación con observaciones válidas, EL SISTEMA actualizará el estado de la orden a "Completado", guardará la fecha/hora de finalización en UTC y mostrará "Mantenimiento confirmado y completado correctamente."
- **RF-2**: SI las observaciones están vacías o contienen menos de 10 caracteres, ENTONCES EL SISTEMA rechazará el envío y mostrará "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres."
- **RF-3**: CUANDO la orden quede en estado "Completado", si el panel asociado poseía alertas activas de falla, EL SISTEMA resolverá automáticamente dichas alertas marcándolas como "Atendidas".
- **RF-4**: SI la orden ya se encuentra en estado "Completado" o "Cancelado", ENTONCES EL SISTEMA impedirá una segunda confirmación y mostrará "Este mantenimiento ya fue completado previamente."
- **RF-5**: CUANDO el técnico presione "Cancelar", EL SISTEMA regresará a la lista de tareas sin modificar el estado pendiente.

## Casos límite
- Intervención donde no fue necesario cambiar repuestos: campo "Componentes reemplazados" queda vacío de forma válida.

## Fuera de alcance
- Carga de fotografías de la reparación (se prevé para una fase posterior).
- Firma digital manuscrita.

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (RF-1, RF-2, RF-3, RF-4).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
