---
name: sdd
description: Úsala siempre que trabajes con Spec-Driven Development en SolarUY (docs/constitution.md o cualquier archivo dentro de specs/) - redactar, revisar o cambiar specs, planes y tareas, o implementar y validar tareas de una spec.
---

# Spec-Driven Development (SDD) en SolarUY

Metodología formal guiada por especificaciones para el desarrollo de SolarUY (.NET 9, Blazor Server, .NET Aspire, EF Core, PostgreSQL y Keycloak).

## Flujo SDD
```
Constitución → Spec → Clarificación → Plan → Tareas → Implementación → Validación → Cambio (Mantenimiento)
```

### Reglas Innegociables del Flujo
1. **Nunca avances a la siguiente fase sin la aprobación explícita del usuario.**
2. **La spec manda:** Si una entidad, atributo, validación o endpoint no está en la spec aprobada, **está prohibido implementarlo**. Si falta una definición, detén el trabajo y pregunta.
3. **Mantenimiento y cambios primero en la spec:** Todo cambio de requisitos se plasma primero en `spec.md`, luego se propaga a `plan.md` y `tasks.md`, y finalmente se refleja en el código y tests.
4. **Estructura por carpeta:** Cada especificación vive en su propia carpeta bajo `specs/NNN-nombre/` conteniendo `spec.md`, `plan.md` y `tasks.md`.
5. **Memoria viva:** Al terminar cada fase o iteración, actualiza de inmediato `MEMORY.md`.
6. **Una spec por requerimiento funcional:** Nunca agrupes varias funcionalidades (ej. "gestión de X" o "CRUD de X") en una sola spec. Cada spec define con precisión: campos y sus reglas (obligatorio, tipo, rango, largo), mensajes exactos al usuario, comportamiento de la pantalla, casos límite y de qué specs depende.

---

## 1. Plantilla de Especificación (`spec.md`)

```markdown
# Spec NNN — <Nombre de la Funcionalidad>

Estado: borrador | aprobada | implementada
Depende de: <specs previas necesarias, o "ninguna">

## Contexto y objetivo
<Qué problema resuelve y por qué aporta valor al sistema SolarUY. Un párrafo conciso.>

## Usuarios / Actores
<Administrador | Personal Especializado (Técnico) | Sistema (Background/Simulador)>

## Historias de usuario
- HU-1: Como <rol>, quiero <acción o capacidad> para <beneficio de negocio>.

## Definiciones
<Glosario o términos específicos del dominio si admiten más de una interpretación (ej: Inversor, Nodo, String de paneles, Inyección a red).>

## Datos
| Campo | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |

## Requisitos funcionales (Criterios de aceptación en EARS)
<Incluir el texto exacto de cada mensaje que ve el usuario (éxito y error).>
- RF-1: CUANDO <evento o acción del usuario>, EL SISTEMA <respuesta esperada>.
- RF-2: SI <condición no deseada, error o validación>, ENTONCES EL SISTEMA <respuesta o mitigación>.
- RF-3: MIENTRAS <estado o condición continua>, EL SISTEMA <comportamiento continuo>.
- RF-4: EL SISTEMA <comportamiento permanente o invariante del negocio>.

## Requisitos no funcionales
<Rendimiento, latencia de SignalR, concurrencia de lecturas, seguridad OpenID/Keycloak, auditoría, idioma español en UI.>

## Casos límite
<Listas vacías, duplicados, pérdida de conexión con inversor/simulador, límites de rango de sensores, concurrencia.>

## Fuera de alcance
<Lo que explícitamente NO se aborda en esta spec e iteración.>

## Criterios de finalización
<Ej.: Reglas de negocio del servicio con tests unitarios en verde, tests de arquitectura en verde y verificación visual de la pantalla Blazor.>

## Dudas abiertas
- [NECESITA ACLARACIÓN] <Pregunta o duda pendiente para el usuario>
```

> **Nota Crítica sobre la Spec**: La especificación describe el **QUÉ** y el **POR QUÉ**. Nada de nombres de clases de C#, tablas de SQL, arquitectura ni rutas de archivos: eso pertenece exclusivamente a `plan.md`.

---

## 2. Plantilla de Plan Técnico (`plan.md`)

