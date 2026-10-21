# Spec 003 — Modificar Parque Solar

Estado: aprobada
Depende de: Spec 001 (Alta) y Spec 002 (Listar)

## Contexto y objetivo
Corregir o actualizar los datos de un parque ya registrado.

## Usuarios / Actores
- **Administrador**. (El control de acceso por rol queda para la spec de Usuarios.)

## Historia de usuario
- **HU-3**: Como Administrador, quiero editar los datos de un parque existente para mantener la información actualizada.

## Datos modificables
Nombre, Latitud, Longitud, Capacidad máxima (kW) y Umbral de alerta (kW), **con las mismas reglas y mensajes de la spec 001 (RF-2)**.
No se modifican desde esta pantalla: identificador, estado ni fecha de creación.

## Requisitos funcionales (EARS)
- **RF-1**: CUANDO el usuario presione "Editar" en una fila del listado (spec 002), EL SISTEMA abrirá el formulario de edición con los datos actuales del parque cargados.
- **RF-2**: CUANDO el Administrador guarde datos válidos, EL SISTEMA actualizará el parque, registrará la fecha de modificación en UTC, mostrará "Parque actualizado correctamente." y volverá al listado.
- **RF-3**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no guardará los cambios y mostrará el mensaje correspondiente de la spec 001 (RF-2).
- **RF-4**: SI el nuevo nombre ya lo usa **otro** parque (sin distinguir mayúsculas/minúsculas y con trim), ENTONCES EL SISTEMA no guardará los cambios y mostrará "Ya existe un parque con el nombre "<nombre>"."
- **RF-5**: SI el parque no existe (por ejemplo, un identificador inválido en la URL), ENTONCES EL SISTEMA mostrará "El parque solicitado no existe." y un enlace para volver al listado.
- **RF-6**: CUANDO el usuario presione "Cancelar", EL SISTEMA volverá al listado sin guardar cambios.
- **RF-7**: SI el parque está Inactivo, ENTONCES EL SISTEMA permitirá igualmente modificar sus datos (el estado no cambia).

## Casos límite
- Cambiar solo mayúsculas del propio nombre ("parque norte" → "Parque Norte") es válido: no choca consigo mismo.
- Guardar sin cambios es válido y actualiza la fecha de modificación.

## Fuera de alcance
- Cambiar el estado (eso es la Baja, spec 004; la reactivación no está definida todavía).
- Control de ediciones simultáneas: si dos personas editan a la vez, queda la última que guarda.
- Historial de cambios.
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización
- Tests unitarios en verde para RF-2, RF-4 y RF-5 (reglas del servicio) y para el caso "cambiar mayúsculas del propio nombre".
- Tests de arquitectura en verde.
- Edición probada a mano en el navegador.

