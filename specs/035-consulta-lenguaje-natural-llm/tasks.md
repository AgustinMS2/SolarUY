# Tareas: Spec 035 — Consulta en Lenguaje Natural con IA (LLM)

- [x] **T1. DTOs de chat, `SolarUyDomainContextRetriever` y `SolarUyKnowledgeAssistant` con tests unitarios (tests primero).** RF-1, RF-2, RF-3, RF-4, RF-5, RF-8
  - Hecho cuando: las reglas de validación de dominio (RF-4), rechazo de credenciales (RF-8), aclaración de ambigüedades (RF-3) y respuestas con datos del sistema pasen en verde con `dotnet test`.

- [x] **T2. Servicio `GeminiLlmService` con fallback automático y registro en DI.** RF-1, RF-2, RF-7
  - Hecho cuando: el servicio permita procesar consultas vía Gemini API si existe `Gemini:ApiKey` o recurra al asistente local de dominio si no está configurada, con registro en DI.

- [x] **T3. Endpoint REST `POST /api/chat/query` en `ChatController` y tests de arquitectura.** RF-1, RF-9
  - Hecho cuando: el endpoint esté expuesto con `[Authorize(Roles = Roles.Any)]`, registrado en `SecurityArchitectureTests.cs` y los 485+ tests sigan en verde.

- [x] **T4. Cliente `ChatApiClient`, registro en DI del Web y acceso en `NavMenu.razor`.** RF-6, RF-9
  - Hecho cuando: `ChatApiClient` esté registrado con autenticación Bearer y el menú de navegación muestre el acceso directo a `/asistente-ia`.

- [x] **T5. Interfaz interactiva de chat `ChatAssistant.razor` en `/asistente-ia` con preguntas sugeridas y limpieza.** RF-1, RF-5, RF-6
  - Hecho cuando: la pantalla de chat permita interactuar en lenguaje natural, muestre las respuestas con tablas/resúmenes, atajos de preguntas frecuentes y botón de reinicio de conversación.

