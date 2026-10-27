# Spec 025 — Ver Mis Mantenimientos (Técnico)

Estado: aprobada
Depende de: Spec 015 (Login), Spec 021 (Reservar Mantenimiento)

## Contexto y objetivo
Ofrecer una vista personalizada y simplificada para los usuarios con rol **Técnico**, donde visualicen exclusivamente las órdenes de trabajo asignadas a su persona, optimizada para su uso en teléfonos móviles durante las inspecciones de campo.

## Usuarios / Actores
- **Técnico**: consulta sus tareas pendientes y agenda diaria.

## Historias de usuario
- **HU-1**: Como Técnico, quiero acceder a una lista exclusiva con mis tareas asignadas para saber a qué parque y panel debo dirigirme hoy y qué trabajo debo realizar.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO un usuario con rol Técnico acceda al menú "Mis Mantenimientos", EL SISTEMA filtrará automáticamente las órdenes asignadas a su identificador de técnico asociado a su sesión activa.
- **RF-2**: EL SISTEMA organizará la vista en tarjetas o lista táctil priorizando las órdenes "Pendientes" del día actual y fechas futuras más próximas.
- **RF-3**: Cada tarjeta de mantenimiento mostrará: Parque solar, Ubicación / Nodo, Número de serie del panel, Tipo de intervención, Hora programada y Motivo del trabajo.
- **RF-4**: En cada orden en estado "Pendiente", EL SISTEMA mostrará un botón destacado "Comenzar / Confirmar Mantenimiento" que llevará al formulario de confirmación (spec 026).
- **RF-5**: SI el técnico no tiene órdenes pendientes asignadas, ENTONCES EL SISTEMA mostrará "No tenés mantenimientos pendientes asignados para hoy. ¡Todo al día!"

## Requisitos no funcionales
- Diseño mobile-first: botones grandes, tipografía legible bajo luz solar y carga ligera.
- Opciones de filtrado rápido: "Hoy", "Próximos", "Completados".

## Casos límite
- Usuario logueado con rol Administrador que accede a esta vista verá un selector para elegir qué técnico simular o ver sus propias asignaciones.

## Fuera de alcance
- Geolocalización GPS del técnico.
- Modo offline (requiere conexión activa al servidor Blazor).

## Criterios de finalización
- Tests unitarios en verde para el filtrado por técnico autenticado en el servicio.
- Tests de arquitectura en verde.
- Verificación en navegador en emulador móvil.

## Dudas abiertas
- Ninguna.
