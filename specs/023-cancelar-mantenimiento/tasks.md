# Tareas: Spec 023 — Cancelar Mantenimiento

- [ ] **T1. MaintenanceService.CancelReservation con tests unitarios (tests primero).** RF-2 a RF-4
  - Hecho cuando: los tests de motivo obligatorio >= 10 caracteres y bloqueo de órdenes completadas pasan en verde.

- [ ] **T2. Endpoint POST /api/maintenances/{id}/cancel.** RF-2 a RF-4
  - Hecho cuando: responde 200/400/404/409 según el caso y persiste el motivo en BD.

- [ ] **T3. Modal de cancelación con TextArea en /mantenimientos.** RF-1 a RF-5
  - Hecho cuando: pide motivo obligatorio y al confirmar deja la orden en estado Cancelado.