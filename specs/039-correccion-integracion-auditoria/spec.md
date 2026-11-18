# Spec 039 — Corrección de Integración Transversal y Hallazgos de Auditoría

Estado: implementada
Depende de: Specs 001 a 038

## Contexto y objetivo
Resolver de forma integral todas las discrepancias entre especificación e implementación y los huecos de diseño entre módulos detectados en la auditoría general del sistema SolarUY (ciclo de fallas simuladas y confirmación de mantenimiento, reserva sobre paneles desactivados, sincronización entre gestión de roles y técnicos, baja de paneles en UI, conteo de mantenimientos pendientes, coherencia de telemetría ante cortes de inyección y fallas de aislamiento, y precisión del asistente de IA).

## Usuarios / Actores
- **Administrador**: gestiona parques, nodos, paneles, técnicos, roles, inyección a red, simulación de fallas y mantenimientos.
- **Personal Especializado (Técnico)**: consulta el directorio de técnicos, visualiza sus mantenimientos asignados y confirma intervenciones.
- **Sistema (Telemetría / Simulador / Asistente IA)**: calcula lecturas en vivo, evalúa alertas y responde consultas en lenguaje natural.

## Historias de usuario
- **HU-1**: Como Técnico o Administrador, quiero que al confirmar un mantenimiento dejando el panel "Operativo", se restablezca cualquier falla simulada activa sobre ese panel para que el mapa de salud y las alertas reflejen la reparación.
- **HU-2**: Como Administrador, quiero poder agendar un mantenimiento sobre un panel aunque haya sido desactivado eléctricamente por seguridad, y dar de baja paneles desde el listado general.
- **HU-3**: Como Administrador, quiero que los cambios de rol en la gestión de usuarios, el conteo de mantenimientos pendientes y el listado de técnicos con sus tareas en curso se mantengan siempre consistentes.
- **HU-4**: Como operador, quiero que la telemetría del parque y del nodo refleje con exactitud los cortes individuales de inyección, el voltaje DC residual en fallas de aislamiento y el estado de paneles desactivados manualmente.

## Datos

| Campo / Elemento | Obligatorio | Regla (tipo, rango, largo, unicidad) |
| :--- | :--- | :--- |
| Mantenimientos asignados en curso (`TechnicianDto`) | Sí (solo lectura) | Entero $\ge 0$. Cantidad total de órdenes en estado `Pendiente` del técnico (sin importar si la hora programada es pasada o futura). En la tabla se muestra como `"<N> asignados"`. |
| Documento de identidad (`Technician`) | Sí | `DocumentId` conserva el formato ingresado con trim (1 a 20 caracteres); `NormalizedDocumentId` almacena solo caracteres alfanuméricos en minúsculas (al menos 1 carácter útil tras quitar puntos y guiones). |

## Requisitos funcionales (Criterios de aceptación en EARS)

### A. Ciclo de Mantenimiento, Fallas Simuladas y Paneles Desactivados (`Specs 021, 022, 026, 031, 032, 037`)
- **RF-1**: CUANDO un usuario confirme una orden de mantenimiento indicando que el estado del panel tras la tarea es `"Operational"` (Operativo), EL SISTEMA finalizará automáticamente cualquier falla simulada activa sobre ese panel, además de marcar sus alertas activas como atendidas.
- **RF-2**: SI un usuario con rol Técnico intenta confirmar una orden de mantenimiento que está asignada a otro técnico distinto al de su sesión activa, ENTONCES EL SISTEMA rechazará la operación y mostrará `"No tenés permiso para confirmar un mantenimiento asignado a otro técnico."` (el rol Administrador mantiene la capacidad de confirmar cualquier orden por contingencia).
- **RF-3**: CUANDO el Administrador cree o modifique una reserva de mantenimiento, EL SISTEMA permitirá seleccionar paneles en estado `"Activo"` o `"Desactivado"` (desactivados remotamente por control de inversor), siempre que pertenezcan a un parque solar en estado `"Activo"`.
- **RF-4**: SI el Administrador intenta crear o modificar una reserva de mantenimiento para un panel cuyo parque solar está `"Inactivo"`, o si al reprogramar la fecha el panel ya posee otra orden pendiente en ese mismo día calendario sin confirmación, ENTONCES EL SISTEMA aplicará las mismas validaciones de bloqueo o confirmación que en el alta de la reserva.
- **RF-5**: CUANDO el Administrador presione `"Agendar Mantenimiento"` desde una alerta de falla en la campana de notificaciones (incluso si ya se encuentra en la pantalla `/mantenimientos/nuevo`), EL SISTEMA actualizará el formulario preseleccionando el panel afectado, el tipo de intervención `"Correctivo"` y la descripción de la falla.

