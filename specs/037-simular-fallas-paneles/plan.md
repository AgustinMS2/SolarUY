# Plan Técnico: Spec 037 — Simular Fallas de Paneles

## Archivos Afectados

- `SolarUY.ApiService`:
  - `Domain/SimulatedFault.cs`: entidad de una falla simulada: `Id`, `FaultType`, `TargetType` (panel o inversor), `TargetId` (id del panel o del inversor), `SolarParkId`, `StartedAtUtc`, `EndedAtUtc` (nulo mientras está activa) y `StartedBy`.
  - `Domain/SimulatedFaultType.cs` y `Domain/SimulatedFaultTarget.cs`: enums de los 4 tipos y de los 2 tipos de equipo.
  - `Simulation/FaultCatalog.cs`: reglas puras de cada tipo: a qué equipo se aplica, severidad (Crítico o Advertencia) y etiqueta en español.
  - `Simulation/FaultTimeline.cs`: línea de tiempo inmutable armada con las fallas; `EffectiveFault(equipoId, instante)` devuelve la de mayor severidad activa en ese instante (inicio incluido, fin excluido). Vacía por defecto.
  - `Simulation/SolarSimulationEngine.cs`: las lecturas del inversor y del panel, la energía del día y el resumen horario reciben la línea de tiempo (por defecto vacía) y la aplican por muestra. `MinuteAggregator.Build` también la recibe.
  - `Simulation/SimulationOptions.cs`: `FaultInjectionEnabled`.
  - `Migrations/`: migración `AddSimulatedFaults` (tabla `SimulatedFaults`).
  - `Data/SolarUyDbContext.cs`: `DbSet<SimulatedFault>` y su configuración (sin llaves foráneas, como la historia: terminar o dar de baja un equipo no borra sus fallas; índice por `(TargetId, StartedAtUtc)`).
  - `Repositories/ISimulatedFaultRepository.cs` y `SimulatedFaultRepository.cs`: `Add`, `GetAll`, `GetActiveByTarget`, `EndAll(instante)`, `EndByTarget`.
  - `Dtos/SimulationFaultDtos.cs`: petición de inyección y respuesta de la pantalla (equipos con su falla activa).
  - `Services/ISimulationFaultService.cs` y `SimulationFaultService.cs`: inyectar, restablecer un equipo, restablecer todo y listar equipos con su falla.
  - `Dtos/PanelTelemetryDto.cs`, `PanelHealthReadingDto` y `NodeTelemetryDto.cs`: campo opcional `Fault` (tipo y etiqueta de la falla activa).
  - `Services/PanelTelemetryService.cs`, `PanelHealthService.cs` y `NodeTelemetryService.cs`: reciben la falla activa y la copian a la lectura; con falla de aislamiento el nodo se reporta como un inversor apagado (sin alarmas espurias).
  - `Services/AlertDetectionRules.cs`: alerta Crítica por falla de aislamiento y Advertencia por degradación severa de un panel (el corte total ya sale como "Falla Crítica" del mapa de salud y la sobretemperatura como la del nodo).
  - Servicios de emisión de las specs 028 a 030 (`NodeTelemetryBackgroundService`, `PanelTelemetryBackgroundService`, `ParkHealthSnapshotBuilder`) y `ParkTelemetryBackgroundService` (027): arman la línea de tiempo con las fallas de la base y se la pasan al motor.
  - `Services/SimulationHistoryService.cs`: ídem para la historia por minuto y por hora.
  - `Repositories/IInverterRepository.cs`: se agrega `GetById` (el servicio de fallas elige el inversor por su id).
  - `Services/ParkHealthBackgroundService.cs`: además de los paneles en Falla Crítica, publica la Advertencia de los paneles degradados por una falla simulada (descubierto al verificar la T5: sin esto la alerta de degradación nunca salía).
  - `Controllers/SimulationFaultsController.cs`: `GET /api/simulation/faults`, `POST /api/simulation/faults`, `DELETE /api/simulation/faults/{targetType}/{targetId}` y `DELETE /api/simulation/faults`, todos para el Administrador.
  - `Program.cs` y `appsettings*.json`: registros y `Simulation:FaultInjectionEnabled` (`true` en desarrollo, `false` por defecto).
