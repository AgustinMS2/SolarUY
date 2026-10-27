# Spec 009 — Alta de Nodo

Estado: aprobada
Depende de: Spec 001 (Alta de Parque), Spec 002 (Listar Parques)

## Contexto y objetivo
Registrar un nuevo nodo de agregación dentro de un parque solar activo. Un nodo agrupa strings de paneles solares y aloja un inversor fotovoltaico. Es la base para estructurar la topología interna del parque.

## Usuarios / Actores
- **Administrador**: registra el nodo en el sistema.

## Historias de usuario
- **HU-1**: Como Administrador, quiero registrar un nodo indicando su nombre y parque solar para comenzar a asociarle inversores y paneles.

## Definiciones
- **Nodo**: Punto de agregación eléctrica y lógica dentro de un parque solar donde convergen paneles solares hacia un inversor.

## Datos

| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Nombre | Sí | Texto. Trim al inicio y fin. De 1 a 100 caracteres. Único dentro del mismo parque solar (sin distinguir mayúsculas/minúsculas). |
| Parque solar | Sí | Identificador de parque existente y activo. Seleccionado de lista desplegable. |

Datos asignados por el sistema: Identificador (GUID), Estado (**Activo**), Fecha de creación en UTC.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador envíe el formulario de alta con todos los campos válidos, EL SISTEMA registrará el nodo en estado "Activo", guardará la fecha de creación en UTC, lo asociará al parque seleccionado y mostrará el mensaje "Nodo creado correctamente."
- **RF-2**: SI algún campo no cumple su regla, ENTONCES EL SISTEMA no registrará el nodo y mostrará:
  - Nombre vacío → "El nombre es obligatorio."
  - Nombre mayor a 100 caracteres → "El nombre no puede superar los 100 caracteres."
  - Nombre duplicado en el mismo parque → "Ya existe un nodo con el nombre \"<nombre>\" en este parque solar."
  - Parque solar inexistente o inactivo → "El parque solar seleccionado no existe o no está activo."
- **RF-3**: CUANDO el alta sea exitosa, EL SISTEMA limpiará el formulario para permitir registrar otro nodo.
- **RF-4**: SI ya existe un nodo (activo o inactivo) con el mismo nombre en el mismo parque (sin distinguir mayúsculas/minúsculas), ENTONCES EL SISTEMA rechazará el alta y mostrará "Ya existe un nodo con el nombre \"<nombre>\" en este parque solar."

## Requisitos no funcionales
- Interfaz en español, responsive para escritorio y celular.
- Dropdown con búsqueda para selección del parque solar.

## Casos límite
- Dos parques distintos pueden tener un nodo con el mismo nombre (ej. "Nodo-01" en Parque A y "Nodo-01" en Parque B es válido).
- "  Nodo Norte  " se guarda como "Nodo Norte".
- "NODO NORTE" se rechaza si ya existe "Nodo Norte" en el mismo parque.

## Fuera de alcance
- Asignación de inversor (spec 013).
- Asignación de paneles (spec 014).
- Control de acceso por rol (spec de Usuarios).

## Criterios de finalización
- Reglas de negocio del servicio con tests unitarios en verde.
- Tests de arquitectura en verde.
- Verificación visual interactiva en Blazor Server.

## Dudas abiertas
- Ninguna.
