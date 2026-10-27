# Tareas: Spec 010 — Listar Nodos

- [ ] **T1. Repositorio GetAll + servicio SolarNodeService.GetAll con tests unitarios (tests primero).** RF-1, RF-2, RF-3
  - Hecho cuando: los tests de ordenamiento por parque y nodo y filtrado por parkId fallaron antes y ahora pasan en verde.

- [ ] **T2. SolarNodesController con GET /api/solar-nodes.** RF-1, RF-3
  - Hecho cuando: responde 200 con la lista de nodos DTO y dotnet test sigue en verde.

- [ ] **T3. SolarNodeApiClient + página /nodos (SolarNodeList.razor).** RF-1 a RF-6
  - Hecho cuando: muestra la grilla con selector de parque, botón "Nuevo nodo", mensaje de lista vacía y estados en color, probado en escritorio y celular.