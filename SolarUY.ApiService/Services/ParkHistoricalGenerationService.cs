using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class ParkHistoricalGenerationService : IParkHistoricalGenerationService
{
    private readonly ISolarParkRepository _parkRepository;
    private readonly IParkGenerationRepository _generationRepository;

    public ParkHistoricalGenerationService(
        ISolarParkRepository parkRepository,
        IParkGenerationRepository generationRepository)
    {
        _parkRepository = parkRepository;
        _generationRepository = generationRepository;
    }

    public ServiceResult<HistoricalGenerationReportDto> GetDailyReport(Guid parkId, DateOnly date)
    {
        var park = _parkRepository.GetById(parkId);
        if (park == null)
        {
            return ServiceResult<HistoricalGenerationReportDto>.Fail(ServiceErrorType.NotFound, "El parque solar especificado no existe.");
        }

        var records = _generationRepository.GetByParkAndDateRange(parkId, date, date);
        var points = new List<HistoricalDataPointDto>();

        double totalEnergyKwh = 0.0;
        double peakPowerKw = 0.0;
        string? peakTimeLocal = null;

        for (int h = 0; h < 24; h++)
        {
            var record = records.FirstOrDefault(r => r.HourLocal == h);
            var energy = record?.EnergyGeneratedKwh ?? 0.0;
            var peak = record?.PeakPowerKw ?? 0.0;
            var label = $"{h:D2}:00";
            var timestamp = date.ToDateTime(new TimeOnly(h, 0));

            totalEnergyKwh += energy;
            if (peak > peakPowerKw)
            {
                peakPowerKw = peak;
                peakTimeLocal = label;
            }

            points.Add(new HistoricalDataPointDto(label, timestamp, Math.Round(energy, 2), Math.Round(peak, 2)));
        }

        bool hasData = records.Count > 0 && totalEnergyKwh > 0;
        double hsp = park.MaxCapacityKw > 0 ? Math.Round(totalEnergyKwh / park.MaxCapacityKw, 1) : 0.0;

        // Previous period comparison: same day of previous week
        double? comparison = null;
        var prevRecords = _generationRepository.GetByParkAndDateRange(parkId, date.AddDays(-7), date.AddDays(-7));
        var prevTotal = prevRecords.Sum(r => r.EnergyGeneratedKwh);
        if (prevTotal > 0)
        {
            comparison = Math.Round(((totalEnergyKwh - prevTotal) / prevTotal) * 100.0, 1);
        }

        var report = new HistoricalGenerationReportDto(
            park.Id,
            park.Name,
            "Day",
            date.ToString("dd/MM/yyyy"),
            "kWh",
            hasData,
            Math.Round(totalEnergyKwh, 2),
            Math.Round(peakPowerKw, 2),
            peakTimeLocal,
            hsp,
            comparison,
            points
        );

        return ServiceResult<HistoricalGenerationReportDto>.Ok(report);
    }

    public ServiceResult<HistoricalGenerationReportDto> GetMonthlyReport(Guid parkId, int year, int month)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
        {
            return ServiceResult<HistoricalGenerationReportDto>.Fail(ServiceErrorType.Validation, "El mes o año especificado es inválido.");
        }

        var park = _parkRepository.GetById(parkId);
        if (park == null)
        {
            return ServiceResult<HistoricalGenerationReportDto>.Fail(ServiceErrorType.NotFound, "El parque solar especificado no existe.");
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var fromDate = new DateOnly(year, month, 1);
        var toDate = new DateOnly(year, month, daysInMonth);

        var records = _generationRepository.GetByParkAndDateRange(parkId, fromDate, toDate);
        var points = new List<HistoricalDataPointDto>();

        double totalEnergyKwh = 0.0;
        double peakPowerKw = 0.0;
        string? peakDateLocal = null;

        var groupedByDay = records.GroupBy(r => r.DateLocal).ToDictionary(g => g.Key, g => g.ToList());

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(year, month, d);
            var dayRecords = groupedByDay.TryGetValue(date, out var list) ? list : [];
            var energy = dayRecords.Sum(r => r.EnergyGeneratedKwh);
            var peak = dayRecords.Count > 0 ? dayRecords.Max(r => r.PeakPowerKw) : 0.0;
            var label = $"{d:D2}";
            var timestamp = date.ToDateTime(TimeOnly.MinValue);

            totalEnergyKwh += energy;
            if (peak > peakPowerKw)
            {
                peakPowerKw = peak;
                peakDateLocal = $"{d:D2}/{month:D2}";
            }

            points.Add(new HistoricalDataPointDto(label, timestamp, Math.Round(energy, 2), Math.Round(peak, 2)));
        }

        bool hasData = records.Count > 0 && totalEnergyKwh > 0;
        double hsp = park.MaxCapacityKw > 0 ? Math.Round(totalEnergyKwh / park.MaxCapacityKw, 1) : 0.0;

        // Previous period comparison: previous month
        double? comparison = null;
        var prevMonthDate = fromDate.AddMonths(-1);
        var prevDaysInMonth = DateTime.DaysInMonth(prevMonthDate.Year, prevMonthDate.Month);
        var prevRecords = _generationRepository.GetByParkAndDateRange(parkId, prevMonthDate, new DateOnly(prevMonthDate.Year, prevMonthDate.Month, prevDaysInMonth));
        var prevTotal = prevRecords.Sum(r => r.EnergyGeneratedKwh);
        if (prevTotal > 0)
        {
            comparison = Math.Round(((totalEnergyKwh - prevTotal) / prevTotal) * 100.0, 1);
        }

        var monthNames = new[] { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Setiembre", "Octubre", "Noviembre", "Diciembre" };
        var periodLabel = $"{monthNames[month]} {year}";

        var report = new HistoricalGenerationReportDto(
            park.Id,
            park.Name,
            "Month",
            periodLabel,
            "kWh",
            hasData,
            Math.Round(totalEnergyKwh, 2),
            Math.Round(peakPowerKw, 2),
            peakDateLocal,
            hsp,
            comparison,
            points
        );

        return ServiceResult<HistoricalGenerationReportDto>.Ok(report);
    }

    public ServiceResult<HistoricalGenerationReportDto> GetYearlyReport(Guid parkId, int year)
    {
        if (year < 2000 || year > 2100)
        {
            return ServiceResult<HistoricalGenerationReportDto>.Fail(ServiceErrorType.Validation, "El año especificado es inválido.");
        }

        var park = _parkRepository.GetById(parkId);
        if (park == null)
        {
            return ServiceResult<HistoricalGenerationReportDto>.Fail(ServiceErrorType.NotFound, "El parque solar especificado no existe.");
        }

        var fromDate = new DateOnly(year, 1, 1);
        var toDate = new DateOnly(year, 12, 31);

        var records = _generationRepository.GetByParkAndDateRange(parkId, fromDate, toDate);
        var points = new List<HistoricalDataPointDto>();

        double totalEnergyKwh = 0.0;
        double peakPowerKw = 0.0;
        string? peakMonthLocal = null;

        var monthShortNames = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
        var groupedByMonth = records.GroupBy(r => r.DateLocal.Month).ToDictionary(g => g.Key, g => g.ToList());

        for (int m = 1; m <= 12; m++)
        {
            var monthRecords = groupedByMonth.TryGetValue(m, out var list) ? list : [];
            var energyKwh = monthRecords.Sum(r => r.EnergyGeneratedKwh);
            var energyMwh = energyKwh / 1000.0;
            var peak = monthRecords.Count > 0 ? monthRecords.Max(r => r.PeakPowerKw) : 0.0;
            var label = monthShortNames[m - 1];
            var timestamp = new DateTime(year, m, 1);

            totalEnergyKwh += energyKwh;
            if (peak > peakPowerKw)
            {
                peakPowerKw = peak;
                peakMonthLocal = label;
            }

            points.Add(new HistoricalDataPointDto(label, timestamp, Math.Round(energyMwh, 3), Math.Round(peak, 2)));
        }

        bool hasData = records.Count > 0 && totalEnergyKwh > 0;
        double totalEnergyMwh = Math.Round(totalEnergyKwh / 1000.0, 3);
        double hsp = park.MaxCapacityKw > 0 ? Math.Round(totalEnergyKwh / park.MaxCapacityKw, 1) : 0.0;

        // Previous year comparison
        double? comparison = null;
        var prevRecords = _generationRepository.GetByParkAndDateRange(parkId, new DateOnly(year - 1, 1, 1), new DateOnly(year - 1, 12, 31));
        var prevTotal = prevRecords.Sum(r => r.EnergyGeneratedKwh);
        if (prevTotal > 0)
        {
            comparison = Math.Round(((totalEnergyKwh - prevTotal) / prevTotal) * 100.0, 1);
        }

        var report = new HistoricalGenerationReportDto(
            park.Id,
            park.Name,
            "Year",
            year.ToString(),
            "MWh",
            hasData,
            totalEnergyMwh,
            Math.Round(peakPowerKw, 2),
            peakMonthLocal,
            hsp,
            comparison,
            points
        );

        return ServiceResult<HistoricalGenerationReportDto>.Ok(report);
    }
}

