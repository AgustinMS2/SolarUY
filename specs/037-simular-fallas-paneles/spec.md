# Spec 037 — Simular Fallas de Paneles

Estado: implementada (2026-11-15)
Depende de: Spec 005 (Paneles), Spec 013 (Inversor a Nodo), Spec 031 (Alertas), Spec 036 (Simulador)

## Contexto y objetivo
Ofrecer un mecanismo interactivo (una pantalla de simulación de fallas para pruebas, con sus endpoints) que permita inyectar anomalías y fallas intencionales en paneles o en el inversor de un nodo (como corte por fusible quemado, caída de rendimiento por suciedad severa, sobretemperatura o falla de aislamiento), con el fin de disparar las alertas en tiempo real y probar el flujo de mantenimiento de punta a punta.

Las fallas se aplican sobre el simulador de la spec 036: este calcula las lecturas como una función pura del equipo y del instante, y ahora además consulta la línea de tiempo de fallas (cuándo empezó y cuándo terminó cada una). Por eso las pantallas en vivo, las alertas y la historia guardada coinciden.

## Usuarios / Actores
- **Administrador**: inyecta, restablece y consulta las fallas simuladas. El "evaluador / tester" de la letra no es un rol aparte: en el sistema los roles son solo Administrador y Técnico, y el Técnico no accede a esta pantalla.

## Historias de usuario
- **HU-1**: Como tester/administrador, quiero inducir fallas simuladas en paneles solares y en el inversor de un nodo para verificar que el sistema dispare las alertas automáticas y permita agendar su mantenimiento correctivo.

## Tipos de fallas simulables

| Tipo de falla | Se aplica a | Comportamiento en simulación | Nivel de alerta |
| :--- | :--- | :--- | :--- |
| **Corte total (circuito abierto)** | Panel | La potencia cae a 0 W de forma instantánea en pleno día. El mapa de salud (spec 030) lo muestra en rojo. | Crítico |
| **Degradación severa / suciedad** | Panel | La eficiencia cae al 30 % de la esperada a esa hora, con una variación propia mínima (sin nubosidad ni sombreados). El mapa de salud lo muestra en ámbar. | Advertencia |
| **Sobretemperatura de inversor** | Inversor | La temperatura interna sube a entre 86 y 90 °C, de día o de noche; la potencia no cambia. | Advertencia |
| **Falla de aislamiento** | Inversor | El inversor se apaga por la protección de fuga a tierra: potencia, corriente, tensión de red y frecuencia en cero, tensión DC residual. | Crítico |

Cada nodo tiene un solo inversor (spec 013), por lo que la falla de inversor se elige por nodo.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el Administrador acceda a la pantalla de simulación de fallas, seleccione un panel solar o el inversor de un nodo y un tipo de falla que corresponda a ese equipo, EL SISTEMA aplicará la anomalía sobre el simulador y mostrará "Falla simulada inyectada con éxito en el panel \"<serie>\"." (o "en el inversor \"<serie>\"." si es un inversor).
- **RF-2**: CUANDO la falla sea inyectada, EL SISTEMA la reflejará en la siguiente lectura emitida por SignalR (ciclo de la spec 036) en las pantallas en vivo de panel (spec 029), nodo (spec 028) y mapa de salud (spec 030), que además mostrarán la falla como motivo ("En Falla" en el panel y en el nodo).
- **RF-3**: CUANDO la lectura anómala sea procesada, EL SISTEMA disparará automáticamente la alerta correspondiente de la spec 031 (Crítico para el corte total y la falla de aislamiento; Advertencia para la degradación severa y la sobretemperatura) y el mapa de salud mostrará el panel en rojo (corte total) o en amarillo (degradación severa).
- **RF-4**: EL SISTEMA proveerá un botón "Restablecer / Reparar Falla Simulada" para cada equipo afectado, que terminará todas sus fallas activas y devolverá su generación a la curva solar normal en la siguiente lectura.
- **RF-5**: EL SISTEMA proveerá un botón global "Restablecer todas las fallas" que terminará todas las fallas activas y devolverá el parque entero a operación normal sin fallas.
- **RF-6**: SI el simulador de fallas no está habilitado en el entorno (`Simulation:FaultInjectionEnabled`), ENTONCES EL SISTEMA rechazará inyectar y restablecer con el mensaje "El simulador de fallas no está habilitado en este entorno." y la pantalla lo avisará. Viene habilitado en desarrollo y deshabilitado por defecto en el resto de los entornos.
- **RF-7**: MIENTRAS una falla de inversor esté activa, EL SISTEMA la aplicará también al cálculo de la historia guardada de la spec 036 (minuto y hora), desde el instante en que se inyectó y hasta el instante en que se restableció, sin reescribir el pasado.
- **RF-8**: SI el usuario no tiene una sesión autenticada con rol Administrador, ENTONCES EL SISTEMA denegará la inyección, el restablecimiento y la consulta de fallas (403, o redirección al inicio de sesión en la pantalla).

