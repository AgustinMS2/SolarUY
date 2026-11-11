# Tareas: Spec 026 — Confirmar Mantenimiento

- [x] **T1. Campo PanelOutcome en MaintenanceOrder + migración EF Core.** RF-1
  - Hecho cuando: la migración agrega la columna nullable a MaintenanceOrders y la solución compila.

- [x] **T2. MaintenanceService.ConfirmMaintenance con tests unitarios (tests primero).** RF-1, RF-2, RF-4, RF-6
  - Hecho cuando: los tests de observaciones (vacías, cortas, largas y límites), componentes, estado del panel, orden inexistente, orden ya cerrada y cierre a Completado con fecha UTC fallaron antes y pasan en verde.

- [x] **T3. Endpoint POST /api/maintenances/{id}/confirm.** RF-1, RF-2, RF-4, RF-6
  - Hecho cuando: responde 200/400/404/409 según el caso, lo pueden usar Técnico y Administrador, y dotnet test (incluidos los tests de arquitectura con la excepción explícita) sigue en verde.

- [x] **T4. Formulario Blazor /mantenimientos/{id}/confirmar y botones "Confirmar".** RF-1, RF-2, RF-4, RF-5
  - Hecho cuando: en el navegador el técnico (desde Mis mantenimientos) y el administrador (desde la lista) registran observaciones, confirman y la orden queda cerrada volviendo al listado; "Cancelar" no cambia nada; se ve bien en celular.

- [ ] **T5. Resolver las alertas activas del panel al completar la orden (BLOQUEADA por la spec 031).** RF-3
  - Hecho cuando: existe la entidad de alertas (spec 031) y la confirmación marca como "Atendidas" las alertas activas del panel, con tests unitarios en verde.
