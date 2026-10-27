# Spec 017 — Gestionar Roles de Usuario

Estado: aprobada
Depende de: Spec 015 (Iniciar Sesión)

## Contexto y objetivo
Establecer el control de acceso basado en roles (RBAC) en SolarUY. Los usuarios se dividen principalmente en **Administrador** (gestión integral, configuración, ABM de infraestructura, órdenes a inversores) y **Técnico** (consultas operativas, mantenimiento asignado, inspección).

## Usuarios / Actores
- **Administrador**: administra la asignación de roles y permisos.

## Historias de usuario
- **HU-1**: Como Administrador, quiero asignar roles a las cuentas de usuario para delimitar las acciones que cada operador puede realizar en la plataforma.

## Roles del Sistema

| Rol | Alcance y Permisos |
| :--- | :--- |
| **Administrador** | Acceso total: Parques, Nodos, Inversores, Paneles, Usuarios, Órdenes de corte, Asignación de mantenimientos y Reportes avanzados. |
| **Técnico** | Acceso restringido: Consulta de topología, lectura de telemetría en tiempo real, gestión de mantenimientos asignados y confirmación de intervenciones. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario autenticado intente acceder a una vista no autorizada para su rol (ej. Técnico intentando acceder a creación de parques), EL SISTEMA impedirá el acceso y mostrará "Acceso no autorizado. No contás con los permisos necesarios para realizar esta acción."
- **RF-2**: EL SISTEMA ocultará en la interfaz gráfica (menús, botones de acción "Editar", "Dar de baja", "Nueva orden") todas las opciones que no correspondan al rol del usuario activo.
- **RF-3**: CUANDO un Administrador cambie el rol de un usuario en el panel de gestión, EL SISTEMA persistirá el nuevo rol y mostrará "Rol de usuario actualizado correctamente."
- **RF-4**: SI la API REST recibe una solicitud de un usuario sin el rol requerido en su token JWT, ENTONCES EL SISTEMA responderá con código HTTP 403 Forbidden y el mensaje "Permisos insuficientes."
- **RF-5**: EL SISTEMA impedirá que el último Administrador activo del sistema se retire su propio rol de Administrador para prevenir bloqueos de administración.

## Casos límite
- Usuario con múltiples roles asignados: hereda la unión de permisos de todos sus roles asignados.
- Token JWT emitido antes de un cambio de rol: se sincroniza al refrescar el token o en el siguiente login.

## Fuera de alcance
- Creación dinámica de nuevos roles personalizados (los roles válidos son fijos: Administrador y Técnico).

## Criterios de finalización
- Tests unitarios y de arquitectura verificando la protección de endpoints con atributos de autorización de rol.
- Verificación manual en navegador con usuarios de prueba de ambos roles.

## Dudas abiertas
- Ninguna.