## Requisitos no funcionales
- Interfaz clara y distinguible con etiqueta de advertencia "ENTORNO DE SIMULACIÓN Y PRUEBAS".
- Disponibilidad en entornos de desarrollo y staging sin impactar producción real (RF-6).
- Las fallas son datos de simulación: no se toca ninguna lectura ni dato real, y terminarlas no borra nada (se guarda cuándo empezaron y cuándo terminaron).
- Con dos copias de la API sobre la misma base ven las mismas fallas y calculan las mismas lecturas (spec 036, RF-6).

## Casos límite
- Dos fallas distintas en el mismo equipo: prevalece la de mayor severidad (Crítico sobre Advertencia). Mientras estén las dos activas, el equipo se comporta con la crítica; al restablecer el equipo se terminan las dos.
- Inyectar una falla que el equipo ya tiene activa: se rechaza con "El equipo ya tiene esa falla activa." (409).
- Inyectar un tipo de falla que no corresponde al equipo (por ejemplo, aislamiento en un panel): se rechaza con 400.
- Equipo inexistente, panel dado de baja o sin nodo activo, o nodo inactivo o sin inversor: se rechaza (404 o 409) con el motivo, porque no emiten lecturas.
- De noche el corte total y la degradación no se notan (la potencia esperada es 0 W y el mapa muestra "Sin sol"); la falla queda activa y se manifiesta al amanecer.
- Restablecer un equipo sin fallas activas: se informa que no tenía fallas y no es error.
- Si un equipo pasa a inactivo o se desasigna con una falla activa, la falla se conserva pero no emite hasta que vuelva a operar; el botón global la termina igual.
- Las fallas de panel no cambian la potencia del parque ni la historia guardada, porque la potencia del parque se calcula con la de sus inversores (spec 036); sí cambian la lectura del panel, el mapa de salud y las alertas.

## Efecto en otras specs
- **Spec 036 (simulador)**: el motor recibe la línea de tiempo de fallas y la aplica por muestra de 10 s, tanto en las lecturas en vivo como en la historia y en la energía acumulada del día. Sin fallas el comportamiento es idéntico al actual.
- **Spec 029 (panel)**: la pantalla muestra la insignia "En Falla" con el tipo de falla (estaba reservada para esta spec).
- **Spec 028 (nodo)**: la pantalla muestra la falla activa del inversor; con falla de aislamiento el inversor figura apagado, sin alarmas espurias de frecuencia ni temperatura. (La spec 028 es de otro integrante del equipo: se actualiza solo este texto y el código de la pantalla; se le avisa.)
- **Spec 030 (mapa de salud)**: el motivo del estado de un panel con falla simulada se muestra en la tarjeta de detalle y los colores salen como hoy (corte total en rojo, degradación en ámbar).
- **Spec 031 (alertas)**: se agrega la alerta Crítica por falla de aislamiento del inversor y la Advertencia por degradación severa de un panel; la tabla provisoria de esa spec deja de marcar la falla de aislamiento como "No (spec 037)". Con esta spec, la alerta Crítica puede probarse en el navegador sin tests.
- **Spec 034 (históricos)**: sus gráficos reflejan la generación nula de un inversor con falla de aislamiento (RF-7).

## Fuera de alcance
- Destrucción de datos reales o alteración permanente de la base de datos: las fallas son datos de simulación con inicio y fin.
- Fallas de paneles que cambien la potencia del parque o la historia (ver casos límite).
- Programar fallas a futuro, fallas con duración automática, o inyectar por lote en varios equipos a la vez (salvo el restablecimiento global).
- Otros tipos de falla de la spec 031 (cortocircuito, paro de emergencia): no están en la tabla de esta spec.
- Pantalla para el Técnico.

## Criterios de finalización
- Tests unitarios en verde para las reglas de inyección y remoción de fallas, la prevalencia por severidad, los efectos de cada falla en el simulador y las nuevas reglas de alerta.
- Tests de arquitectura en verde.
- Verificación del ciclo completo en el navegador: inyectar falla → recibir el aviso de alerta → ver el mapa en rojo → restablecer → ver el mapa en verde; y la falla de inversor reflejada en el nodo, en la alerta y en la historia.

## Decisiones de la clarificación (2026-11-15)
- **Las fallas viven en una tabla de la base** (`SimulatedFaults`, una fila por falla con inicio y fin) y no en memoria: con dos copias de la API ambas ven las mismas fallas, sobreviven a un reinicio y el simulador sigue siendo determinista. Restablecer termina la falla (le pone fecha de fin) en lugar de borrarla.
- **Los 4 tipos de la tabla**: dos de panel y dos de inversor. La spec original tenía requisitos solo de paneles; se extienden a inversores (RF-1, RF-2, RF-7) y se agrega la regla de alerta Crítica por falla de aislamiento.
- **La historia refleja las fallas** desde que se inyectan (RF-7), de modo que lo que ve el operador en vivo, lo guardado por minuto y los reportes históricos coincidan.
- **El avance es por fases**, con la aprobación del usuario del plan y de las tareas antes de implementar, y de a tres tareas.
- Supuestos a confirmar con la aprobación: acceso solo para el Administrador (no hay rol de tester); el interruptor `Simulation:FaultInjectionEnabled` viene en `true` en `appsettings.Development.json` y en `false` en `appsettings.json`; la degradación entrega el 30 % de lo esperado y la sobretemperatura se mantiene entre 86 y 90 °C.

## Dudas abiertas
- Ninguna.
