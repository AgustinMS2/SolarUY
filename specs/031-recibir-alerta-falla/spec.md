# Spec 031 — Recibir Alerta de Falla

Estado: borrador
Depende de: Spec 027 (Monitoreo Parque), Spec 030 (Salud Paneles)

## Contexto y objetivo
Alertar de forma reactiva e inmediata a los operadores y técnicos conectados cuando un sensor, panel o inversor detecta una falla operativa (como desconexión, sobrecalentamiento o caída abrupta de generación), visualizándose como notificaciones toast y en una campana de alertas en la barra superior.

## Usuarios / Actores
- **Administrador** y **Técnico**: reciben avisos inmediatos para actuar.

## Historias de usuario
- **HU-1**: Como operador, quiero recibir notificaciones sonoras/visuales inmediatas cuando se produce una falla para coordinar la reparación con el equipo técnico sin demoras.

## Tipos y niveles de alerta

| Nivel | Severidad | Evento desencadenante |
| :--- | :--- | :--- |
| **Crítico** | Rojo | Cortocircuito, fallo de aislamiento, paro de emergencia del inversor, panel en cero en pleno sol. |
| **Advertencia** | Amarillo | Generación por debajo del umbral de alerta del parque, temperatura alta en inversor (> 75 °C). |
| **Informativo** | Azul | Desconexión programada, inicio/fin de inyección a red. |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el sistema de telemetría o el simulador detecte una condición de falla, EL SISTEMA creará un registro de alerta con: Parque, Equipo (nodo o panel), Severidad, Descripción del fallo y Fecha/hora UTC.
- **RF-2**: EL SISTEMA emitirá un evento SignalR a todos los clientes web conectados del parque, mostrando un aviso flotante (Toast) en pantalla con el texto exacto: "[<Severidad>] Falla detectada en <Equipo> (<Parque>): <Descripción>".
- **RF-3**: EL SISTEMA incrementará el contador de alertas no leídas en el icono de campana de la barra de navegación.
- **RF-4**: CUANDO el usuario haga clic en la notificación o en el centro de alertas, EL SISTEMA mostrará el detalle de la falla y un botón de acceso directo "Agendar Mantenimiento" (spec 021) con los datos del equipo pre-cargados.
- **RF-5**: EL SISTEMA permitirá al operador marcar la alerta como "Reconocida" o "Descartada", cambiando su estado en el historial.

## Requisitos no funcionales
- Emisión inmediata del evento SignalR tras la detección (< 300 ms).
- Notificación no invasiva: el toast desaparece tras 8 segundos o al cerrarlo, pero permanece en el centro de notificaciones.

## Casos límite
- Ráfaga masiva de fallas (ej. corte general de red): el sistema agrupa alertas similares para evitar saturación de toasts ("Se detectaron 15 fallas simultáneas en Parque X").

## Fuera de alcance
- Envío de SMS o emails automáticos (solo notificaciones in-app vía SignalR).

## Criterios de finalización
- Tests unitarios en verde para las reglas de generación de alertas del servicio.
- Verificación del toast y campana en navegador emitiendo alertas de prueba.

## Dudas abiertas
- Ninguna.
