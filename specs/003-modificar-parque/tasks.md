# Tareas: Spec 003 — Modificar Parque Solar

- [x] **T1. Repositorio: `GetById`, `ExistsByNormalizedName(…, excludeId)`, `Update`.** RF-2, RF-4, RF-5
  - Hecho cuando: compila y `dotnet test` sigue en verde.

- [x] **T2. Servicio: extraer `Validate`, agregar `GetById` y `Update`, con tests unitarios (tests primero).** RF-2 a RF-5, RF-7
  - Hecho cuando: los tests del plan fallaron antes de implementar y ahora pasan en verde; los tests de alta (spec 001) siguen en verde.

- [x] **T3. Controlador: `GET /{id}` y `PUT /{id}`.** RF-1 a RF-5
  - Hecho cuando: responden 200/404 y 204/400/404/409 según el caso y `dotnet test` sigue en verde.

- [x] **T4. Página `/parques/{id}/editar` + botón "Editar" en el listado.** RF-1 a RF-6
  - Hecho cuando: en el navegador se edita un parque, se ven los errores, "Cancelar" vuelve sin guardar, un Id inexistente muestra el mensaje de RF-5 y la pantalla se ve bien en celular.
