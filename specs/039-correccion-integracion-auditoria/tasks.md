# Tareas de Implementación: Spec 039 — Corrección de Integración Transversal y Hallazgos de Auditoría

- [x] **T1. Reglas de negocio (TDD) en `MaintenanceService` y `SolarPanelService` (cierre de falla simulada al confirmar, validación de técnico asignado, paneles desactivados/parques activos y limpieza al cambiar de parque o dar de baja).** RF-1, RF-2, RF-3, RF-4, RF-6, RF-7
  - Hecho cuando: los nuevos tests unitarios en `MaintenanceServiceConfirmTests`, `MaintenanceServiceReservationTests`, `MaintenanceServiceUpdateReservationTests`, `PanelServiceUpdateTests` y `PanelServiceDeleteTests` se escriben primero en rojo y pasan en verde con `dotnet test`.

- [x] **T2. Actualizar `MaintenanceRepository.CountPendingByTechnician`, `MaintenancesController` y `PanelsController`.** RF-2, RF-6, RF-11
  - Hecho cuando: `CountPendingByTechnician` contabiliza todas las órdenes `Pending` del técnico; `MaintenancesController.Confirm` pasa el email y rol del usuario autenticado; la solución compila y `dotnet test` pasa en verde.

- [x] **T3. Actualizar vistas Blazor de Paneles, Nodos y Reserva de Mantenimiento (`PanelList.razor`, `PanelDelete.razor`, `SolarNodeCreate.razor`, `SolarNodeList.razor`, `MaintenanceCreate.razor`).** RF-3, RF-5, RF-6, RF-8, RF-9
  - Hecho cuando: `/paneles` muestra el botón `"Dar de baja"` y `PanelDelete.razor` muestra mensajes y redirige con confirmación; `/nodos/nuevo` mantiene visible el mensaje de éxito y filtra parques activos; `/mantenimientos/nuevo` actualiza desde alertas en `OnParametersSet` con tipo `"Correctivo"` e incluye paneles `"Desactivado"`; la solución compila sin errores.

- [x] **T4. Reglas de negocio (TDD) en `TechnicianService` (`DocumentId` visual, documento normalizado no vacío, `PendingMaintenancesCount`) y `UserRoleService` (sincronización con `Technicians`).** RF-10, RF-12, RF-13
  - Hecho cuando: los tests unitarios en `TechnicianServiceCreateTests`, `TechnicianServiceGetAllTests` y `UserRoleServiceTests` fallan primero en rojo y luego pasan todos en verde con `dotnet test`.

- [x] **T5. Actualizar vistas Blazor de Técnicos y Navegación (`TechnicianList.razor`, `TechnicianCreate.razor`, `NavMenu.razor`).** RF-10
  - Hecho cuando: `/tecnicos` muestra la columna `"Mantenimientos asignados en curso"` (`"<N> asignados"`), permite buscar documentos con o sin puntos/guiones, `/tecnicos/nuevo` navega a `/tecnicos` al cancelar, y `NavMenu.razor` habilita `/tecnicos` para el rol `Técnico` y protege las secciones operativas con `<AuthorizeView>`.

- [x] **T6. Actualizar `MyMaintenances.razor` para evitar error 404 inicial cuando ingresa el Administrador.** RF-14
  - Hecho cuando: si el usuario autenticado es Administrador y no posee ficha propia de técnico, la vista selecciona automáticamente el primer técnico disponible (o muestra lista vacía) sin mostrar alerta de error 404.

- [x] **T7. Reglas de negocio (TDD) en `GridInjectionService`, `NodeTelemetryService`, `AlertDetectionRules`, `PanelHealthService`, `SolarUyDomainContextRetriever` y `SolarUyKnowledgeAssistant`.** RF-15, RF-16, RF-17, RF-18, RF-20
  - Hecho cuando: los nuevos tests unitarios para validación de nodo/parque inactivo en corte de inyección, preservación de tensión DC residual en `InsulationFault`, supresión de alertas con inyección cortada, motivo `"Panel desactivado manualmente"`, orden ascendente de próximos mantenimientos y detección de parques inexistentes pasan en verde con `dotnet test`.

- [x] **T8. Actualizar `ParkTelemetryBackgroundService`, `SimulationFaultService`, `GeminiLlmService`, `ChatAssistant.razor` y `ParkMonitoring.razor`.** RF-15, RF-18, RF-19, RF-20
  - Hecho cuando: el monitoreo del parque excluye inversores con inyección cortada, interpola `(<umbral> kW)` y avisa cuando no tiene inversores; `/simulacion-fallas` incluye paneles `Deactivated`; el chat no duplica el mensaje del usuario hacia Gemini; `dotnet test` pasa en verde.

- [x] **T9. Normalizar estado documental en `specs/001-*` a `specs/037-*` (`Estado: implementada` y checkboxes completados) y actualizar `MEMORY.md`.**
  - Hecho cuando: todos los archivos `spec.md`, `tasks.md` y `MEMORY.md` están sincronizados y `dotnet test` ejecuta toda la suite en verde.
