using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface IParkGenerationRepository
{
    IReadOnlyList<ParkHourlyGeneration> GetByParkAndDateRange(Guid parkId, DateOnly fromDate, DateOnly toDate);
    void AddRange(IEnumerable<ParkHourlyGeneration> records);
    bool HasAnyForPark(Guid parkId);
}

