namespace SolarUY.ApiService.Simulation;

/// <summary>
/// Calendario solar de Uruguay (Spec 036): amanecer y atardecer según la época del año, con la hora oficial
/// UTC−3 y una aproximación sinusoidal de la duración del día. Funciones puras: dependen solo de la fecha y la hora
/// locales que se les pasan.
/// </summary>
public static class SolarCalendar
{
    /// <summary>Mediodía solar en hora oficial de Uruguay (UTC−3), alrededor de las 12:45.</summary>
    public const double SolarNoonHour = 12.75;

    // Duración del día: 12,2 h de media con ±2,1 h de variación anual (14,3 h el 21 de dic. y 10,1 h el 21 de jun.).
    private const double MeanDayLengthHours = 12.2;
    private const double DayLengthAmplitudeHours = 2.1;
    private const int LongestDayOfYear = 355;
    private const double DaysPerYear = 365.0;

    /// <summary>Hora oficial de Uruguay (UTC−3 todo el año) de un instante UTC.</summary>
    public static DateTime ToLocal(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) + TimeSpan.FromHours(-3);

    public static double DayLengthHours(int dayOfYear)
    {
        var angle = 2.0 * Math.PI * (dayOfYear - LongestDayOfYear) / DaysPerYear;
        return MeanDayLengthHours + (DayLengthAmplitudeHours * Math.Cos(angle));
    }

    /// <summary>Hora local del amanecer (en horas decimales) de la fecha dada; se ignora la hora del día.</summary>
    public static double SunriseHour(DateTime localDate) => SolarNoonHour - (DayLengthHours(localDate.DayOfYear) / 2.0);

    /// <summary>Hora local del atardecer (en horas decimales) de la fecha dada; se ignora la hora del día.</summary>
    public static double SunsetHour(DateTime localDate) => SolarNoonHour + (DayLengthHours(localDate.DayOfYear) / 2.0);

    /// <summary>Hay sol entre el amanecer y el atardecer de la fecha (los extremos no cuentan).</summary>
    public static bool IsDaylight(DateTime localTime)
    {
        var hour = localTime.TimeOfDay.TotalHours;
        return hour > SunriseHour(localTime) && hour < SunsetHour(localTime);
    }

    /// <summary>
    /// Factor de generación entre 0 y 1: <c>sen(π × (hora − amanecer) / (atardecer − amanecer))</c> con sol y 0 de noche.
    /// Vale 1 al mediodía solar.
    /// </summary>
    public static double Factor(DateTime localTime)
    {
        var sunrise = SunriseHour(localTime);
        var sunset = SunsetHour(localTime);
        var hour = localTime.TimeOfDay.TotalHours;

        if (hour <= sunrise || hour >= sunset)
        {
            return 0.0;
        }

        return Math.Sin(Math.PI * (hour - sunrise) / (sunset - sunrise));
    }
}
