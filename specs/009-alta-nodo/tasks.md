# Tareas: Spec 009 — Alta de Nodo

- [ ] **T1. Entidad SolarNode + SolarUyDbContext + migración.** RF-1
  - Hecho cuando: la migración crea la tabla SolarNodes con índice único en (SolarParkId, NormalizedName) y la solución compila.

- [ ] **T2. Repositorio ISolarNodeRepository (ExistsByName, Add).** RF-1, RF-2
  - Hecho cuando: compila, está registrado en DI y los tests existentes siguen en verde.

- [ ] **T3. SolarNodeService.Create con tests unitarios (tests primero).** RF-1, RF-2, RF-4
  - Hecho cuando: los tests de nombre vacío, longitud > 100, parque inexistente y nombre duplicado en el mismo parque fallaron antes de implementar y ahora pasan en verde.

- [ ] **T4. SolarNodesController con POST /api/solar-nodes.** RF-1, RF-2
  - Hecho cuando: responde 201/400/404/409 según el caso y dotnet test sigue en verde.

- [ ] **T5. SolarNodeApiClient + página /nodos/nuevo.** RF-1, RF-2, RF-3
  - Hecho cuando: en el navegador se puede registrar un nodo con parque desplegable, se ven los errores inline, el formulario se limpia al guardar y la vista es responsive.