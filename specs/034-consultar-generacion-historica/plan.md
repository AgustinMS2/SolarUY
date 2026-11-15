# Plan Técnico: Spec 034 — Consultar Generación Histórica

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Domain/ParkHourlyGeneration.cs`: Entidad para registrar y consultar generación agregada horaria por parque.
  - `Data/SolarUyDbContext.cs`: `DbSet<ParkHourlyGeneration>`, configuración de tabla, claves foráneas e índices únicos compuestos por `(SolarParkId, DateLocal, HourLocal)`.
  - `Migrations/`: Migración EF Core `AddParkHourlyGeneration`.
  - `Repositories/IParkGenerationRepository.cs` y `ParkGenerationRepository.cs`: Métodos de consulta de rangos de fechas por parque y persistencia por lote.
  - `Services/IHistoricalGenerationSeeder.cs` y `HistoricalGenerationSeeder.cs`: Precarga determinista de datos históricos del último año para parques existentes en base a la curva solar astronómica de Uruguay (`America/Montevideo`).
  - `Dtos/HistoricalGenerationDtos.cs`: `HistoricalGenerationReportDto`, `HistoricalDataPointDto`, `HistoricalSummaryMetricsDto`.
  - `Services/IParkHistoricalGenerationService.cs` y `ParkHistoricalGenerationService.cs`: Lógica de negocio de agregación temporal (Día, Mes, Año), cálculo de Horas de Sol Equivalente (HSP), picos de potencia y comparativas porcentuales con períodos anteriores.
  - `Controllers/SolarParksController.cs` (o `ParkHistoricalController.cs`): Endpoints `GET /api/solar-parks/{id:guid}/history/daily`, `/monthly`, `/yearly` protegidos con `[Authorize(Roles = Roles.Any)]`.
  - `Program.cs`: Registro de repositorio, servicios y ejecución del seeder al inicio.
- `SolarUY.Web`:
  - `Services/ParkHistoricalApiClient.cs`: Cliente HTTP para consultar los reportes históricos.
  - `Components/Pages/ParkHistoricalGeneration.razor`: Página completa en `/historicos` y `/parques/{parkId:guid}/historico` con selector de parque, pestañas (Día, Mes, Año), tarjetas de métricas resumen, gráfica SVG nativa interactiva con hover tooltips y botón de exportación CSV con codificación UTF-8 con BOM.
  - `Components/Layout/NavMenu.razor`: Acceso directo en el menú lateral bajo navegación principal.
  - `Components/Pages/SolarParkList.razor` y `ParkMonitoring.razor`: Botón de acceso directo "Histórico" hacia el parque seleccionado.
- `SolarUY.Tests`:
  - `Fakes/FakeParkGenerationRepository.cs`: Repositorio simulado en memoria para tests rápidos.
  - `Services/ParkHistoricalGenerationServiceTests.cs`: Tests unitarios TDD para cálculos de 24 horas, agrupación mensual, agrupación anual, HSP, picos y comparativas porcentuales.
  - Tests de arquitectura (`SecurityArchitectureTests.cs`).

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Entidad `ParkHourlyGeneration`**:
  ```csharp
  public class ParkHourlyGeneration
  {
      public Guid Id { get; set; }
      public Guid SolarParkId { get; set; }
      public SolarPark? SolarPark { get; set; }
      public DateTime TimestampUtc { get; set; }
      public DateOnly DateLocal { get; set; } // Fecha en hora de Uruguay (America/Montevideo)
      public int HourLocal { get; set; }      // 0..23
      public double EnergyGeneratedKwh { get; set; }
      public double PeakPowerKw { get; set; }
  }
  ```
- **Índices en `SolarUyDbContext`**:
  - `HasIndex(e => new { e.SolarParkId, e.DateLocal, e.HourLocal }).IsUnique();`
  - `HasIndex(e => new { e.SolarParkId, e.DateLocal });`

---

## Servicios y Lógica de Dominio

### `IParkHistoricalGenerationService`
```csharp
public interface IParkHistoricalGenerationService
{
    ServiceResult<HistoricalGenerationReportDto> GetDailyReport(Guid parkId, DateOnly date);
    ServiceResult<HistoricalGenerationReportDto> GetMonthlyReport(Guid parkId, int year, int month);
    ServiceResult<HistoricalGenerationReportDto> GetYearlyReport(Guid parkId, int year);
}
```

### Reglas de Negocio en `ParkHistoricalGenerationService`:
1. **Vista Diaria (`GetDailyReport`)**:
   - Consulta las 24 horas del día `date` para el parque.
   - Puntos de la gráfica: 24 elementos (00:00 a 23:00) en `kWh`.
   - Si no hay datos registrados, retorna reporte con puntos en 0 y flag `HasData = false` (RF-8).
   - Generación total: $\sum_{h=0}^{23} \text{EnergyGeneratedKwh}$.
   - Pico máximo: $\max(\text{PeakPowerKw})$ y la hora local en que se registró.
   - Horas de sol equivalente: $\text{HSP} = \frac{\text{TotalEnergyKwh}}{\text{park.MaxCapacityKw}}$ (redondeado a 1 decimal).
   - Comparativa: se consulta el mismo día de la semana anterior (`date.AddDays(-7)`). Diferencia porcentual: $\frac{\text{Actual} - \text{Previo}}{\text{Previo}} \times 100$.
2. **Vista Mensual (`GetMonthlyReport`)**:
   - Rango: del día 1 al último día del mes (`DateTime.DaysInMonth(year, month)`).
   - Puntos de la gráfica: 28 a 31 elementos (Día 1, Día 2, ...) en `kWh`.
   - Comparativa: mes calendario anterior (`month - 1`, ajustando año si es enero).
3. **Vista Anual (`GetYearlyReport`)**:
   - Rango: del 1 de enero al 31 de diciembre del año `year`.
   - Puntos de la gráfica: 12 elementos (Ene, Feb, ..., Dic) convertidos a `MWh` ($\text{MWh} = \frac{\text{kWh}}{1000}$).
   - Comparativa: año calendario anterior (`year - 1`).

---

## API y Controladores REST (`SolarParksController.cs`)

- `GET /api/solar-parks/{id:guid}/history/daily?date=yyyy-MM-dd`
- `GET /api/solar-parks/{id:guid}/history/monthly?year=yyyy&month=MM`
- `GET /api/solar-parks/{id:guid}/history/yearly?year=yyyy`
- Atributo de autorización: `[Authorize(Roles = Roles.Any)]` (Administrador y Técnico).
- Respuestas: `200 OK` con `HistoricalGenerationReportDto`, `400 BadRequest` (parámetros inválidos), `404 NotFound` (parque inexistente).

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

1. **Ruta `/historicos`**:
   - Selector de Parque Solar (`<select class="form-select">`).
   - Pestañas superiores estilizadas: "Día", "Mes", "Año".
   - Selectores temporales adaptados al modo:
     - Día: `<input type="date" />` (máximo hoy).
     - Mes: selector de mes y año.
     - Año: selector de año.
   - 4 Tarjetas de Métricas en rejilla superior:
     1. Generación Total (con badge verde/rojo de comparativa vs período previo).
     2. Pico de Potencia Instantánea y hora de ocurrencia.
     3. Horas de Sol Equivalente (HSP).
     4. Capacidad Nominal de Placa.
   - Gráfica interactiva SVG:
     - Ejes X e Y con marcas legibles.
     - Barras de generación con gradiente SolarOps Clean (`#0284c7` a `#38bdf8`).
     - Tooltip interactivo flotante en hover con fecha/hora y valor formateado a dos decimales.
   - Botón "Exportar a CSV" (`bi-download`):
     - Descarga client-side en formato UTF-8 con BOM: `Fecha_Hora_Local,Parque,Energia_kWh,Potencia_Pico_kW`.
   - Mensaje de estado vacío amigable cuando `HasData == false` (RF-8).

---

## Estrategia de Testing

- **Tests unitarios TDD (`ParkHistoricalGenerationServiceTests.cs`)**:
  - Agregación horaria de 24 horas y sumas de energía.
  - Agregación de días para mes completo y año bisiesto.
  - Agregación mensual a anual y conversión correcta a MWh.
  - Cálculo de Horas de Sol Equivalente (HSP) con división por capacidad nominal.
  - Comparativas porcentuales positivas, negativas y caso base de período previo nulo.
  - Manejo de parque inexistente o sin datos.
- **Tests de arquitectura (`SecurityArchitectureTests.cs`)**:
  - Verificación de políticas `[Authorize]` en los nuevos endpoints.

