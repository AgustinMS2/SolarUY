# Spec 013 — Asignar Inversor a Nodo

Estado: borrador
Depende de: Spec 009 (Alta de Nodo), Spec 010 (Listar Nodos)

## Contexto y objetivo
Vincular un inversor fotovoltaico específico a un nodo de agregación. Cada nodo cuenta como máximo con un inversor activo encargado de la conversión de corriente continua a alterna y de la telemetría del conjunto.

## Usuarios / Actores
- **Administrador**: configura la asignación de hardware.

## Historias de usuario
- **HU-1**: Como Administrador, quiero asignar un inversor con su número de serie, marca, modelo y capacidad a un nodo para habilitar la recopilación de datos y telemetría.

## Datos del inversor

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Número de serie | Sí | Texto. Trim al inicio y fin. 1 a 50 caracteres. Único en todo el sistema. |
| Marca y Modelo | Sí | Texto. 1 a 100 caracteres. |
| Capacidad nominal (kW) | Sí | Número decimal mayor a 0. No puede superar la capacidad máxima del parque. |
| Dirección IP / Endpoint | No | Texto o IP de comunicación con el inversor (opcional). |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador asigne un inversor válido a un nodo activo, EL SISTEMA registrará la asignación, actualizará la fecha de modificación del nodo y mostrará "Inversor asignado correctamente al nodo."
- **RF-2**: SI el nodo ya cuenta con un inversor asignado, ENTONCES EL SISTEMA solicitará confirmación de reemplazo: "El nodo ya tiene el inversor \"<serie_actual>\". ¿Deseás reemplazarlo por \"<serie_nueva>\"?".
- **RF-3**: SI el número de serie del inversor ya está asignado a otro nodo activo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "El inversor con número de serie \"<serie>\" ya se encuentra asignado al nodo \"<otro_nodo>\"."
- **RF-4**: SI la capacidad del inversor es menor o igual a 0, ENTONCES EL SISTEMA mostrará "La capacidad nominal del inversor debe ser mayor a 0."
- **RF-5**: SI la capacidad del inversor supera la capacidad máxima del parque solar, ENTONCES EL SISTEMA mostrará "La capacidad nominal del inversor no puede superar la capacidad máxima del parque solar."
- **RF-6**: SI el nodo se encuentra en estado Inactivo, ENTONCES EL SISTEMA rechazará la asignación y mostrará "No se puede asignar un inversor a un nodo inactivo."

## Casos límite
- Desvincular un inversor deja al nodo en estado "Sin inversor asignado".
- Un inversor dado de baja en un nodo previo puede reasignarse tras desvinculación formal.

## Fuera de alcance
- Telemetría en tiempo real del inversor (specs de Monitoreo).
- Control de corte del inversor (specs de Control).

## Criterios de finalización
- Tests unitarios en verde para las reglas del servicio (RF-3, RF-4, RF-5, RF-6).
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna.
