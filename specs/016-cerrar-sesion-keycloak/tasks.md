# Tareas: Spec 016 — Cerrar Sesión (Single Sign-Out)

- [x] **T1. Endpoint /account/logout en SolarUY.Web.** RF-2
  - Hecho cuando: ejecuta SignOutAsync de Cookie y OIDC redirigiendo al logout de Keycloak e invalidando tokens.

- [x] **T2. Modal de confirmación de logout en la barra de navegación.** RF-1, RF-3
  - Hecho cuando: al presionar "Cerrar sesión" pide confirmación y al confirmar finaliza la sesión redirigiendo al home.

- [x] **T3. Aviso "La sesión ha finalizado." al invalidarse la sesión fuera de esta pestaña.** RF-4
  - Hecho cuando: con la sesión invalidada en Keycloak, al navegar o al usar una pantalla ya abierta el sistema redirige a la home pública mostrando "La sesión ha finalizado.", y las rutas protegidas piden iniciar sesión de nuevo.
