# Tareas: Spec 014 — Asignar Panel a Nodo

- [x] **T1. Propiedad SolarNodeId en SolarPanel + migración EF Core.** RF-1
  - Hecho cuando: la migración agrega la columna nullable con su llave foránea e índice a SolarPanels y la solución compila.

- [x] **T2. SolarPanelService.AssignPanelsToNode con tests unitarios (tests primero).** RF-1 a RF-4, RF-6 a RF-8
  - Hecho cuando: los tests de asignación en lote, límite de 50, nodo y paneles inexistentes o inactivos, parque cruzado, reasignación con confirmación y advertencia de potencia fallaron antes y pasan en verde.

- [x] **T3. SolarPanelService.UnassignPanelsFromNode con tests unitarios (tests primero).** RF-5, RF-7, RF-8
  - Hecho cuando: los tests de desasociación (éxito, panel que no pertenece al nodo, lote vacío, nodo inactivo permitido, panel inactivo permitido) fallaron antes y pasan en verde.

- [x] **T4. Listados: cantidad de paneles en nodos y nodo de cada panel (tests primero).** RF-9
  - Hecho cuando: los tests de conteo de paneles por nodo y de nombre de nodo en el listado de paneles fallaron antes y pasan en verde.

- [x] **T5. Endpoints POST y DELETE /api/solar-nodes/{id}/panels.** RF-1 a RF-8
  - Hecho cuando: responden 200/400/404/409 (con y sin pedido de confirmación) según el caso, exigen rol Administrador y dotnet test sigue en verde.

- [x] **T6. Modal de paneles del nodo y columna "Nodo" en el listado de paneles.** RF-1, RF-3, RF-5, RF-6, RF-9
  - Hecho cuando: en el navegador el Administrador asocia, reasigna (con confirmación) y desasocia paneles desde la lista de nodos, el contador de paneles se actualiza, el listado de paneles muestra el nodo o "Sin asignar" y el Técnico no ve los botones; probado en escritorio y celular.
