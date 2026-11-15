# Tareas: Spec 030 — Ver Estado de Salud de Paneles

- [x] **T1. DTOs y `PanelHealthService` con tests unitarios (tests primero).** RF-2, RF-3, RF-4
  - Hecho cuando: los tests de clasificación (Normal ≥ 80 % inclusive, Degradado entre 10 % inclusive y 80 %, Falla < 10 %, Sin sol de noche, Inactivo por baja, sin nodo y nodo inactivo), rendimiento bajo (< 40 %), contadores y último mantenimiento (la Completada más reciente) fallaron antes y pasan en verde con `dotnet test`.

- [x] **T2. `ParkHealthSnapshotBuilder` y endpoint `GET /api/solar-parks/{id}/panel-health`.** RF-1, RF-3
  - Hecho cuando: el endpoint responde 200 con los paneles agrupados por nodo, su último mantenimiento y una primera lectura, 404 con "El parque solicitado no existe.", exige estar autenticado (Administrador o Técnico) y `dotnet test` (incluidos los tests de arquitectura) sigue en verde.

- [x] **T3. Hub `ParkHealthHub` y `ParkHealthBackgroundService`.** RF-5, RF-7
  - Hecho cuando: el hub está mapeado en `/hubs/park-health` con `[Authorize(Roles = Roles.Any)]`, y con la aplicación corriendo el servicio emite cada 3 s un evento por parque activo al grupo `park-health-{id}`; `dotnet test` sigue en verde.

- [x] **T4. Pantalla Blazor `/parques/{id:guid}/salud` (`ParkHealth.razor`).** RF-1 a RF-7
  - Hecho cuando: en el navegador la página muestra la cuadrícula agrupada por nodo, la leyenda fija y los seis contadores; la tarjeta aparece al hacer clic o pasar el cursor con su botón "Ver detalle"; los colores y contadores cambian en vivo; se ven los mensajes de parque sin paneles, inactivo e inexistente y "Reconectando con el parque..."; se desuscribe al salir y se ve bien en celular.

- [x] **T5. Botón "Salud" en el listado de parques (`SolarParkList.razor`).** RF-1
  - Hecho cuando: en `/parques` cada parque activo tiene el botón "Salud" que abre `/parques/{id}/salud`, verificado en el navegador.
