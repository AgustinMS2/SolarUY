# Tareas: Spec 016 — Cerrar Sesión (Single Sign-Out)

- [ ] **T1. Endpoint /account/logout en SolarUY.Web.** RF-2, RF-4
  - Hecho cuando: ejecuta SignOutAsync de Cookie y OIDC redirigiendo al logout de Keycloak e invalidando tokens.

- [ ] **T2. Modal de confirmación de logout en la barra de navegación.** RF-1, RF-3
  - Hecho cuando: al presionar "Cerrar sesión" pide confirmación y al confirmar finaliza la sesión redirigiendo al home.