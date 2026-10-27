# Tareas: Spec 011 — Modificar Nodo

- [ ] **T1. Repositorio: GetById, ExistsByName(..., excludeId), Update.** RF-2, RF-4
  - Hecho cuando: compila y dotnet test sigue en verde.

- [ ] **T2. Servicio: SolarNodeService.Update con tests unitarios (tests primero).** RF-2 a RF-5
  - Hecho cuando: los tests de nombre duplicado en el mismo parque (excluyendo propio Id), nodo inexistente y actualización válida pasan en verde.

- [ ] **T3. Controlador: GET /api/solar-nodes/{id} y PUT /api/solar-nodes/{id}.** RF-1 a RF-5
  - Hecho cuando: responden 200/404 y 204/400/404/409 según el caso y dotnet test sigue en verde.

- [ ] **T4. Página /nodos/{id}/editar + botón "Editar" en el listado.** RF-1 a RF-6
  - Hecho cuando: en el navegador se edita el nodo, el parque queda bloqueado, "Cancelar" vuelve sin guardar y se actualiza en el listado.