# Spec 001 — CRUD de Parques Solares

Estado: aprobada

## Contexto y objetivo
Permitir a los Administradores de SolarUY realizar las operaciones básicas de gestión (Alta, Listado, Modificación y Baja lógica) sobre los parques solares fotovoltaicos. Esta especificación se enfoca exclusivamente en la entidad Parque Solar y sus atributos básicos, sentando la base para la posterior asociación de nodos e inversores en especificaciones independientes.

## Usuarios / Actores
- **Administrador**: Puede crear, modificar y dar de baja lógica parques solares.
- **Técnico**: Puede consultar el listado y detalle de los parques solares.

## Historias de usuario
- **HU-1**: Como Administrador, quiero dar de alta un nuevo parque solar con su nombre, coordenadas y capacidad instalada.
- **HU-2**: Como Usuario (Administrador o Técnico), quiero visualizar el listado de parques solares para conocer su estado general.
- **HU-3**: Como Administrador, quiero editar los datos de un parque solar existente para mantener actualizada la información.
- **HU-4**: Como Administrador, quiero dar de baja lógica un parque solar para desactivarlo sin eliminar su registro de la base de datos.

## Definiciones
- **Parque Solar**: Entidad principal que representa una instalación fotovoltaica con nombre, ubicación geográfica (latitud, longitud), capacidad máxima en kW, umbral de alerta de potencia y estado operativo.
- **Baja Lógica**: Cambio de estado de un parque a 'Inactivo', conservando el registro en la base de datos para preservar la trazabilidad.

## Requisitos funcionales (EARS)
- **RF-1 (Alta de parque)**: CUANDO el Administrador envíe los datos requeridos (nombre no vacío, coordenadas válidas y capacidad máxima > 0), EL SISTEMA registrará el parque solar en estado 'Activo' y retornará su identificador.
- **RF-2 (Control de nombre duplicado)**: SI el Administrador intenta registrar o renombrar un parque con un nombre que ya existe (sin distinguir mayúsculas de minúsculas), ENTONCES EL SISTEMA rechazará la operación informando que el nombre ya está registrado.
- **RF-3 (Listado de parques)**: CUANDO un usuario consulte los parques, EL SISTEMA retornará todos los parques registrados (activos e inactivos) ordenados alfabéticamente por nombre.
- **RF-4 (Modificación de datos)**: CUANDO el Administrador actualice los datos de un parque existente, EL SISTEMA guardará los cambios y actualizará la fecha de modificación en UTC.
- **RF-5 (Baja lógica)**: CUANDO el Administrador confirme la baja de un parque activo, EL SISTEMA cambiará su estado a 'Inactivo' y registrará la fecha de actualización.

## Requisitos no funcionales
- **Seguridad**: Altas, modificaciones y bajas restringidas a usuarios con rol `Administrador`. Consulta permitida a `Administrador` y `Técnico`.
- **Arquitectura**: Controladores tradicionales (`Controllers/`) y métodos sincrónicos estándar.
- **Interfaz**: Páginas en Blazor Server en español, con diseño limpio y responsive.

## Casos límite
- Nombres con espacios iniciales o finales se limpian con trim.
- Latitud debe estar en [-90, 90] y longitud en [-180, 180].
- Capacidad máxima y umbrales deben ser valores positivos.

## Fuera de alcance
- Asociación y validaciones cruzadas con Nodos e Inversores (se abordarán en las specs de Nodos e Inversores).
- Telemetría y streaming en tiempo real (especificación posterior de Monitoreo).

## Criterios de finalización
- Verificación de reglas de negocio principales mediante tests unitarios simples.
- Funcionalidad navegable y probada interactivamente en Blazor Server (escritorio y móvil).
