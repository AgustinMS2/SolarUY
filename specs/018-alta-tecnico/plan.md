# Plan Técnico: Spec 018 — Alta de Técnico

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/Technician.cs`: entidad de técnico (Id, FullName, DocumentId, NormalizedDocumentId, Email, NormalizedEmail, PhoneNumber, Specialty, Status, CreatedAtUtc).
  - `Data/SolarUyDbContext.cs`: `DbSet<Technician>`, índices únicos en `NormalizedDocumentId` y `NormalizedEmail`.
  - `Repositories/ITechnicianRepository.cs` e implementación: métodos `ExistsByDocument(string doc)`, `ExistsByEmail(string email)`, `Add(Technician tech)`.
  - `Services/ITechnicianService.cs` y `TechnicianService.cs`: método `Create(CreateTechnicianRequest request)`.
  - `Dtos/CreateTechnicianRequest.cs`: DTO de alta.
  - `Controllers/TechniciansController.cs`: endpoint `POST /api/technicians`.
- `SolarUY.Web`:
  - `Services/TechnicianApiClient.cs`: método `CreateAsync(CreateTechnicianRequest request)`.
  - `Components/Pages/TechnicianCreate.razor` (ruta `/tecnicos/nuevo`): formulario de alta con validación inline.
- `SolarUY.Tests`:
  - `Fakes/FakeTechnicianRepository.cs`: repositorio falso para tests.
  - `Services/TechnicianServiceCreateTests.cs`: tests unitarios de validación de documento, email duplicado y teléfonos.

## Modelo de Datos y Persistencia
```csharp
public class Technician
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string NormalizedDocumentId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public TechnicianStatus Status { get; set; } = TechnicianStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
}

public enum TechnicianStatus { Active = 1, Inactive = 2 }
```
- Configuración en DbContext:
  - Índices únicos en `NormalizedDocumentId` y `NormalizedEmail`.
  - Migración: `AddTechnicianEntity`.

## Servicios y Lógica de Dominio Pura
- `TechnicianService.Create`:
  - Valida formato de cédula/documento, email y teléfono.
  - Valida unicidad con `ExistsByDocument` y `ExistsByEmail`.
  - Persiste el técnico en estado `Active` con fecha UTC. Retorna `ServiceResult<Guid>.Ok(tech.Id)`.

## API y Comunicación
- `POST /api/technicians`
  - Body: `CreateTechnicianRequest`
  - Respuestas: `201 Created` (`{ id }`), `400 Bad Request`, `409 Conflict`.

## Estrategia de Tests
- **Tests unitarios**:
  - Alta exitosa devuelve Id y estado Activo.
  - Documento duplicado devuelve Conflict.
  - Email duplicado devuelve Conflict.
  - Formato de email inválido devuelve Validation.
