# Tareas: Spec 022 — Modificar Reserva de Mantenimiento

- [ ] **T1. MaintenanceService.UpdateReservation con tests unitarios (tests primero).** RF-2 a RF-4
  - Hecho cuando: los tests de reprogramación de fecha, cambio de técnico y bloqueo de órdenes no pendientes pasan en verde.

- [ ] **T2. Endpoint PUT /api/maintenances/{id}.** RF-2 a RF-4
  - Hecho cuando: responde 204/400/404/409 según el caso.

- [ ] **T3. Vista Blazor /mantenimientos/{id}/editar.** RF-1 a RF-5
  - Hecho cuando: permite editar fecha y técnico en órdenes pendientes con confirmación de éxito.