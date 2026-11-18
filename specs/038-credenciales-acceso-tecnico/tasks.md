# Tareas de Implementación: Spec 038 — Provisión de Credenciales y Sincronización de Acceso de Técnicos

- [x] **T1. Extender `CreateTechnicianRequest`, `IKeycloakUserRepository` y `KeycloakUserRepository` para crear y deshabilitar cuentas de técnico.** RF-1, RF-3, RF-5
  - Hecho cuando: `CreateTechnicianRequest` incluye `Password`; `IKeycloakUserRepository`, `KeycloakUserRepository` y `FakeKeycloakUserRepository` implementan `ExistsByEmail`, `CreateTechnicianUser` (con contraseña permanente y rol `Técnico`) y `DisableByEmail`; la solución compila y `dotnet test` sigue en verde.

- [x] **T2. Reglas de negocio en `TechnicianService` (TDD: validación de contraseña/especialidad, provisión en Keycloak, atomicidad y baja sincronizada).** RF-1, RF-2, RF-3, RF-5
  - Hecho cuando: los tests unitarios en `TechnicianServiceCreateTests` y `TechnicianServiceDeactivateTests` se escriben primero en rojo y luego pasan todos en verde con `dotnet test`.

- [x] **T3. Mapeo de `ServiceErrorType.Unavailable` (503) en `TechniciansController` y sincronización de datos semilla (`SolarUY-realm.json` y `DbInitializer`).** RF-3, RF-5, RF-6
  - Hecho cuando: `TechniciansController` devuelve 503 ante `Unavailable`; `SolarUY-realm.json` y `DbInitializer.cs` tienen los tres técnicos iniciales sincronizados y con documentos normalizados; `dotnet test` (incluidos los tests de arquitectura) pasa en verde.

- [x] **T4. Actualizar formulario `TechnicianCreate.razor` con el campo de contraseña y verificar flujo en navegador.** RF-1, RF-2, RF-4
  - Hecho cuando: `/tecnicos/nuevo` permite ingresar la contraseña (con opción de mostrar/ocultar), valida las reglas, registra el técnico en Keycloak y BD, y el técnico recién creado puede iniciar sesión y ver sus tareas en `/mis-mantenimientos`.