### B. Paneles, Nodos y Parques (`Specs 006, 007, 008, 009, 010, 014`)
- **RF-6**: MIENTRAS un panel en el listado `/paneles` no se encuentre en estado `"Inactivo"`, EL SISTEMA mostrará al Administrador el botón `"Dar de baja"`, el cual navegará a `/paneles/baja/{id}`, presentará los mensajes de error o confirmación correspondientes y, tras confirmar la baja, desasignará el panel de su nodo, cerrará sus fallas simuladas/alertas activas y volverá a `/paneles` mostrando `"Panel dado de baja correctamente."`
- **RF-7**: SI el Administrador modifica el parque solar de un panel que se encontraba asignado a un nodo del parque anterior, ENTONCES EL SISTEMA desvinculará automáticamente al panel de dicho nodo (`SolarNodeId = null`).
- **RF-8**: CUANDO el Administrador cree un nodo en `/nodos/nuevo`, EL SISTEMA ofrecerá en el selector únicamente parques en estado `"Activo"`, limpiará el formulario sin recargar la página completa y mantendrá visible el mensaje `"Nodo creado correctamente."`
- **RF-9**: SI falla la carga del listado en `/nodos`, ENTONCES EL SISTEMA mostrará `"No se pudo cargar el listado de nodos. Intentá nuevamente."`; y en `/paneles` los mensajes usarán el término en español `"paneles"` (`"No hay paneles registrados."` y `"No se pudo cargar el listado de paneles. Intentá nuevamente."`).

### C. Técnicos y Gestión de Roles (`Specs 017, 018, 019, 020, 025, 038`)
- **RF-10**: CUANDO se consulte el listado de técnicos en `/tecnicos`, EL SISTEMA mostrará la columna `"Mantenimientos asignados en curso"` con el texto `"<cantidad> asignados"` para cada técnico, y permitirá el acceso desde el menú lateral tanto a Administradores como a Técnicos.
- **RF-11**: CUANDO se verifique si un técnico posee mantenimientos pendientes (tanto para el conteo en `/tecnicos` como para impedir su baja en la Spec 020), EL SISTEMA contabilizará todas las órdenes en estado `"Pendiente"` asignadas al técnico, independientemente de si su fecha/hora programada ya transcurrió o es futura.
- **RF-12**: CUANDO el Administrador dé de alta a un técnico, EL SISTEMA conservará el formato legible del documento en `DocumentId` y validará que, tras quitar puntos y guiones, el documento contenga al menos un carácter alfanumérico; si no lo tiene, mostrará `"El documento de identidad es obligatorio."`
- **RF-13**: CUANDO el Administrador cambie el rol de un usuario en `/usuarios`:
  - SI pasa de `"Técnico"` a `"Administrador"` y posee mantenimientos pendientes, ENTONCES EL SISTEMA rechazará el cambio mostrando `"No se puede cambiar el rol del técnico porque tiene <cantidad> mantenimiento(s) pendiente(s) de resolución."`; si no tiene pendientes, cambiará su rol y pasará su ficha de técnico a `"Inactivo"`.
  - SI pasa de `"Administrador"` a `"Técnico"`, ENTONCES EL SISTEMA reactivará su ficha de técnico si existía o creará una ficha básica activa asociada a su correo electrónico para que pueda acceder a `"Mis Mantenimientos"`.
