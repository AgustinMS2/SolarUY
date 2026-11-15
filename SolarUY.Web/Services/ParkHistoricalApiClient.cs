using System.Net.Http.Json;

namespace SolarUY.Web.Services;

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

public class ParkHistoricalApiClient(HttpClient httpClient)
{
    public async Task<HistoricalGenerationReportDto?> GetDailyReportAsync(Guid parkId, DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            var formattedDate = date.ToString("yyyy-MM-dd");
            return await httpClient.GetFromJsonAsync<HistoricalGenerationReportDto>(
                $"/api/solar-parks/{parkId}/history/daily?date={formattedDate}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<HistoricalGenerationReportDto?> GetMonthlyReportAsync(Guid parkId, int year, int month, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<HistoricalGenerationReportDto>(
                $"/api/solar-parks/{parkId}/history/monthly?year={year}&month={month}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<HistoricalGenerationReportDto?> GetYearlyReportAsync(Guid parkId, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<HistoricalGenerationReportDto>(
                $"/api/solar-parks/{parkId}/history/yearly?year={year}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}

