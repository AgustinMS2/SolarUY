# Tareas: Spec 017 — Gestionar Roles de Usuario

- [ ] **T1. Mapeo de claims de roles Keycloak a .NET ClaimTypes.Role.** RF-1, RF-4
  - Hecho cuando: el token JWT mapea los roles Administrador y Técnico de forma reconocible por User.IsInRole.

- [ ] **T2. Protección de controladores con [Authorize(Roles = ...)].** RF-1, RF-4
  - Hecho cuando: un técnico recibe 403 Forbidden al intentar endpoints de administración y los tests de arquitectura lo verifican.

- [ ] **T3. Protección de UI con <AuthorizeView Roles = ...>.** RF-1, RF-2
  - Hecho cuando: botones de creación, edición y baja no se renderizan para usuarios con rol Técnico.