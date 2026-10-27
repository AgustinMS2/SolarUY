# Spec 033 — Activar y Desactivar Inyección a la Red

Estado: borrador
Depende de: Spec 001 (Parques), Spec 013 (Inversores)

## Contexto y objetivo
Proporcionar un control maestro de desconexión o reconexión de la inyección eléctrica hacia la red de distribución para un parque solar completo o para un inversor individual, ante requerimientos de la distribuidora eléctrica (curtailment), sobretensiones en la red o emergencias del parque.

## Usuarios / Actores
- **Administrador**: ejecuta la orden de corte o apertura de interruptores de inyección.

## Historias de usuario
- **HU-1**: Como Administrador, quiero deshabilitar la inyección a la red de un parque o inversor para cumplir con solicitudes de la red eléctrica o prevenir daños ante sobretensión.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador presione "Corte de Inyección a Red" en el panel de control del parque, EL SISTEMA desplegará un modal con advertencia crítica: "¡Atención! Estás a punto de cortar la inyección a la red de todo el parque \"<nombre_parque>\". La potencia entregada pasará a 0 kW inmediatamente. Escribí 'CONFIRMAR' para proceder."
- **RF-2**: CUANDO el Administrador escriba 'CONFIRMAR' y acepte, EL SISTEMA enviará la orden de desacople a todos los inversores del parque, registrará el evento de seguridad en auditoría y mostrará "Inyección a la red desactivada. El parque no está entregando energía a la red."
- **RF-3**: SI el texto de confirmación no coincide exactamente con 'CONFIRMAR', ENTONCES EL SISTEMA mantendrá el botón deshabilitado y no enviará ninguna orden.
- **RF-4**: MIENTRAS la inyección esté desactivada, el panel de monitoreo mostrará un banner fijo rojo en la parte superior: "INYECCIÓN A LA RED DETENIDA ADMINISTRATIVAMENTE".
- **RF-5**: CUANDO el Administrador presione "Reanudar Inyección a Red" y confirme, EL SISTEMA ordenará la sincronización de los inversores con la red, levantará el banner y mostrará "Inyección a la red reanudada exitosamente."
- **RF-6**: SI un usuario con rol distinto de Administrador intenta ejecutar esta orden, EL SISTEMA responderá 403 Forbidden y registrará un intento de violación de seguridad.

## Requisitos no funcionales
- Tiempo de respuesta de la orden < 1 segundo hacia el simulador/inversor.
- Registro inmutable de auditoría con IP, usuario, timestamp y motivo del corte.

## Casos límite
- Desconexión por inversor individual sin cortar el parque entero: se admite el mismo flujo seleccionando el inversor específico.

## Fuera de alcance
- Apertura física de reconectadores de media tensión externos (se opera sobre el inversor).

## Criterios de finalización
- Tests unitarios en verde para validaciones de confirmación y autorización.
- Tests de arquitectura en verde.
- Verificación en navegador.

## Dudas abiertas
- Ninguna.
