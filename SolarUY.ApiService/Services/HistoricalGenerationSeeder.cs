using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class HistoricalGenerationSeeder : IHistoricalGenerationSeeder
{
    private readonly ISolarParkRepository _parkRepository;
    private readonly IParkGenerationRepository _generationRepository;
    private readonly ILogger<HistoricalGenerationSeeder> _logger;

    public HistoricalGenerationSeeder(
        ISolarParkRepository parkRepository,
        IParkGenerationRepository generationRepository,
        ILogger<HistoricalGenerationSeeder> logger)
    {
        _parkRepository = parkRepository;
        _generationRepository = generationRepository;
        _logger = logger;
    }

    public void SeedIfEmpty()
    {
        var parks = _parkRepository.GetAll().Where(p => p.Status == SolarParkStatus.Active).ToList();
        if (parks.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3)); // Hora local Uruguay
        var currentYear = today.Year;

        foreach (var park in parks)
        {
            if (_generationRepository.HasAnyForPark(park.Id))
            {
                continue;
            }

            _logger.LogInformation("Precargando histórico de generación para parque {ParkName}...", park.Name);
            var records = new List<ParkHourlyGeneration>();

            // 1) Generar para todo el año en curso hasta hoy
            var startDate = new DateOnly(currentYear, 1, 1);
            for (var d = startDate; d <= today; d = d.AddDays(1))
            {
                GenerateDayRecords(park, d, records);
            }

            // 2) Generar muestras para el año anterior (para permitir comparativa anual de la Spec 034)
            var prevYearStartDate = new DateOnly(currentYear - 1, 1, 1);
            var prevYearEndDate = new DateOnly(currentYear - 1, 12, 31);
            // Generar 1 semana por mes del año anterior para alimentar los totales mensuales
            for (var d = prevYearStartDate; d <= prevYearEndDate; d = d.AddDays(3))
            {
                GenerateDayRecords(park, d, records);
            }

            _generationRepository.AddRange(records);
            _logger.LogInformation("Se precargaron {Count} registros horarios para el parque {ParkName}.", records.Count, park.Name);
        }
    }

    private static void GenerateDayRecords(SolarPark park, DateOnly date, List<ParkHourlyGeneration> destination)
    {
        var dayOfYear = date.DayNumber;
        // Variación estacional suave y clima diario determinista (no aleatorio para consistencia en recargas)
        var seasonalFactor = 0.75 + (0.25 * Math.Cos(((date.Month - 1) / 12.0) * 2 * Math.PI));
        var weatherFactor = 0.85 + (0.15 * Math.Sin(dayOfYear * 13.0));
        var combinedFactor = Math.Clamp(seasonalFactor * weatherFactor, 0.4, 1.0);

        for (int h = 0; h < 24; h++)
        {
            double energy = 0.0;
            double peak = 0.0;

            if (h >= 6 && h <= 19)
            {
                var solarAngle = ((h - 6.0) / 14.0) * Math.PI;
                var baseSolar = Math.Sin(solarAngle);
                peak = Math.Round(park.MaxCapacityKw * baseSolar * combinedFactor, 1);
                energy = Math.Round(peak * 0.95, 1);
            }

            // TimestampUtc: restar 3 horas (Uruguay es UTC-3)
            var localDt = date.ToDateTime(new TimeOnly(h, 0));
            var utcDt = DateTime.SpecifyKind(localDt.AddHours(3), DateTimeKind.Utc);

            destination.Add(new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = park.Id,
                DateLocal = date,
                HourLocal = h,
                TimestampUtc = utcDt,
                EnergyGeneratedKwh = energy,
                PeakPowerKw = peak
            });
        }
    }
}

