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
- Tras el logout, el usuario es redirigido a la pantalla de bienvenida pública (`/`).

## Sesión finalizada desde otro lugar (RF-4)
- Casos que detecta el sistema: (a) el servidor ya no tiene los tokens de la sesión (Keycloak rechazó la renovación o el servidor se reinició) y el navegador pide una página; (b) el usuario está en una página interactiva y una llamada a la API ya no puede llevar token.
- En ambos casos se limpia la cookie local y se redirige a `/?sesion=finalizada`; `Components/Pages/Home.razor` muestra el aviso "La sesión ha finalizado." cuando recibe ese parámetro.
- Endpoint local `/account/session-ended` (solo limpia la cookie local, sin llamar a Keycloak porque su sesión ya terminó) usado por el caso (b).
- Archivos: `Security/KeycloakAuthenticationExtensions.cs`, `Security/BearerTokenHandler.cs`, `Components/Pages/Home.razor`.

## Estrategia de Tests
- **Pruebas manuales**:
  - Verificar que la cookie `SolarUY.Cookie` se elimina.
  - Verificar que al presionar el botón "Atrás" en el navegador no se puede acceder al contenido privado sin volver a iniciar sesión.
