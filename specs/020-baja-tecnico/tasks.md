# Tareas: Spec 020 — Baja de Técnico

- [ ] **T1. TechnicianService.Deactivate con tests unitarios (tests primero).** RF-2, RF-3, RF-5
  - Hecho cuando: los tests de bloqueo por tareas pendientes y baja lógica válida pasan en verde.

- [ ] **T2. Endpoint DELETE /api/technicians/{id}.** RF-2, RF-3
  - Hecho cuando: responde 204/404/409 según el caso y dotnet test sigue en verde.

- [ ] **T3. Botón "Dar de baja" + confirmación en /tecnicos.** RF-1 a RF-5
  - Hecho cuando: solo aparece en técnicos activos y al confirmar actualiza el estado a Inactivo.