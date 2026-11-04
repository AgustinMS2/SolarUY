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

## Definiciones
- **Administrador activo**: usuario con el rol Administrador cuya cuenta no está deshabilitada.
- **Panel de gestión de usuarios**: pantalla exclusiva del Administrador donde se listan los usuarios del sistema y se cambia el rol de cada uno.

## Datos del panel de gestión de usuarios

| Campo | Obligatorio | Regla |
| :--- | :--- | :--- |
| Nombre de usuario | (solo lectura) | Identificador de acceso del usuario, tal como existe en el servicio de autenticación. |
| Nombre completo | (solo lectura) | Nombre y apellido del usuario. |
| Correo electrónico | (solo lectura) | Correo del usuario. |
| Estado | (solo lectura) | "Activo" o "Deshabilitado". |
| Rol | Sí | Uno de los roles fijos del sistema: "Administrador" o "Técnico". Seleccionado de una lista desplegable. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario autenticado intente acceder a una vista no autorizada para su rol (ej. Técnico intentando acceder a creación de parques), EL SISTEMA impedirá el acceso y mostrará "Acceso no autorizado. No contás con los permisos necesarios para realizar esta acción."
- **RF-2**: EL SISTEMA ocultará en la interfaz gráfica (menús, botones de acción "Editar", "Dar de baja", "Nueva orden") todas las opciones que no correspondan al rol del usuario activo.
- **RF-3**: CUANDO un Administrador seleccione un rol distinto para un usuario en el panel de gestión y guarde el cambio, EL SISTEMA persistirá el nuevo rol (reemplazando el rol de sistema anterior del usuario) y mostrará "Rol de usuario actualizado correctamente."
- **RF-4**: SI la API REST recibe una solicitud de un usuario sin el rol requerido en su token JWT, ENTONCES EL SISTEMA responderá con código HTTP 403 Forbidden y el mensaje "Permisos insuficientes."
- **RF-5**: SI se intenta quitar el rol de Administrador al último Administrador activo del sistema, ENTONCES EL SISTEMA rechazará el cambio, mantendrá su rol y mostrará "No se puede quitar el rol de Administrador al último administrador activo del sistema." (previene bloqueos de administración).
- **RF-6**: CUANDO un Administrador ingrese al panel de gestión de usuarios, EL SISTEMA mostrará la lista de todos los usuarios del sistema con nombre de usuario, nombre completo, correo, estado y rol actual, ordenada alfabéticamente por nombre de usuario.
- **RF-7**: SI el servicio de autenticación no responde al listar usuarios o al guardar un cambio de rol, ENTONCES EL SISTEMA no modificará ningún rol y mostrará "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente."

## Casos límite
- Usuario con múltiples roles asignados: hereda la unión de permisos de todos sus roles asignados.
- Token JWT emitido antes de un cambio de rol: se sincroniza al refrescar el token o en el siguiente login.
- Usuario deshabilitado con rol Administrador: aparece en la lista, pero no cuenta como Administrador activo para RF-5.
- Usuario inexistente al guardar (eliminado mientras tanto): se rechaza el cambio y se muestra "El usuario solicitado no existe."
- Rol enviado distinto de "Administrador" o "Técnico": se rechaza el cambio y se muestra "El rol seleccionado no es válido."
- Un Administrador puede cambiarse el rol a sí mismo siempre que exista otro Administrador activo.
- Dos Administradores que se quitan el rol a la vez: la verificación de RF-5 se hace al guardar, sin garantía transaccional entre ambos pedidos (limitación aceptada).

## Fuera de alcance
- Creación dinámica de nuevos roles personalizados (los roles válidos son fijos: Administrador y Técnico).
- Alta, baja o deshabilitación de usuarios, cambio de contraseñas y datos personales (alta de técnicos: spec 018).
- Asignar varios roles de sistema a un mismo usuario desde el panel (cada usuario queda con un único rol de sistema).

## Criterios de finalización
- Tests unitarios y de arquitectura verificando la protección de endpoints con atributos de autorización de rol.
- Tests unitarios en verde para las reglas del servicio de gestión de roles (RF-3 y RF-5).
- Verificación manual en navegador con usuarios de prueba de ambos roles, incluido el cambio de rol y el rechazo al último Administrador.

## Dudas abiertas
- Ninguna. Decisiones tomadas: cada usuario tiene un único rol de sistema (el panel reemplaza el anterior); el panel lista a todos los usuarios; se aprueban RF-6 y RF-7, los textos nuevos de RF-5 y RF-7 y de los casos límite, y la redacción condicional de RF-5.
