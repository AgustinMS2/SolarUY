# Plan Técnico: Spec 027 — Ver Generación en Tiempo Real del Parque

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Hubs/ParkTelemetryHub.cs`: Hub SignalR protegido con `[Authorize(Roles = Roles.Any)]` que gestiona grupos de suscripción por parque (`SubscribeToPark(Guid parkId)` y `UnsubscribeFromPark(Guid parkId)`).
  - `Dtos/ParkTelemetryDto.cs`: DTO con las métricas transmitidas (`ParkId`, `ParkName`, `CurrentPowerKw`, `MaxCapacityKw`, `CapacityPercentage`, `EnergyTodayKwh`, `AlertThresholdKw`, `IsAlertActive`, `TimestampUtc`).
  - `Services/IParkTelemetryService.cs` y `ParkTelemetryService.cs`: servicio de dominio puro con lógica de cálculo de métricas y evaluación del umbral de alerta según horario solar.
  - `Services/ParkTelemetryBackgroundService.cs`: `BackgroundService` en segundo plano que periódicamente (cada 3-5 segundos) genera/emite lecturas al hub SignalR para los parques activos.
  - `Program.cs`: configuración y mapeo del hub SignalR (`builder.Services.AddSignalR()` y `app.MapHub<ParkTelemetryHub>("/hubs/park-telemetry")`).
- `SolarUY.Web`:
  - `Components/Pages/ParkMonitoring.razor` (ruta `/parques/{id:guid}/monitoreo`): vista en tiempo real adaptada al diseño "SolarOps Clean", con gauge/tacómetro radial SVG, tarjetas de métricas, alertas reactivas y estado del enlace SignalR.
  - `Components/Pages/SolarParkList.razor`: adición de acción / botón directo "Monitorear" en el listado de parques.
  - `SolarUY.Web.csproj`: referencia al paquete `Microsoft.AspNetCore.SignalR.Client`.
- `SolarUY.Tests`:
  - `Services/ParkTelemetryServiceTests.cs`: tests unitarios sobre las reglas puras de cálculo de telemetría y condiciones de alerta.
  - `Architecture/ArchitectureTests.cs`: verificación de reglas arquitecturales y seguridad en endpoints.

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Sin cambios de esquema ni migraciones**: Esta especificación consume la entidad existente `SolarPark` (`MaxCapacityKw`, `PowerAlertThresholdKw`, `Status`, `Name`) y sus equipos asociados para calcular la telemetría en tiempo real.
- La persistencia a largo plazo de series temporales de generación histórica se aborda en la Spec 034.

---

## Servicios y Lógica de Dominio Pura

- `IParkTelemetryService` desacoplado de frameworks HTTP:
  - `ParkTelemetryDto CalculateMetrics(SolarPark park, double currentPowerKw, double energyTodayKwh, DateTime localTimeUruguay)`
- **Reglas de cálculo (funciones puras)**:
  - **Porcentaje de capacidad**:
    $$\text{Percentage} = \frac{\text{CurrentPowerKw}}{\text{MaxCapacityKw}} \times 100$$
    (redondeado a 1 decimal, acotado entre 0% y 100%).
  - **Evaluación de umbral de alerta (RF-3)**:
    - Se considera horario diurno en Uruguay entre las 06:00 y las 20:00 (hora local).
    - `IsAlertActive = park.Status == Active && IsDaylightHour(localTimeUruguay) && currentPowerKw < park.PowerAlertThresholdKw`.
  - **Parque inactivo o noche (Casos límite)**:
    - Si el parque no está en estado `Active` o es de noche, `CurrentPowerKw` se reporta en `0.0`.

---

## API y Comunicación

### Hub SignalR
- **Ruta**: `/hubs/park-telemetry`
- **Seguridad**: `[Authorize(Roles = Roles.Any)]` a nivel de clase del Hub.
- **Métodos invocables por el cliente**:
  - `SubscribeToPark(Guid parkId)`: asocia el `ConnectionId` al grupo `park-{parkId}` y emite inmediatamente la última lectura disponible.
  - `UnsubscribeFromPark(Guid parkId)`: remueve la conexión del grupo.
- **Evento emitido hacia los clientes**:
  - `ReceiveParkTelemetry(ParkTelemetryDto telemetry)` enviado a `Clients.Group($"park-{parkId}")`.

### Servicio en Segundo Plano (`ParkTelemetryBackgroundService`)
- Corre como `IHostedService` (`BackgroundService`) con un intervalo configurable de 3 a 5 segundos (RF-2).
- En cada ciclo, evalúa los parques activos y emite las métricas actualizadas a través de `IHubContext<ParkTelemetryHub>`.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **Componente**: `ParkMonitoring.razor` en `/parques/{id:guid}/monitoreo`:
  - **Encabezado y Navegación**:
    - Título con nombre del parque y capacidad contratada.
    - Indicador visual del enlace SignalR con icono y badge:
      - 🟢 *Conectado en vivo*
      - 🟡 *Reconectando con el parque...* (RF-4)
      - 🔴 *Desconectado*
    - Botón "Volver a Parques".
  - **Indicador Principal**:
    - Gauge / Tacómetro radial responsivo (implementado en SVG puro ligero, sin librerías pesadas externas), con aguja animada fluida que refleja los kW instantáneos y el % de capacidad.
  - **Cuadrícula de Métricas Clave**:
    - Tarjeta 1: Potencia instantánea (kW).
    - Tarjeta 2: Porcentaje de uso sobre la capacidad máxima (%).
    - Tarjeta 3: Energía acumulada del día (kWh).
    - Tarjeta 4: Umbral de alerta configurado (kW).
  - **Banner de Alerta Reactivo (RF-3)**:
    - Si `IsAlertActive` es true, muestra alerta ámbar visible: *"Generación por debajo del umbral de alerta (<umbral> kW)."*
  - **Ciclo de vida y desconexión (RF-5)**:
    - `HubConnection` con `.WithAutomaticReconnect()`.
    - Implementa `IAsyncDisposable` para desuscribirse del grupo y cerrar la conexión al abandonar la pantalla.

---

## Estrategia de Tests

- **Tests unitarios (`ParkTelemetryServiceTests.cs`)**:
  - Cálculo exacto de porcentaje sobre capacidad máxima.
  - Activación de alerta cuando la potencia generada es inferior al umbral durante horas de sol.
  - No activación de alerta fuera de horas de sol (de noche) aunque la potencia sea 0.
  - Tratamiento adecuado cuando el parque está inactivo.
- **Tests de arquitectura**:
  - Verificación de que el Hub SignalR respeta las directivas de seguridad `[Authorize]` y convive con los controladores REST.
- **Verificación UI interactiva**:
  - Validación en navegador de la reactividad SignalR, reconexión automática y animación del gauge.

