# Spec 005 — Alta de Panel Solar

Estado: aprobada

## Contexto y objetivo
Registrar un panel solar nuevo en SolarUY. Cada panel pertenece a un parque solar existente. Es la primera funcionalidad del módulo Paneles y crea la base (datos y pantalla) sobre la que se apoyan Listar (006), Modificar (007) y Baja (008).

## Usuarios / Actores
- **Administrador**: da de alta el panel. (El control de acceso por rol queda para la spec de Usuarios; ver "Fuera de alcance".)

## Historia de usuario
- **HU-1**: Como Administrador, quiero registrar un panel solar con su número de serie, potencia y eficiencia, asociado a un parque existente, para empezar a gestionarlo en el sistema.

## Datos del panel

| Campo | Obligatorio | Regla |
| :--- | :--- | :--- |
| Número de serie | Sí | Texto. Se quitan espacios al inicio y al final. Entre 1 y 50 caracteres. Único sin distinguir mayúsculas/minúsculas. |
| Potencia (W) | Sí | Número entero mayor a 0. |
| Eficiencia (%) | Sí | Número decimal entre 0.1 y 100 (inclusive). |
| Parque solar | Sí | Debe existir un parque activo en el sistema. Seleccionado de un listado desplegable. |

Datos que asigna el sistema (no los carga el usuario): identificador, estado = **Activo**, fecha de creación (UTC), asociación al parque solar.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de alta con todos los campos válidos, EL SISTEMA registrará el panel en estado "Activo", guardará la fecha de creación en UTC, asociará el panel al parque solar seleccionado y mostrará el mensaje "Panel creado correctamente."
- **RF-2**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no registrará el panel y mostrará junto al campo el mensaje correspondiente:
  - Número de serie vacío → "El número de serie es obligatorio."
  - Número de serie de más de 50 caracteres → "El número de serie no puede superar los 50 caracteres."
  - Número de serie duplicado → "Ya existe un panel con el número de serie "<serie>"."
  - Potencia ≤ 0 → "La potencia debe ser mayor a 0."
  - Eficiencia ≤ 0 → "La eficiencia debe ser mayor a 0."
  - Eficiencia > 100 → "La eficiencia no puede superar el 100."
  - Parque solar inexistente o inactivo → "El parque solar seleccionado no existe o no está activo."
- **RF-3**: CUANDO el alta sea exitosa, EL SISTEMA limpiará el formulario para permitir cargar otro panel.
- **RF-4**: SI ya existe un panel (activo o inactivo) con el mismo número de serie, sin distinguir mayúsculas/minúsculas y luego de quitar espacios al inicio y al final, ENTONCES EL SISTEMA no registrará el panel y mostrará "Ya existe un panel con el número de serie "<serie>"."

## Requisitos no funcionales

- Pantalla en español, usable en escritorio y en celular.
- Los valores numéricos se ingresan con punto o coma según la configuración del navegador.
- El campo "Parque solar" muestra un dropdown con los parques activos (spec 002).

## Casos límite

- "  Panel-001  " se guarda como "Panel-001".
- "panel-001" se rechaza si ya existe "Panel-001" (aunque esté inactivo).
- Potencia 0 y eficiencia 0 son inválidas.
- Eficiencia 100 es válida.

## Fuera de alcance

- Control de acceso por rol (spec de Usuarios). Por ahora cualquier usuario de la aplicación puede dar de alta.
- Listado, modificación y baja (specs 006, 007 y 008).
- Nodos, inversores. (Ya definidos en otras specs si aplican).

## Criterios de finalización

- Tests unitarios en verde para RF-1, RF-2 y RF-3 (reglas del servicio).
- Tests de arquitectura en verde.
- Alta probada a mano en el navegador: caso válido, cada error de RF-2 y número de serie duplicado.