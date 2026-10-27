# Plan Técnico: Spec 013 — Asignar Inversor a Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/Inverter.cs`: entidad de inversor (Id, SerieNumber, NormalizedSerieNumber, BrandAndModel, NominalCapacityKw, SolarNodeId, Status, CreatedAtUtc).
  - `Data/SolarUyDbContext.cs`: `DbSet<Inverter>`, índice único en `NormalizedSerieNumber`, relación uno-a-uno o uno-a-muchos con `SolarNode`.
  - `Repositories/IInverterRepository.cs` e implementación: métodos `GetBySerie(string serie)`, `GetByNodeId(Guid nodeId)`, `Add(Inverter inv)`, `Update(Inverter inv)`.
  - `Services/ISolarNodeService.cs`: método `AssignInverter(Guid nodeId, AssignInverterRequest request)`.
  - `Dtos/AssignInverterRequest.cs`: DTO con `string SerieNumber`, `string BrandAndModel`, `double NominalCapacityKw`.
  - `Controllers/SolarNodesController.cs`: endpoint `POST /api/solar-nodes/{id}/inverter`.
- `SolarUY.Web`:
  - `Services/SolarNodeApiClient.cs`: método `AssignInverterAsync(Guid nodeId, AssignInverterRequest request)`.
  - `Components/Pages/SolarNodeInverterModal.razor`: componente modal para configurar y asignar inversor al nodo.
- `SolarUY.Tests`:
  - `Services/SolarNodeAssignInverterTests.cs`: tests unitarios de validación de capacidad, serie única y estados de nodo.

## Modelo de Datos y Persistencia
```csharp
public class Inverter
{
    public Guid Id { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public string NormalizedSerieNumber { get; set; } = string.Empty;
    public string BrandAndModel { get; set; } = string.Empty;
    public double NominalCapacityKw { get; set; }
    public Guid SolarNodeId { get; set; }
    public SolarNode? SolarNode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```
- Configuración en DbContext:
  - Índice único en `NormalizedSerieNumber`.
  - Capacidad nominal decimal mayor a 0.
- Migración: `AddInverterEntity`.

## Servicios y Lógica de Dominio Pura
- `AssignInverter(nodeId, request)`:
  - Verifica que el nodo exista y esté Activo. Si es Inactivo -> `Fail(Validation, "No se puede asignar un inversor a un nodo inactivo.")`.
  - Valida que `NominalCapacityKw > 0`.
  - Comprueba contra el parque del nodo que `NominalCapacityKw <= park.MaxCapacityKw`.
  - Valida que el número de serie no esté asignado a otro nodo activo.
  - Si el nodo ya posee inversor, reemplaza o actualiza el registro. Retorna `Ok()`.

## API y Comunicación
- `POST /api/solar-nodes/{id}/inverter`
  - Body: `AssignInverterRequest`
  - Respuestas: `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.

## Estrategia de Tests
- **Tests unitarios**:
  - Asignación exitosa vincula el inversor al nodo.
  - Serie duplicada en otro nodo arroja conflicto.
  - Capacidad <= 0 o mayor a la capacidad máxima del parque arroja error de validación.
  - Nodo inactivo rechaza la asignación.
