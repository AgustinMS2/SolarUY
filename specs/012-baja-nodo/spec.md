# Spec 012 — Baja de Nodo

Estado: aprobada
Depende de: Spec 009 (Alta), Spec 010 (Listar), Spec 011 (Modificar)

## Contexto y objetivo
Desactivar un nodo que deja de operar en el parque solar mediante una baja lógica, preservando su historial de telemetría y lecturas sin borrar datos físicos.

## Usuarios / Actores
- **Administrador**: ejecuta la baja del nodo.

## Historias de usuario
- **HU-1**: Como Administrador, quiero dar de baja un nodo para desactivarlo de las operaciones activas sin perder su histórico ni el de sus paneles asociados.

## Definiciones
- **Baja lógica**: El nodo pasa a estado "Inactivo". Los registros se conservan para auditoría y reportes históricos.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione "Dar de baja" en la fila de un nodo Activo del listado, EL SISTEMA solicitará confirmación con el texto "¿Confirmás la baja del nodo \"<nombre>\"?" y los botones "Confirmar" y "Cancelar".
- **RF-2**: CUANDO el Administrador confirme la baja, EL SISTEMA cambiará el estado del nodo a "Inactivo", registrará la fecha de modificación en UTC, mostrará "Nodo dado de baja correctamente." y actualizará el listado.
- **RF-3**: CUANDO el usuario presione "Cancelar" en la confirmación, EL SISTEMA cerrará el diálogo sin aplicar modificaciones.
- **RF-4**: EL SISTEMA no mostrará el botón "Dar de baja" en nodos que ya se encuentren Inactivos.
- **RF-5**: SI se intenta desactivar un nodo que ya está Inactivo, ENTONCES EL SISTEMA rechazará la acción y mostrará "El nodo ya está inactivo."
- **RF-6**: SI el nodo solicitado no existe, ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe."

## Casos límite
- Un nodo dado de baja sigue reservando su nombre dentro de ese parque para evitar colisiones históricas.
- La baja del nodo no borra los paneles ni el inversor, pero impide registrar nuevas lecturas operativas mientras esté inactivo.

## Fuera de alcance
- Reactivación de nodos inactivos.
- Borrado físico en base de datos.

## Criterios de finalización
- Tests unitarios en verde para RF-2, RF-5 y RF-6.
- Tests de arquitectura en verde.
- Verificación manual en navegador (confirmar y cancelar).

## Dudas abiertas
- Ninguna.
