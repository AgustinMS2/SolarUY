# Spec 007 — Modificar Panel Solar

Estado: pendiente

Depende de: Spec 005 (Alta) y Spec 006 (Listar)

## Contexto y objetivo
Corregir o actualizar los datos de un panel ya registrado. El administrador puede actualizar la potencia, eficiencia o asociar el panel a un parque diferente.

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-3**: Como Administrador, quiero editar los datos de un panel existente para mantener la información actualizada (potencia, eficiencia, parque asociado).

## Datos modificables

Nombre, Potencia (W), Eficiencia (%), **con las mismas reglas y mensajes de la spec 005 (RF-2)**. No se modifican desde esta pantalla: identificador, estado ni fecha de creación. Sí se puede cambiar el parque solar asociado.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Editar" en una fila del listado (spec 006), EL SISTEMA abrirá el formulario de edición con los datos actuales del panel cargados.
- **RF-2**: CUANDO el Administrador guarde datos válidos, EL SISTEMA actualizará el panel, registrará la fecha de modificación en UTC, mostrará "Panel actualizado correctamente." y volverá al listado.
- **RF-3**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no guardará los cambios y mostrará el mensaje correspondiente de la spec 005 (RF-2).
- **RF-4**: SI el nuevo número de serie ya lo usa **otro** panel (sin distinguir mayúsculas/minúsculas y con trim), ENTONCES EL SISTEMA no guardará los cambios y mostrará "Ya existe un panel con el número de serie "<serie>".".
- **RF-5**: SI el panel no existe (por ejemplo, un identificador inválido en la URL), ENTONCES EL SISTEMA mostrará "El panel solicitado no existe." y un enlace para volver al listado.
- **RF-6**: CUANDO el usuario presione "Cancelar", EL SISTEMA volverá al listado sin guardar cambios.
- **RF-7**: SI el panel está Inactivo, ENTONCES EL SISTEMA permitirá igualmente modificar sus datos (el estado no cambia).

## Casos límite

- Cambiar solo la potencia o la eficiencia es válido.
- Cambiar el número de serie del propio panel a sí mismo es válido: no chica consigo mismo (ej. "panel-001" → "Panel-001" es válido).
- Guardar sin cambios es válido y actualiza la fecha de modificación.

## Fuera de alcance

- Cambiar el estado (eso es la Baja, spec 008; la reactivación no está definida todavía).
- Control de ediciones simultáneas: si dos personas editan a la vez, queda la última que guarda.
- Historial de cambios.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Tests unitarios en verde para RF-3, RF-4 y RF-5 (reglas del servicio) y para el caso "cambiar mayúsculas del propio nombre".
- Tests de arquitectura en verde.
- Edición probada a mano en el navegador.