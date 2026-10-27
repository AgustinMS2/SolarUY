# Tareas: Spec 026 — Confirmar Mantenimiento

- [ ] **T1. MaintenanceService.ConfirmMaintenance con tests unitarios (tests primero).** RF-1 a RF-4
  - Hecho cuando: los tests de observaciones obligatorias, cierre a Completado y resolución de alertas pasan en verde.

- [ ] **T2. Endpoint POST /api/maintenances/{id}/confirm.** RF-1 a RF-4
  - Hecho cuando: responde 200/400/404/409 y guarda notas y fecha de finalización.

- [ ] **T3. Formulario Blazor /mantenimientos/{id}/confirmar.** RF-1 a RF-5
  - Hecho cuando: el técnico registra observaciones, confirma y la orden queda cerrada volviendo al listado.