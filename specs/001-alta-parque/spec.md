# Spec 001 — Alta de Parque Solar

Estado: implementada

## Contexto y objetivo
Registrar un parque solar nuevo en SolarUY. Es la primera funcionalidad del módulo Parques y crea la base (datos y pantalla) sobre la que se apoyan Listar (002), Modificar (003) y Baja (004).

## Usuarios / Actores
- **Administrador**: da de alta el parque. (El control de acceso por rol queda para la spec de Usuarios; ver "Fuera de alcance".)

## Historia de usuario
- **HU-1**: Como Administrador, quiero registrar un parque solar con su nombre, ubicación, capacidad y umbral de alerta para empezar a gestionarlo en el sistema.

## Datos del parque

| Campo | Obligatorio | Regla |
| :--- | :--- | :--- |
| Nombre | Sí | Texto. Se quitan espacios al inicio y al final. Entre 1 y 100 caracteres. Único sin distinguir mayúsculas/minúsculas. |
| Latitud | Sí | Número decimal entre -90 y 90 (inclusive). |
| Longitud | Sí | Número decimal entre -180 y 180 (inclusive). |
| Capacidad máxima (kW) | Sí | Número decimal mayor a 0. |
| Umbral de alerta de potencia (kW) | Sí | Número decimal mayor a 0 y menor o igual a la capacidad máxima. |

Datos que asigna el sistema (no los carga el usuario): identificador, estado = **Activo**, fecha de creación (UTC).

## Requisitos funcionales (EARS)
- **RF-1**: CUANDO el Administrador envíe el formulario de alta con todos los campos válidos, EL SISTEMA registrará el parque en estado "Activo", guardará la fecha de creación en UTC y mostrará el mensaje "Parque creado correctamente."
- **RF-2**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no registrará el parque y mostrará junto al campo el mensaje correspondiente:
  - Nombre vacío → "El nombre es obligatorio."
  - Nombre de más de 100 caracteres → "El nombre no puede superar los 100 caracteres."
  - Latitud fuera de rango → "La latitud debe estar entre -90 y 90."
  - Longitud fuera de rango → "La longitud debe estar entre -180 y 180."
  - Capacidad ≤ 0 → "La capacidad máxima debe ser mayor a 0."
  - Umbral ≤ 0 o mayor a la capacidad → "El umbral de alerta debe ser mayor a 0 y no puede superar la capacidad máxima."
- **RF-3**: SI ya existe un parque (activo o inactivo) con el mismo nombre, sin distinguir mayúsculas/minúsculas y luego de quitar espacios al inicio y al final, ENTONCES EL SISTEMA no registrará el parque y mostrará "Ya existe un parque con el nombre "<nombre>"."
- **RF-4**: CUANDO el alta sea exitosa, EL SISTEMA limpiará el formulario para permitir cargar otro parque.

## Requisitos no funcionales
- Pantalla en español, usable en escritorio y en celular.
- Los valores decimales se ingresan con punto o coma según la configuración del navegador.

## Casos límite
- "  Parque Norte  " se guarda como "Parque Norte".
- "parque norte" se rechaza si ya existe "Parque Norte" (aunque esté inactivo).
- Latitud -90 / 90 y longitud -180 / 180 son válidas (límites incluidos).
- Umbral igual a la capacidad máxima es válido.

## Fuera de alcance
- Control de acceso por rol (spec de Usuarios). Por ahora cualquier usuario de la aplicación puede dar de alta.
- Listado, modificación y baja (specs 002, 003 y 004).
- Nodos, inversores y paneles.

## Criterios de finalización
- Tests unitarios en verde para RF-1, RF-2 y RF-3 (reglas del servicio).
- Tests de arquitectura en verde.
- Alta probada a mano en el navegador: caso válido, cada error de RF-2 y nombre duplicado.

