namespace SolarUY.ApiService.Simulation;

/// <summary>Rango de tiempo UTC con el límite inferior incluido y el superior excluido.</summary>
public readonly record struct TimeRange(DateTime FromUtc, DateTime ToUtc);

/// <summary>
/// Calcula qué parte de la historia falta guardar (Spec 036, RF-8). Funciones puras sobre fechas UTC.
/// </summary>
public static class HistoryGapPlanner
{
    // Uruguay: UTC−3 todo el año.
    private static readonly TimeSpan UruguayOffset = TimeSpan.FromHours(-3);

    /// <summary>
    /// Minutos que faltan de un inversor: desde el mayor entre el comienzo de la ventana, su alta y el minuto siguiente al
    /// último guardado, hasta el último minuto completo. <c>null</c> si no falta ninguno.
    /// </summary>
    public static TimeRange? MissingMinutes(DateTime? lastSavedMinuteUtc, DateTime createdAtUtc, DateTime nowUtc, int backfillDays)
    {
        var to = Floor(nowUtc, TimeSpan.TicksPerMinute);
        var from = Max(Floor(nowUtc.AddDays(-backfillDays), TimeSpan.TicksPerMinute), Ceiling(createdAtUtc, TimeSpan.TicksPerMinute));

        if (lastSavedMinuteUtc.HasValue)
        {
            from = Max(from, Utc(lastSavedMinuteUtc.Value).AddMinutes(1));
        }

        return from < to ? new TimeRange(from, to) : null;
    }

    /// <summary>Comienzo de la historia horaria: las 00:00 locales del 1 de enero del año anterior, en UTC.</summary>
    public static DateTime HourlyWindowStartUtc(DateTime nowUtc)
    {
        var localYear = (Utc(nowUtc) + UruguayOffset).Year;
        return new DateTime(localYear - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc) - UruguayOffset;
    }

    /// <summary>
    /// Horas que faltan de un parque: desde el mayor entre el comienzo de la ventana, el alta de su primer equipo y la hora
    /// siguiente a la última guardada, hasta la última hora completa. <c>null</c> si no falta ninguna.
    /// </summary>
    public static TimeRange? MissingHours(DateTime? lastSavedHourUtc, DateTime firstEquipmentCreatedAtUtc, DateTime nowUtc)
    {
        var to = Floor(nowUtc, TimeSpan.TicksPerHour);
        var from = Max(HourlyWindowStartUtc(nowUtc), Ceiling(firstEquipmentCreatedAtUtc, TimeSpan.TicksPerHour));

        if (lastSavedHourUtc.HasValue)
        {
            from = Max(from, Utc(lastSavedHourUtc.Value).AddHours(1));
        }

        return from < to ? new TimeRange(from, to) : null;
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

    private static DateTime Floor(DateTime value, long unitTicks)
        => new(value.Ticks - (value.Ticks % unitTicks), DateTimeKind.Utc);

    private static DateTime Ceiling(DateTime value, long unitTicks)
    {
        var remainder = value.Ticks % unitTicks;
        return new DateTime(remainder == 0 ? value.Ticks : value.Ticks + (unitTicks - remainder), DateTimeKind.Utc);
    }
}
