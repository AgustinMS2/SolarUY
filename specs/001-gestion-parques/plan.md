# Plan Técnico: Spec 001 — Gestión de Parques Solares

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`: Entidad `SolarPark`, `DbContext`, repositorio `ISolarParkRepository`/`SolarParkRepository`, servicio `ISolarParkService`/`SolarParkService`, endpoints REST (`/api/solar-parks`), migración EF Core.
- `SolarUY.Web`: Páginas Blazor Server `SolarParks.razor` (listado), `SolarParkForm.razor` (crear/editar), componente `SolarParkCard.razor`, servicio cliente `SolarParkClientService`, validaciones y feedback UI.
- `SolarUY.AppHost`: Registro de proyecto `SolarUY.ApiService` y cadena de conexión PostgreSQL (ya configurada en infraestructura base).
- `SolarUY.Tests`: Tests unitarios para servicio y repositorio, tests de integración para endpoints API, tests de componente Blazor (bUnit).

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

### Entidad `SolarPark` (C#)
```csharp
public class SolarPark
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string TimeZoneId { get; private set; } // IANA tz database name, ej. "America/Montevideo"
    public double MaxCapacityKw { get; private set; }
    public double PowerAlertThresholdKw { get; private set; }
    public SolarParkStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } // Concurrencia optimista

    // Navigation
    public ICollection<Node> Nodes { get; private set; } = new List<Node>();

    // Factory methods para encapsular validaciones de negocio
    public static SolarPark Create(...)
    public void Update(...)
    public void Deactivate(...)
}
```

### Enum `SolarParkStatus`
```csharp
public enum SolarParkStatus { Active = 1, Inactive = 2 }
```

### Configuración en `SolarUyDbContext`
- Tabla `SolarParks` con PK `Id` (Guid, `gen_random_uuid()`).
- Índice único en `Name` (case-insensitive via `citext` o collation `CI`).
- Check constraints: `Latitude BETWEEN -90 AND 90`, `Longitude BETWEEN -180 AND 180`, `MaxCapacityKw > 0`.
- `RowVersion` configurado como `xmin` (system column) o `byte[]` con `[Timestamp]` para concurrencia optimista.
- Relación 1:N con `Node` (cascade delete: restrict — la baja lógica del parque no borra nodos).

### Migración requerida
```bash
dotnet ef migrations add AddSolarParks --project SolarUY.ApiService
dotnet ef database update --project SolarUY.ApiService
```

---

## Servicios y Lógica de Dominio Pura

### `ISolarParkRepository`
```csharp
interface ISolarParkRepository
{
    Task<SolarPark?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<SolarPark?> GetByNameAsync(string name, CancellationToken ct); // case-insensitive
    Task<IReadOnlyList<SolarPark>> GetAllAsync(CancellationToken ct); // ordenados por Name
    Task AddAsync(SolarPark park, CancellationToken ct);
    Task UpdateAsync(SolarPark park, CancellationToken ct); // lanza DbUpdateConcurrencyException si RowVersion cambió
    Task<bool> HasActiveInjectingNodesAsync(Guid parkId, CancellationToken ct);
}
```

### `ISolarParkService` (lógica de dominio pura, sin dependencias de infraestructura)
```csharp
interface ISolarParkService
{
    Task<Result<Guid>> CreateAsync(CreateSolarParkRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(Guid id, UpdateSolarParkRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<SolarParkDto>> GetAllAsync(CancellationToken ct);
    Task<SolarParkDto?> GetByIdAsync(Guid id, CancellationToken ct);
}
```

**Reglas de negocio implementadas en el servicio:**
- **RF-1**: Validar nombre único (repo), coords en rango, `MaxCapacityKw > 0` → crea con `Status = Active`, `CreatedAtUtc = DateTime.UtcNow`.
- **RF-2**: `GetByNameAsync` antes de crear; si existe → `Result.Fail("Nombre ya en uso")`.
- **RF-4**: `UpdateAsync` actualiza campos permitidos, setea `UpdatedAtUtc = DateTime.UtcNow`, valida concurrencia via `RowVersion`.
- **RF-5**: `DeactivateAsync` llama `repo.HasActiveInjectingNodesAsync`; si `true` → `Result.Fail("No se puede dar de baja: nodos con inyección activa")`.
- **RF-6**: Si elegible → `park.Deactivate()` setea `Status = Inactive`, `UpdatedAtUtc = DateTime.UtcNow`.

**Nota sobre fechas**: Todo cálculo temporal recibe `DateTime utcNow` como parámetro inyectado (testeable), nunca `DateTime.UtcNow` directo.

---

## API y Comunicación

### Endpoints REST (`SolarParksController` / Minimal API)
| Verbo | Ruta | Descripción | Códigos | Roles |
|-------|------|-------------|---------|-------|
| POST | `/api/solar-parks` | Crear parque (RF-1, RF-2) | 201, 400, 409 | Administrador |
| GET | `/api/solar-parks` | Listar todos (RF-3) | 200 | Administrador, Técnico |
| GET | `/api/solar-parks/{id}` | Detalle parque | 200, 404 | Administrador, Técnico |
| PUT | `/api/solar-parks/{id}` | Modificar config (RF-4) | 204, 400, 404, 409 | Administrador |
| DELETE | `/api/solar-parks/{id}` | Baja lógica (RF-5, RF-6) | 204, 400, 404, 409 | Administrador |

### DTOs
```csharp
record CreateSolarParkRequest(string Name, double Latitude, double Longitude, string TimeZoneId, double MaxCapacityKw, double PowerAlertThresholdKw);
record UpdateSolarParkRequest(string Name, double Latitude, double Longitude, string TimeZoneId, double MaxCapacityKw, double PowerAlertThresholdKw, byte[] RowVersion);
record SolarParkDto(Guid Id, string Name, double Latitude, double Longitude, string TimeZoneId, double MaxCapacityKw, double PowerAlertThresholdKw, SolarParkStatus Status, int NodesCount, double TotalInstalledPowerKw, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
```

### Autorización
- Policy `RequireAdministratorRole` en POST/PUT/DELETE.
- Policy `RequireAnyRole` (Administrador | Técnico) en GET.

---

## Interfaz de Usuario (Blazor Server)

### Páginas y Componentes
1. **`Pages/SolarParks.razor`** — Listado principal
   - Tabla responsiva: Nombre, Estado (badge Activo/Inactivo), Nodos, Potencia (kW), Acciones.
   - Botón "Nuevo parque" → navega a formulario.
   - Fila clickeable → navega a edición.
   - Spinner mientras carga; toast para errores.

2. **`Pages/SolarParkForm.razor`** — Crear / Editar (compartido)
   - `EditForm` con `DataAnnotationsValidator`.
   - Campos: Nombre (required, max 200), Latitud (number, step=0.000001, min=-90 max=90), Longitud (min=-180 max=180), Zona horaria (select con IANA zones), Capacidad máxima (number, min=0.1), Umbral alerta (number, min=0).
   - Botones: Guardar, Cancelar, Eliminar (solo en edición, confirma con modal).
   - En edición: campo oculto `RowVersion` para concurrencia.

3. **`Components/SolarParkCard.razor`** — Tarjeta resumen (opcional, para dashboard futuro).

### Servicio cliente `SolarParkClientService`
- HttpClient tipado, manejo de errores 409/400 → mensajes amigables.
- Invalida caché local tras mutaciones.

### Validaciones UI (lado cliente + servidor)
- `Name`: required, trim, max 200, único (validación remota al blur).
- Coordenadas: rangos válidos, 6 decimales.
- `MaxCapacityKw > 0`.
- `PowerAlertThresholdKw >= 0` y `<= MaxCapacityKw` (advertencia, no bloqueo).

---

## Decisiones Técnicas y Alternativas Descartadas

| Decisión | Justificación | Alternativa descartada |
|----------|---------------|------------------------|
| **Columnas SQL dedicadas** para configuraciones | Queryable, indexable, tipado fuerte, migraciones explícitas; alineado con decisión de clarificación. | JSONB — flexible pero pierde constraints y consultas eficientes. |
| **Concurrencia optimista con `RowVersion` (byte[])** | Estándar EF Core, bajo overhead, funciona con PostgreSQL `xmin` o columna `bytea`. | Concurrencia pesimista (locks) — innecesaria para baja contención admin. |
| **`citext` para nombre único case-insensitive** | Nativo PostgreSQL, evita `LOWER()` en índices y consultas. | `COLLATE "C"` o `UPPER()` en índice — más verboso. |
| **Service layer pura (sin `ILogger`, `HttpContext`)** | Testeable unitario sin mocks de infraestructura; reglas de negocio aisladas. | Lógica en Controller/Endpoint — acopla a ASP.NET Core. |
| **Blazor Server (no WebAssembly)** | Requisito de arquitectura; SignalR nativo para tiempo real futuro; SEO y carga inicial rápida. | Blazor WebAssembly — no cumple requisito de stack. |
| **Minimal API vs Controllers** | Minimal API: menos boilerplate, `MapGroup`, filtros de autorización limpios. | Controllers con atributos — más ceremonioso. |

---

## Matriz de Cobertura y Estrategia de Tests

| RF | Test Unitario (Servicio) | Test Integración (API) | Test Componente (bUnit) |
|----|--------------------------|------------------------|-------------------------|
| RF-1 | `CreateAsync_Valido_RetornaId` | `POST /api/solar-parks → 201` | Formulario crea y navega a listado |
| RF-2 | `CreateAsync_Duplicado_Falla` | `POST duplicado → 409` | Validación remota muestra error |
| RF-3 | `GetAllAsync_OrdenadoPorNombre` | `GET /api/solar-parks → 200, array ordenado` | Tabla renderiza ordenada, badges estado |
| RF-4 | `UpdateAsync_ActualizaYTimestamp` | `PUT → 204, UpdatedAtUtc cambiado` | Edición guarda, muestra toast éxito |
| RF-5 | `DeactivateAsync_ConNodosInyectando_Falla` | `DELETE con nodos activos → 400` | Botón eliminar deshabilitado/mensaje |
| RF-6 | `DeactivateAsync_Elegible_CambiaEstado` | `DELETE elegible → 204, Status=Inactive` | Modal confirma, lista actualiza badge |

### Estrategia
- **Unitarios** (`SolarUY.Tests.Unit`): `ISolarParkService` con `ISolarParkRepository` mockeado (Moq), `DateTime` inyectado vía `Func<DateTime>` o `TimeProvider` (NET9). Cobertura ≥ 90% en servicio.
- **Integración** (`SolarUY.Tests.Integration`): `WebApplicationFactory` + Testcontainers PostgreSQL, migraciones aplicadas en `InitializeAsync`. Tests end-to-end de endpoints con `HttpClient`.
- **UI** (`SolarUY.Tests.Components`): bUnit para `SolarParks.razor` y `SolarParkForm.razor`; renderizado, interacciones, validaciones.
- **Ejecución**: `dotnet test` en CI y local; todos en verde antes de merge.

---

## Próximos Pasos (para `tasks.md`)
1. Migración EF Core + entidad `SolarPark` + `DbContext` config.
2. Repositorio `ISolarParkRepository` + implementación.
3. Servicio `ISolarParkService` + lógica de negocio + tests unitarios.
4. Endpoints API + autorización + tests integración.
5. Página listado Blazor + servicio cliente.
6. Formulario crear/editar + validaciones + tests componente.
7. Verificación visual en navegador (escritorio + móvil).