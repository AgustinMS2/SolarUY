# Spec 035 — Consulta en Lenguaje Natural con IA (LLM)

Estado: borrador
Depende de: Spec 034 (Históricos), Spec 024 (Mantenimientos)

## Contexto y objetivo
Integrar un asistente inteligente conversacional en SolarUY potenciado por un modelo de lenguaje (LLM - Gemini API) que permita a los operadores consultar el estado del sistema, generación histórica, anomalías y mantenimientos mediante preguntas en lenguaje natural en español (ej. "¿Cuál fue el parque más productivo del mes pasado?", "¿Cuántos paneles fallaron en Salto esta semana?").

## Usuarios / Actores
- **Administrador** y **Técnico**: formulan preguntas en lenguaje cotidiano.

## Historias de usuario
- **HU-1**: Como operador, quiero realizar preguntas en lenguaje natural sobre la generación y los mantenimientos para obtener respuestas inmediatas sin tener que navegar por múltiples filtros y reportes.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario ingrese una consulta en el cuadro de texto del asistente (ej. "¿Cuánto generó el parque Salto en junio?"), EL SISTEMA procesará la pregunta con el LLM, ejecutará las consultas de datos pertinentes sobre el dominio y responderá en texto claro y profesional en español.
- **RF-2**: EL SISTEMA restringirá el contexto del LLM exclusivamente a la información de SolarUY (parques, nodos, paneles, telemetría histórica, alertas y mantenimientos).
- **RF-3**: SI la pregunta del usuario es ambigua o no especifica el parque o período, ENTONCES EL SISTEMA solicitará aclaración amablemente: "Por favor indicame a qué parque o rango de fechas te referís para poder darte el dato exacto."
- **RF-4**: SI la pregunta no está relacionada con la gestión ni los datos de SolarUY (ej. "¿Cómo hacer una torta?"), ENTONCES EL SISTEMA responderá: "Solo puedo responder consultas relacionadas con la operación, generación y mantenimiento de los parques solares de SolarUY."
- **RF-5**: EL SISTEMA incluirá en la respuesta, cuando aplique, una pequeña tabla o resumen numérico de respaldo con los datos obtenidos de la base de datos.
- **RF-6**: EL SISTEMA mantendrá el historial de la conversación durante la sesión activa en una ventana lateral tipo chat deslizable.

## Requisitos no funcionales
- Tiempo de respuesta del asistente < 4 segundos (con indicador visual de pensamiento / "Generando respuesta...").
- Manejo seguro de credenciales de la API del LLM sin exponer API Keys en el cliente Blazor.

## Casos límite
- Consultas sobre períodos sin datos: responde con honestidad "No se registran datos de generación para el parque X en las fechas solicitadas."
- Preguntas sobre fallas de seguridad: no expone contraseñas, hashes ni tokens de Keycloak.

## Fuera de alcance
- Ejecutar acciones destructivas o de control mediante chat (el asistente es de solo lectura y consulta; no puede dar de baja parques ni cortar inyección).

## Criterios de finalización
- Tests unitarios en verde para los prompts de sistema y funciones de extracción de datos (tool calling / function calling).
- Tests de arquitectura en verde.
- Verificación de chat interactivo en navegador.

## Dudas abiertas
- Ninguna.
