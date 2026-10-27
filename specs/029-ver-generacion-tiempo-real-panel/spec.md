# Spec 029 — Ver Generación en Tiempo Real del Panel

Estado: borrador
Depende de: Spec 006 (Listar Panel), Spec 028 (Monitoreo Nodo)

## Contexto y objetivo
Permitir el monitoreo granular a nivel de módulo o string de panel fotovoltaico, observando la potencia instantánea generada (Watts), la eficiencia calculada en vivo y la comparación contra la potencia nominal de placa.

## Usuarios / Actores
- **Administrador** y **Técnico**: supervisan la producción individual de paneles.

## Historias de usuario
- **HU-1**: Como Técnico, quiero ver la generación en vivo de un panel solar para verificar si produce la potencia esperada según su especificación.

## Requisitos funcionales (EARS)

- **RF-1**: CUANDO el usuario acceda al detalle de un panel solar, EL SISTEMA mostrará los datos de placa (Potencia W, Eficiencia %, Parque, Nodo) y un visor de generación instantánea en tiempo real.
- **RF-2**: MIENTRAS se reciban lecturas telemétricas del panel (vía el nodo al que pertenece), EL SISTEMA actualizará en vivo: Potencia actual (W), Eficiencia instantánea estimada (%) y Porcentaje de rendimiento respecto al valor nominal.
- **RF-3**: SI la potencia generada es inferior al 40% de la esperada en condiciones de sol pleno, ENTONCES EL SISTEMA mostrará un indicador de "Rendimiento bajo / Posible sombreado o suciedad".
- **RF-4**: SI el panel se encuentra desactivado administrativamente, EL SISTEMA mostrará "Panel desactivado manualmente" con potencia 0 W.
- **RF-5**: SI el panel no está asignado a ningún nodo, EL SISTEMA mostrará "Panel no conectado a la red de un nodo."

## Requisitos no funcionales
- Consumo moderado de red (agrupación de eventos si se monitorean múltiples paneles simultáneos).

## Casos límite
- Panel con falla reportada por el simulador: muestra 0 W y badge "En Falla".

## Fuera de alcance
- Calibración de sensores de panel.

## Criterios de finalización
- Verificación interactiva en Blazor Server.
- Tests de arquitectura en verde.

## Dudas abiertas
- Ninguna.
