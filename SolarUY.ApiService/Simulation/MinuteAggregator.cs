using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Simulation;

/// <summary>
/// Resume un minuto de un inversor (Spec 036, RF-3) a partir de las 6 lecturas del motor, una cada 10 s.
/// Función pura: el mismo inversor y el mismo minuto dan siempre el mismo registro.
/// </summary>
public static class MinuteAggregator
{
    private const double MaxNormalFrequencyDeviationHz = 0.5;
    private const double MaxNormalTemperatureC = 75.0;

    public static InverterMinuteReading Build(
        ISolarSimulationEngine engine,
        Guid inverterId,
        Guid parkId,
        Guid nodeId,
        double nominalCapacityKw,
        DateTime minuteStartUtc,
        FaultTimeline? faults = null)
    {
        if (minuteStartUtc.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException("El minuto debe empezar en un minuto exacto.", nameof(minuteStartUtc));
        }

        double power = 0.0, maxPower = 0.0, energy = 0.0, dcVoltage = 0.0, dcCurrent = 0.0, acVoltage = 0.0;
        double frequency = 0.0, temperature = 0.0, maxTemperature = double.MinValue;
        var anomalous = 0;

        for (var i = 0; i < SolarSimulationEngine.SamplesPerMinute; i++)
        {
            var sample = engine.ReadInverter(inverterId, parkId, nominalCapacityKw, minuteStartUtc.AddSeconds(i * SolarSimulationEngine.SampleSeconds), faults);

            power += sample.PowerKw;
            maxPower = Math.Max(maxPower, sample.PowerKw);
            energy += sample.PowerKw * SolarSimulationEngine.SampleSeconds / 3600.0;
            dcVoltage += sample.DcVoltageV;
            dcCurrent += sample.DcCurrentA;
            acVoltage += sample.AcVoltageV;
            frequency += sample.FrequencyHz;
            temperature += sample.TemperatureC;
            maxTemperature = Math.Max(maxTemperature, sample.TemperatureC);

            if (Math.Abs(sample.FrequencyHz - 50.0) > MaxNormalFrequencyDeviationHz || sample.TemperatureC > MaxNormalTemperatureC)
            {
                anomalous++;
            }
        }

        const double n = SolarSimulationEngine.SamplesPerMinute;
        return new InverterMinuteReading
        {
            InverterId = inverterId,
            MinuteUtc = DateTime.SpecifyKind(minuteStartUtc, DateTimeKind.Utc),
            SolarParkId = parkId,
            SolarNodeId = nodeId,
            AvgPowerKw = power / n,
            MaxPowerKw = maxPower,
            EnergyKwh = energy,
            AvgDcVoltageV = dcVoltage / n,
            AvgDcCurrentA = dcCurrent / n,
            AvgAcVoltageV = acVoltage / n,
            AvgFrequencyHz = frequency / n,
            AvgTemperatureC = temperature / n,
            MaxTemperatureC = maxTemperature,
            SampleCount = SolarSimulationEngine.SamplesPerMinute,
            AnomalousSamples = anomalous
        };
    }
}
