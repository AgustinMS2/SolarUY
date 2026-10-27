# Tareas: Spec 015 — Iniciar Sesión con Keycloak (OIDC)

- [ ] **T1. Orquestación de Keycloak en SolarUY.AppHost.** RF-1
  - Hecho cuando: Aspire levanta el contenedor de Keycloak con el realm solaruy-realm.json importado y accesible.

- [ ] **T2. Autenticación JWT Bearer en SolarUY.ApiService.** RF-5
  - Hecho cuando: la API valida tokens emitidos por Keycloak y rechaza llamadas anónimas en rutas protegidas con 401.

- [ ] **T3. OpenID Connect + Cookies en SolarUY.Web.** RF-1, RF-2, RF-4
  - Hecho cuando: rutas protegidas redirigen al login de Keycloak y retornan con la sesión autenticada.

- [ ] **T4. Barra superior con identidad del usuario conectado.** RF-2
  - Hecho cuando: se visualiza el nombre, rol y botón de logout en MainLayout.razor usando <AuthorizeView>.