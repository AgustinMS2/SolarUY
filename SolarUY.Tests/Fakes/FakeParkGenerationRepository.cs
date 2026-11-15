using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeParkGenerationRepository : IParkGenerationRepository
{
    private readonly List<ParkHourlyGeneration> _records = new();

    public IReadOnlyList<ParkHourlyGeneration> Records => _records;

    public IReadOnlyList<ParkHourlyGeneration> GetByParkAndDateRange(Guid parkId, DateOnly fromDate, DateOnly toDate)
    {
        return _records
            .Where(g => g.SolarParkId == parkId && g.DateLocal >= fromDate && g.DateLocal <= toDate)
            .OrderBy(g => g.DateLocal)
            .ThenBy(g => g.HourLocal)
            .ToList();
    }

    public void AddRange(IEnumerable<ParkHourlyGeneration> records)
    {
        _records.AddRange(records);
    }

    public bool HasAnyForPark(Guid parkId)
    {
        return _records.Any(g => g.SolarParkId == parkId);
    }
}

