using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeParkGenerationRepository : IParkGenerationRepository
{
    private readonly List<ParkHourlyGeneration> _records = new();

    public IReadOnlyList<ParkHourlyGeneration> Records => _records;

    /// <summary>Cuántas veces se llamó a <see cref="AddMissing"/> (spec 036).</summary>
    public int AddMissingCalls { get; private set; }

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

    public void AddMissing(IReadOnlyCollection<ParkHourlyGeneration> records)
    {
        AddMissingCalls++;
        foreach (var record in records)
        {
            if (!_records.Any(r => r.SolarParkId == record.SolarParkId && r.DateLocal == record.DateLocal && r.HourLocal == record.HourLocal))
            {
                _records.Add(record);
            }
        }
    }

    public DateTime? GetLastHourByPark(Guid parkId)
    {
        var hours = _records.Where(r => r.SolarParkId == parkId).Select(r => r.TimestampUtc).ToList();
        return hours.Count == 0 ? null : hours.Max();
    }
}
