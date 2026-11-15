namespace SolarUY.ApiService.Simulation;

/// <summary>Lectura instantánea de un inversor simulado.</summary>
public record InverterReading(
    double PowerKw,
    double DcVoltageV,
    double DcCurrentA,
    double AcVoltageV,
    double FrequencyHz,
    double TemperatureC);

/// <summary>Inversor a simular: su identificador y su capacidad nominal.</summary>
public readonly record struct SimulatedInverter(Guid InverterId, double NominalCapacityKw);

/// <summary>Resumen de una hora completa de un parque: energía generada (kWh) y potencia pico (kW).</summary>
public readonly record struct ParkHourSummary(double EnergyKwh, double PeakPowerKw);

/// <summary>
/// Motor único de simulación (Spec 036). Todas las lecturas son funciones puras del equipo y del instante: el mismo
/// equipo en el mismo instante da siempre el mismo valor, en cualquier pantalla y en cualquier copia del servidor.
/// </summary>
public interface ISolarSimulationEngine
{
    /// <summary>Lectura del inversor en un instante UTC.</summary>
    InverterReading ReadInverter(Guid inverterId, Guid parkId, double nominalCapacityKw, DateTime utc);

    /// <summary>Potencia del panel en W (potencia de placa × curva solar × nubosidad del parque × variación propia).</summary>
    double ReadPanelPowerW(Guid panelId, Guid parkId, int nominalPowerW, DateTime utc);

    /// <summary>Potencia del parque: la suma de la potencia de sus inversores (0 si no tiene).</summary>
    double ReadParkPowerKw(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime utc);

    /// <summary>Energía de un minuto de un inversor: la potencia de las 6 muestras (una cada 10 s) × 10 s.</summary>
    double InverterMinuteEnergyKwh(Guid inverterId, Guid parkId, double nominalCapacityKw, DateTime minuteStartUtc);

    /// <summary>
    /// Energía y potencia pico del parque en una hora completa (que debe empezar en una hora en punto): recorre las 360
    /// muestras de 10 s y suma la potencia de los inversores. La energía coincide con la suma de los minutos de la hora.
    /// </summary>
    ParkHourSummary SummarizeParkHour(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime hourStartUtc);

    /// <summary>Energía acumulada del parque desde las 00:00 locales hasta el instante dado.</summary>
    double EnergyTodayKwh(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime utc);
}

public class SolarSimulationEngine : ISolarSimulationEngine
{
    public const int SampleSeconds = 10;
    public const int SamplesPerMinute = 6;

    // Uruguay: UTC−3 todo el año.
    private static readonly TimeSpan UruguayOffset = TimeSpan.FromHours(-3);

    // Canales de ruido independientes.
    private const int CloudChannel = 0;
    private const int OwnPowerChannel = 1;
    private const int DcVoltageChannel = 2;
    private const int AcVoltageChannel = 3;
    private const int FrequencyAnomalyChannel = 4;
    private const int FrequencyDirectionChannel = 5;
    private const int FrequencyJitterChannel = 6;
    private const int TemperatureChannel = 7;
    private const int ShadeGateChannel = 8;
    private const int ShadeDepthChannel = 9;
    private const int ResidualVoltageChannel = 10;
    private const int PanelOwnChannel = 11;

    private const double CloudAmplitude = 0.08;          // nubosidad pasajera de ±8 %
    private const int CloudPeriodSeconds = 180;
    private const double InverterVariation = 0.02;       // variación propia de cada inversor
    private const double PanelVariation = 0.03;          // variación propia de cada panel
    private const double AnomalousFrequencyChance = 0.04;
    private const double ShadeChance = 0.03;              // 3 % de los bloques de 10 minutos con sombreado
    private const int ShadeBlockSeconds = 600;

    public InverterReading ReadInverter(Guid inverterId, Guid parkId, double nominalCapacityKw, DateTime utc)
    {
        var local = ToLocal(utc);
        var curve = SolarCalendar.Factor(local);

        var cloud = CloudFactor(parkId, utc);
        var own = 1.0 + (InverterVariation * DeterministicNoise.Smooth(inverterId, utc, 30, OwnPowerChannel));
        var powerKw = nominalCapacityKw > 0.0
            ? Math.Clamp(nominalCapacityKw * curve * cloud * own, 0.0, nominalCapacityKw)
            : 0.0;
        var load = nominalCapacityKw > 0.0 ? powerKw / nominalCapacityKw : 0.0;

        // De noche el inversor solo conserva un voltaje DC residual muy bajo.
        var dcVoltage = powerKw <= 0.0
            ? 8.0 + (4.0 * DeterministicNoise.Smooth(inverterId, utc, 60, ResidualVoltageChannel))
            : Math.Clamp((740.0 - (180.0 * load)) * (1.0 + (0.02 * DeterministicNoise.Smooth(inverterId, utc, 45, DcVoltageChannel))), 200.0, 1000.0);

        var dcCurrent = powerKw > 0.0 ? powerKw * 1000.0 / dcVoltage : 0.0;
        var acVoltage = 380.0 * (1.0 + (0.01 * DeterministicNoise.Smooth(inverterId, utc, 40, AcVoltageChannel)));

        // Temperatura: ambiente del día (mínima a las 03:00 y máxima a las 15:00) más el calentamiento por carga.
        var ambient = 18.0 + (8.0 * Math.Sin(2.0 * Math.PI * (local.TimeOfDay.TotalHours - 9.0) / 24.0));
        var temperature = ambient + (50.0 * load * (1.0 + (0.08 * DeterministicNoise.Smooth(inverterId, utc, 60, TemperatureChannel))));

        return new InverterReading(powerKw, dcVoltage, dcCurrent, acVoltage, FrequencyAt(inverterId, utc), temperature);
    }

