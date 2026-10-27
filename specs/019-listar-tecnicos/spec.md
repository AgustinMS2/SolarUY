# Spec 019 — Listar Técnicos

Estado: borrador
Depende de: Spec 018 (Alta de Técnico)

## Contexto y objetivo
Visualizar la nómina de técnicos registrados en SolarUY, conociendo su disponibilidad, especialidad, datos de contacto y estado, para su selección en la agenda de mantenimiento.

## Usuarios / Actores
- **Administrador**: consulta la nómina para asignación de tareas.
- **Técnico**: consulta el directorio de colegas de guardia.

## Historias de usuario
- **HU-1**: Como usuario, quiero ver el listado de técnicos para consultar su información de contacto y disponibilidad operativa.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario acceda a la sección de técnicos, EL SISTEMA mostrará una tabla con: Nombre completo, Documento, Correo electrónico, Teléfono, Especialidad, Mantenimientos asignados en curso y Estado ("Activo" / "Inactivo").
- **RF-2**: EL SISTEMA ordenará el listado alfabéticamente por nombre completo.
- **RF-3**: EL SISTEMA permitirá filtrar la lista por estado (Todos, Activos, Inactivos) y buscar por texto libre en nombre o documento.
- **RF-4**: SI no hay técnicos registrados que coincidan con la búsqueda, ENTONCES EL SISTEMA mostrará "No se encontraron técnicos registrados."
- **RF-5**: EL SISTEMA mostrará un botón "Nuevo técnico" exclusivo para Administradores que redirigirá al formulario de alta (spec 018).

## Requisitos no funcionales
- Paginación o renderizado eficiente para nóminas de personal.
- Visualización responsive en celular para consultas rápidas en campo.

## Casos límite
- Técnico sin mantenimientos en curso muestra "0 asignados".
- Técnicos dados de baja se visualizan claramente en estado Inactivo.

## Fuera de alcance
- Asignación directa a mantenimientos desde esta pantalla (se hace en la spec 021 de Reserva).
- Modificación de perfil del técnico.

## Criterios de finalización
- Tests unitarios del servicio verificando orden y filtros.
- Tests de arquitectura en verde.
- Verificación manual en navegador.

## Dudas abiertas
- Ninguna.
