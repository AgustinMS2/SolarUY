# Spec 029 — Ver Generación en Tiempo Real del Panel

Estado: aprobada
Depende de: Spec 006 (Listar Panel), Spec 028 (Monitoreo Nodo)

## Contexto y objetivo
Permitir el monitoreo granular a nivel de módulo o string de panel fotovoltaico, observando la potencia instantánea generada (Watts), la eficiencia calculada en vivo y la comparación contra la potencia nominal de placa.

## Usuarios / Actores
- **Administrador** y **Técnico**: supervisan la producción individual de paneles.

## Historias de usuario
- **HU-1**: Como Técnico, quiero ver la generación en vivo de un panel solar para verificar si produce la potencia esperada según su especificación.

## Métricas del panel

| Métrica | Unidad | Cálculo |
| :--- | :--- | :--- |
| Potencia actual | W | Lectura instantánea del panel. |
| Potencia esperada a la hora actual | W | Potencia de placa × sen(π × (hora − amanecer) / (atardecer − amanecer)) entre el amanecer y el atardecer de la fecha (hora Uruguay, calendario de la spec 036); 0 W fuera de ese horario. Es la misma curva solar que usan las simulaciones de las specs 027 y 028. |
| Porcentaje de la placa | % | (Potencia actual / Potencia de placa) × 100. |
| Eficiencia instantánea estimada | % | Eficiencia de placa × (Potencia actual / Potencia esperada). Si la potencia esperada es 0 W, se muestra "—". |
| Estado del enlace SignalR | - | Conectado (verde) / Reconectando (amarillo) / Desconectado (rojo). |

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario acceda a la vista de monitoreo de un panel (`/paneles/{id}/monitoreo`, desde el botón "Monitorear" del listado de paneles), EL SISTEMA mostrará los datos de placa (Potencia W, Eficiencia %, Parque, Nodo) y un visor de generación instantánea en tiempo real.
- **RF-2**: MIENTRAS se reciban lecturas telemétricas del panel (vía el nodo al que pertenece), EL SISTEMA actualizará en vivo: Potencia actual (W), Eficiencia instantánea estimada (%) y Porcentaje de la potencia de placa.
- **RF-3**: SI la potencia actual es inferior al 40 % de la potencia esperada a la hora actual, ENTONCES EL SISTEMA mostrará un indicador de "Rendimiento bajo / Posible sombreado o suciedad". La alerta no se evalúa cuando la potencia esperada es 0 W (de noche).
- **RF-4**: SI el panel no está asignado a ningún nodo, ENTONCES EL SISTEMA mostrará "Panel no conectado a la red de un nodo." y no abrirá la conexión de telemetría en vivo.
- **RF-5**: SI se pierde la conexión con el hub de SignalR, ENTONCES EL SISTEMA mostrará "Reconectando con el panel..." y mantendrá visible la última lectura recibida con una marca de advertencia.
- **RF-6**: CUANDO el usuario navegue a otra pantalla, EL SISTEMA cerrará la suscripción al grupo SignalR del panel para optimizar recursos del servidor.

## Requisitos no funcionales
- Consumo moderado de red (agrupación de eventos si se monitorean múltiples paneles simultáneamente).
- Diseño responsive "SolarOps Clean", usable en navegadores móviles.

## Casos límite
- De noche (potencia esperada 0 W): se muestra 0 W, la eficiencia estimada aparece como "—" y no se emite la alerta de rendimiento bajo.
- Panel Inactivo (dado de baja en la spec 008): el listado no muestra el botón "Monitorear"; si se accede por URL directa, se muestra "El panel se encuentra inactivo." sin abrir la conexión en vivo.
- Panel asignado a un nodo Inactivo: no se reciben lecturas; se muestra "El nodo del panel se encuentra inactivo." sin abrir la conexión en vivo.
- Panel inexistente: se muestra "El panel solicitado no existe."

## Fuera de alcance
- Calibración de sensores de panel.
- "Panel desactivado manualmente" con potencia 0 W: depende del activar/desactivar de la spec 032 y se implementa ahí (estaba como RF-4 en el borrador de esta spec).
- Panel con falla reportada por el simulador y badge "En Falla": se implementa en la spec 037 (estaba en los casos límite del borrador).

## Criterios de finalización
- Verificación interactiva en Blazor Server recibiendo eventos de SignalR.
- Botón "Monitorear" accesible desde el listado de paneles (`/paneles`) para los paneles activos.
- Tests unitarios de la curva esperada y de las reglas de cálculo y de alerta en verde.
- Tests de arquitectura en verde.

## Decisiones de la clarificación (2026-11-15)
- La pantalla es una ruta nueva `/paneles/{id}/monitoreo` con botón en el listado, igual que en las specs 027 y 028.
- El "Panel desactivado manualmente" (RF-4 del borrador) se deja para la spec 032.
- El caso "En Falla" del simulador se mueve a la spec 037.
- El umbral del 40 % se compara contra la potencia esperada a la hora actual, no contra la placa.
- Agregados respecto al borrador para alinearla con las specs 027 y 028 (a confirmar en la aprobación): RF-5 y RF-6 (reconexión y cierre de suscripción) y los casos límite de panel inactivo, nodo inactivo y panel inexistente.

## Dudas abiertas
- Ninguna.
