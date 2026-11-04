# Plan Técnico: Spec 013 — Asignar Inversor a Nodo

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - `Domain/Inverter.cs`: entidad de inversor (Id, SerieNumber, NormalizedSerieNumber, BrandAndModel, NominalCapacityKw, Endpoint, SolarNodeId, CreatedAtUtc). Sin estado propio.
  - `Data/SolarUyDbContext.cs`: `DbSet<Inverter>`, índice único en `NormalizedSerieNumber`, índice único en `SolarNodeId` (máximo un inversor por nodo) y relación con `SolarNode` (`DeleteBehavior.Restrict`).
  - `Repositories/IInverterRepository.cs` e implementación `InverterRepository.cs`: `Inverter? GetBySerie(string normalizedSerie)`, `Inverter? GetByNodeId(Guid nodeId)`, `IReadOnlyList<Inverter> GetByNodeIds(IReadOnlyCollection<Guid> nodeIds)` (una sola consulta para el listado), `void Add(Inverter inverter)`, `void Update(Inverter inverter)`, `void Remove(Inverter inverter)`.
  - `Repositories/ISolarNodeRepository.cs` + implementación: `void Update(SolarNode node)` (para guardar la fecha de modificación del nodo).
  - `Services/ISolarNodeService.cs` y `SolarNodeService.cs`: `ServiceResult AssignInverter(Guid nodeId, AssignInverterRequest request)` y `ServiceResult UnassignInverter(Guid nodeId)`. `SolarNodeService` recibe además `IInverterRepository`.
  - `Services/SolarNodeService.cs` (`GetAll` y `GetById`): el DTO del nodo incluye la serie del inversor asignado (pendiente heredado de la spec 010).
  - `Services/ServiceResult.cs`: nuevo valor `ConfirmationRequired` en `ServiceErrorType` (reutilizable por la spec 014).
  - `Dtos/AssignInverterRequest.cs`: `(string? SerieNumber, string? BrandAndModel, double NominalCapacityKw, string? Endpoint, bool ConfirmReplace)`.
  - `Controllers/SolarNodesController.cs`: `POST` y `DELETE /api/solar-nodes/{id}/inverter`, ambos `[Authorize(Roles = Roles.Administrator)]`.
- `SolarUY.Web`:
  - `SolarNodeApiClient`: métodos `AssignInverterAsync` y `UnassignInverterAsync`. Hoy la clase vive en `SolarUY.ApiService/Services` (ubicación heredada de la spec 009); se extiende donde está y moverla se trata aparte.
  - `Components/Pages/SolarNodeInverterModal.razor`: modal para asignar, reemplazar y desvincular el inversor del nodo.
  - `Components/Pages/SolarNodeList.razor`: botones "Asignar inversor" / "Cambiar inversor" y "Desvincular" (solo Administrador) y la columna de inversor muestra la serie asignada.
- `SolarUY.Tests`:
  - `Fakes/FakeInverterRepository.cs`.
  - `Services/SolarNodeAssignInverterTests.cs` y `Services/SolarNodeUnassignInverterTests.cs`.
  - Los tests existentes de `SolarNodeService` pasan a construirlo con el repositorio de inversores falso.

## Modelo de Datos y Persistencia
```csharp
public class Inverter
{
    public Guid Id { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public string NormalizedSerieNumber { get; set; } = string.Empty;
    public string BrandAndModel { get; set; } = string.Empty;
    public double NominalCapacityKw { get; set; }
    public string? Endpoint { get; set; }
    public Guid SolarNodeId { get; set; }
    public SolarNode? SolarNode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```
- Configuración en DbContext:
  - `SerieNumber` y `NormalizedSerieNumber` obligatorios, máximo 50; `BrandAndModel` obligatorio, máximo 100; `Endpoint` máximo 100.
  - Índice único en `NormalizedSerieNumber` y en `SolarNodeId`.
- Migración: `AddInverterEntity`.

