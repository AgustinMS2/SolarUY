# Spec 014 — Asignar Panel a Nodo

Estado: borrador
Depende de: Spec 005 (Alta Panel), Spec 009 (Alta Nodo), Spec 010 (Listar Nodos)

## Contexto y objetivo
Conectar y asociar físicamente paneles solares a un nodo de agregación. Un panel solar pertenece a un parque y se conecta a un nodo específico dentro de ese mismo parque para conformar los strings de generación.

## Usuarios / Actores
- **Administrador**: organiza la distribución de paneles en los nodos.

## Historias de usuario
- **HU-1**: Como Administrador, quiero asociar paneles solares registrados a un nodo específico para estructurar los circuitos de generación y monitoreo.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador seleccione uno o más paneles disponibles y los asocie a un nodo activo del mismo parque, EL SISTEMA registrará la vinculación y mostrará "Paneles asignados correctamente al nodo."
- **RF-2**: EL SISTEMA solo permitirá asociar paneles que pertenezcan al mismo parque solar que el nodo destino.
- **RF-3**: SI se intenta asociar un panel que ya está asignado a otro nodo, ENTONCES EL SISTEMA solicitará confirmación: "El panel \"<serie>\" ya pertenece al nodo \"<nodo_origen>\". ¿Deseás reasignarlo a este nodo?".
- **RF-4**: SI el panel o el nodo están en estado Inactivo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "No se pueden asociar paneles inactivos o a nodos inactivos."
- **RF-5**: EL SISTEMA permitirá desasociar un panel de un nodo, dejándolo en estado disponible dentro del parque.
- **RF-6**: SI la suma de potencia de los paneles asignados supera la capacidad máxima admisible del inversor del nodo, ENTONCES EL SISTEMA emitirá una advertencia: "La potencia acumulada de los paneles (<potencia_total> kW) supera la capacidad nominal del inversor (<capacidad> kW)." (permite confirmar bajo responsabilidad del administrador).

## Casos límite
- Asignación múltiple en lote (batch) de hasta 50 paneles a la vez.
- Paneles sin nodo asociado aparecen en el listado como "Sin asignar".

## Fuera de alcance
- Monitoreo de generación por panel (specs de Monitoreo).
- Tareas de mantenimiento sobre el panel (specs de Mantenimiento).

## Criterios de finalización
- Tests unitarios en verde para validaciones de parque cruzado (RF-2) y nodos inactivos (RF-4).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
