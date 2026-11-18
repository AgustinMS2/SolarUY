# Spec 035 — Consulta en Lenguaje Natural con IA (LLM)

Estado: implementada
Depende de: Spec 002 (Listar Parques), Spec 024 (Listar Mantenimientos), Spec 031 (Alertas), Spec 034 (Generación Histórica)

## Contexto y objetivo
Integrar un asistente inteligente conversacional en SolarUY potenciado por un modelo de lenguaje (LLM - Gemini API) que permita a los operadores consultar el estado operativo, métricas de generación histórica, anomalías y órdenes de mantenimiento mediante preguntas en lenguaje natural en español (ej. *"¿Cuánto generó el parque Salto Grande en noviembre?"*, *"¿Hay alertas activas de sobretemperatura?"*, *"¿Qué mantenimientos están pendientes para esta semana?"*), presentando respuestas claras respaldadas por resúmenes tabulados.

## Usuarios / Actores
- **Administrador** y **Técnico**: formulan preguntas y analizan información del sistema mediante lenguaje cotidiano.

## Historias de usuario
- **HU-1**: Como operador, quiero realizar preguntas en lenguaje natural sobre generación, parques, mantenimientos y alertas para obtener respuestas inmediatas y consolidadas sin tener que navegar por múltiples filtros y pantallas.
- **HU-2**: Como operador, quiero contar con un panel de chat interactivo que mantenga el hilo de la conversación durante mi sesión y me ofrezca preguntas sugeridas para agilizar el análisis operativo.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario ingrese una consulta en el cuadro de texto del asistente (ej. "¿Cuánto generó el parque Salto Grande hoy?"), EL SISTEMA procesará la pregunta con el servicio de IA, recopilará los datos pertinentes del dominio (parques, generación histórica, alertas y mantenimientos) y responderá en texto claro, estructurado y profesional en español.
- **RF-2**: EL SISTEMA restringirá el ámbito de conocimiento del asistente exclusivamente a la información de SolarUY mediante un *system prompt* estricto y herramientas de consulta de dominio (function calling / tool calling).
- **RF-3**: SI la pregunta del usuario es ambigua o no especifica el parque o período temporal necesario, ENTONCES EL SISTEMA responderá solicitando aclaración: *"Por favor indicame a qué parque solar o rango de fechas te referís para poder darte el dato exacto."*
- **RF-4**: SI la pregunta no está relacionada con la gestión, generación, equipamiento ni mantenimiento de SolarUY (ej. consultas generales fuera de dominio), ENTONCES EL SISTEMA responderá: *"Solo puedo responder consultas relacionadas con la operación, generación, equipos y mantenimientos de los parques solares de SolarUY."*
- **RF-5**: CUANDO la respuesta involucre series de datos o comparativas numéricas, EL SISTEMA incluirá en el mensaje un bloque con formato Markdown o tabla de respaldo con las cifras obtenidas (ej. total de kWh, horas de sol equivalente o listado de órdenes).
- **RF-6**: EL SISTEMA proveerá una interfaz de chat interactiva en `/asistente-ia` (y accesible desde el menú de navegación) con:
  - Historial de mensajes de la sesión (mensajes del usuario y respuestas del asistente).
  - Indicador de estado y spinner animado *"Consultando datos de SolarUY..."* mientras se procesa la respuesta.
  - Botones de preguntas frecuentes o sugeridas de un clic (ej. *"Resumen de alertas activas"*, *"Generación de hoy por parque"*, *"Mantenimientos pendientes"*).
  - Botón para limpiar o reiniciar la conversación.
- **RF-7**: SI no se encuentra configurada una clave de API externa (`Gemini:ApiKey`), ENTONCES EL SISTEMA utilizará el proveedor de consultas de dominio local de respaldo (`SolarUyKnowledgeAssistant`), garantizando que las consultas clave de generación, parques, alertas y mantenimientos continúen respondiendo con datos reales de la base de datos sin errores ni interrupciones.
- **RF-8**: SI el usuario intenta solicitar datos sensibles de seguridad (como contraseñas, hashes, secretos de autenticación o tokens de Keycloak), ENTONCES EL SISTEMA denegará la solicitud respondiendo que no está autorizado a divulgar información de credenciales ni de seguridad interna.
- **RF-9**: SI el usuario no tiene una sesión autenticada con rol `Administrador` o `Técnico`, ENTONCES EL SISTEMA denegará el acceso al asistente respondiendo HTTP 401/403 o redirigiendo al inicio de sesión.

## Requisitos no funcionales
- **Seguridad**: Las claves de API del LLM se almacenan exclusivamente en el backend (`SolarUY.ApiService`) en variables de configuración protegidas; nunca se transmiten ni exponen al cliente Blazor.
- **Rendimiento**: Tiempo de respuesta < 4 segundos para consultas de IA con feedback visual inmediato en la interfaz.
- **Diseño (SolarOps Clean)**: Interfaz de chat moderna con burbujas de diálogo diferenciadas, scroll automático al último mensaje y soporte responsive (escritorio y móvil).
- **Idioma**: Respuestas y mensajes al usuario íntegramente en español con formato Markdown para legibilidad.

## Casos límite
- Consultas sobre parques inexistentes: El asistente informa que dicho parque no existe en el inventario de SolarUY y lista los parques registrados disponibles.
- Fallo transitorio en el servicio externo de IA: En caso de error de red con la API externa, el sistema recurre al asistente de dominio local informando la respuesta basada en datos directos.

## Fuera de alcance
- Ejecución de comandos mutantes o destructivos mediante lenguaje natural (el asistente es estrictamente de solo lectura / consulta; no puede dar de baja parques, crear reservas ni desactivar paneles).
- Fine-tuning o reentrenamiento de modelos masivos (se utiliza inferencia con Retrieval-Augmented Generation / tool calling en tiempo de ejecución).

## Criterios de finalización
- Tests unitarios en verde para las reglas de filtrado de dominio, detección de intenciones, denegación fuera de alcance y generación de respuestas con datos reales.
- Tests de arquitectura en verde para los endpoints de chat (`/api/chat/query`).
- Verificación en navegador de la pantalla `/asistente-ia` con preguntas sugeridas, respuesta contextual y limpieza de historial.