- `SolarUY.Web`:
  - `Services/SimulationFaultApiClient.cs` y registro en `Program.cs`.
  - `Components/Pages/SimulationFaults.razor`: pantalla `/simulacion-fallas` (solo Administrador).
  - `Components/Layout/NavMenu.razor`: entrada "Simulación de fallas" para el Administrador.
  - `Components/Pages/PanelMonitoring.razor`, `NodeMonitoring.razor` y `ParkHealth.razor`: insignia "En Falla" y motivo.
- `specs/029-*`, `specs/030-*` y `specs/031-*`: se actualizan los textos que esta spec cumple o cambia. `specs/028-*` no se toca (se avisa al integrante).
- `SolarUY.Tests`: pruebas del catálogo y la línea de tiempo, de los efectos en el motor, de las reglas de alerta y del servicio de fallas, más un repositorio falso.

---

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)

- **Nueva tabla `SimulatedFaults`** con migración `AddSimulatedFaults`:
  - Clave primaria `Id`. `FaultType` y `TargetType` como enteros, `TargetId`, `SolarParkId` (para listar y restablecer por parque), `StartedAtUtc`, `EndedAtUtc` (nulo si está activa) y `StartedBy` (máx. 256, usuario autenticado).
  - Sin llaves foráneas a propósito: un equipo dado de baja conserva su historia de fallas, igual que la historia de la spec 036.
  - Índice `(TargetId, StartedAtUtc)` para armar la línea de tiempo de un equipo.
- Volumen: una fila por falla inyectada; es una tabla chica, por lo que la línea de tiempo se arma con todas las filas.
- **Restablecer no borra**: pone `EndedAtUtc` a las fallas activas del equipo. La historia guardada de la spec 036 ya contiene los minutos con falla y los que se calculen de nuevo coinciden porque usan las mismas fechas de inicio y fin.

---

## Lógica de Dominio y Reglas de Negocio (Puras)

- **`FaultCatalog`**: `SimulatedFaultType` → equipo al que aplica, severidad y etiqueta.
  - Panel: `OpenCircuit` (Crítico, "Corte total") y `SevereDegradation` (Advertencia, "Degradación severa / suciedad").
  - Inversor: `InsulationFault` (Crítico, "Falla de aislamiento") y `InverterOverTemperature` (Advertencia, "Sobretemperatura de inversor").
- **`FaultTimeline.EffectiveFault(equipoId, instante)`**: de las fallas del equipo con `inicio <= instante < fin` (o sin fin), la de mayor severidad; si empatan, la más reciente. Con la línea vacía no hay falla.
- **Efectos en el motor (por muestra de 10 s)**:
  - Corte total de un panel: potencia 0 W.
  - Degradación severa: `esperada × 0,30 × (1 ± 3 % propio)`, sin nubosidad ni sombreado, de modo que siempre cae entre el 10 % y el 40 % de lo esperado (estado Degradado, rendimiento bajo, nunca Falla Crítica).
  - Sobretemperatura: temperatura entre 86 y 90 °C (ruido determinista), potencia sin cambios.
  - Falla de aislamiento: potencia, corriente DC, tensión AC y frecuencia en 0 y tensión DC residual, como de noche.
  - Sin falla, todo es idéntico a la spec 036 (los tests existentes del motor siguen valiendo).
- **`SimulationFaultService`** (sincrónico, `ServiceResult`):
  - `Inject(tipoEquipo, equipoId, tipoFalla, usuario)`: habilitado por configuración (si no, `Unavailable`); el tipo de falla debe corresponder al equipo (`Validation`); el equipo debe existir (`NotFound`) y emitir lecturas: panel activo con nodo activo, o nodo activo con inversor (`Conflict`); si ya tiene esa falla activa, `Conflict`. Devuelve el mensaje de la RF-1 con la serie.
  - `ClearTarget(tipoEquipo, equipoId)`: termina todas las fallas activas del equipo; sin fallas devuelve éxito con el aviso de que no tenía.
  - `ClearAll()`: termina todas las fallas activas.
  - `GetOverview()`: lista los paneles y los inversores con su falla efectiva activa, más el indicador de habilitado.
