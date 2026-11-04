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

## Gestión de usuarios y roles (RF-3, RF-5, RF-6, RF-7)

### Proyectos y archivos afectados
- `SolarUY.AppHost/Realms/solaruy-realm.json`: cliente de servicio `solaruy-api-admin` (confidencial, `serviceAccountsEnabled`, flujo client credentials) cuyo usuario de servicio recibe los roles de `realm-management`: `view-users`, `manage-users` y `view-realm`.
- `SolarUY.ApiService`:
  - `Repositories/IKeycloakUserRepository.cs` y `KeycloakUserRepository.cs`: acceso a la Admin REST API de Keycloak. Métodos: `IReadOnlyList<KeycloakUser> GetAll()`, `KeycloakUser? GetById(string id)`, `void ReplaceSystemRole(string id, string role)`. Obtiene el token del cliente de servicio por client credentials y lo reutiliza hasta que vence (con `TimeProvider`).
  - `Services/IUserRoleService.cs` y `UserRoleService.cs`: `ServiceResult<IReadOnlyList<UserDto>> GetAll()` y `ServiceResult ChangeRole(string userId, ChangeUserRoleRequest request)`. `Services/ServiceResult.cs`: nuevo valor `Unavailable` en `ServiceErrorType`.
  - `Dtos/UserDto.cs` (`Id, Username, FullName, Email, Enabled, Role`) y `Dtos/ChangeUserRoleRequest.cs` (`Role`).
  - `Controllers/UsersController.cs`: `GET /api/users` y `PUT /api/users/{id}/role`, ambos `[Authorize(Roles = Roles.Administrator)]`.
  - `appsettings.json`: sección `Keycloak` con `AdminClientId` y `AdminClientSecret`.
- `SolarUY.Web`:
  - `Services/UsersApiClient.cs` (con `BearerTokenHandler`).
  - `Components/Pages/UserList.razor` (ruta `/usuarios`, `[Authorize(Roles = AppRoles.Administrator)]`): tabla con lista desplegable de rol y botón "Guardar" por fila.
  - `Components/Layout/NavMenu.razor`: enlace "Usuarios" visible solo para Administrador.
- `SolarUY.Tests`: `Fakes/FakeKeycloakUserRepository.cs`, `Services/UserRoleServiceTests.cs`.

### Reglas del servicio (sincrónico)
- `GetAll()`: ordena por nombre de usuario sin distinguir mayúsculas (RF-6). El rol mostrado es el rol de sistema del usuario ("Administrador" o "Técnico"; vacío si no tiene ninguno).
- `ChangeRole(userId, request)`, en este orden:
  1. Rol distinto de "Administrador" o "Técnico" -> `Fail(Validation, "El rol seleccionado no es válido.")`.
  2. Usuario inexistente -> `Fail(NotFound, "El usuario solicitado no existe.")`.
  3. RF-5: si el usuario es Administrador, el nuevo rol no es Administrador y no existe otro Administrador activo -> `Fail(Conflict, "No se puede quitar el rol de Administrador al último administrador activo del sistema.")`.
  4. Reemplaza el rol de sistema en Keycloak (quita el anterior y agrega el nuevo; no toca roles internos como `offline_access`), registra el evento con `ILogger<T>` y devuelve `Ok()`.
- Si el repositorio no puede contactar a Keycloak lanza `AuthenticationServiceUnavailableException`; el servicio la captura y devuelve `Fail(Unavailable, "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.")`. Se agrega el valor `Unavailable` a `ServiceErrorType` (aditivo: los controladores existentes lo tratan en su rama por defecto) y el controlador de usuarios lo traduce a `503 Service Unavailable`.

### API
- `GET /api/users` -> `200 OK` con `IReadOnlyList<UserDto>`, `503`.
- `PUT /api/users/{id}/role` con `ChangeUserRoleRequest` -> `204 No Content`, `400` (rol inválido), `404` (usuario inexistente), `409` (último administrador), `503`.
- Endpoints de la Admin REST API de Keycloak que usa el repositorio: `GET /admin/realms/SolarUY/users`, `GET /users/{id}/role-mappings/realm`, `GET /roles/{nombre}` y `POST`/`DELETE /users/{id}/role-mappings/realm`.

### Interfaz (Blazor Server)
- `/usuarios`: spinner de carga, tabla (Usuario, Nombre, Correo, Estado, Rol, Acciones), mensaje de éxito "Rol de usuario actualizado correctamente.", errores de RF-5 y RF-7 en una alerta; el botón "Guardar" se habilita solo si el rol seleccionado cambió.
- Tras cambiar el rol propio se avisa que el cambio se aplica al renovar el token o en el próximo inicio de sesión.

### Decisiones específicas de esta spec
- El repositorio de Keycloak usa `HttpClient.Send` sincrónico para respetar el modelo sincrónico de la constitución.
- Se usa un cliente de servicio dedicado (`solaruy-api-admin`) con permisos mínimos, en lugar de reutilizar credenciales de un administrador humano.

## Estrategia de Tests
- **Tests de arquitectura**:
  - Validar que los controladores de API poseen atributos de autorización.
- **Tests manuales**:
  - Iniciar sesión como Técnico e intentar navegar a `/parques/nuevo`: debe redirigir a `/access-denied` o mostrar mensaje de error.
  - Verificar que el botón "Dar de baja" no aparece para el rol Técnico.
- **Tests unitarios** (`UserRoleServiceTests`, con repositorio falso):
  - Cambio válido reemplaza el rol (RF-3).
  - Rol inválido devuelve Validation; usuario inexistente devuelve NotFound.
  - Quitar el rol al único Administrador activo devuelve Conflict y no modifica nada (RF-5).
  - Con dos Administradores activos, quitarle el rol a uno se permite.
  - Un Administrador deshabilitado no cuenta como activo para RF-5.
  - Listado ordenado por nombre de usuario (RF-6).
- **Tests manuales** (RF-3, RF-5, RF-6, RF-7): como `admin` abrir `/usuarios`, cambiar `tecnico` a Administrador y volver a Técnico; intentar quitar el rol al único Administrador; detener Keycloak y comprobar el mensaje de RF-7.
