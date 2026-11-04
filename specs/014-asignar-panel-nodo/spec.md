# Spec 014 — Asignar Panel a Nodo

Estado: aprobada
Depende de: Spec 005 (Alta Panel), Spec 009 (Alta Nodo), Spec 010 (Listar Nodos), Spec 013 (Asignar Inversor a Nodo)

## Contexto y objetivo
Conectar y asociar físicamente paneles solares a un nodo de agregación. Un panel solar pertenece a un parque y se conecta a un nodo específico dentro de ese mismo parque para conformar los strings de generación.

## Usuarios / Actores
- **Administrador**: organiza la distribución de paneles en los nodos.

## Historias de usuario
- **HU-1**: Como Administrador, quiero asociar paneles solares registrados a un nodo específico para estructurar los circuitos de generación y monitoreo.
- **HU-2**: Como Administrador, quiero desasociar paneles de un nodo para dejarlos disponibles dentro del parque.

## Definiciones
- **Panel disponible**: panel activo que no está asociado a ningún nodo ("Sin asignar").
- **Potencia acumulada de un nodo**: suma de la potencia de todos los paneles activos asociados al nodo, expresada en kW (la potencia de cada panel se registra en W).
- **Reasignar**: asociar a un nodo un panel que hoy pertenece a otro nodo del mismo parque.

## Datos de la operación

| Campo | Obligatorio | Regla |
| :--- | :--- | :--- |
| Nodo destino | Sí | Nodo existente y activo (para asociar). |
| Paneles | Sí | De 1 a 50 paneles existentes, activos y del mismo parque que el nodo (para asociar). |
| Confirmación | No | El Administrador confirma las advertencias (reasignación y potencia) para continuar. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador seleccione uno o más paneles disponibles y los asocie a un nodo activo del mismo parque, EL SISTEMA registrará la vinculación y mostrará "Paneles asignados correctamente al nodo."
- **RF-2**: EL SISTEMA solo permitirá asociar paneles que pertenezcan al mismo parque solar que el nodo destino.
  - SI algún panel pertenece a otro parque, ENTONCES EL SISTEMA rechazará la asignación completa y mostrará "No se pueden asociar paneles de otro parque."
- **RF-3**: SI se intenta asociar un panel que ya está asignado a otro nodo, ENTONCES EL SISTEMA solicitará confirmación: "El panel \"<serie>\" ya pertenece al nodo \"<nodo_origen>\". ¿Deseás reasignarlo a este nodo?" y no modificará nada hasta recibir la respuesta.
  - SI hay más de un panel en esa situación, EL SISTEMA mencionará el primero y agregará "Se reasignarán también <n> paneles más." (en singular, "1 panel más", cuando n es 1)
  - CUANDO el Administrador confirme, EL SISTEMA reasignará los paneles y mostrará "Paneles asignados correctamente al nodo."
  - CUANDO el Administrador cancele, EL SISTEMA no modificará nada.
  - Los paneles que ya pertenecen al nodo destino se ignoran, sin error ni advertencia.
- **RF-4**: SI el panel o el nodo están en estado Inactivo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "No se pueden asociar paneles inactivos o a nodos inactivos."
- **RF-5**: EL SISTEMA permitirá desasociar un panel de un nodo, dejándolo en estado disponible dentro del parque.
  - CUANDO el Administrador desasocie uno o más paneles del nodo, EL SISTEMA los dejará "Sin asignar" y mostrará "Paneles desasociados correctamente del nodo."
  - SI algún panel no está asociado a ese nodo, ENTONCES EL SISTEMA rechazará la operación completa y mostrará "El panel \"<serie>\" no está asociado a este nodo."
- **RF-6**: SI la suma de potencia de los paneles asignados supera la capacidad máxima admisible del inversor del nodo, ENTONCES EL SISTEMA emitirá una advertencia: "La potencia acumulada de los paneles (<potencia_total> kW) supera la capacidad nominal del inversor (<capacidad> kW)." (permite confirmar bajo responsabilidad del administrador).
  - La advertencia se calcula sobre la potencia acumulada del nodo después de la asignación. Si el nodo no tiene inversor, no hay advertencia.
  - Las advertencias de RF-3 y RF-6 se muestran juntas en una sola confirmación. CUANDO el Administrador confirme, EL SISTEMA completará la asignación; CUANDO cancele, no modificará nada.
- **RF-7**: SI el Administrador no selecciona ningún panel, ENTONCES EL SISTEMA mostrará "Seleccioná al menos un panel."; SI selecciona más de 50, ENTONCES EL SISTEMA mostrará "Se pueden asignar hasta 50 paneles a la vez." (aplica a asociar y a desasociar).
- **RF-8**: SI el nodo no existe, ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe."; SI algún panel seleccionado no existe, ENTONCES EL SISTEMA mostrará "Uno o más paneles seleccionados no existen."
- **RF-9**: EL SISTEMA mostrará la cantidad real de paneles asociados en el listado de nodos y, en el listado de paneles, el nodo de cada panel o "Sin asignar" si no tiene.

## Requisitos no funcionales
- Interfaz en español, responsive para escritorio y celular.
- Las acciones de asociar, reasignar y desasociar solo están disponibles para el Administrador.

## Casos límite
- Asignación múltiple en lote (batch) de hasta 50 paneles a la vez.
- Paneles sin nodo asociado aparecen en el listado como "Sin asignar".
- Desasociar se permite también en un nodo inactivo y para paneles inactivos (para dejarlos libres).
- Un panel dado de baja (spec 008) que ya estaba asociado a un nodo conserva su asociación, no cuenta para la potencia acumulada y puede desasociarse.
- Orden de validación, cuando hay varios errores: nodo inexistente, lote vacío o de más de 50, nodo inactivo, panel inexistente, panel inactivo, panel de otro parque y, por último, las advertencias de RF-3 y RF-6.
- Si todos los paneles seleccionados ya pertenecen al nodo destino, la operación termina bien sin cambios.

## Fuera de alcance
- Monitoreo de generación por panel (specs de Monitoreo).
- Tareas de mantenimiento sobre el panel (specs de Mantenimiento).
- Qué ocurre con las asociaciones cuando se da de baja un nodo (spec 012).

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (RF-1 a RF-9), en particular parque cruzado (RF-2), nodos y paneles inactivos (RF-4), reasignación (RF-3) y advertencia de potencia (RF-6).
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna. Decisiones tomadas: las advertencias de reasignación y de potencia se confirman juntas con una sola respuesta; la potencia acumulada cuenta solo paneles activos del nodo; desasociar es una operación en lote con el mismo límite de 50; el listado de nodos pasa a mostrar la cantidad real de paneles.
