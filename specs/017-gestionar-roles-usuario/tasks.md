# Tareas: Spec 017 — Gestionar Roles de Usuario

- [x] **T1. Mapeo de claims de roles Keycloak a .NET ClaimTypes.Role.** RF-1, RF-4
  - Hecho cuando: el token JWT mapea los roles Administrador y Técnico de forma reconocible por User.IsInRole.

- [x] **T2. Protección de controladores con [Authorize(Roles = ...)].** RF-1, RF-4
  - Hecho cuando: un técnico recibe 403 Forbidden al intentar endpoints de administración y los tests de arquitectura lo verifican.

- [x] **T3. Protección de UI con <AuthorizeView Roles = ...>.** RF-1, RF-2
  - Hecho cuando: botones de creación, edición y baja no se renderizan para usuarios con rol Técnico.

- [x] **T4. Acceso a la Admin API de Keycloak: cliente de servicio y repositorio de usuarios.** RF-3, RF-6, RF-7
  - Hecho cuando: el realm define el cliente de servicio `solaruy-api-admin` con los roles mínimos de `realm-management`; existe `IKeycloakUserRepository` (listar, buscar por id y reemplazar el rol de sistema) con su configuración; `ServiceErrorType` incluye `Unavailable`; la solución compila y `dotnet test` sigue en verde.

- [x] **T5. `UserRoleService`: listar usuarios y cambiar rol (tests primero).** RF-3, RF-6, RF-7
  - Hecho cuando: los tests (cambio válido reemplaza el rol, rol inválido, usuario inexistente, Keycloak no disponible, listado ordenado por nombre de usuario) fallaron antes de implementar y ahora pasan en verde.

- [x] **T6. `UsersController`: `GET /api/users` y `PUT /api/users/{id}/role`.** RF-3, RF-6, RF-7
  - Hecho cuando: responden 200/204/400/404/503 según el caso, ambos exigen `[Authorize(Roles = Roles.Administrator)]` y `dotnet test` (incluidos los tests de arquitectura de seguridad) sigue en verde.

- [x] **T7. Pantalla `/usuarios`: `UsersApiClient`, `UserList.razor` y enlace en el menú.** RF-3, RF-6, RF-7
  - Hecho cuando: en el navegador el Administrador ve la lista, cambia el rol de un usuario y ve "Rol de usuario actualizado correctamente."; el botón "Guardar" solo se habilita si el rol cambió; el Técnico no ve el enlace ni puede abrir la pantalla; la vista es responsive.

- [x] **T8. Regla del último Administrador activo (tests primero).** RF-5
  - Hecho cuando: los tests (el único Administrador activo no puede dejar de serlo, se permite con dos activos, un Administrador deshabilitado no cuenta) fallaron antes y ahora pasan; el endpoint responde 409 y la pantalla muestra "No se puede quitar el rol de Administrador al último administrador activo del sistema." sin cambiar el rol.
