# Spec 016 — Cerrar Sesión (Single Sign-Out)

Estado: borrador
Depende de: Spec 015 (Iniciar Sesión con Keycloak)

## Contexto y objetivo
Permitir a los usuarios finalizar su sesión activa en SolarUY de manera segura, revocando los tokens emitidos por Keycloak y limpiando las cookies de autenticación locales para evitar accesos no autorizados en terminales compartidas.

## Usuarios / Actores
- **Administrador** y **Técnico**: cierran su sesión de trabajo.

## Historias de usuario
- **HU-1**: Como usuario autenticado, quiero cerrar mi sesión para garantizar que nadie más pueda operar el sistema desde mi dispositivo.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario presione el botón "Cerrar sesión" en la barra de navegación, EL SISTEMA solicitará confirmación rápida: "¿Deseás cerrar la sesión actual?".
- **RF-2**: CUANDO el usuario confirme el cierre, EL SISTEMA destruirá la cookie de sesión local, notificará a Keycloak para invalidar la sesión OIDC (Single Sign-Out) y redirigirá al usuario a la pantalla de bienvenida pública o login.
- **RF-3**: CUANDO el usuario cancele la confirmación, EL SISTEMA mantendrá la sesión activa en el estado actual sin recargar la página.
- **RF-4**: SI la sesión ya fue invalidada desde otra pestaña o dispositivo, ENTONCES EL SISTEMA redirigirá de inmediato al login mostrando "La sesión ha finalizado."

## Requisitos no funcionales
- Cierre de sesión inmediato (latencia < 500 ms).
- Limpieza total de estado en memoria del cliente Blazor.

## Casos límite
- Cerrar sesión con pestañas secundarias abiertas: todas las pestañas pierden acceso a endpoints protegidos en la siguiente petición.

## Fuera de alcance
- Cierre de sesión remoto forzado desde un panel de auditoría (se gestiona directamente en Keycloak admin).

## Criterios de finalización
- Verificación en navegador del flujo de logout y comprobación de que el botón Atrás no reabre la sesión.

## Dudas abiertas
- Ninguna.
