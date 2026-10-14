---
name: local-dates
description: Úsala siempre que escribas, modifiques o revises código que trabaje con fechas, telemetría solar, rangos temporales o registros históricos en SolarUY.
---

# Manejo de Fechas y Telemetría en SolarUY

El manejo incorrecto de husos horarios y fechas es una de las principales fuentes de errores en telemetría de energía solar y registros históricos (diarios, mensuales y anuales). Sigue esta guía siempre que toques código con fechas y marcas temporales.

## Reglas para SolarUY (.NET / C# y PostgreSQL)
- **Persistencia en UTC**: Toda marca temporal de mediciones de inversores, sensores y auditoría se guarda en base de datos en UTC utilizando `DateTimeOffset` o `DateTime` con `DateTimeKind.Utc`.
- **Huso Horario de Parques**: Los cálculos de generación por día, mes o año se proyectan a la zona horaria oficial de Uruguay (`America/Montevideo` / UTC-3).
- **Fechas de Calendario sin Hora**: Para representar días específicos de mantenimiento o resúmenes diarios, utiliza `DateOnly` en lugar de `DateTime` a las 00:00:00 para evitar desfasajes por conversiones horarias.
- **Sin `DateTime.Now` acoplado**: Nunca utilices directamente `DateTime.Now` ni `DateTime.UtcNow` dentro de la lógica de negocio pura o cálculos de energía. Inyecta `TimeProvider` (estándar en .NET 8/9) para permitir pruebas unitarias con fechas simuladas o congeladas.
- **Series de tiempo en PostgreSQL**: En consultas de agrupación temporal (TimescaleDB / EF Core), agrupa utilizando funciones con zona horaria explícita (`date_trunc('day', timestamp AT TIME ZONE 'America/Montevideo')`).

## Checklist de Revisión
- [ ] ¿Hay algún uso directo de `DateTime.Now` en servicios de cálculo o entidades?
- [ ] ¿Se utiliza `TimeProvider` o se pasa la fecha como parámetro en funciones puras?
- [ ] ¿Se confunden fechas de calendario (`DateOnly`) con instantes en el tiempo (`DateTimeOffset`)?
- [ ] ¿Qué ocurre con lecturas de telemetría recibidas a las 23:59 o a las 00:01? ¿Pertenecen al día correcto en hora local de Uruguay?
- [ ] ¿Se previenen errores en cambios de huso horario o años bisiestos al calcular medias anuales o mensuales?

## Al Terminar
Indica qué puntos del checklist has comprobado y cómo han quedado validados con `dotnet test`.
