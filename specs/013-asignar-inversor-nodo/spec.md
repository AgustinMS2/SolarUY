# Spec 013 — Asignar Inversor a Nodo

Estado: aprobada
Depende de: Spec 009 (Alta de Nodo), Spec 010 (Listar Nodos)

## Contexto y objetivo
Vincular un inversor fotovoltaico específico a un nodo de agregación. Cada nodo cuenta como máximo con un inversor activo encargado de la conversión de corriente continua a alterna y de la telemetría del conjunto.

## Usuarios / Actores
- **Administrador**: configura la asignación de hardware.

## Historias de usuario
- **HU-1**: Como Administrador, quiero asignar un inversor con su número de serie, marca, modelo y capacidad a un nodo para habilitar la recopilación de datos y telemetría.
- **HU-2**: Como Administrador, quiero desvincular el inversor de un nodo para dejarlo "Sin inversor asignado" y liberar ese inversor.

## Definiciones
- **Inversor**: equipo que convierte la corriente continua de los paneles en corriente alterna y reporta la telemetría del nodo.
- **Reemplazar un inversor**: asignar a un nodo que ya tiene inversor otro con distinto número de serie. El inversor anterior deja de existir en el sistema (no se conserva historial) y su número de serie queda libre.
- **Actualizar un inversor**: asignar a un nodo un inversor con el mismo número de serie que ya tiene. Solo cambian sus demás datos.

## Datos del inversor

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Número de serie | Sí | Texto. Trim al inicio y fin. 1 a 50 caracteres. Único entre todos los inversores del sistema, sin distinguir mayúsculas/minúsculas. |
| Marca y Modelo | Sí | Texto. Trim al inicio y fin. 1 a 100 caracteres. |
| Capacidad nominal (kW) | Sí | Número decimal mayor a 0. No puede superar la capacidad máxima del parque del nodo. |
| Dirección IP / Endpoint | No | Texto opcional. Trim al inicio y fin. Hasta 100 caracteres. No se valida el formato. |

Datos asignados por el sistema: Identificador, nodo al que pertenece y fecha de creación en UTC.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador asigne un inversor válido a un nodo activo, EL SISTEMA registrará la asignación, actualizará la fecha de modificación del nodo y mostrará "Inversor asignado correctamente al nodo."
- **RF-2**: SI el nodo ya cuenta con un inversor de distinto número de serie, ENTONCES EL SISTEMA solicitará confirmación de reemplazo: "El nodo ya tiene el inversor \"<serie_actual>\". ¿Deseás reemplazarlo por \"<serie_nueva>\"?" y no modificará nada hasta recibir la respuesta.
  - CUANDO el Administrador confirme, EL SISTEMA reemplazará el inversor, actualizará la fecha de modificación del nodo y mostrará "Inversor asignado correctamente al nodo."
  - CUANDO el Administrador cancele, EL SISTEMA no modificará nada.
- **RF-3**: SI el número de serie del inversor ya está asignado a otro nodo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "El inversor con número de serie \"<serie>\" ya se encuentra asignado al nodo \"<otro_nodo>\"."
- **RF-4**: SI la capacidad del inversor es menor o igual a 0, ENTONCES EL SISTEMA mostrará "La capacidad nominal del inversor debe ser mayor a 0."
- **RF-5**: SI la capacidad del inversor supera la capacidad máxima del parque solar, ENTONCES EL SISTEMA mostrará "La capacidad nominal del inversor no puede superar la capacidad máxima del parque solar."
- **RF-6**: SI el nodo se encuentra en estado Inactivo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "No se puede asignar un inversor a un nodo inactivo."
- **RF-7**: CUANDO el Administrador desvincule el inversor de un nodo, EL SISTEMA eliminará la asignación, actualizará la fecha de modificación del nodo, dejará al nodo "Sin inversor asignado" y mostrará "Inversor desvinculado correctamente del nodo." SI el nodo no tiene inversor, ENTONCES EL SISTEMA mostrará "El nodo no tiene un inversor asignado."
- **RF-8**: SI algún dato de texto del inversor no cumple su regla, ENTONCES EL SISTEMA no registrará la asignación y mostrará:
  - Número de serie vacío → "El número de serie es obligatorio."
  - Número de serie mayor a 50 caracteres → "El número de serie no puede superar los 50 caracteres."
  - Marca y modelo vacío → "La marca y modelo son obligatorios."
  - Marca y modelo mayor a 100 caracteres → "La marca y modelo no pueden superar los 100 caracteres."
  - Dirección o endpoint mayor a 100 caracteres → "La dirección o endpoint no puede superar los 100 caracteres."
- **RF-9**: SI el nodo no existe, ENTONCES EL SISTEMA mostrará "El nodo solicitado no existe."
- **RF-10**: CUANDO el Administrador asigne a un nodo el mismo número de serie que ya tiene, EL SISTEMA actualizará los demás datos del inversor, sin pedir confirmación de reemplazo, y mostrará "Inversor asignado correctamente al nodo."

## Requisitos no funcionales
- Interfaz en español, responsive para escritorio y celular.
- Las acciones de asignar, reemplazar y desvincular solo están disponibles para el Administrador.

## Casos límite
- Desvincular un inversor deja al nodo en estado "Sin inversor asignado". El inversor desvinculado deja de existir y su número de serie puede asignarse a otro nodo.
- Un inversor reemplazado también deja de existir, y su número de serie queda libre para otro nodo.
- Desvincular se permite también en un nodo inactivo (para liberar el inversor); asignar no (RF-6).
- El estado del parque no agrega restricciones: solo se valida el estado del nodo.
- Orden de validación, cuando hay varios errores: nodo inexistente, nodo inactivo, datos de texto, capacidad, número de serie ya asignado a otro nodo y, por último, la confirmación de reemplazo.
- Dos asignaciones simultáneas con la misma serie: la base de datos impide el duplicado.

## Fuera de alcance
- Telemetría en tiempo real del inversor (specs de Monitoreo).
- Control de corte del inversor (specs de Control).
- Historial de inversores reemplazados o desvinculados.

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (RF-2 a RF-10).
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna. Decisiones tomadas: el reemplazo se confirma desde la pantalla y el inversor anterior se elimina sin historial; el número de serie es único entre todos los inversores existentes; el inversor no tiene estado propio; la dirección o endpoint es opcional de hasta 100 caracteres sin validar formato; se agrega el requisito de desvincular (RF-7).
