# Tareas: Spec 002 — Listar Parques Solares

- [x] **T1. `GetAll` en repositorio y servicio + `SolarParkDto`, con test unitario del orden (test primero).** RF-1, RF-2, RF-3
  - Hecho cuando: el test de orden falló antes de implementar y ahora pasa en verde junto con los de arquitectura.

- [x] **T2. `GET /api/solar-parks` en el controlador.** RF-1
  - Hecho cuando: el endpoint devuelve 200 con la lista (y lista vacía si no hay parques) y `dotnet test` sigue en verde.

- [x] **T3. Página `/parques` en Blazor.** RF-1 a RF-6
  - Hecho cuando: en el navegador se ve la tabla ordenada con los badges de estado, el mensaje de lista vacía, el botón "Nuevo parque" funciona, el mensaje de error aparece con la API detenida y la pantalla se ve bien en celular.