- **Reglas de alerta** (`AlertDetectionRules`):
  - `FromNodeTelemetry`: además de la sobretemperatura, falla de aislamiento ⇒ Crítica "Falla de aislamiento en inversor: protección de fuga a tierra, inversor apagado."
  - `FromPanelHealth`: Falla Crítica como hoy (el texto menciona el corte total si es el caso) y, para un panel Degradado con la falla de degradación activa ⇒ Advertencia "Degradación severa del panel: X W de Y W esperados."
  - La deduplicación de 60 s y la consolidación a la tercera detección de la spec 031 se mantienen sin cambios.
- La línea de tiempo se lee en cada ciclo (cada 5 s) y en cada pasada de la historia; no hay estado compartido en memoria, por lo que dos copias de la API ven lo mismo.

---

## API y Comunicación en Tiempo Real

- `GET /api/simulation/faults` (Administrador): `{ enabled, panels: [...], inverters: [...] }` con id, serie, parque, nodo y falla activa de cada equipo que emite lecturas.
- `POST /api/simulation/faults` (Administrador): cuerpo `{ targetType, targetId, faultType }`; 200 con `{ message }` (RF-1); 400, 404, 409 o 503 según el servicio; los mensajes de error van en `{ message }` como el resto de los controladores.
- `DELETE /api/simulation/faults/{targetType}/{targetId}` (RF-4) y `DELETE /api/simulation/faults` (RF-5), solo Administrador.
- No hay hubs nuevos: las fallas viajan en las lecturas de los hubs de las specs 028, 029 y 030 (campo `Fault`), que ya emiten cada 5 s.
- Alertas: los servicios de emisión publican las nuevas detecciones por el publicador de la spec 031, que ya las emite por SignalR y las muestra en la campana y los avisos.

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

- **Pantalla `/simulacion-fallas`** (solo Administrador, enlace en el menú):
  - Etiqueta de advertencia "ENTORNO DE SIMULACIÓN Y PRUEBAS" y aviso si el simulador está deshabilitado.
  - Dos tablas, paneles e inversores, con parque, nodo, falla activa y, para cada equipo, un selector del tipo de falla (solo los que le corresponden) y el botón "Inyectar falla"; mensajes de éxito y de error en una alerta.
  - Botón "Restablecer / Reparar Falla Simulada" en cada equipo con falla y botón global "Restablecer todas las fallas" (con confirmación en el navegador).
- **Pantalla de panel (029)**: insignia "En Falla" con el tipo cuando la lectura trae `Fault`. **Pantalla de nodo (028)**: banner con la falla del inversor. **Mapa de salud (030)**: motivo en la tarjeta de detalle.
- Vista responsive: las tablas pasan a tarjetas en móvil.

---

## Estrategia de Testing

- **Unit tests (TDD, rojo → verde)**:
  - `FaultCatalogTests` y `FaultTimelineTests`: equipo y severidad de cada tipo, prevalencia de la crítica sobre la advertencia, límites de inicio y fin, equipos sin fallas y línea vacía.
  - `SolarSimulationEngineFaultTests`: corte total en 0 W; degradación entre 10 % y 40 % de lo esperado; sobretemperatura entre 86 y 90 °C con potencia igual; aislamiento con todo en cero; el cambio ocurre exactamente al inicio y termina al fin; `MinuteAggregator`, el resumen horario y la energía del día reflejan la falla.
  - `SimulationFaultServiceTests`: inyección válida con el mensaje de la spec, tipo que no corresponde, equipo inexistente o que no emite, duplicada, simulador deshabilitado, restablecer uno y todos, y listado.
  - `AlertDetectionRulesTests` (ampliado), `NodeTelemetryServiceTests` y `PanelHealthServiceTests`: la falla se copia a la lectura y las reglas nuevas.
- **Tests de arquitectura**: en verde; los endpoints mutantes nuevos llevan `[Authorize(Roles = Roles.Administrator)]`.
- **Verificación con la aplicación corriendo** (base real y navegador, escritorio y móvil): el ciclo de la spec (inyectar corte total → aviso de alerta Crítica → mapa en rojo → restablecer → mapa en verde), la degradación en ámbar con su Advertencia, la sobretemperatura y la falla de aislamiento en el nodo, que esta última baje la potencia del parque y quede en la historia, que el Técnico no vea ni pueda usar la pantalla y que no haya errores en la consola.
