# Spec 011 — Modificar Nodo Solar

Estado: pendiente

Depende de: Spec 009 (Alta) y Spec 010 (Listar)

## Contexto y objetivo
Corregir o actualizar los datos de un nodo ya registrado. El administrador puede actualizar el nombre, el tipo o asociar el nodo a un parque diferente.

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-3**: Como Administrador, quiero editar los datos de un nodo existente para mantener la información actualizada (nombre, tipo, parque asociado).

## Datos modificables

Identificador, Nombre, Tipo, **con las mismas reglas y mensajes de la spec 009 (RF-2)**. No se modifican desde esta pantalla: identificador (solo lectura), estado ni fecha de creación. Sí se puede cambiar el parque solar asociado.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Editar" en una fila del listado (spec 010), EL SISTEMA abrirá el formulario de edición con los datos actuales del nodo cargados.
- **RF-2**: CUANDO el Administrador guarde datos válidos, EL SISTEMA actualizará el nodo, registrará la fecha de modificación en UTC, mostrará "Nodo actualizado correctamente." y volverá al listado.
- **RF-3**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no guardará los cambios y mostrará el mensaje correspondiente de la spec 009 (RF-2).
- **RF-4**: SI el nuevo identificador ya lo usa **otro** nodo (sin distinguir mayúsculas/minúsculas y con trim), ENTONCES EL SISTEMA no guardará los cambios y mostrará "Ya existe un nodo con el identificador "<identificador>".".
- **RF-5**: SI el nodo no existe (por ejemplo, un identificador inválido en la URL), ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe." y un enlace para volver al listado.
- **RF-6**: CUANDO el usuario presione "Cancelar", EL SISTEMA volverá al listado sin guardar cambios.
- **RF-7**: SI el nodo está Inactivo, ENTONCES EL SISTEMA permitirá igualmente modificar sus datos (el estado no cambia).

## Casos límite

- Cambiar solo el nombre o el tipo es válido.
- Cambiar el identificador del propio nodo a sí mismo es válido: no chica consigo mismo (ej. "nodo-01" → "Nodo-01" es válido).
- Guardar sin cambios es válido y actualiza la fecha de modificación.

## Fuera de alcance

- Cambiar el estado (eso es la Baja, spec 012; la reactivación no está definida todavía).
- Control de ediciones simultáneas: si dos personas editan a la vez, queda la última que guarda.
- Historial de cambios.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización

- Tests unitarios en verde para RF-3, RF-4 y RF-5 (reglas del servicio) y para el caso "cambiar mayúsculas del propio nombre".
- Tests de arquitectura en verde.
- Edición probada a mano en el navegador.