using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeInverterMinuteReadingRepository : IInverterMinuteReadingRepository
{
    /// <summary>Registros "persistidos" por el fake (spec 036).</summary>
    public List<InverterMinuteReading> Readings { get; } = [];

    /// <summary>Cuántas veces se llamó a <see cref="AddMissing"/>.</summary>
    public int AddMissingCalls { get; private set; }

    /// <summary>Se ejecuta al final de cada llamada a <see cref="AddMissing"/> (permite cancelar a mitad de camino).</summary>
    public Action? OnAddMissing { get; set; }

    public void AddMissing(IReadOnlyCollection<InverterMinuteReading> readings)
    {
        AddMissingCalls++;
        foreach (var reading in readings)
        {
            if (!Readings.Any(r => r.InverterId == reading.InverterId && r.MinuteUtc == reading.MinuteUtc))
            {
                Readings.Add(reading);
            }
        }

        OnAddMissing?.Invoke();
    }

    public IReadOnlyDictionary<Guid, DateTime> GetLastMinuteByInverter()
    {
        return Readings
            .GroupBy(r => r.InverterId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.MinuteUtc));
    }

    public IReadOnlyList<InverterMinuteReading> GetByPark(Guid parkId, DateTime fromUtc, DateTime toUtc)
    {
        return Readings
            .Where(r => r.SolarParkId == parkId && r.MinuteUtc >= fromUtc && r.MinuteUtc < toUtc)
            .OrderBy(r => r.MinuteUtc)
            .ToList();
    }

    public int Count() => Readings.Count;
}
