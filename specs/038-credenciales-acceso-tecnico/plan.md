# Plan Técnico: Spec 038 — Provisión de Credenciales y Sincronización de Acceso de Técnicos

## Proyectos y Archivos Afectados
- `SolarUY.AppHost`:
  - `Realms/SolarUY-realm.json`: Sincronización de las cuentas de usuario de los técnicos semilla (`tecnico@solaruy.local` como Carlos Méndez, `mrodriguez@solaruy.local` como Mariana Rodríguez habilitada, y `gvarela@solaruy.local` como Gonzalo Varela deshabilitado, todos con rol `Técnico`).
- `SolarUY.ApiService`:
  - `Data/DbInitializer.cs`: Corrección de `NormalizedDocumentId` sin puntos ni guiones para los tres técnicos iniciales.
  - `Dtos/CreateTechnicianRequest.cs`: Adición de la propiedad `Password` con sus anotaciones de validación.
  - `Repositories/IKeycloakUserRepository.cs` y `Repositories/KeycloakUserRepository.cs`: Extensión del repositorio con métodos sincrónicos `ExistsByEmail(string email)`, `CreateTechnicianUser(string email, string fullName, string password)` y `DisableByEmail(string email)`.
  - `Services/TechnicianService.cs`: Validación de `Password` (6–100 caracteres) y de `Specialty` (máx. 100 caracteres), verificación de email único también en Keycloak, provisión de usuario en Keycloak antes de persistir el técnico en BD, y deshabilitación en Keycloak al dar de baja (`Deactivate`).
  - `Controllers/TechniciansController.cs`: Traducción de `ServiceErrorType.Unavailable` a HTTP `503 Service Unavailable` en `Create` y `Deactivate`.
- `SolarUY.Web`:
  - `Components/Pages/TechnicianCreate.razor`: Incorporación del campo obligatorio de contraseña (`type="password"`, con botón para alternar visibilidad) en el formulario de alta de técnico.
- `SolarUY.Tests`:
  - `Fakes/FakeKeycloakUserRepository.cs`: Implementación en memoria de `ExistsByEmail`, `CreateTechnicianUser` y `DisableByEmail`.
  - `Services/TechnicianServiceCreateTests.cs` y `Services/TechnicianServiceDeactivateTests.cs`: Tests unitarios para validación de contraseña, especialidad, creación sincronizada en Keycloak, conflicto de email en Keycloak, fallo de comunicación con Keycloak (`Unavailable`) y deshabilitación de cuenta al dar de baja.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)
- No requiere nuevas entidades ni migraciones de EF Core: la contraseña se delega exclusivamente a Keycloak y nunca se almacena en la tabla `Technicians`.
- En `DbInitializer.cs` se normalizan los valores de `NormalizedDocumentId` de los técnicos semilla (`"45678901"`, `"38901234"`, `"23456789"`).

## Servicios y Lógica de Dominio Pura
- `IKeycloakUserRepository`:
  - `bool ExistsByEmail(string email)`: Consulta `GET /admin/realms/{realm}/users?email={email}&exact=true` (y por `username` igual al email) para detectar si el correo ya tiene cuenta en Keycloak.
  - `string CreateTechnicianUser(string email, string fullName, string password)`:
    1. Separa `fullName` en `firstName` y `lastName` por el primer espacio.
    2. Envía `POST /admin/realms/{realm}/users` con `username = email`, `email = email`, `enabled = true`, `emailVerified = true` y credencial `type = "password"`, `value = password`, `temporary = false`.
    3. Obtiene el ID del usuario creado (desde la cabecera `Location` o buscando por `username`) y le asigna el rol de realm `Técnico` mediante `POST /admin/realms/{realm}/users/{id}/role-mappings/realm`.
  - `void DisableByEmail(string email)`: Busca el usuario por email en Keycloak y, si existe, actualiza su estado con `PUT /admin/realms/{realm}/users/{id}` estableciendo `enabled = false`.
- `TechnicianService`:
  - Recibe `IKeycloakUserRepository` (opcional con fallback en constructores existentes o inyectado por DI).
  - En `Create(CreateTechnicianRequest request)`:
    1. Ejecuta todas las validaciones de dominio (`FullName`, `DocumentId`, `Email`, `PhoneNumber`, `Specialty` ≤ 100, `Password` 6–100 caracteres).
    2. Verifica unicidad de documento y correo en `ITechnicianRepository`.
    3. Verifica en `IKeycloakUserRepository.ExistsByEmail(normalizedEmail)` que el correo no esté registrado en Keycloak; si existe, devuelve `Fail(Conflict, "El correo electrónico \"<email>\" ya se encuentra registrado.")`.
    4. Invoca `CreateTechnicianUser(normalizedEmail, trimmedName, request.Password)`. Si lanza `AuthenticationServiceUnavailableException`, captura y devuelve `Fail(Unavailable, "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.")` sin insertar en la base de datos local.
    5. Persiste la entidad `Technician` en PostgreSQL y retorna `Ok(technician.Id)`.
  - En `Deactivate(Guid id)`:
    1. Tras validar que el técnico existe, está activo y no tiene mantenimientos pendientes, invoca `DisableByEmail(technician.NormalizedEmail)`. Si falla la comunicación con Keycloak, retorna `Fail(Unavailable, "No se pudo comunicar con el servicio de autenticación. Intentá nuevamente.")` sin alterar el estado local.
    2. Marca `technician.Status = TechnicianStatus.Inactive` y actualiza en el repositorio.

## API y Comunicación
- `POST /api/technicians`:
  - Recibe `CreateTechnicianRequest` (ahora con `Password`).
  - Códigos de respuesta: `201 Created`, `400 Bad Request` (`Validation`), `409 Conflict` (`Conflict`), `503 Service Unavailable` (`Unavailable`).
- `DELETE /api/technicians/{id:guid}`:
  - Códigos de respuesta: `204 No Content`, `404 Not Found`, `409 Conflict`, `503 Service Unavailable` (`Unavailable`).

## Interfaz de Usuario (Blazor Server)
- `TechnicianCreate.razor` (`/tecnicos/nuevo`):
  - Agrega el campo obligatorio **Contraseña de acceso** (`input type="password"` con opción de mostrar/ocultar e indicación de mínimo 6 caracteres).
  - Al enviar con éxito, limpia todos los campos (incluida la contraseña) y muestra `"Técnico registrado correctamente."`
  - Si ocurre un error (400, 409 o 503), muestra el mensaje exacto retornado por la API.

## Estrategia de Tests
- **Tests de arquitectura**: Todos los tests de arquitectura existentes deben permanecer en verde.
- **Tests unitarios (`SolarUY.Tests`)**:
  - `TechnicianServiceCreateTests`: Validación de contraseña vacía/corta/larga, especialidad mayor a 100 caracteres, creación conjunta en `FakeKeycloakUserRepository` con rol `Técnico`, rechazo por correo existente en Keycloak, y manejo de `AuthenticationServiceUnavailableException` sin persistir en BD local.
  - `TechnicianServiceDeactivateTests`: Deshabilitación de la cuenta en `FakeKeycloakUserRepository` al dar de baja al técnico y rechazo con `Unavailable` si Keycloak falla.
- **UI Blazor**: Verificación interactiva en navegador creando un técnico nuevo, iniciando sesión con sus credenciales y consultando `/mis-mantenimientos`.