```markdown
# Plan Técnico: Spec NNN — <Nombre de la Funcionalidad>

## Proyectos y Archivos Afectados
- `SolarUY.ApiService`: Entidades de dominio, DbContext, repositorios/servicios y controladores en `Controllers/` (no Minimal APIs).
- `SolarUY.Web`: Páginas Blazor Server, componentes Razor, servicios cliente o hubs SignalR.
- `SolarUY.AppHost`: Configuración de orquestación Aspire si requiere recursos nuevos (PostgreSQL, contenedores).
- `SolarUY.Tests`: Tests de arquitectura (globales) y tests unitarios de las reglas de negocio.

## Modelo de Datos y Persistencia (EF Core & PostgreSQL)
- Clases de entidad C# (propiedades, tipos y relaciones).
- Configuración en DbContext (`OnModelCreating`, índices, llaves foráneas).
- Migración requerida (`dotnet ef migrations add ...`).

## Servicios y Lógica de Dominio Pura
- Interfaces y clases de servicio desacopladas de la interfaz y de frameworks HTTP.
- Métodos sincrónicos (sin `Task`/`async`), salvo que la spec lo justifique y el equipo lo apruebe.
- Los servicios pueden recibir `ILogger<T>` para registrar eventos y errores.
- Cálculos o algoritmos en funciones puras (fechas pasadas como parámetro, sin DateTime.Now oculto).
- Manejo explícito de excepciones y validaciones de negocio.

## API y Comunicación
- Definición de endpoints REST (verbos HTTP, rutas, DTOs de entrada y salida, códigos de estado).
- Eventos SignalR si aplica telemetría en tiempo real.

## Interfaz de Usuario (Blazor Server)
- Maquetación y componentes Razor a crear o modificar.
- Flujo de interacción del usuario y feedback (errores, confirmaciones, spinners de carga).

## Decisiones específicas de esta spec (opcional)
- Solo si algo se aparta de `docs/constitution.md` o es propio de este caso de uso. Una o dos líneas cada una, que el equipo pueda explicar y defender.
- **No** armar una tabla de decisiones por caso de uso: las reglas generales (capas, async, logging, persistencia, nombres de BD) van en la constitución.
- No mezclar niveles de abstracción (ej. una regla de la capa de Servicio junto a una regla de nombres de columnas en PostgreSQL).

## Estrategia de Tests
- **Tests de arquitectura** (globales, basados en los del proyecto de ejemplo del curso): siempre deben quedar en verde.
- **Tests unitarios**: solo para las reglas de negocio del servicio de esta spec (validaciones, duplicados, cambios de estado). Nada de tests para entidades, migraciones, controladores ni DTOs.
- **UI Blazor**: verificación manual en navegador (sin bUnit).
- **Sin tests de integración ni Testcontainers** salvo que el usuario los pida explícitamente para un caso de uso puntual.
```

---

## 3. Plantilla de Tareas (`tasks.md`)

```markdown
# Tareas de Implementación: Spec NNN — <Nombre>

- [ ] **T1. <Descripción concisa de la tarea>.** RF-1
  - Hecho cuando: <Comprobación verificable y objetiva, ej: 'Test X pasa en verde con dotnet test'>.

- [ ] **T2. <Descripción concisa de la tarea>.** RF-2, RF-3
  - Hecho cuando: <Comprobación verificable>.

- [ ] **T3. <Componente Blazor / Interfaz>.** RF-4
  - Hecho cuando: <La vista renderiza los datos y se valida en navegador/DevTools>.
```

- Cada tarea debe insumir un máximo de **20 a 30 minutos**.
- Deben ordenarse estrictamente por dependencias (ej. modelo/migración → lógica/servicio → endpoint → interfaz).
- Si resultan más de 10 tareas, se debe proponer dividir la spec en dos fases.

---

## 4. Reglas de Implementación (Paso a Paso)
1. Toma **exclusivamente la siguiente tarea pendiente** (ej. T1).
2. **Tests según el tipo de tarea:**
   - **Tarea de servicio con reglas de negocio**: escribe primero los tests unitarios de esas reglas, comprueba que fallan con `dotnet test`, implementa lo mínimo y comprueba que pasan.
   - **Resto de tareas** (entidades, migraciones, repositorios, controladores, DTOs): implementa, compila y verifica que los tests existentes (incluidos los de arquitectura) siguen en verde. No agregues tests nuevos.
3. Para tareas de interfaz gráfica Blazor, verifica visualmente la pantalla en el navegador.
4. Marca el checkbox de la tarea en `tasks.md` (`- [x] **T1...**`).
5. **DETENTE INMEDIATAMENTE**. Muestra el resultado al usuario y no comiences la siguiente tarea hasta recibir su confirmación.
