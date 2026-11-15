# Spec 032 — Activar y Desactivar Panel

Estado: aprobada (aclarada y confirmada por el usuario el 2026-11-15)
Depende de: Spec 005 (Alta Panel), Spec 013 (Inversor a Nodo)

## Contexto y objetivo
Permitir a los Administradores enviar órdenes remotas de corte (desactivación eléctrica) o reconexión (activación) sobre un panel solar específico mediante la API del inversor, con fines de seguridad durante inspecciones o aislamiento de celdas dañadas.

## Usuarios / Actores
- **Administrador**: ejecuta órdenes de control sobre el equipamiento.

## Historias de usuario
- **HU-1**: Como Administrador, quiero desactivar remotamente un panel solar para aislarlo de la generación mientras se realizan reparaciones físicas sin apagar el resto del nodo.

## Definiciones
- **"Operativo"**: el estado existente `Activo` del panel. **"Desactivado"** es un estado nuevo, distinto de **"Inactivo"** (baja lógica de la spec 008): un panel desactivado sigue existiendo y puede reactivarse.
- **Mantenimiento "En Proceso"**: toda orden de mantenimiento en estado `Pendiente` (no completada ni cancelada) asociada al panel.
- **Bitácora**: registro de auditoría de órdenes de control (quién, cuándo UTC, panel y orden enviada).

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador presione "Desactivar panel" en la vista de detalle de un panel activo, EL SISTEMA solicitará confirmación con advertencia de seguridad: "¿Confirmás la desconexión eléctrica del panel \"<serie>\"? La producción de este módulo se interrumpirá de inmediato."
- **RF-2**: CUANDO el Administrador confirme la desactivación, EL SISTEMA enviará la orden a la API del inversor/simulador, registrará el evento de control en la bitácora con usuario y fecha UTC, cambiará el estado operativo a "Desactivado" y mostrará "Orden de desconexión enviada correctamente. El panel ha sido desactivado."
- **RF-3**: CUANDO el Administrador presione "Activar panel" en un panel desactivado, EL SISTEMA solicitará confirmación: "¿Confirmás la reactivación del panel \"<serie>\" para reanudar su inyección?".
- **RF-4**: CUANDO se confirme la reactivación, EL SISTEMA enviará la orden de habilitación al inversor, registrará el evento, reestablecerá el estado a "Operativo" y mostrará "Panel reactivado correctamente. Generación reanudada."
- **RF-5**: SI el panel se encuentra asociado a una orden de mantenimiento en estado "Pendiente" (interpretación acordada de "En Proceso"), ENTONCES EL SISTEMA impedirá su reactivación remota y mostrará "No se puede reactivar el panel mientras haya personal trabajando en un mantenimiento activo."
- **RF-6**: SI el usuario autenticado tiene rol Técnico, EL SISTEMA no mostrará los botones de activación/desactivación y denegará la acción con HTTP 403 Forbidden ("Acción reservada exclusivamente para Administradores").

## Requisitos no funcionales
- Registro de auditoría estricto (quién, cuándo, panel, orden enviada).
- Feedback visual inmediato con spinner mientras la orden viaja hacia el inversor.

## Casos límite
- Pérdida de comunicación con el inversor: EL SISTEMA muestra "Error de comunicación con el inversor. La orden no pudo confirmarse." y mantiene el estado previo.
- Panel sin nodo asignado o con nodo sin inversor: EL SISTEMA permite la operación, no envía orden y registra el evento en la bitácora con la marca "orden no enviada".

## Fuera de alcance
- Ajuste de ángulo o inclinación de paneles (trackers).
- Comunicación HTTP real contra el endpoint del inversor: se implementa con la spec 036 (Simular Lecturas del Inversor); en esta spec la orden se envía mediante un stub detrás de una interfaz.

## Criterios de finalización
- Tests unitarios en verde para las validaciones del servicio (roles, estados y bloqueos por mantenimiento).
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna.
