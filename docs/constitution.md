# Constitución — SolarUY

Principios innegociables del proyecto. Toda especificación (`spec.md`), plan técnico (`plan.md`) y tarea (`tasks.md`) debe cumplirlos estrictamente.

1. **Arquitectura y simplicidad guiada por Aspire**: Solución .NET 9 orquestada con .NET Aspire, compuesta por Blazor Server (tiempo real mediante SignalR para monitoreo), API REST con controladores tradicionales (`[ApiController]` / `ControllerBase` en `SolarUY.ApiService/Controllers/`), PostgreSQL con Entity Framework Core estándar y Keycloak para autenticación OpenID Connect. Se evita Minimal APIs en `Program.cs` para mantener la API modular y organizada.
2. **Modelo de programación sincrónico y controlado**: Se emplean métodos normales sincrónicos (sin `Task`/`async` innecesario) en servicios y repositorios para mantener el control estricto del flujo de ejecución, simplicidad conceptual y facilidad de depuración y defensa académica.
3. **Lógica de negocio desacoplada y observable**: La lógica de negocio y cálculos residen en clases de servicio desacopladas de los controladores y de la interfaz Blazor. Los servicios pueden utilizar `ILogger<T>` para registrar eventos y errores de forma estándar.
4. **Estrategia de testing pragmática**: 
   - **Tests de arquitectura** como base global para asegurar el cumplimiento de capas y dependencias del sistema.
   - **Tests unitarios focalizados** para reglas de negocio y lógica crítica de casos de uso seleccionados.
   - **Validación UI**: La interfaz Blazor Server se valida de forma interactiva en navegador / DevTools (funcionalidad, respuesta y diseño responsive). Se prescinde de suites sobredimensionadas como `bUnit` o Testcontainers para cada caso de uso.
5. **Persistencia limpia y sin sobreingeniería**: EF Core sobre PostgreSQL con migraciones reproducibles. Las validaciones de unicidad y normalización se resuelven de forma estándar en C# y EF Core, sin dependencias de extensiones específicas del motor de BD (como `citext`). Fechas almacenadas en UTC y presentadas en hora local de Uruguay.
6. **La spec manda y alcance controlado**: Ninguna funcionalidad se implementa sin spec activa. **Una spec por requerimiento funcional** (ej. Alta de parque, Listar parques, Modificar parque y Baja de parque son cuatro specs distintas), con campos, reglas, mensajes y comportamiento de pantalla definidos con precisión. Primero las entidades simples, después las relaciones. Ante dudas o discrepancias, se consulta antes de codificar.
7. **Idioma y convenciones**: Código fuente en C# (clases, métodos, interfaces, entidades) en inglés idiomático; UI, mensajes al usuario y documentación funcional en español.
