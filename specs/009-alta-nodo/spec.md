# Spec 009 — Alta de Nodo Solar

Estado: pendiente

## Contexto y objetivo
Registrar un nodo solar nuevo en SolarUY. Cada nodo representa un punto de monitoreo o control dentro de un parque solar existente. Es la primera funcionalidad del módulo Nodos y crea la base (datos y pantalla) sobre la que se apoyan Listar (010), Modificar (011) y Baja (012).

## Usuarios / Actores
- **Administrador**: da de alta el nodo. (El control de acceso por rol queda para la spec de Usuarios; ver "Fuera de alcance".)

## Historia de usuario
- **HU-1**: Como Administrador, quiero registrar un nodo de monitoreo con su identificador y tipo, asociado a un parque existente, para empezar a gestionarlo en el sistema.

## Datos del nodo

| Campo | Obligatorio | Regla |
| :--- | :--- | :--- |
| Identificador | Sí | Texto. Se quitan espacios al inicio y al final. Entre 1 y 50 caracteres. Único sin distinguir mayúsculas/minúsculas. |
| Nombre | Sí | Texto. Entre 1 y 100 caracteres. |
| Tipo | Sí | Catálogo fijo: "Monitoreo", "Control", "Medición", "Protección". |
| Parque solar | Sí | Debe existir un parque activo en el sistema. Seleccionado de un listado desplegable. |
| Inversor asociado | No | Inversor opcional ya existente en el sistema. |

Datos que asigna el sistema (no los carga el usuario): identificador (normalizado), estado = **Activo**, fecha de creación (UTC), asociación al parque solar.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de alta con todos los campos obligatorios válidos, EL SISTEMA registrará el nodo en estado "Activo", guardará la fecha de creación en UTC, asociará el nodo al parque solar seleccionado y mostrará el mensaje "Nodo creado correctamente."
- **RF-2**: SI algún campo obligatorio no cumple su regla, ENTONCES EL SISTEMA no registrará el nodo y mostrará junto al campo el mensaje correspondiente:
  - Identificador vacío → "El identificador es obligatorio."
  - Identificador de más de 50 caracteres → "El identificador no puede superar los 50 caracteres."
  - Identificador duplicado → "Ya existe un nodo con el identificador "<identificador>"."
  - Nombre vacío → "El nombre es obligatorio."
  - Nombre de más de 100 caracteres → "El nombre no puede superar los 100 caracteres."
  - Tipo inválido → "El tipo de nodo debe ser uno de los valores permitidos."
  - Parque solar inexistente o inactivo → "El parque solar seleccionado no existe o no está activo."
- **RF-3**: CUANDO el alta sea exitosa, EL SISTEMA limpiará el formulario para permitir cargar otro nodo.
- **RF-4**: SI ya existe un nodo (activo o inactivo) con el mismo identificador, sin distinguir mayúsculas/minúsculas y luego de quitar espacios al inicio y al final, ENTONCES EL SISTEMA no registrará el nodo y mostrará "Ya existe un nodo con el identificador "<identificador>"."

## Requisitos no funcionales

- Pantalla en español, usable en escritorio y en celular.
- El campo "Tipo" utiliza un dropdown con valores del catálogo definido.
- El campo "Parque solar" muestra un dropdown con los parques activos (spec 002).
- El campo "Inversor asociado" muestra un dropdown con inversores disponibles (puede estar vacío).

## Casos límite

- "  Nodo-01  " se guarda como "Nodo-01".
- "nodo-01" se rechaza si ya existe "Nodo-01" (aunque esté inactivo).
- Los tipos del catálogo son cerrados: no se admiten valores personalizados.

## Fuera de alcance

- Control de acceso por rol (spec de Usuarios). Por ahora cualquier usuario de la aplicación puede dar de alta.
- Listado, modificación y baja (specs 010, 011 y 012).
- Inversores (definidos en otras specs si aplican).

## Criterios de finalización

- Tests unitarios en verde para RF-1, RF-2 y RF-3 (reglas del servicio).
- Tests de arquitectura en verde.
- Alta probada a mano en el navegador: caso válido, cada error de RF-2 y identificador duplicado.