# Plan Técnico: Spec 021 — Reservar Fecha de Mantenimiento

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/MaintenanceOrder.cs`: entidad de orden de mantenimiento (Id, SolarPanelId, TechnicianId, Type, ScheduledDateUtc, Description, Status, CreatedAtUtc, CompletedAtUtc, Notes, ReplacedParts).
  - `Data/SolarUyDbContext.cs`: `DbSet<MaintenanceOrder>`, relaciones con `SolarPanel` y `Technician`.
  - `Repositories/IMaintenanceRepository.cs` e implementación: métodos `Add(MaintenanceOrder order)`, `HasPendingForPanel(Guid panelId, DateTime dateUtc)`.
  - `Services/IMaintenanceService.cs` y `MaintenanceService.cs`: método `CreateReservation(CreateMaintenanceReservationRequest request)`.
  - `Dtos/CreateMaintenanceReservationRequest.cs`: DTO de reserva.
  - `Controllers/MaintenancesController.cs`: endpoint `POST /api/maintenances`.
- `SolarUY.Web`:
  - `Services/MaintenanceApiClient.cs`: método `CreateReservationAsync(CreateMaintenanceReservationRequest request)`.
  - `Components/Pages/MaintenanceCreate.razor` (ruta `/mantenimientos/nuevo`): formulario con selector de panel, técnico y datetime picker.
- `SolarUY.Tests`:
  - `Fakes/FakeMaintenanceRepository.cs`: repositorio falso para tests.
  - `Services/MaintenanceServiceReservationTests.cs`: tests unitarios de validación de fechas futuras y estados de panel/técnico.

## Modelo de Datos y Persistencia
```csharp
public class MaintenanceOrder
{
    public Guid Id { get; set; }
    public Guid SolarPanelId { get; set; }
    public SolarPanel? SolarPanel { get; set; }
    public Guid TechnicianId { get; set; }
    public Technician? Technician { get; set; }
    public MaintenanceType Type { get; set; }
    public DateTime ScheduledDateUtc { get; set; }
    public string Description { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
    public string? CancellationReason { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ReplacedParts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public enum MaintenanceType { Preventive = 1, Corrective = 2 }
public enum MaintenanceStatus { Pending = 1, Completed = 2, Cancelled = 3 }
```
- Migración: `AddMaintenanceOrderEntity`.

## Servicios y Lógica de Dominio Pura
- `CreateReservation(CreateMaintenanceReservationRequest request)`:
  - Valida que `request.ScheduledDateUtc > DateTime.UtcNow.AddHours(1)`.
  - Valida que el panel solar exista y esté Activo en `ISolarPanelRepository`.
  - Valida que el técnico exista y esté Activo en `ITechnicianRepository`.
  - Valida que la descripción tenga entre 5 y 500 caracteres.
  - Persiste la orden en estado `Pending`. Retorna `ServiceResult<Guid>.Ok(order.Id)`.

## API y Comunicación
- `POST /api/maintenances`
  - Body: `CreateMaintenanceReservationRequest`
  - Respuestas: `201 Created` (`{ id }`), `400 Bad Request`, `404 Not Found`.

## Estrategia de Tests
- **Tests unitarios**:
  - Reserva válida genera orden en estado Pending y fecha UTC.
  - Fecha en el pasado devuelve Validation.
  - Panel o técnico inactivo devuelve Validation / NotFound.
  - Descripción vacía devuelve Validation.
