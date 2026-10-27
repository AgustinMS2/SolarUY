# Plan Técnico: Spec 015 — Iniciar Sesión con Keycloak (OIDC)

## Proyectos y Archivos Afectados
- `SolarUY.AppHost`:
  - `Program.cs`: configuración del contenedor de Keycloak (`AddKeycloak("keycloak")`), importación de realm `solaruy-realm.json`.
- `SolarUY.ServiceDefaults`:
  - Extensiones para configuración de autenticación JWT Bearer en APIs.
- `SolarUY.ApiService`:
  - `Program.cs`: `builder.Services.AddAuthentication().AddJwtBearer(...)`, `app.UseAuthentication()`, `app.UseAuthorization()`.
- `SolarUY.Web`:
  - `Program.cs`: configuración de OpenID Connect (`AddAuthentication(options => ...).AddCookie().AddOpenIdConnect(...)`).
  - `Components/Layout/NavMenu.razor` y `MainLayout.razor`: visualización del usuario conectado, avatar y botón de Login/Logout con `<AuthorizeView>`.

## Integración OpenID Connect y Keycloak
- Protocolo: OpenID Connect (OIDC) con flujo Authorization Code + PKCE.
- Realm de Keycloak: `SolarUY`.
- Clientes configurados:
  - `solaruy-web` (confidencial con PKCE para Blazor Server).
  - `solaruy-api` (audience para validación de tokens JWT en backend).
- Scopes: `openid`, `profile`, `email`, `roles`.

## Flujo de Seguridad en Blazor Server
- Configuración de `App.razor` con `<CascadingAuthenticationState>`.
- `<AuthorizeRouteView>` para interceptar rutas no autorizadas y disparar el challenge de redirección hacia Keycloak.
- Almacenamiento seguro del token de acceso en la sesión del circuito de SignalR de Blazor Server para adjuntarlo a las peticiones HTTP hacia `ApiService`.

## Estrategia de Tests
- **Tests de arquitectura**: verificar que los controladores de API poseen decoradores `[Authorize]` o políticas de seguridad.
- **Pruebas manuales**: flujo completo de redirección a Keycloak con usuario de prueba (`admin` / `admin123`), login exitoso y visualización de identidad en la barra superior.
