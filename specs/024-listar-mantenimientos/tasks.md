# Tareas: Spec 024 — Listar Mantenimientos

- [x] **T1. MaintenanceService.GetAll con tests unitarios (tests primero).** RF-1 a RF-3
  - Hecho cuando: los tests de orden cronológico descendente y filtros combinados pasan en verde.

- [x] **T2. Endpoint GET /api/maintenances.** RF-1, RF-3
  - Hecho cuando: responde 200 con la lista filtrada de órdenes.

- [x] **T3. Vista Blazor /mantenimientos (MaintenanceList.razor).** RF-1 a RF-5
  - Hecho cuando: muestra la grilla con barra de filtros por estado, parque, técnico y fechas, con badges en color.

- [x] **T4. Botón "Nuevo mantenimiento" siempre visible para el Administrador.** RF-6
  - Hecho cuando: en el navegador, con la lista vacía y con resultados, el Administrador ve el botón en el encabezado y este abre `/mantenimientos/nuevo`; el Técnico no lo ve.
