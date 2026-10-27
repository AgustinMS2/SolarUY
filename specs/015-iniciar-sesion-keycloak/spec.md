# Spec 015 — Iniciar Sesión con Keycloak (OIDC)

Estado: aprobada
Depende de: Ninguna (Infraestructura base de seguridad)

## Contexto y objetivo
Permitir a los usuarios del sistema (Administradores y Técnicos) autenticarse de forma segura mediante el protocolo estándar OpenID Connect (OIDC) utilizando el servidor de identidad Keycloak containerizado en Aspire.

## Usuarios / Actores
- **Administrador**: accede a funciones de gestión y configuración global.
- **Personal Especializado (Técnico)**: accede a tareas de mantenimiento e inspección.

## Historias de usuario
- **HU-1**: Como usuario de SolarUY, quiero iniciar sesión con mis credenciales corporativas para acceder a las funciones del sistema según mi perfil.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario no autenticado intente acceder a una ruta protegida de SolarUY, EL SISTEMA lo redirigirá automáticamente a la página de inicio de sesión de Keycloak.
- **RF-2**: CUANDO el usuario ingrese credenciales válidas en Keycloak, EL SISTEMA lo redirigirá de regreso a SolarUY, creará la sesión segura y mostrará en la barra superior su nombre de usuario, rol y un botón de cierre de sesión.
- **RF-3**: SI las credenciales son incorrectas, ENTONCES EL SISTEMA (en la pantalla de Keycloak) mostrará "Usuario o contraseña inválidos." y permanecerá en la pantalla de autenticación.
- **RF-4**: CUANDO la sesión expire por inactividad (configuración estándar de 30 minutos), EL SISTEMA requerirá reautenticación al intentar realizar una nueva acción.
- **RF-5**: MIENTRAS el usuario esté autenticado, las peticiones HTTP desde el cliente Blazor hacia `SolarUY.ApiService` incluirán el token Bearer correspondiente en la cabecera `Authorization`.

## Requisitos no funcionales
- Comunicación cifrada bajo HTTPS.
- Compatibilidad OIDC estándar con Keycloak (flujo Authorization Code con PKCE).
- Experiencia fluida de login en escritorio y celular.

## Casos límite
- Usuario con cuenta deshabilitada en Keycloak recibe el mensaje "Tu cuenta se encuentra deshabilitada. Contacta al administrador."
- Pérdida momentánea de conexión con Keycloak muestra "Servicio de autenticación no disponible. Intentá más tarde."

## Fuera de alcance
- Auto-registro de usuarios (los usuarios son provistos por el Administrador).
- Recuperación de contraseña por SMS.

## Criterios de finalización
- Verificación del flujo completo de login con credenciales de prueba en navegador.
- Redirección y claims validados en Blazor Server y API.

## Dudas abiertas
- Ninguna.
