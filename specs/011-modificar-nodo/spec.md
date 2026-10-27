# Spec 011 — Modificar Nodo

Estado: borrador
Depende de: Spec 009 (Alta de Nodo), Spec 010 (Listar Nodos)

## Contexto y objetivo
Permitir la actualización de la información de un nodo registrado (por ejemplo, corrección de su nombre identificador) sin alterar su historial ni sus relaciones ya consolidadas.

## Usuarios / Actores
- **Administrador**: edita los datos del nodo.

## Historias de usuario
- **HU-1**: Como Administrador, quiero modificar el nombre de un nodo para corregir errores tipográficos o reflejar cambios de señalización en campo.

## Datos modificables
- **Nombre**: Obligatorio. Texto de 1 a 100 caracteres. Único dentro del mismo parque solar (sin distinguir mayúsculas/minúsculas).
- *No modificables desde esta pantalla*: Parque solar asociado, fecha de creación ni estado.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Editar" en una fila del listado de nodos, EL SISTEMA abrirá el formulario de edición con el nombre actual y el parque bloqueado (solo lectura).
- **RF-2**: CUANDO el Administrador guarde un nombre válido, EL SISTEMA actualizará el nodo, registrará la fecha de modificación en UTC, mostrará "Nodo actualizado correctamente." y volverá al listado.
- **RF-3**: SI el nombre está vacío o supera los 100 caracteres, ENTONCES EL SISTEMA no guardará cambios y mostrará el mensaje correspondiente de la spec 009 (RF-2).
- **RF-4**: SI el nuevo nombre ya lo utiliza **otro** nodo dentro del mismo parque (sin distinguir mayúsculas/minúsculas y con trim), ENTONCES EL SISTEMA rechazará el guardado y mostrará "Ya existe un nodo con el nombre \"<nombre>\" en este parque solar."
- **RF-5**: SI el nodo solicitado no existe, ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe." y un botón para volver al listado.
- **RF-6**: CUANDO el usuario presione "Cancelar", EL SISTEMA volverá al listado sin persistir cambios.

## Casos límite
- Cambiar solo las mayúsculas/minúsculas del propio nodo (ej. "nodo-01" → "Nodo-01") es válido y no colisiona consigo mismo.
- Guardar sin cambios actualiza la fecha de modificación.

## Fuera de alcance
- Reasignar el nodo a otro parque solar distinto (los nodos son fijos a su ubicación física).
- Modificar el estado (eso corresponde a la spec 012 de Baja).

## Criterios de finalización
- Tests unitarios en verde para RF-3, RF-4 y RF-5.
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
