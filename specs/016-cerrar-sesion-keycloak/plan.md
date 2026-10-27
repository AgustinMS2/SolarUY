# Plan Técnico: Spec 016 — Cerrar Sesión (Single Sign-Out)

## Proyectos y Archivos Afectados
- `SolarUY.Web`:
  - `Program.cs`: configuración de `SignOutScheme` y endpoint local `/logout` que dispara `SignOutAsync`.
  - `Components/Layout/NavMenu.razor`: enlace o botón de Logout con confirmación.
  - `Components/Pages/LogoutConfirmationModal.razor`: modal de confirmación de cierre de sesión.

## Mecanismo de Single Sign-Out
- Endpoint `/account/logout` en Blazor / ASP.NET Core:
  - Ejecuta `await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`.
  - Ejecuta `await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme)`.
  - Redirecciona al endpoint `protocol/openid-connect/logout` de Keycloak con `id_token_hint` y `post_logout_redirect_uri`.
  - Invalida la sesión activa en el servidor Keycloak, garantizando Single Sign-Out en todas las aplicaciones clientes del realm.

## UI y Feedback
- Diálogo de confirmación rápido: "¿Deseás cerrar la sesión actual?".
- Si el usuario cancela, no se ejecuta ninguna petición.
- Tras el logout, el usuario es redirigido a la página principal pública o a la pantalla de bienvenida.

## Estrategia de Tests
- **Pruebas manuales**:
  - Verificar que la cookie `SolarUY.Cookie` se elimina.
  - Verificar que al presionar el botón "Atrás" en el navegador no se puede acceder al contenido privado sin volver a iniciar sesión.
