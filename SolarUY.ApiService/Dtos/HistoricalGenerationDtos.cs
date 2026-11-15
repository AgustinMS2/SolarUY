namespace SolarUY.ApiService.Dtos;

public record HistoricalDataPointDto(
    string Label,
    DateTime LocalTimestamp,
    double Energy,
    double PeakPowerKw
);

public record HistoricalGenerationReportDto(
    Guid ParkId,
    string ParkName,
    string Granularity,
    string PeriodLabel,
    string Unit,
    bool HasData,
    double TotalEnergy,
    double PeakPowerKw,
    string? PeakPowerTimeLocal,
    double EquivalentSunHours,
    double? PreviousPeriodComparisonPercent,
    IReadOnlyList<HistoricalDataPointDto> Points
);