    public double ReadPanelPowerW(Guid panelId, Guid parkId, int nominalPowerW, DateTime utc)
    {
        if (nominalPowerW <= 0)
        {
            return 0.0;
        }

        var expected = nominalPowerW * SolarCalendar.Factor(ToLocal(utc));
        if (expected <= 0.0)
        {
            return 0.0;
        }

        var own = 1.0 + (PanelVariation * DeterministicNoise.Smooth(panelId, utc, 30, PanelOwnChannel));

        // Sombreado o suciedad ocasional, reproducible: en algunos bloques de 10 minutos el panel cae al 20–35 %.
        var block = (long)Math.Floor(SecondsSinceEpoch(utc) / ShadeBlockSeconds);
        var shade = DeterministicNoise.Unit(panelId, block, ShadeGateChannel) < ShadeChance
            ? 0.20 + (0.15 * DeterministicNoise.Unit(panelId, block, ShadeDepthChannel))
            : 1.0;

        return Math.Clamp(expected * CloudFactor(parkId, utc) * own * shade, 0.0, nominalPowerW);
    }

    public double ReadParkPowerKw(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime utc)
    {
        double total = 0.0;
        foreach (var inverter in inverters)
        {
            total += ReadInverter(inverter.InverterId, parkId, inverter.NominalCapacityKw, utc).PowerKw;
        }

        return total;
    }

    public double InverterMinuteEnergyKwh(Guid inverterId, Guid parkId, double nominalCapacityKw, DateTime minuteStartUtc)
    {
        double energy = 0.0;
        for (var i = 0; i < SamplesPerMinute; i++)
        {
            energy += ReadInverter(inverterId, parkId, nominalCapacityKw, minuteStartUtc.AddSeconds(i * SampleSeconds)).PowerKw * SampleSeconds / 3600.0;
        }

        return energy;
    }

    public ParkHourSummary SummarizeParkHour(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime hourStartUtc)
    {
        if (hourStartUtc.Ticks % TimeSpan.TicksPerHour != 0)
        {
            throw new ArgumentException("La hora debe empezar en una hora en punto.", nameof(hourStartUtc));
        }

        if (inverters.Count == 0 || IsNightHour(hourStartUtc))
        {
            return new ParkHourSummary(0.0, 0.0);
        }

        const int samplesPerHour = SamplesPerMinute * 60;
        double energy = 0.0;
        double peak = 0.0;

        for (var i = 0; i < samplesPerHour; i++)
        {
            var sampleUtc = hourStartUtc.AddSeconds(i * SampleSeconds);
            double power = 0.0;
            foreach (var inverter in inverters)
            {
                power += ReadInverter(inverter.InverterId, parkId, inverter.NominalCapacityKw, sampleUtc).PowerKw;
            }

            energy += power * SampleSeconds / 3600.0;
            peak = Math.Max(peak, power);
        }

        return new ParkHourSummary(energy, peak);
    }

    public double EnergyTodayKwh(Guid parkId, IReadOnlyCollection<SimulatedInverter> inverters, DateTime utc)
    {
        if (inverters.Count == 0)
        {
            return 0.0;
        }

        // Muestras cada 10 s desde las 00:00 locales hasta el instante (sin incluirlo): para los minutos completos coincide
        // exactamente con la suma de InverterMinuteEnergyKwh, que es lo que se guarda en la historia.
        var localMidnight = ToLocal(utc).Date;
        var startUtc = DateTime.SpecifyKind(localMidnight - UruguayOffset, DateTimeKind.Utc);

        double energy = 0.0;
        for (var sample = startUtc; sample < utc; sample = sample.AddSeconds(SampleSeconds))
        {
            foreach (var inverter in inverters)
            {
                energy += ReadInverter(inverter.InverterId, parkId, inverter.NominalCapacityKw, sample).PowerKw * SampleSeconds / 3600.0;
            }
        }

        return energy;
    }

    // Una hora entera sin sol (termina antes del amanecer o empieza después del atardecer) no se muestrea: vale 0.
    private static bool IsNightHour(DateTime hourStartUtc)
    {
        var local = ToLocal(hourStartUtc);
        var startHour = local.TimeOfDay.TotalHours;
        return startHour + 1.0 <= SolarCalendar.SunriseHour(local) || startHour >= SolarCalendar.SunsetHour(local);
    }

    // La nubosidad es la misma para todos los equipos de un parque en un instante.
    private static double CloudFactor(Guid parkId, DateTime utc)
    {
        return 1.0 + (CloudAmplitude * DeterministicNoise.Smooth(parkId, utc, CloudPeriodSeconds, CloudChannel));
    }

    // 50 Hz con variación leve y, en ~4 % de los minutos, un desvío reproducible fuera de 50 ± 0,5 Hz.
    private static double FrequencyAt(Guid inverterId, DateTime utc)
    {
        var minute = (long)Math.Floor(SecondsSinceEpoch(utc) / 60.0);

        if (DeterministicNoise.Unit(inverterId, minute, FrequencyAnomalyChannel) < AnomalousFrequencyChance)
        {
            return DeterministicNoise.Unit(inverterId, minute, FrequencyDirectionChannel) < 0.5 ? 49.2 : 50.8;
        }

        return 50.0 + (0.1 * DeterministicNoise.Smooth(inverterId, utc, 20, FrequencyJitterChannel));
    }

    private static DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) + UruguayOffset;

    private static double SecondsSinceEpoch(DateTime utc)
        => (utc.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks) / (double)TimeSpan.TicksPerSecond;
}
