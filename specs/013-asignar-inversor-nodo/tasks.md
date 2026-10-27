# Tareas: Spec 013 — Asignar Inversor a Nodo

- [ ] **T1. Entidad Inverter + SolarUyDbContext + migración.** RF-1
  - Hecho cuando: la migración crea la tabla Inverters con índice único en NormalizedSerieNumber y compila.

- [ ] **T2. IInverterRepository + SolarNodeService.AssignInverter con tests unitarios (tests primero).** RF-1 a RF-6
  - Hecho cuando: los tests de capacidad nominal > 0, límite contra capacidad del parque, serie duplicada y nodo inactivo pasan en verde.

- [ ] **T3. Endpoint POST /api/solar-nodes/{id}/inverter.** RF-1 a RF-6
  - Hecho cuando: responde 200/400/404/409 según el caso y dotnet test sigue en verde.

- [ ] **T4. Modal de asignación de inversor en la UI.** RF-1 a RF-3
  - Hecho cuando: desde la vista de nodo se puede cargar el inversor y la tabla muestra la serie asignada.