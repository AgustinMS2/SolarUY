# Tareas: Spec 004 — Baja de Parque Solar

- [x] **T1. `SolarParkService.Deactivate` con tests unitarios (tests primero).** RF-2, RF-5, RF-6
  - Hecho cuando: los 3 tests del plan fallaron antes de implementar y ahora pasan en verde junto con el resto.

- [ ] **T2. `DELETE /api/solar-parks/{id}` en el controlador.** RF-2, RF-5, RF-6
  - Hecho cuando: responde 204/404/409 según el caso y `dotnet test` sigue en verde.

- [ ] **T3. Botón "Dar de baja" + confirmación en el listado.** RF-1 a RF-5
  - Hecho cuando: en el navegador el botón solo aparece en parques activos, "Cancelar" no cambia nada, "Confirmar" deja el parque Inactivo y el listado se actualiza; se ve bien en celular.
