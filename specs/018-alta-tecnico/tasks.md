# Tareas: Spec 018 — Alta de Técnico

- [ ] **T1. Entidad Technician + SolarUyDbContext + migración.** RF-1
  - Hecho cuando: la migración crea la tabla Technicians con índices únicos en documento y email, y compila.

- [ ] **T2. Repositorio ITechnicianRepository (ExistsByDocument, ExistsByEmail, Add).** RF-1, RF-2
  - Hecho cuando: compila, está registrado en DI y los tests siguen en verde.

- [ ] **T3. TechnicianService.Create con tests unitarios (tests primero).** RF-1, RF-2
  - Hecho cuando: los tests de documento duplicado, email duplicado y formato inválido pasan en verde.

- [ ] **T4. TechniciansController con POST /api/technicians.** RF-1, RF-2
  - Hecho cuando: responde 201/400/409 según el caso y dotnet test sigue en verde.

- [ ] **T5. TechnicianApiClient + vista /tecnicos/nuevo.** RF-1, RF-2, RF-3
  - Hecho cuando: en el navegador se registra un técnico con feedback de validación y limpieza de formulario.