## Servicios y Lógica de Dominio Pura
- `AssignInverter(nodeId, request)`, en este orden (el primer error corta):
  1. Nodo inexistente -> `Fail(NotFound, "El nodo solicitado no existe.")`.
  2. Nodo inactivo -> `Fail(Validation, "No se puede asignar un inversor a un nodo inactivo.")`.
  3. Serie vacía o mayor a 50, marca y modelo vacío o mayor a 100, endpoint mayor a 100 -> `Fail(Validation, ...)` con los mensajes de RF-8 (con trim).
  4. `NominalCapacityKw <= 0` -> `Fail(Validation, ...)` (RF-4).
  5. `NominalCapacityKw > park.MaxCapacityKw` -> `Fail(Validation, ...)` (RF-5); el parque se obtiene con `ISolarParkRepository`.
  6. La serie normalizada pertenece a un inversor de otro nodo -> `Fail(Conflict, ...)` con el nombre de ese nodo (RF-3).
  7. El nodo ya tiene un inversor de distinta serie y `ConfirmReplace` es falso -> `Fail(ConfirmationRequired, mensaje de RF-2)`.
  8. Si el nodo ya tiene inversor, se actualiza ese mismo registro (reemplazo o actualización de datos: el anterior deja de existir y no hay historial); si no, se agrega uno nuevo. Se actualiza `UpdatedAtUtc` del nodo y se registra el evento con `ILogger<T>`. Retorna `Ok()`.
- `UnassignInverter(nodeId)`: nodo inexistente -> `NotFound`; sin inversor -> `Fail(NotFound, "El nodo no tiene un inversor asignado.")`; si no, elimina el inversor, actualiza `UpdatedAtUtc` del nodo y retorna `Ok()`. Se permite en nodos inactivos.
- Fechas con `DateTime.UtcNow`, igual que el resto de los servicios existentes.

## API y Comunicación
- `POST /api/solar-nodes/{id}/inverter`
  - Body: `AssignInverterRequest`
  - Respuestas: `200 OK`, `400 Bad Request` (validaciones), `404 Not Found`, `409 Conflict` (serie en otro nodo) y `409 Conflict` con `{ message, requiresConfirmation: true }` cuando falta confirmar el reemplazo.
- `DELETE /api/solar-nodes/{id}/inverter`
  - Respuestas: `204 No Content`, `404 Not Found`.

## Interfaz de Usuario (Blazor Server)
- En la lista de nodos, el Administrador ve "Asignar inversor" (nodo sin inversor) o "Cambiar inversor" y "Desvincular" (nodo con inversor); el Técnico no ve estos botones.
- Modal con número de serie, marca y modelo, capacidad (kW) y dirección o endpoint opcional; los errores se muestran dentro del modal.
- Si la API responde que falta confirmación, el modal muestra el mensaje de RF-2 con "Confirmar" y "Cancelar"; "Confirmar" reenvía el pedido con `ConfirmReplace = true` y "Cancelar" no cambia nada.
- "Desvincular" pide confirmación simple antes de llamar a la API.
- La columna de inversor muestra la serie asignada o "Sin inversor asignado".

## Decisiones específicas de esta spec
- El reemplazo actualiza el registro existente del nodo en lugar de borrar y crear otro: evita conflictos con los índices únicos y cumple "sin historial".
- `ConfirmationRequired` es un tipo de error propio para que la pantalla distinga "falta confirmar" de un conflicto real.

## Estrategia de Tests
- **Tests de arquitectura**: siguen en verde (los nuevos endpoints son solo de Administrador).
- **Tests unitarios** (solo reglas del servicio):
  - Asignación exitosa vincula el inversor al nodo y actualiza la fecha del nodo.
  - Nodo inexistente e inactivo rechazan.
  - Datos de texto inválidos (serie vacía o larga, marca y modelo vacío o largo, endpoint largo).
  - Capacidad menor o igual a 0 y mayor a la del parque.
  - Serie asignada a otro nodo devuelve Conflict con el nombre del nodo.
  - Nodo con inversor de otra serie pide confirmación y no modifica; con confirmación reemplaza.
  - Misma serie en el mismo nodo actualiza sin confirmación.
  - Desvincular: éxito, nodo sin inversor, nodo inexistente y nodo inactivo permitido.
  - El listado y el detalle de nodos muestran la serie del inversor asignado (o ninguna).
- **UI Blazor**: verificación manual en navegador (sin bUnit).
