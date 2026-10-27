# Plan Técnico: Spec 017 — Gestionar Roles de Usuario

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Security/KeycloakRoleClaimTransformation.cs`: transformación de claims para mapear los roles del token JWT (en `realm_access.roles`) a `ClaimTypes.Role` estándar de .NET.
  - Decoradores `[Authorize(Roles = "Administrador")]` en endpoints de gestión, corte y edición.
  - Decoradores `[Authorize(Roles = "Administrador,Técnico")]` en endpoints de consulta y telemetría.
- `SolarUY.Web`:
  - Componentes Razor utilizando `<AuthorizeView Roles="Administrador">` para renderizar condicionalmente botones destructivos ("Editar", "Dar de baja", "Nueva orden").
  - `Components/Pages/AccessDenied.razor`: vista amigable para respuestas 403 Forbidden.
- `SolarUY.Tests`:
  - `Architecture/SecurityArchitectureTests.cs`: test de arquitectura que asegura que ningún endpoint sensible carezca de atributo `[Authorize]`.

## Roles y Claims
- Rol `Administrador`: asignado a usuarios con privilegios totales.
- Rol `Técnico`: asignado a operadores de campo y mantenimiento.
- Mapeo de Claims:
  - Keycloak expone roles en el claim `realm_access` con un array JSON de strings.
  - En `Program.cs`, se configura el mapper de JWT:
    ```csharp
    options.TokenValidationParameters.RoleClaimType = "roles";
    ```
    o mediante `IClaimsTransformation`.

## Estrategia de Tests
- **Tests de arquitectura**:
  - Validar que los controladores de API poseen atributos de autorización.
- **Tests manuales**:
  - Iniciar sesión como Técnico e intentar navegar a `/parques/nuevo`: debe redirigir a `/access-denied` o mostrar mensaje de error.
  - Verificar que el botón "Dar de baja" no aparece para el rol Técnico.
