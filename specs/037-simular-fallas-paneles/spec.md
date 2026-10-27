# Spec 037 — Simular Fallas de Paneles

Estado: borrador
Depende de: Spec 005 (Paneles), Spec 031 (Alertas), Spec 036 (Simulador)

## Contexto y objetivo
Ofrecer un mecanismo interactivo (panel de control para pruebas o endpoints dedicados de simulación) que permita inyectar anomalías y fallas intencionales en paneles o inversores seleccionados (como corte por fusible quemado, caída de rendimiento por suciedad severa o desconexión física), con el fin de disparar las alertas en tiempo real y probar el flujo de mantenimiento end-to-end.

## Usuarios / Actores
- **Administrador** y **Evaluador / Tester**: inyectan o limpian fallas simuladas.

## Historias de usuario
- **HU-1**: Como tester/administrador, quiero inducir fallas simuladas en paneles solares para verificar que el sistema dispare las alertas automáticas y permita agendar su mantenimiento correctivo.

## Tipos de fallas simulables

| Tipo de Falla | Comportamiento en Simulación | Nivel de Alerta |
| :--- | :--- | :--- |
| **Corte Total (Circuito abierto)** | Potencia cae a 0 W de forma instantánea en pleno día. | Crítico |
| **Degradación Severa / Suciedad** | Eficiencia cae al 30% del valor nominal. | Advertencia |
| **Sobretemperatura de Inversor** | Temperatura interna se eleva por encima de 85 °C. | Advertencia |
| **Falla de Aislamiento** | Inversor se apaga por protección de fuga a tierra. | Crítico |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario acceda al panel de simulación de fallas y seleccione un panel solar y un tipo de falla, EL SISTEMA aplicará la anomalía sobre el generador de telemetría y mostrará "Falla simulada inyectada con éxito en el panel \"<serie>\"."
- **RF-2**: CUANDO la falla sea inyectada, el servicio de simulación (spec 036) reflejará de inmediato los valores alterados en la siguiente lectura emitida por SignalR.
- **RF-3**: CUANDO la lectura anómala sea procesada, EL SISTEMA disparará automáticamente la alerta correspondiente (spec 031) y actualizará el color del panel a rojo o amarillo en el mapa de salud (spec 030).
- **RF-4**: EL SISTEMA proveerá un botón "Restablecer / Reparar Falla Simulada" para cada panel afectado, el cual devolverá los parámetros de generación a su curva solar normal.
- **RF-5**: EL SISTEMA proveerá un botón global "Restablecer todas las fallas" que devolverá el parque entero a operación normal 100% limpia.

## Requisitos no funcionales
- Interfaz clara y distinguible con etiqueta de advertencia "ENTORNO DE SIMULACIÓN Y PRUEBAS".
- Disponibilidad en entornos de desarrollo y staging sin impactar producción real.

## Casos límite
- Inyectar dos fallas distintas en el mismo panel prevalece la de mayor severidad (Crítico sobre Advertencia).

## Fuera de alcance
- Destrucción de datos reales o alteración permanente de la base de datos (las fallas son efímeras o controladas por un flag de simulación).

## Criterios de finalización
- Tests unitarios en verde para las reglas de inyección y remoción de fallas.
- Verificación del ciclo completo en navegador: Inyectar falla → Recibir toast de alerta → Ver mapa en rojo → Restablecer → Ver mapa en verde.

## Dudas abiertas
- Ninguna.
