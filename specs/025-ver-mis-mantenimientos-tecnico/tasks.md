# Tareas: Spec 025 — Ver Mis Mantenimientos (Técnico)

- [ ] **T1. MaintenanceService.GetMyMaintenances con tests unitarios (tests primero).** RF-1, RF-2
  - Hecho cuando: el test de filtrado por email del técnico y priorización de tareas pendientes pasa en verde.

- [ ] **T2. Endpoint GET /api/maintenances/mine con [Authorize(Roles = "Técnico")].** RF-1
  - Hecho cuando: responde 200 con las tareas asignadas al usuario logueado.

- [ ] **T3. Vista móvil Blazor /mis-mantenimientos.** RF-1 a RF-5
  - Hecho cuando: muestra las tarjetas táctiles con botón directo "Confirmar Mantenimiento" probado en celular.