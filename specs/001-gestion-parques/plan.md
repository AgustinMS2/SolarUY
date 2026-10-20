# Plan Técnico: Spec 001 — CRUD de Parques Solares

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`:
  - Modelo y persistencia: `SolarPark` (entidad), `SolarParkStatus` (enum), configuración en `SolarUyDbContext`, migración EF Core.
  - Acceso a datos: `ISolarParkRepository` / `SolarParkRepository` (EF Core sincrónico).
  - Lógica de negocio: `ISolarParkService` / `SolarParkService` (con `ILogger<SolarParkService>`).
  - API HTTP: `Controllers/SolarParksController.cs` (hereda de `ControllerBase`, rutas estándar `/api/solar-parks`).
  - DTOs: `CreateSolarParkRequest`, `UpdateSolarParkRequest`, `SolarParkDto`.
- `SolarUY.Web`:
  - Cliente HTTP: `SolarParkClientService`.
  - Vistas Blazor Server: `Pages/SolarParks.razor` (listado) y `Pages/SolarParkForm.razor` (alta/edición).
- `SolarUY.Tests`:
  - Tests unitarios para `SolarParkService` (validación de reglas de negocio y manejo de errores con repositorio mockeado).
  - Tests de arquitectura iniciales (verificación de convención de controladores y capas).

---

## Modelo de Datos y Persistencia

### Entidad `SolarPark`
```csharp
public class SolarPark
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double MaxCapacityKw { get; set; }
    public double PowerAlertThresholdKw { get; set; }
    public SolarParkStatus Status { get; set; } = SolarParkStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum SolarParkStatus
{
    Active = 1,
    Inactive = 2
}
```

### Configuración en `SolarUyDbContext`
- Tabla: `SolarParks`.
- Clave primaria: `Id`.
- Índice único en `Name` a nivel de base de datos.
- Coordenadas y capacidades validadas a nivel de lógica en el servicio y mediante data annotations.

---

## Capa de Negocio y Repositorio (Métodos Sincrónicos)

### Interfaz `ISolarParkRepository`
```csharp
public interface ISolarParkRepository
{
    SolarPark? GetById(Guid id);
    SolarPark? GetByName(string name);
    IReadOnlyList<SolarPark> GetAll();
    void Add(SolarPark park);
    void Update(SolarPark park);
}
```

### Interfaz `ISolarParkService`
```csharp
public interface ISolarParkService
{
    Result<Guid> Create(CreateSolarParkRequest request);
    Result Update(Guid id, UpdateSolarParkRequest request);
    Result Deactivate(Guid id);
    IReadOnlyList<SolarParkDto> GetAll();
    SolarParkDto? GetById(Guid id);
}
```

### Implementación del Servicio (`SolarParkService`)
- Recibe por inyección de dependencias `ISolarParkRepository` e `ILogger<SolarParkService>`.
- **Regla RF-1 (Creación)**: Valida campos requeridos (nombre no vacío, capacidad > 0, coordenadas). Asigna `Status = Active` y fecha UTC.
- **Regla RF-2 (Unicidad de Nombre)**: Consulta `GetByName(request.Name.Trim())`. Si ya existe otro parque con ese nombre (comparando sin distinguir mayúsculas/minúsculas), retorna error de duplicado.
- **Regla RF-3 (Listado)**: Obtiene todos los parques ordenados por nombre y los proyecta a `SolarParkDto`.
- **Regla RF-4 (Modificación)**: Verifica existencia, valida duplicado si el nombre cambió, actualiza datos y registra `UpdatedAtUtc = DateTime.UtcNow`.
- **Regla RF-5 (Baja Lógica)**: Setea `Status = Inactive` y actualiza `UpdatedAtUtc`.

---

## Controlador HTTP (`SolarParksController`)

Ubicación: `SolarUY.ApiService/Controllers/SolarParksController.cs`

```csharp
[ApiController]
[Route("api/solar-parks")]
public class SolarParksController : ControllerBase
{
    private readonly ISolarParkService _service;
    private readonly ILogger<SolarParksController> _logger;

    public SolarParksController(ISolarParkService service, ILogger<SolarParksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var park = _service.GetById(id);
        return park is null ? NotFound() : Ok(park);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateSolarParkRequest request)
    {
        var result = _service.Create(request);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] UpdateSolarParkRequest request)
    {
        var result = _service.Update(id, request);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Deactivate(Guid id)
    {
        var result = _service.Deactivate(id);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }
}
```

---

## Interfaz de Usuario (Blazor Server)

1. **`SolarParkClientService`**: Servicio inyectable en Blazor para invocar el API HTTP de forma limpia y manejar deserialización y errores.
2. **`Pages/SolarParks.razor`**:
   - Tabla que lista los parques (Nombre, Coordenadas, Capacidad kW, Estado con badge Activo/Inactivo).
   - Botón para dar de alta nuevo parque y enlaces para editar o desactivar.
3. **`Pages/SolarParkForm.razor`**:
   - Formulario sencillo con validación de campos (`EditForm`).
   - Reutilizable para alta y edición según parámetro `Id`.

---

## Estrategia de Pruebas

1. **Tests de Arquitectura** (`SolarUY.Tests`):
   - Verificar que los controladores hereden de `ControllerBase` y estén en el namespace/carpeta correspondiente.
   - Verificar que los servicios no dependan de la capa Web.
2. **Tests Unitarios de Dominio**:
   - Pruebas simples para `SolarParkService`:
     - Crear parque con datos válidos -> Éxito.
     - Crear parque con nombre duplicado -> Falla con mensaje explicativo.
     - Modificar parque existente -> Éxito y actualiza fecha.
     - Dar de baja lógica -> Cambia estado a Inactivo.
3. **Verificación de UI**:
   - Prueba manual e interactiva en navegador (flujo completo: alta -> listado -> edición -> baja lógica) y comprobación en consola de desarrollador.
   - Se elimina la necesidad de suites pesadas como `bUnit` o Testcontainers para este caso de uso.