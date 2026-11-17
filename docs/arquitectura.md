# Arquitectura de SolarUY

Diagramas técnicos de la solución. Están escritos en [Mermaid](https://mermaid.js.org/): GitHub los muestra directamente y en VS Code se ven con la vista previa de Markdown (extensión "Markdown Preview Mermaid Support"). Los principios de diseño están en [`constitution.md`](constitution.md) y el detalle de cada funcionalidad en [`../specs/`](../specs/).

**Contenido**

1. [Visión general](#1-visión-general)
2. [Capas de la API y reglas de dependencia](#2-capas-de-la-api-y-reglas-de-dependencia)
3. [Modelo de datos](#3-modelo-de-datos)
4. [Monitoreo en tiempo real](#4-monitoreo-en-tiempo-real)
5. [Cómo el simulador produce una lectura](#5-cómo-el-simulador-produce-una-lectura)
6. [Historia de generación](#6-historia-de-generación)
7. [Autenticación y autorización](#7-autenticación-y-autorización)
8. [Ciclos de vida (estados)](#8-ciclos-de-vida-estados)
9. [Consulta en lenguaje natural](#9-consulta-en-lenguaje-natural)
10. [Metodología de trabajo (SDD)](#10-metodología-de-trabajo-sdd)
11. [Ejecución y varias copias de la API](#11-ejecución-y-varias-copias-de-la-api)

---

## 1. Visión general

SolarUY es una aplicación .NET 9 orquestada con .NET Aspire. El navegador solo habla con la aplicación Blazor Server; esta consulta a la API con el token del usuario y recibe los datos en vivo por SignalR. La identidad la resuelve Keycloak (OpenID Connect) y los datos viven en PostgreSQL. No se interactúa con inversores reales: el simulador de la API ocupa el lugar de la API local del inversor.

```mermaid
flowchart LR
    U["Usuario<br/>Administrador o Técnico"] -->|HTTPS| W

    subgraph AppHost[".NET Aspire — SolarUY.AppHost"]
        W["SolarUY.Web<br/>Blazor Server"]
        A["SolarUY.ApiService<br/>API REST, hubs SignalR<br/>y servicios en segundo plano"]
        K["Keycloak<br/>OpenID Connect"]
        P[("PostgreSQL<br/>solaruydb")]
    end

    G["Google Gemini<br/>(servicio externo, opcional)"]

    W -->|"login OIDC"| K
    W -->|"HTTP + Bearer"| A
    W <-->|"SignalR (WebSocket)"| A
    A -->|"valida el JWT"| K
    A -->|"EF Core"| P
    A -->|"consultas en lenguaje natural"| G
```

| Proyecto | Responsabilidad |
| :--- | :--- |
| `SolarUY.AppHost` | Orquesta Postgres, Keycloak (con el realm importado), la API y el Web, y sus dependencias de arranque. |
| `SolarUY.ApiService` | Controladores REST, hubs SignalR, servicios de negocio, repositorios, simulador y servicios en segundo plano. |
| `SolarUY.Web` | Páginas Blazor Server, clientes HTTP hacia la API y conexiones SignalR. |
| `SolarUY.ServiceDefaults` | Configuración común: salud (`/health`), telemetría y descubrimiento de servicios. |
| `SolarUY.Tests` | Tests unitarios de reglas de negocio y tests de arquitectura. |

---

## 2. Capas de la API y reglas de dependencia

Los servicios y repositorios son síncronos y no conocen HTTP. Los controladores solo hablan con servicios. Los tests de arquitectura (`SolarUY.Tests/Architecture`) verifican estas reglas en cada ejecución de `dotnet test`.

```mermaid
flowchart TB
    subgraph Web["SolarUY.Web"]
        Pages["Páginas Razor<br/>Components/Pages"] --> Clients["Clientes HTTP<br/>Services/*ApiClient"]
    end

    Clients -->|"HTTP + Bearer"| Controllers

    subgraph Api["SolarUY.ApiService"]
        Controllers["Controllers<br/>[Authorize(Roles)]"] --> Services["Services<br/>reglas de negocio"]
        Services --> Repos["Repositories"]
        Repos --> Db["SolarUyDbContext<br/>EF Core"]
        Services --> Sim["Simulation<br/>motor de lecturas (funciones puras)"]
        BG["Servicios en segundo plano<br/>emisión en vivo e historia"] --> Services
        BG --> Sim
        BG --> Hubs["Hubs SignalR"]
    end

    Db --> PG[("PostgreSQL")]
```

Reglas que cumple el código y que los tests de arquitectura comprueban:

- Los controladores no dependen de los repositorios.
- Los servicios y los repositorios no dependen de controladores ni de MVC.
- Todos los controladores exigen un rol (`[Authorize(Roles = ...)]`).
- Toda operación que modifica datos es solo para el Administrador, salvo una lista corta y justificada (confirmar un mantenimiento, reconocer una alerta y consultar al asistente).

---

## 3. Modelo de datos

Los usuarios y sus roles no están en la base: los gestiona Keycloak. Las líneas punteadas son referencias por identificador **sin llave foránea**, a propósito: dar de baja un equipo no debe borrar ni bloquear su historia ni sus fallas.

```mermaid
erDiagram
    SolarPark ||--o{ SolarNode : "tiene"
    SolarPark ||--o{ SolarPanel : "posee"
    SolarNode ||--o| Inverter : "tiene uno"
    SolarNode |o--o{ SolarPanel : "agrupa"
    SolarPanel ||--o{ MaintenanceOrder : "recibe"
    Technician ||--o{ MaintenanceOrder : "realiza"
    SolarPark ||--o{ Alert : "genera"
    SolarPanel ||--o{ PanelControlEvent : "bitácora"
    SolarPark ||--o{ GridInjectionEvent : "bitácora"
    SolarPark ||--o{ ParkHourlyGeneration : "historia horaria"
    Inverter ||..o{ InverterMinuteReading : "historia por minuto"
    Inverter ||..o{ SimulatedFault : "falla simulada"
    SolarPanel ||..o{ SimulatedFault : "falla simulada"

    SolarPark {
        guid Id PK
        string Name
        double MaxCapacityKw
        double PowerAlertThresholdKw
        string Status
        bool GridInjectionEnabled
    }
    SolarNode {
        guid Id PK
        string Name
        guid SolarParkId FK
        string Status
    }
    Inverter {
        guid Id PK
        string SerieNumber
        double NominalCapacityKw
        guid SolarNodeId FK
        bool GridInjectionEnabled
    }
    SolarPanel {
        guid Id PK
        string SerieNumber
        int PowerW
        double Efficiency
        guid SolarParkId FK
        guid SolarNodeId FK
        string Status
    }
    Technician {
        guid Id PK
        string FullName
        string DocumentId
        string Email
        string Specialty
        string Status
    }
    MaintenanceOrder {
        guid Id PK
        guid SolarPanelId FK
        guid TechnicianId FK
        string Type
        datetime ScheduledDateUtc
        string Status
        string ResolutionNotes
    }
    Alert {
        guid Id PK
        guid SolarParkId FK
        string EquipmentType
        guid EquipmentId
        string Severity
        string Status
        int OccurrenceCount
    }
    PanelControlEvent {
        guid Id PK
        guid SolarPanelId FK
        string Action
        string OrderedBy
        datetime OrderedAtUtc
    }
    GridInjectionEvent {
        guid Id PK
        guid ParkId FK
        string TargetType
        string Action
        string OrderedBy
        string Reason
    }
    ParkHourlyGeneration {
        guid Id PK
        guid SolarParkId FK
        datetime TimestampUtc
        double EnergyGeneratedKwh
        double PeakPowerKw
    }
    InverterMinuteReading {
        guid InverterId PK
        datetime MinuteUtc PK
        double AvgPowerKw
        double EnergyKwh
        double AvgTemperatureC
    }
    SimulatedFault {
        guid Id PK
        string FaultType
        guid TargetId
        datetime StartedAtUtc
        datetime EndedAtUtc
    }
```

---

## 4. Monitoreo en tiempo real

Un servicio en segundo plano por tipo de pantalla (parque, nodo, panel y salud) despierta en los mismos instantes, múltiplos de `Simulation:IntervalSeconds` (5 segundos por defecto), y calcula todo sobre ese mismo instante. Por eso las cuatro pantallas muestran los mismos valores. Cada pantalla se suscribe al grupo SignalR de su equipo (`park-{id}`, `node-{id}`, `panel-{id}` y `park-health-{id}`) y solo recibe lo suyo.

```mermaid
sequenceDiagram
    autonumber
    participant Reloj as SimulationClock
    participant Em as Servicio de emisión
    participant Rep as Repositorios
    participant Mot as Motor de simulación
    participant Ser as Servicio de telemetría
    participant Hub as Hub SignalR
    participant UI as Pantalla Blazor
    participant Al as Publicador de alertas

    Reloj->>Em: despierta en un múltiplo del intervalo
    Em->>Rep: equipos activos y fallas simuladas
    Rep-->>Em: parques, nodos, inversores, paneles y fallas
    Em->>Mot: lectura (equipo, instante, fallas)
    Mot-->>Em: potencia, tensiones, frecuencia y temperatura
    Em->>Ser: calcular métricas y umbrales
    Ser-->>Em: DTO con banderas (sobretemperatura, falla, etc.)
    Em->>Hub: enviar al grupo del equipo
    Hub-->>UI: lectura en vivo
    Em->>Al: detección de falla (si corresponde)
    Al->>Rep: registra la alerta (deduplicado de 60 s)
    Al->>Hub: notifica por el hub de alertas
    Hub-->>UI: campana con contador y aviso
```

Las pantallas de monitoreo están en `/parques/{id}/monitoreo`, `/nodos/{id}/monitoreo`, `/paneles/{id}/monitoreo` y `/parques/{id}/salud` (mapa de salud). Si se pierde la conexión muestran "Reconectando…" y conservan la última lectura.

---

## 5. Cómo el simulador produce una lectura

La lectura es una función pura del equipo y del instante: el mismo equipo en el mismo instante da siempre el mismo valor, en cualquier pantalla y en cualquier copia del servidor. El ruido (nubosidad, variación de cada equipo) sale de un generador determinista, no de números al azar.

```mermaid
flowchart LR
    E["Equipo + instante UTC"] --> C["SolarCalendar<br/>amanecer y atardecer de la fecha"]
    C --> F["Curva solar<br/>seno entre amanecer y atardecer"]
    F --> M["× nubosidad del parque<br/>× variación propia del equipo"]
    M --> T{"FaultTimeline<br/>¿hay una falla vigente?"}
    T -->|no| R["Lectura"]
    T -->|sí| X["Efecto de la falla<br/>corte, degradación, sobretemperatura<br/>o falla de aislamiento"]
    X --> R
    R --> L1["Pantallas en vivo"]
    R --> L2["Resumen por minuto de cada inversor"]
    R --> L3["Resumen horario de cada parque"]
```

La duración del día cambia con la época del año (de unas 10,1 horas el 21 de junio a unas 14,3 el 21 de diciembre), y la potencia del parque es la suma de la de sus inversores.

---

## 6. Historia de generación

Al arrancar, la API completa en segundo plano la historia que falta, con la misma función del simulador, sin demorar el inicio. Un candado de PostgreSQL hace que, si hay varias copias de la API, una sola complete la historia a la vez. Luego, cada minuto guarda el minuto y la hora que se cerraron. El guardado es idempotente: repetirlo no duplica registros.

```mermaid
flowchart TB
    S["Arranque de la API<br/>(la base puede estar vacía)"] --> B["SimulationHistoryBackgroundService<br/>en segundo plano"]
    B --> K{"Candado de PostgreSQL<br/>¿otra copia está completando?"}
    K -->|sí| W["Espera al próximo minuto"]
    K -->|no| P["HistoryGapPlanner<br/>qué horas y minutos faltan"]
    P --> H["Horas: desde el 1 de enero del año anterior<br/>a ParkHourlyGenerations"]
    P --> M["Minutos: últimos 30 días<br/>a InverterMinuteReadings"]
    H --> Q["Reportes históricos<br/>por día, mes y año"]
    M --> Q2["Detalle por inversor<br/>para futuros reportes"]
    B --> L["Cada minuto: guarda el minuto<br/>y la hora cerrados"]
```

---

## 7. Autenticación y autorización

La autenticación es externa y estándar (OpenID Connect con Keycloak). Los roles de Keycloak son **Administrador** y **Técnico**. El Web guarda la sesión en una cookie y llama a la API con el token del usuario (renovándolo cuando vence); la API valida el JWT y aplica los roles.

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario
    participant W as SolarUY.Web
    participant K as Keycloak
    participant A as SolarUY.ApiService

    U->>W: abre una página protegida
    W-->>U: redirige al login de Keycloak
    U->>K: usuario y contraseña
    K-->>W: código de autorización
    W->>K: canjea el código
    K-->>W: access token, id token y refresh token
    W-->>U: cookie de sesión
    U->>W: usa la aplicación
    W->>A: petición HTTP con el access token (Bearer)
    A->>K: valida el JWT con las claves públicas
    A-->>W: respuesta según el rol (403 si no corresponde)
    Note over W,A: los hubs SignalR reciben el token en la query (access_token)
```

| Rol | Puede |
| :--- | :--- |
| Administrador | Todo: parques, nodos, inversores, paneles, técnicos, mantenimientos, control remoto, usuarios y roles, simulador de fallas. |
| Técnico | Consultar (monitoreo, históricos, alertas, asistente) y atender sus mantenimientos (ver y confirmar los suyos). |

---

## 8. Ciclos de vida (estados)

**Alerta de falla**

```mermaid
stateDiagram-v2
    [*] --> Pendiente: falla detectada
    Pendiente --> Reconocida: reconocer
    Reconocida --> Descartada: descartar
    Pendiente --> Atendida: mantenimiento del panel completado
    Reconocida --> Atendida: mantenimiento del panel completado
    Descartada --> [*]: purga nocturna (más de 24 h)
```

Una misma falla detectada de nuevo dentro de 60 s no crea otra alerta; la tercera detección en 5 minutos se consolida en una sola con un contador.

**Orden de mantenimiento**

```mermaid
stateDiagram-v2
    [*] --> Pendiente: reserva (Administrador)
    Pendiente --> Pendiente: modificar fecha o técnico
    Pendiente --> Completada: confirmar (Técnico)
    Pendiente --> Cancelada: cancelar con motivo (Administrador)
    Completada --> [*]
    Cancelada --> [*]
```

**Panel solar**

```mermaid
stateDiagram-v2
    [*] --> Activo
    Activo --> Desactivado: orden remota de corte
    Desactivado --> Activo: orden de reconexión
    Activo --> Inactivo: baja lógica
```

**Falla simulada (entorno de pruebas)**

```mermaid
stateDiagram-v2
    [*] --> Activa: inyectar (Administrador)
    Activa --> Terminada: restablecer el equipo o todas
    Terminada --> [*]: queda guardada con su fecha de fin
```

---

## 9. Consulta en lenguaje natural

Solo para consultas de información. Si hay una clave de Gemini configurada, el asistente le envía la pregunta junto con una foto del estado del sistema (parques, alertas, mantenimientos y generación); si no hay clave o Gemini falla, responde un asistente local que usa los mismos datos.

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario
    participant P as Pantalla del asistente
    participant C as ChatController
    participant L as GeminiLlmService
    participant R as Contexto del dominio
    participant G as Google Gemini
    participant Loc as Asistente local

    U->>P: "¿Cómo está funcionando todo hoy?"
    P->>C: POST /api/chat/query
    C->>L: procesar la consulta
    alt hay clave y Gemini responde
        L->>R: estado actual (parques, alertas, mantenimientos, generación)
        R-->>L: datos
        L->>G: pregunta + contexto
        G-->>L: respuesta en Markdown
    else sin clave, o error de Gemini
        L->>Loc: procesar la consulta
        Loc-->>L: respuesta con los datos del sistema
    end
    L-->>C: respuesta
    C-->>P: respuesta
    P-->>U: texto con tablas y sugerencias
```

---

## 10. Metodología de trabajo (SDD)

El proyecto se desarrolla con IA siguiendo Spec-Driven Development: la especificación manda y ninguna fase se salta sin aprobación. La memoria del proyecto (`MEMORY.md`) se actualiza al terminar cada fase.

```mermaid
flowchart LR
    C["Constitución<br/>docs/constitution.md"] --> S["Spec<br/>spec.md"]
    S --> Cl["Clarificación"]
    Cl --> Pl["Plan<br/>plan.md"]
    Pl --> T["Tareas<br/>tasks.md"]
    T --> I["Implementación<br/>hasta 3 tareas por vez"]
    I --> V["Validación<br/>tests y navegador"]
    V --> M["MEMORY.md<br/>estado y siguiente paso"]
    M -.->|"siguiente spec"| S
    V -.->|"cambio"| S
```

En la implementación solo las reglas de negocio de los servicios llevan tests unitarios (primero en rojo); la interfaz se valida en el navegador y los tests de arquitectura deben seguir en verde.

---

## 11. Ejecución y varias copias de la API

Se ejecuta con `dotnet run --project SolarUY.AppHost` (requiere Docker y .NET 9). El AppHost levanta hoy una copia de la API y una del Web.

Las piezas que podrían correr en más de una copia de la API están pensadas para convivir:

- **Lecturas deterministas:** dos copias calculan exactamente los mismos valores para el mismo equipo e instante.
- **Historia idempotente:** el guardado por minuto y por hora no duplica registros, y un candado de PostgreSQL evita que dos copias completen la historia a la vez.
- **Estado en la base:** las fallas simuladas, las bitácoras de control y las alertas viven en PostgreSQL, no en la memoria de una copia.