- **RF-14**: CUANDO un usuario con rol Administrador ingrese a `/mis-mantenimientos` sin tener ficha propia de técnico, EL SISTEMA seleccionará automáticamente el primer técnico disponible (o mostrará la lista vacía si no hay técnicos) sin desplegar un mensaje de error 404.

### D. Telemetría, Control de Inversor, Simulador y Asistente IA (`Specs 027, 028, 030, 033, 035, 036, 037`)
- **RF-15**: MIENTRAS un inversor tenga la inyección a la red detenida administrativamente (`GridInjectionEnabled == false`), EL SISTEMA no sumará su potencia ni su energía a la telemetría en tiempo real del parque solar al que pertenece, ni emitirá alertas de falla sobre ese inversor.
- **RF-16**: SI se intenta cortar o reanudar la inyección a la red de un inversor cuyo nodo o parque solar se encuentra en estado `"Inactivo"`, ENTONCES EL SISTEMA rechazará la orden con conflicto indicando `"No se puede operar sobre un nodo o parque inactivo."`
- **RF-17**: MIENTRAS un inversor tenga activa una falla simulada de tipo `"InsulationFault"` (Falla de aislamiento) y su inyección no esté detenida administrativamente, EL SISTEMA conservará en la telemetría del nodo la tensión DC residual y la temperatura ambiente calculadas por el motor de simulación.
- **RF-18**: MIENTRAS un panel se encuentre en estado `"Desactivado"` (desactivado remotamente), EL SISTEMA mostrará en el Mapa de Salud (`/parques/{id}/salud`) el motivo `"Panel desactivado manualmente"`, descontará su producción de la potencia del inversor/nodo y permitirá restablecer sus fallas simuladas desde `/simulacion-fallas`.
- **RF-19**: MIENTRAS un parque solar en `/parques/{id}/monitoreo` esté inactivo o no tenga inversores activos asociados, EL SISTEMA mostrará `"El parque se encuentra inactivo o sin equipos asociados."`; y cuando la generación diurna caiga bajo el umbral, mostrará `"Generación por debajo del umbral de alerta (<umbral> kW)."`
- **RF-20**: CUANDO un usuario realice una consulta en `/asistente-ia`, EL SISTEMA enviará al modelo los 5 mantenimientos pendientes más próximos en el tiempo (orden ascendente), evitará duplicar el último mensaje del usuario en el historial enviado al proveedor LLM y, si el usuario consulta en el motor local por un parque cuyo nombre no existe en el inventario, responderá indicando que ese parque no existe y listará los parques registrados.

## Requisitos no funcionales
- Cumplimiento de todos los tests de arquitectura y unitarios existentes más los nuevos tests de regresión.
- Uso consistente de zona horaria de Uruguay (`UTC-3`) en las vistas de monitoreo y chat.

## Casos límite
- Un técnico con órdenes `Pendientes` programadas para horas anteriores del mismo día sigue figurando con esas órdenes en curso y no puede ser dado de baja hasta que se completen, reasignen o cancelen.
- Si en `/tecnicos/nuevo` o `/mantenimientos/nuevo` el usuario presiona `"Cancelar"`, el sistema navega de regreso al listado correspondiente (`/tecnicos` o `/mantenimientos`).

## Fuera de alcance
- Creación de nuevas entidades de base de datos o cambios de esquema (todas las correcciones operan sobre el esquema actual).

## Criterios de finalización
- Tests unitarios en verde para las reglas de negocio corregidas en servicios.
- Tests de arquitectura en verde.
- Verificación de compilación limpia de toda la solución y actualización documental de estados en `specs/` y `MEMORY.md`.

## Dudas abiertas
- Ninguna.
