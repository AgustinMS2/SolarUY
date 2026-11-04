# Tareas: Spec 013 — Asignar Inversor a Nodo

- [x] **T1. Entidad Inverter + SolarUyDbContext + migración.** RF-1
  - Hecho cuando: la migración crea la tabla Inverters con índices únicos en NormalizedSerieNumber y en SolarNodeId, y la solución compila.

- [x] **T2. IInverterRepository + SolarNodeService.AssignInverter con tests unitarios (tests primero).** RF-1 a RF-6, RF-8, RF-9, RF-10
  - Hecho cuando: los tests de nodo inexistente e inactivo, datos de texto, capacidad, límite contra el parque, serie duplicada, confirmación de reemplazo y actualización de la misma serie fallaron antes y pasan en verde.

- [x] **T3. SolarNodeService.UnassignInverter con tests unitarios (tests primero).** RF-7, RF-9
  - Hecho cuando: los tests de desvinculación (éxito, nodo sin inversor, nodo inexistente, nodo inactivo permitido) fallaron antes y pasan en verde.

- [x] **T4. Endpoints POST y DELETE /api/solar-nodes/{id}/inverter.** RF-1 a RF-10
  - Hecho cuando: responden 200/204/400/404/409 (con y sin pedido de confirmación) según el caso, exigen rol Administrador y dotnet test sigue en verde.

- [ ] **T5. Modal de asignación, reemplazo y desvinculación en la UI.** RF-1 a RF-3, RF-7
  - Hecho cuando: desde la lista de nodos el Administrador asigna, reemplaza (con confirmación) y desvincula un inversor, la tabla muestra la serie asignada y el Técnico no ve los botones; probado en escritorio y celular.
