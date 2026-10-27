# Tareas: Spec 012 — Baja de Nodo

- [ ] **T1. SolarNodeService.Deactivate con tests unitarios (tests primero).** RF-2, RF-5, RF-6
  - Hecho cuando: los 3 tests (éxito, ya inactivo, inexistente) fallaron antes de implementar y ahora pasan en verde.

- [ ] **T2. DELETE /api/solar-nodes/{id} en el controlador.** RF-2, RF-5, RF-6
  - Hecho cuando: responde 204/404/409 según el caso y dotnet test sigue en verde.

- [ ] **T3. Botón "Dar de baja" + modal de confirmación en /nodos.** RF-1 a RF-5
  - Hecho cuando: el botón solo aparece en nodos activos, el modal pide confirmación y tras aceptar deja el nodo Inactivo en el listado.