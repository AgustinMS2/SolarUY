# Plan Técnico: Spec 035 — Consulta en Lenguaje Natural con IA (LLM)

## Proyectos y Archivos Afectados

- `SolarUY.ApiService`:
  - `Dtos/ChatDtos.cs`: `ChatMessageDto`, `ChatQueryRequest`, `ChatQueryResponse`.
  - `Services/ISolarUyDomainContextRetriever.cs` y `SolarUyDomainContextRetriever.cs`: Extractor de datos del dominio (parques activos, métricas de generación de la Spec 034, alertas activas de la Spec 031 y órdenes de mantenimiento pendientes de la Spec 024).
  - `Services/ISolarUyLlmService.cs`: Interfaz de procesamiento de lenguaje natural.
  - `Services/SolarUyKnowledgeAssistant.cs`: Motor de conocimiento de dominio y procesamiento semántico local (procesa preguntas clave sobre generación, equipos, alertas, mantenimientos, rechazo fuera de dominio y bloqueo de datos sensibles).
  - `Services/GeminiLlmService.cs`: Implementación que consume Google Gemini REST API (`gemini-2.5-flash` / `gemini-2.0-flash`) inyectando el contexto de dominio y system prompt cuando `Gemini:ApiKey` está configurada, con fallback automático a `SolarUyKnowledgeAssistant`.
  - `Controllers/ChatController.cs`: Endpoint `POST /api/chat/query` protegido con `[Authorize(Roles = Roles.Any)]`.
  - `Program.cs`: Registro de servicios y cliente HTTP de Gemini en DI.
- `SolarUY.Web`:
  - `Services/ChatApiClient.cs`: Cliente HTTP para comunicarse con `POST /api/chat/query`.
  - `Components/Pages/ChatAssistant.razor`: Interfaz de chat interactiva en `/asistente-ia` con burbujas de conversación, preguntas sugeridas, spinner de carga, renderizado de respuestas formateadas y botón de reinicio de chat.
  - `Components/Layout/NavMenu.razor`: Enlace de navegación con icono de estrellas (`bi-stars`).
  - `Program.cs`: Registro de `ChatApiClient` con token Bearer de Keycloak.
- `SolarUY.Tests`:
  - `Services/SolarUyKnowledgeAssistantTests.cs`: Suite de tests unitarios TDD verificando rechazo fuera de dominio (RF-4), rechazo de credenciales (RF-8), aclaración de ambigüedad (RF-3) y respuestas correctas de parques, alertas, generación y mantenimientos (RF-1, RF-5).
  - `Architecture/SecurityArchitectureTests.cs`: Registro justificado de `ChatController.Query` en la lista permitida para ambos roles (`TechnicianAllowedMutations`).

---

## Modelo de Datos y DTOs

```csharp
public record ChatMessageDto(string Role, string Content, DateTime TimestampUtc);

public record ChatQueryRequest(string Message, List<ChatMessageDto>? History = null);

public record ChatQueryResponse(string Answer, string? Source = null);
```

---

## Servicios y Lógica de Negocio

### 1. `ISolarUyDomainContextRetriever`
Recopila de forma eficiente los hechos actuales del sistema utilizando los repositorios ya existentes:
- Parques solares activos y capacidades nominales (`ISolarParkRepository`).
- Alertas activas y severidades (`IAlertRepository`).
- Mantenimientos pendientes agendados (`IMaintenanceRepository`).
- Generación de hoy y del mes actual (`IParkGenerationRepository`).

### 2. `SolarUyKnowledgeAssistant` (Reglas de Negocio Puras)
- **Rechazo fuera de dominio (RF-4)**: Si el mensaje no contiene términos del dominio (parque, solar, generación, energía, panel, nodo, inversor, mantenimiento, falla, alerta, salud), responde el mensaje normativo: *"Solo puedo responder consultas relacionadas con la operación, generación, equipos y mantenimientos de los parques solares de SolarUY."*.
- **Bloqueo de seguridad (RF-8)**: Si detecta intentos de consultar credenciales (contraseña, password, token, keycloak, secreto, hash), deniega la solicitud inmediatamente.
- **Aclaración por ambigüedad (RF-3)**: Si se consulta por métricas sin especificar parque cuando hay múltiples parques, solicita precisión amablemente.
- **Consultas de Parques, Generación, Alertas y Mantenimientos (RF-1, RF-5)**: Responde con el estado exacto extraído de la base de datos y adjunta una tabla o resumen estructurado en Markdown.

### 3. `GeminiLlmService`
- Si `IConfiguration["Gemini:ApiKey"]` tiene valor, invoca la API `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}` enviando el system prompt con las restricciones y el contexto de dominio.
- Si no hay API key o si ocurre un fallo de conectividad externa, delega de inmediato en `SolarUyKnowledgeAssistant`.

---

## Controladores y Seguridad

- **`ChatController.cs`**:
  ```csharp
  [Authorize(Roles = Roles.Any)]
  [ApiController]
  [Route("api/chat")]
  public class ChatController : ControllerBase
  {
      [Authorize(Roles = Roles.Any)]
      [HttpPost("query")]
      public async Task<ActionResult<ChatQueryResponse>> Query([FromBody] ChatQueryRequest request, CancellationToken ct)
  }
  ```
- En `SecurityArchitectureTests.cs`: se incluye `"ChatController.Query"` en `TechnicianAllowedMutations` con la justificación formal de la Spec 035 (HU-1: Administrador y Técnico acceden al chat).

---

## Interfaz de Usuario (Blazor Server — SolarOps Clean)

1. **Ruta `/asistente-ia`**:
   - Cabecera con título "Asistente SolarUY IA" y badge de modelo ("Gemini IA" o "Asistente Local").
   - Mensaje de bienvenida inicial indicando temas de consulta disponibles.
   - Píldoras de preguntas frecuentes clickeables:
     - "¿Cuáles son los parques solares activos?"
     - "¿Hay alertas de falla activas?"
     - "¿Qué mantenimientos están pendientes?"
     - "¿Cuánto se ha generado este mes?"
   - Historial de mensajes con burbujas:
     - Usuario: color azul primario (`bg-primary text-white`), alineado a la derecha.
     - Asistente: tarjeta con borde suave y fondo claro (`bg-white border text-dark`), con soporte para tablas Markdown y texto enriquecido.
   - Caja de entrada con atajo Enter, botón de envío e indicador con spinner animado durante el procesamiento.
   - Botón "Limpiar chat" para reiniciar la conversación.

---

## Estrategia de Testing

- **Tests unitarios TDD (`SolarUyKnowledgeAssistantTests.cs`)**:
  - Detección y rechazo de preguntas fuera de dominio (RF-4).
  - Bloqueo y protección de credenciales y seguridad (RF-8).
  - Detección de ambigüedad y pedido de aclaración (RF-3).
  - Consulta de parques registrados y sus capacidades.
  - Consulta de alertas activas y severidades.
  - Consulta de mantenimientos pendientes con técnico y fecha.
  - Consulta de generación energética con tabla o resumen.
- **Tests de arquitectura (`SecurityArchitectureTests.cs`)**:
  - Pasan en verde verificando la autorización de roles para `ChatController.Query`.

