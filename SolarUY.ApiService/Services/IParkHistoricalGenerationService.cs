using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface IParkHistoricalGenerationService
{
    ServiceResult<HistoricalGenerationReportDto> GetDailyReport(Guid parkId, DateOnly date);
    ServiceResult<HistoricalGenerationReportDto> GetMonthlyReport(Guid parkId, int year, int month);
    ServiceResult<HistoricalGenerationReportDto> GetYearlyReport(Guid parkId, int year);
}

