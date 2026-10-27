# Tareas: Spec 021 — Reservar Fecha de Mantenimiento

- [ ] **T1. Entidad MaintenanceOrder + SolarUyDbContext + migración.** RF-1
  - Hecho cuando: la migración crea la tabla MaintenanceOrders con FK a paneles y técnicos, y compila.

- [ ] **T2. IMaintenanceRepository + MaintenanceService.CreateReservation con tests unitarios (tests primero).** RF-1 a RF-3
  - Hecho cuando: los tests de fecha futura obligatoria, panel activo, técnico activo y descripción pasan en verde.

- [ ] **T3. Endpoint POST /api/maintenances.** RF-1, RF-2
  - Hecho cuando: responde 201/400/404 y dotnet test sigue en verde.

- [ ] **T4. MaintenanceApiClient + formulario /mantenimientos/nuevo.** RF-1 a RF-4
  - Hecho cuando: en el navegador se agenda la orden con selectores de panel y técnico en hora de Uruguay.