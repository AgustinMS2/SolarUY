# Tareas: Spec 014 — Asignar Panel a Nodo

- [ ] **T1. Propiedad SolarNodeId en SolarPanel + migración EF Core.** RF-1
  - Hecho cuando: la migración agrega la columna nullable a SolarPanels y la solución compila.

- [ ] **T2. SolarPanelService.AssignPanelsToNode con tests unitarios (tests primero).** RF-1 a RF-4
  - Hecho cuando: los tests de asignación en lote, rechazo de paneles de parque cruzado y paneles inactivos pasan en verde.

- [ ] **T3. Endpoint POST /api/solar-nodes/{id}/panels.** RF-1, RF-2, RF-4
  - Hecho cuando: responde 200/400/404 y dotnet test sigue en verde.

- [ ] **T4. Modal con selector múltiple de paneles disponibles.** RF-1 a RF-6
  - Hecho cuando: en la UI se seleccionan paneles del parque y se vinculan al nodo actualizándose el contador de paneles.